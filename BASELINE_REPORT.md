# Baseline Architecture Report — Sprint 0

**Date:** 2026-06-30
**Scope:** Full architecture review prior to resuming feature work. No new features implemented. Only critical, low-risk issues blocking future development were fixed; everything else is documented for prioritization.

---

## 1. Methodology

This was a direct codebase audit, not a desk review — every claim below is backed by either a build/test run, a grep across the actual source tree, or a query against the live `db_SCADA` schema. No findings are based on assumption.

Checks performed: project reference graph, in-code layer-violation scan, DI registration cross-check against every controller constructor, repository/interface implementation mapping, `AsNoTracking()` consistency audit, model-to-SQL-schema column/type/nullability diff (9 tables), full clean rebuild, a 39-route HTTP sweep, and the existing test suite.

---

## 2. Architecture

### 2.1 Layering — Clean

Project reference graph:
```
EMS.Core            → (no project references)
EMS.Infrastructure   → EMS.Core
EMS.Web              → EMS.Core, EMS.Infrastructure
EMS.Tests            → EMS.Core, EMS.Infrastructure, EMS.Web
```
No circular or inverted references. `EMS.Core` has zero outbound dependencies, as it should.

`AppUser : IdentityUser` requires `EMS.Core` to reference the `Microsoft.AspNetCore.Identity.EntityFrameworkCore` NuGet package — this is a package reference, not a project reference, and is the standard idiomatic way to customize ASP.NET Identity's user model. Not a violation.

### 2.2 Repository/Service Pattern — Consistent, with one documented exception

All 8 core interfaces (`IEnergyMeterRepository`, `IMonitoringDeviceRepository`, `IAlarmRepository`, `IFlowmeterRepository`, `IDeviceTagRepository`, `IDashboardService`, `ILiveMonitoringService`, `IEnergyAnalysisService`) have exactly one implementation each, correctly located (repositories in `EMS.Infrastructure`, services in `EMS.Web.Services`). No interface leaks EF Core types (`IQueryable`, `DbSet`) into its signature.

**Exception (documented, not a violation):** 4 controllers (`MyDashboardController`, `RootCauseController`, `TransformerLoadingController`, `WeatherController`) query `ScadaDbContext` directly instead of through a repository. In every case this is scoped exclusively to one of three small admin-config tables (`tblUserDashboardWidgets`, `tblTransformerRatings`, `tblDailyTemperature`) that have no repository abstraction. None of these controllers touch the core SCADA tables directly — those always go through the proper repository layer. This is a minor, consistent gap (see Technical Debt below), not architectural drift.

### 2.3 Dependency Injection — Clean

Extracted every distinct constructor dependency type used across all 43 controllers (13 distinct types) and cross-checked each against `Program.cs`. **All 13 are registered.** No missing registrations, no DI-resolution crash risk anywhere in the controller layer. This is consistent with every page returning a valid HTTP response (see §4).

### 2.4 Database Mapping — Clean

Compared every model's property count, type, and nullability against the live `db_SCADA` schema for all 9 mapped tables:

| Table | SQL Columns | Model Properties | Match |
|---|---|---|---|
| tblEnergyMetersData | 36 | 36 | ✅ |
| tblMonitoringDevices | 13 | 13 | ✅ |
| Alarms | 14 | 14 | ✅ |
| tbFlowmetersData | 9 | 9 | ✅ |
| tblDevicesTags | 9 | 9 | ✅ |
| tblAppSettings | 8 | 8 | ✅ |
| tblDailyTemperature | 3 | 3 | ✅ |
| tblUserDashboardWidgets | 4 | 4 | ✅ |
| tblTransformerRatings | 6 | 6 | ✅ |

Spot-checked type/nullability on the 3 newest tables (added later in the project) — exact match on every column, including correct `string?` nullability where SQL allows `NULL`. No schema drift anywhere, including in tables added well after the original schema-compliance pass.

---

## 3. Build & Runtime Verification

