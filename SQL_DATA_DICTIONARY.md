# SQL Data Dictionary — db_SCADA

**Date:** 2026-06-30
**Sprint:** 1 — SQL Feasibility Audit
**Method:** Every table, column, type, and nullability below was queried directly from the live `db_SCADA` database (`INFORMATION_SCHEMA.COLUMNS`), not inferred from code. No analytic claim in this document assumes data that hasn't been confirmed present.

This document has two parts: (1) the data dictionary itself, and (2) the feature feasibility classification for every recommendation from the prior Product Review.

---

## Part 1 — Database Inventory

`db_SCADA` contains **18 tables**: 10 application tables (the system's actual data), 7 ASP.NET Identity tables, and `__EFMigrationsHistory`.

> **Standing critical note, repeated here because it's directly relevant to a SQL feasibility audit:** the 7 `AspNet*` Identity tables live inside `db_SCADA`, the same database the SCADA gateway owns. This must be resolved (moved to a separate database, or auth removed) before any deployment to the client's real plant SQL Server — `db_SCADA` there must be treated as read-only by this application.

### 1.1 `tblEnergyMetersData` — the core energy data table

36 columns. Primary table for nearly every page in the application.

| Column | Type | Nullable | Meaning | Unit | Source | Frequency | Possible Analytics | Used By |
|---|---|---|---|---|---|---|---|---|
| SrNo | bigint | No | Row identity / primary key | — | Auto-increment | Per row | None (key only) | All |
| MeterNo | int | Yes | Logical meter ID (1=Floor1, 2=Floor2, 3=Floor3 in this install) | — | Gateway config | Per row | Grouping key for all per-meter analytics | All |
| MeterName, MeterLocation, MeterBrand, MeterModel, Type1 | nvarchar | Yes | Denormalized meter metadata, repeated on every row | — | Gateway config | Per row | Display labels only; redundant with `tblMonitoringDevices` | Reports, faceplate |
| DateTime | datetime | Yes | Timestamp of the reading | — | Gateway clock | Variable — demo data is hourly; **real-world cadence not yet confirmed** | Time-series grouping key for everything | All |
| VoltL1N, VoltL2N, VoltL3N | float | Yes | Phase-to-neutral voltage | Volts | Meter (Janitza) | Per row | Voltage imbalance (NEMA MG1), out-of-range duration, frequency-adjacent trending | Power Quality, Voltage Imbalance, Voltage Out-of-Range |
| VoltL1L2, VoltL2L3, VoltL1L3 | decimal(6,3) | Yes | Phase-to-phase (line) voltage | Volts | Meter | Per row | Delta-side imbalance, distinct from line-neutral | Line Voltage Balance |
| CurrentL1, CurrentL2, CurrentL3 | float | Yes | Per-phase current | Amps | Meter | Per row | Phase current balance (not yet built as its own page — current values only feed power calcs today) | Power Quality (indirectly) |
| PowerL1, PowerL2, PowerL3 | float | Yes | Per-phase real power | kW | Meter | Per row | Per-phase load distribution (not currently surfaced as its own analytic) | — (unused beyond raw storage) |
| kWtotal | float | Yes | Total instantaneous real power | kW | Meter | Per row | Peak demand, baseload, demand duration curve, diversity factor, capacity headroom | Baseload, Demand Curve, Diversity Factor, Capacity Headroom, Dashboard |
| kVAtotal | float | Yes | Total instantaneous apparent power | kVA | Meter | Per row | Capacity headroom, transformer loading | Capacity Headroom, Transformer Loading |
| kVARtotal | float | Yes | Total instantaneous reactive power | kVAR | Meter | Per row | Not currently used as an instantaneous trend (only the cumulative `kVARh` register is surfaced) | — (stored, unused) |
| PFL1 | float | Yes | Phase 1 power factor | unitless (0–1) | Meter | Per row | PF tracking, PF penalty | Most PF-driven pages, via `PowerFactorHelper` (3-phase avg) |
| PFL2, PFL3 | decimal(4,2) | Yes | Phase 2/3 power factor | unitless | Meter | Per row | Same as PFL1 — only used in combination since the 3-phase-average fix | Same as above |
| MFreq | decimal(5,2) | Yes | Grid frequency | Hz | Meter | Per row | Frequency stability, excursion detection | Frequency Stability page |
| kWh | decimal(18,0) | Yes | Real energy register | kWh | Meter | Per row | **Semantics unconfirmed** — see §1.6 below. Used for nearly all cost/consumption analytics where the contamination filter has been applied | Cost Dashboard, Budget, QBR, Reactive Power, Carbon (partial — see Sprint 0 finding) |
| kVAh | decimal(18,0) | Yes | Apparent energy register | kVAh | Meter | Per row | Reactive fraction, implied PF cross-check | Reactive Power |
| kVARh | decimal(18,0) | Yes | Reactive energy register | kVARh | Meter | Per row | Reactive power trending, capacitor-sizing motivation | Reactive Power |
| HarmonicV1, HarmonicV2, HarmonicV3 | decimal(3,1) | Yes | Per-phase voltage harmonic index | % (assumed) | Meter | Per row | **Semantics unconfirmed against meter register map** — no individual harmonic order (3rd/5th/7th...) is present, only a single combined value per phase. Currently labeled "Harmonic Index" (not THD) in Equipment Health; still labeled "Harmonic Distortion"/THD-adjacent in Power Quality | Equipment Health, Power Quality |
| HarmonicI1, HarmonicI2, HarmonicI3 | decimal(4,1) | Yes | Per-phase current harmonic index | % (assumed) | Meter | Per row | Same caveat as voltage harmonics. Only integrated into Equipment Health's combined score; not used standalone | Equipment Health |

**Critical open question (§1.6):** in the 21 original real-gateway rows (2026-06-27, 18:00–18:15), `kWh` behaves like a cumulative lifetime register (climbing 1240→1245→1250 over 12 minutes) with `kVAh`/`kVARh` reporting exactly 0, while the synthetic demo rows show `kWh` as small, consistent, hourly-interval values with all three populated and physically consistent. **This has not been resolved.** Until it is, any `kWh`-derived figure on a page without the defensive filter (see Sprint 0 finding — 20 of 23 files) should be treated as provisionally unverified.

### 1.2 `tblMonitoringDevices` — device/meter metadata

13 columns: `SrNo`, `DeviceID`, `DeviceType`, `DeviceName`, `Model`, `MasterDevice`, `IPAddress`, `Port`, `Protocols`, `Location`, `Description`, `IsActive`, `GroupName`. Source: gateway configuration, not a live reading — updates only when the device list itself changes. Used for: meter naming, location grouping, Live Monitoring online/offline status (via `IsActive`), Floor Map zone assignment. Note: rows are triplicated in this install (each device appears 3x, identical values) — a known gateway re-registration artifact, already handled in code via `GroupBy(...).First()`.

### 1.3 `Alarms` — alarm/event log

14 columns: `AlarmID`, `DeviceID`, `DeviceName`, `DeviceLocation`, `TagName`, `TagValue`, `Threshold`, `Condition`, `Severity` (tinyint: 1=info, 2=warning, 3=critical), `Message`, `IsActive`, `AckBy`, `AckTime`, `CreatedAt`. Source: gateway-side threshold evaluation, written once per alarm event (not periodic). Currently 12 rows total in this install. Used for: Alarm page, Alarm Response Time, Root Cause Context, Briefing's "critical alarm" KPI. Possible analytics not yet built: false-positive/miscalibration scoring (needs more volume than 12 rows to be meaningful).

### 1.4 `tbFlowmetersData` — flow/level readings

9 columns: `SrNo`, `DeviceName`, `IPAddress`, `DateTime`, `MeterNo`, `InformationType` (`Level` or `Flow_Rate`), `Data` (decimal(7,5)), `DataUnit`, `Area`. **This table was empty (0 rows) in the live database** until 672 demo rows were seeded during Sprint work on the Flow Monitoring page — there is no real plant flow/level data in this system yet. Used for: Flow & Level Monitoring page only.

### 1.5 `tblDevicesTags` — Modbus register map (partial)

9 columns: `SrNo`, `DeviceType`, `DeviceModel`, `TagName`, `TagAddress`, `DataType`, `SizeBits`, `ScaleFactor`, `RegisterCount`. This is the actual Modbus register definition table the gateway would use to poll devices. **It only defines 5 distinct tags**: `Voltage_L1` (0x0001), `Current_L1` (0x0003), `Power_Total` (0x0005), `Level` (0x1001), `Flow_Rate` (0x1003) — each triplicated (15 rows, 5 unique). **This does not explain the 30+ populated columns in `tblEnergyMetersData`** (harmonics, per-phase PF, frequency, all three phase voltages/currents, both energy registers). Either the real production gateway has a fuller tag configuration that was never reflected back into this table, or this table predates additional tags added directly at the meter/gateway level. This is a genuine, unresolved discrepancy between the documented register map and the actual data — relevant to any future neutral-current or harmonic-order question, since this table is the only documented evidence of what the gateway is configured to poll.

### 1.6 `tblDevices` — a second, separate device catalog (previously uncataloged)

6 columns: `SrNo`, `Name`, `Type`, `Model`, `Brand`, `Description`. Discovered during this audit — not referenced anywhere in `EMS.Core`/`EMS.Web`/`EMS.API`. 15 rows (5 unique devices × 3, same triplication pattern as `tblMonitoringDevices`). Lists only **2 energy meters** (Floor 1, Floor 2) — **missing Meter 3 (Floor 3)**, which exists in every other table. This table is stale/incomplete relative to the rest of the system and is not used by any code in this codebase.

### 1.7 Settings & admin-config tables (application-owned, not SCADA data)

- **`tblAppSettings`** (8 cols): key-value admin-editable configuration — tariff rates, shift hours, nominal voltage/frequency, CO2 factor, contracted demand, budget target, PF target. Not SCADA data; entirely application-managed.
- **`tblDailyTemperature`** (3 cols): manually-entered daily temperature, used only by Weather Correlation. No external weather API — confirmed still the case.
- **`tblTransformerRatings`** (6 cols): manually-entered transformer nameplate data (kVA, cooling class), used only by Transformer Loading.
- **`tblUserDashboardWidgets`** (4 cols): per-user widget layout for My Dashboard.

None of these four represent SCADA/electrical measurements — they are admin/config data and were never claimed otherwise.

---

## Part 2 — DTO & API Surface

### 2.1 Named DTOs/ViewModels (EMS.Core + EMS.Web)

19 DTO classes in `EMS.Core/DTOs` (`DashboardDtos.cs`, `LiveMonitoringDtos.cs`) and 11 ViewModel classes in `EMS.Web/Models`. All were checked for field-level correctness against the source `EnergyMeterData`/`Alarm`/`MonitoringDevice` models during earlier schema-compliance work this project — no new discrepancies found in this pass.

**Important note on DTO discipline:** only the original Phase A pages (Dashboard, Briefing, Cost Dashboard, Energy Analysis, Meter Faceplate, Live Monitoring) use named, strongly-typed DTOs. **Every controller built from Shift Comparison (B1) onward — roughly 33 of 41 feature pages — passes data to its view via `ViewBag` and anonymous types instead of named DTOs.** This is a real, consistent pattern across the whole later build-out, not a one-off. It is not a correctness defect (the app works), but it means there is no compile-time-checked contract for ~80% of the application's data surface, and no DTO layer to reuse if `EMS.API` or a future mobile app needed the same shaped data — each would need its own hand-written response model.

### 2.2 EMS.API — 9 endpoints across 3 controllers (currently does not build — see Sprint 0)

| Endpoint | Method | Returns | SQL Tables Touched | Notes |
|---|---|---|---|---|
| `/api/v1/auth/register` | POST | `{ message, userId }` | AspNetUsers (via Identity) | Functional pattern, not yet rebuilt-verified |
| `/api/v1/auth/login` | POST | `{ message, userId, email }` | AspNetUsers | — |
| `/api/v1/auth/me` | GET | `UserInfoDto` | AspNetUsers, AspNetUserRoles | — |
| `/api/v1/auth/logout` | POST | `{ message }` | — (cookie/session only) | — |
| `/api/v1/energyanalysis/consumption` | GET | `ConsumptionAnalysisDto` | tblEnergyMetersData | **Build error**: `decimal`→`double` mismatches on Sum/Average/Min (kWh is now decimal). No contamination filter. |
| `/api/v1/energyanalysis/trend` | GET | `List<TrendPointDto>` | tblEnergyMetersData | **Build error**: `d.DateTime.Hour`/`.Date` on now-nullable `DateTime?`. No contamination filter. |
| `/api/v1/energyanalysis/top-consumers` | GET | `List<TopConsumerAnalysisDto>` | tblEnergyMetersData | **Build error**: same decimal/nullable issues. No contamination filter. |
| `/api/v1/meters/live` | GET | `LiveMonitoringResponseDto` | tblEnergyMetersData, tblMonitoringDevices | Delegates to the same `ILiveMonitoringService` used by `EMS.Web` — likely still correct once the project builds. |
| `/api/v1/meters/{meterId}/details` | GET | `MeterDetailsDto` | tblEnergyMetersData | **Engineering integrity issue**: `PFL1 ?? 0.96` and `MFreq ?? 50.0` silently substitute fake, plausible-looking default values when a reading is null, with nothing in the response indicating the value is a fallback rather than a measurement. This is exactly the kind of fabricated-data risk this audit is meant to catch — flagged for a fix whenever `EMS.API` is revived, not invented-around here. |

---

## Part 3 — Product Review Recommendation Feasibility Classification

**Legend:** A = Fully Supported (data exists, ready to build) · B = Supported via derived calculation (data exists, needs computation, possibly a new admin-config table) · C = Requires SCADA/gateway configuration changes · D = Requires new hardware/meter capability · E = Impossible with this architecture

| # | Recommendation | Class | Basis |
|---|---|---|---|
| 1 | 3-phase average PF (not PFL1-only) | A | `PFL1`, `PFL2`, `PFL3` all present — already built |
| 2 | Diversity factor | A | `kWtotal` per meter, per timestamp — already built |
| 3 | Frequency stability trend | A | `MFreq` — already built |
| 4 | Reactive power (kVARh) page | A | `kWh`, `kVAh`, `kVARh` — already built |
| 5 | Carbon/ESG report | A | `kWh` + admin `CO2Factor` setting — already built |
| 6 | Alarm acknowledgment response-time analytics | A | `Alarms.CreatedAt`/`AckTime` — already built |
| 7 | Voltage out-of-range duration (periodic, not true sag/swell) | A | `VoltL1N/L2N/L3N` — already built, correctly labeled as a weaker substitute |
| 8 | Line-to-line voltage balance | A | `VoltL1L2/L2L3/L1L3` — already built |
| 9 | Flow/level monitoring | A | `tbFlowmetersData` exists (was empty, now has demo data) — already built |
| 10 | Current-harmonic integration into Equipment Health | A | `HarmonicI1-3` — already built |
| 11 | Transformer loading % | B | `kVAtotal` exists; needs admin-entered nameplate kVA (now `tblTransformerRatings`) — already built, but rests on an **unverified 1:1 meter-to-transformer assumption** |
| 12 | kVA-vs-contracted-capacity headroom | B | `kVAtotal` exists; needs admin-entered contract value (now a setting) — already built |
| 13 | Root-cause correlation context | B | Derived from `Alarms` + `tblEnergyMetersData` + `tblDailyTemperature` + shift settings — already built, deliberately scoped to correlation, not diagnosis |
| 14 | Energy intensity (kWh per unit produced) | E | No production/output table exists anywhere in `db_SCADA`. Correctly not built. Would require either a new manual-entry table (same weakness as weather) or a real MES/ERP integration — out of this database's reach entirely as-is |
| 15 | Neutral current monitoring | D (pending C) | No `Current_N` column, no tag in `tblDevicesTags`. **Blocked pending Hassan's answer** on whether a spare CT exists (→ C, config-only) or not (→ D, new hardware) |
| 16 | True %THD (V & I) per individual harmonic order | D (pending C) | Only combined `HarmonicV1-3`/`HarmonicI1-3` exist, no per-order breakdown in `tblDevicesTags` or the data. **Blocked pending Hassan's answer** on meter capability |
| 17 | Voltage sag/swell/transient event log (IEEE 1159) | E | Requires sub-cycle event-triggered sampling. This system polls periodically (interval unconfirmed but at best per-second); architecturally cannot see a 100ms event regardless of polling frequency. Not a configuration or data question — this is a hard ceiling of the polling architecture itself |
| 18 | Flicker (Pst/Plt, IEC 61000-4-15) | E | Same reasoning as #17 — requires purpose-built PQ analyzer hardware, not obtainable via periodic SQL polling under any configuration |
| 19 | Cable/transformer I²R energy loss estimate | B | Theoretically derivable from `kWtotal`/`CurrentL1-3` if cable/transformer impedance values were admin-entered (similar pattern to `tblTransformerRatings`) — not yet built, but the same Group B pattern applies |
| 20 | Tariff optimization simulator (shift load to off-peak) | A | All inputs (`kWh`, tariff settings, shift hours) already exist — not yet built, pure derived-analytics work |
| 21 | Demand peak prediction (next-15-min warning) | A | `kWtotal` time series exists — statistically derivable (e.g. simple trend extrapolation), not yet built. Must be labeled as a statistical estimate, not a guarantee, consistent with the Forecast page's existing honesty problem |
| 22 | Equipment utilization scoring (uptime × load factor) | A | `kWtotal`, `IsActive` (device table) — derivable, not yet built |
| 23 | Phase loading balance optimizer | B | `CurrentL1-3` exist; "suggest load reassignment" requires a recommendation engine, not just a measurement — derived analytic, more design work than a typical Group A item |
| 24 | PF degradation trend (capacitor health proxy) | A | `PFL1-3` time series already exists — pure trend analysis, not yet built |
| 25 | Transformer thermal trend vs ambient | B | Needs `tblDailyTemperature` (already exists, manual-entry caveat applies) cross-referenced with `kVAtotal`/transformer loading — derivable but compounds the weather-data reliability weakness |
| 26 | Maintenance-event correlation library | B | Same data sources as Root Cause Context (#13), extended over time as more alarms accumulate — architecturally ready, but needs alarm volume this install doesn't have yet (12 rows) to be meaningful |
| 27 | Benchmark scoring vs ISO 50001 EnPI / sector norms | E (without external data) | Requires an external benchmark dataset (sector energy-intensity norms) that does not exist anywhere in this system and isn't derivable from `db_SCADA` alone |
| 28 | False-positive alarm scoring | B | `Alarms.IsActive`/`AckBy`/`AckTime` exist — derivable, but meaningless at 12 total alarm rows; needs real operational volume first |
| 29 | Capacity headroom forecast (when will I hit transformer/feeder limits) | B | Combines Capacity Headroom (#12, built) with a trend projection over `kVAtotal` history — derivable, not yet built |
| 30 | Auto-generated executive digest email | A | All source data already exists across existing pages; this is a delivery-mechanism feature (email service), not a data question |
| 31 | Multi-site/portfolio rollup | E (with current schema) | Every table in `db_SCADA` is implicitly single-plant — there is no site/plant identifier column anywhere. Would require a schema change (a new `SiteID` concept threaded through every table), which is a SCADA-side architecture decision, not just an application feature |
| 32 | Real-time waveform capture (sub-cycle) | E | Same hard ceiling as #17/#18 — fundamentally incompatible with periodic SQL polling, regardless of configuration |
| 33 | Single-line diagram with live overlay | A (display only) | All the live values it would show (`kWtotal`, status, alarms) already exist; this is a UI/visualization feature (like Floor Map, already built), not a new data requirement |
| 34 | Protective device coordination reference | E | Not measurable data at all — this is static engineering reference documentation (relay settings, breaker curves), not something derivable from telemetry |

---

## Part 4 — Summary

- **10 of 34 recommendations (29%) are Class A and already built** as of this session.
- **9 are Class A, not yet built** — pure derived analytics, no new data needed, the highest-value remaining backlog.
- **8 are Class B** — need one new admin-config table or cross-referenced data, same pattern already proven 3 times (`tblTransformerRatings`, contracted-demand setting, `tblDailyTemperature`).
- **2 are Class D, currently blocked** pending the client's confirmation of meter capability (neutral current, individual harmonic orders).
- **5 are Class E** — genuinely impossible with this architecture as it stands: true sag/swell/transient capture, flicker, real-time waveform, multi-site rollup (schema-level), and external-benchmark scoring. None of these should be promised in a client demo regardless of effort invested, because no amount of software work changes a polling-architecture or missing-schema-concept limitation.

No feature in this document was invented, estimated, or assumed present without a direct schema query confirming it.