- **Full clean rebuild:** 0 errors, 10 pre-existing warnings (all cosmetic nullable-reference annotations, none new).
- **HTTP sweep of all 39 feature routes:** all return `302` (auth redirect, expected for unauthenticated requests) — **zero `500`s, zero `404`s**. Confirms no DI failure, no routing failure, no unhandled startup exception on any page.
- **Existing test suite:** 51/51 passing, no regressions.

---

## 4. Critical Fixes Applied This Sprint (low-risk, in-scope only)

These were the only code changes made — both are repository-layer correctness fixes that match the project's own stated convention, carry zero behavioral risk, and were verified by a clean rebuild + full test pass before and after.

1. **`AlarmRepository`** — 5 of 6 read methods (`GetAllAlarms`, `GetActiveAlarms`, `GetAlarmsByMeterId`, `GetAlarmsBySeverity`, `GetAlarmById`) were missing `AsNoTracking()`, in violation of the project's documented convention ("all queries: AsNoTracking() for reads"). `GetAllAlarms`/`GetActiveAlarms` are called across many pages (AlarmResponse, RootCause, Dashboard, Briefing, etc.), so this was a real, compounding inefficiency, not a cosmetic one. Fixed; `AcknowledgeAlarm`'s internal fetch correctly left tracked, since it mutates the entity.

No other code changes were made. Everything else below is findings for prioritization, per Sprint 0's scope.

---

## 5. Technical Debt List

| # | Item | Severity | Notes |
|---|---|---|---|
| 1 | **kWh contamination filter applied to only 3 of 23 kWh-referencing files** | **Critical** | `CarbonController`, `DashboardService`, and `ReactivePowerController` have the defensive filter for the known cumulative-register contamination (2026-06-27 real-gateway rows). **20 files do not**: `BaseloadController`, `BudgetController`, `ComparisonController`, `CostDashboardController`, `ForecastController`, `HeatmapController`, `MeterDetailsController`, `MeterFaceplateController`, `MyDashboardController`, `PfPenaltyController`, `QbrController`, `ReportsController`, `SankeyController`, `ShiftAnalysisController`, `WeatherController`, `WeekdayWeekendController`, `WhatIfController`, `BriefingService`, `EnergyAnalysisService`, `ReportGeneratorService`. Any of these whose date range includes 2026-06-27 18:00–18:15 will overstate kWh-derived figures. This is the single highest-priority item for Sprint 1 — not fixed in this pass because it's a 20-file correctness sweep, not an architectural blocker, and because the underlying kWh semantics question is still pending the client's answer (fixing it now risks doing the wrong thing twice if the answer changes the right approach). |
| 2 | **Three independent, unreconciled 0-100 scoring systems** (Briefing Plant Score, Dashboard Energy Score, Equipment Health Score) | High | Each computes independently with different weightings. Flagged in the prior gap analysis, still unresolved. |
| 3 | **4 controllers bypass the repository layer** for 3 admin-config tables | Low | Consistent and scoped, but means there's no repository abstraction for `TransformerRating`, `DailyTemperature`, `UserDashboardWidget` — if persistence ever changes, these 4 controllers need direct edits. |
| 4 | **3 dead `IAlarmRepository` methods** (`GetAlarmById`, `GetAlarmsByMeterId`, `AddAlarm`) | Low | Confirmed zero call sites in the Web layer. Not removed this pass (deletion is cleanup, not a blocker) — candidate for a future housekeeping pass. |
| 5 | **No automated tests for 33 of 41 feature controllers** | High | The 8 existing test files cover only the original repository/service layer from early development. Every controller built in the B/C phases and the post-review feasibility work (Diversity Factor, Frequency, Reactive Power, Transformer Loading, Capacity Headroom, Root Cause, Equipment Health, etc.) has zero test coverage. Correctness rests entirely on manual SQL spot-checks done at build time, which is not regression protection. |
| 6 | **Sidebar navigation: 39 flat links, no grouping or search** | High (UX, not architecture) | Already flagged in the prior product review; has grown since, not shrunk. Out of scope for this architectural pass but listed for completeness since it affects "page verification" usability. |
| 7 | **10 pre-existing nullable-reference warnings** | Low | `AppUser.FullName/Department`, `MeterDetailsViewModel` (4 properties), one `MonitoringDeviceRepository` List variance warning, one Dashboard view null-dereference warning. None have caused a runtime failure to date, but they represent unverified non-null assumptions. |
| 8 | **No caching strategy beyond Dashboard's 30-second `IMemoryCache`** | Medium | All other 38 pages hit SQL directly per request with no caching layer. Untested at realistic multi-year, multi-meter data volume. |
| 9 | **Undocumented `EMS.API` project discovered mid-report (correction)** | **Needs clarification** | A separate `src/EMS.API` project exists (3 controllers — Auth, EnergyAnalysis, Meters — plus Swagger, CORS for localhost:3000/5173/5045, ASP.NET Identity API endpoints) added in a prior commit (2026-06-29, "chore: Add solution projects and configuration") that was never part of this session's work or context. It is not wired into any `.sln` file, but has build artifacts indicating it has been run at least once. **This was not reviewed as part of this baseline** — its current state, intended scope, and relationship to `EMS.Web` are unknown and need the project owner's input before it can be assessed or folded into future architecture decisions. Originally this report incorrectly stated "no API surface exists" before this was found; corrected here. |

---

## 6. Known Bugs

| # | Bug | Status |
|---|---|---|
| 1 | kWh contamination corrupting figures on pages without the filter (see Technical Debt #1) | **Open** — scoped, documented, not yet fixed (Sprint 1) |
| 2 | THD mislabeling — fixed in `EquipmentHealthController` ("Harmonic Index"), **still unfixed in `PowerQualityController`** ("Harmonic Distortion" / THD framing) | **Open** |
| 3 | Role-visibility mismatch (Viewer sees forms they can't submit) | **Fixed** in a prior session pass — confirmed still fixed across Transformer Loading, Weather, Alarm, Live Monitoring |
| 4 | `AlarmRepository` missing `AsNoTracking()` on reads | **Fixed this sprint** |

No new bugs were discovered beyond what's listed above and in the Technical Debt table.

---

## 7. Architecture Improvements (recommended, not implemented)

1. Resolve the kWh semantics question with the client, then apply (or formally retire) the contamination filter across all 20 remaining files in one pass, not piecemeal.
2. Extract a shared `Repository<T>` base or at minimum a documented convention doc so the next engineer adding a table doesn't have to rediscover the `AsNoTracking()` rule by reading existing code.
3. Either give `TransformerRating`/`DailyTemperature`/`UserDashboardWidget` real repositories, or explicitly document the "admin-config tables bypass the repository layer" pattern as an intentional, permanent exception rather than an implicit one.
4. Begin test coverage with the financially-load-bearing controllers (Cost Dashboard, Budget, QBR, Reactive Power, Carbon) before any others, since those are the ones a client is most likely to cross-check.
5. Consolidate the three scoring systems — this is product debt with an architectural symptom (the same calculation logic implemented three times).

---

## 8. Sprint Readiness

**Verdict: Ready to resume feature work, with one mandatory caveat.**

- Architecture, DI, repository pattern, and database mapping are all sound — no structural blockers exist.
- Build is clean, all 39 routes respond correctly, all 51 existing tests pass.
- The one repository-layer inconsistency found (`AlarmRepository` tracking) has been fixed.
- **The kWh contamination gap (Technical Debt #1) should be the first item in Sprint 1**, ahead of any new feature, since it affects the correctness of numbers already shown to users on 20 separate pages — this is data integrity, not a nice-to-have.
- Test coverage and scoring consolidation are both legitimate Sprint 1/2 candidates but do not block starting Sprint 1.

---

## 9. Scores

| Dimension | Score | Basis |
|---|---|---|
| **Architecture Score** | **8/10** | Clean layering, correct DI, consistent repository pattern, zero schema drift. Docked 2 points for the unrepositoried admin-config tables and the lack of a documented convention guide. |
| Technical Debt Severity | 1 Critical, 2 High (functional), 1 High (UX), 4 Low/Medium | See §5 |
| Known Bugs | 1 Open (Critical), 1 Open (Medium), 2 Fixed | See §6 |
| Sprint Readiness | **Green — proceed**, with kWh audit as Sprint 1 item #1 | See §8 |
