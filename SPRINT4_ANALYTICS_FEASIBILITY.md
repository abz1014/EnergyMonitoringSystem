# Sprint 4 — Advanced Analytics Feasibility Review

Reviewed against `db_SCADA` directly (`sqlcmd`), not against assumptions. Current data
footprint at time of review: **7 days** of `tblEnergyMetersData` (2026-06-23 to
2026-06-29), 3 active energy meters (Meter 1/Floor1, Meter 2/Floor2, Meter 3/Floor3).

## Already built — reject as duplicate work

| Requested | Already exists at | Notes |
|---|---|---|
| Peak Demand | `/EnergyAnalysis?metric=peak`, `/DiversityFactor` | Per-meter and system-wide peak kW already computed from `kWtotal`. |
| Baseload | `/TimeOfUse` (Baseload tab) | Night-window minimum-load analysis already built. |
| Shift Comparison | `/TimeOfUse` (Shift tab) | Morning/Afternoon/Night consumption split already built. |
| Weekend Comparison | `/TimeOfUse` (Weekday vs Weekend tab) | Already built. |
| Cost Estimation | `/CostDashboard` | Tariff-rate-based cost already built. |
| Consumption Trends | `/EnergyAnalysis`, `/Forecast` (relabeled "Trend Projection") | Already built. |
| Consumption Heatmaps | `/Heatmap` | Already built. |
| Area Comparison | `/Comparison` | **Rejected as a distinct feature.** `tblMonitoringDevices.Location` maps 1:1 to `MeterNo` in this install (Floor 1 = Meter 1, Floor 2 = Meter 2, Floor 3 = Meter 3 — confirmed via direct query). An "Area Comparison" page would query and render identical data to the existing meter Comparison page under different labels. Building it would be duplicate UI, not a new analytic. If the plant later installs multiple meters per floor/area, this becomes a real, distinct feature — not the case today. |

## New — buildable today (Class A, zero new data required)

### 1. Load Profile (typical daily load shape)
- **Input columns:** `tblEnergyMetersData.kWtotal`, `DateTime`, `MeterNo`, `kVAh`, `kVARh`
- **Formula:** For each hour-of-day (0–23), `AVG(kWtotal)` across all qualifying readings in the selected date range, per meter and system-wide (sum of per-meter hourly averages).
- **Engineering assumptions:** (1) `kWtotal` is instantaneous demand at poll time, not an energy total, so straight averaging per hour bucket is valid. (2) Readings are excluded where `kVAh = 0 AND kVARh = 0` — the established contamination signature for 21 known-bad rows in this dataset (same filter already applied to Dashboard, Carbon, ReactivePower). (3) Averaging across only 7 available days means the "typical" shape is currently a 7-day average, not a statistically stable seasonal pattern — page explicitly labels the day count used.
- **Confidence:** High for the calculation itself; Medium for how representative it is, given only 7 days of history exist right now.
- **SQL source:** `tblEnergyMetersData`

### 2. Meter Ranking
- **Input columns:** `tblEnergyMetersData.kWh`, `MeterNo`, `MeterName`, `DateTime`, `kVAh`, `kVARh`
- **Formula:** `SUM(kWh)` per meter over the selected period, sorted descending, with each meter's % share of total plant consumption.
- **Engineering assumptions:** Same contamination exclusion as above. **This inherits the unresolved kWh semantics question already tracked in memory (`kwh-semantics-pending-confirmation.md`)** — whether `kWh` is cumulative-register or interval-delta is still unconfirmed by Hassan. If cumulative, summing raw `kWh` across rows double-counts and the ranking's relative order is still likely correct (all meters affected proportionally) but absolute totals are not. The page surfaces this caveat directly rather than hiding it.
- **Confidence:** Medium — gated on the open semantics question. Relative ranking is more trustworthy than absolute kWh figures shown.
- **SQL source:** `tblEnergyMetersData`

### 3. Daily Variance
- **Input columns:** `tblEnergyMetersData.kWh`, `DateTime`, `kVAh`, `kVARh`
- **Formula:** Per day, `SUM(kWh)` system-wide; variance = `(today - yesterday) / yesterday * 100`.
- **Engineering assumptions:** Same contamination exclusion and same kWh semantics caveat as Meter Ranking. 7 days of history supports day-over-day comparison starting from day 2.
- **Confidence:** Medium, same gating as above.
- **SQL source:** `tblEnergyMetersData`

### 4. Monthly Variance
- **Input columns:** same as Daily Variance, grouped by calendar month.
- **Formula:** `SUM(kWh)` per month; variance = `(thisMonth - lastMonth) / lastMonth * 100`.
- **Engineering assumptions:** Same as above, **plus**: with only 7 days of total history (all within June 2026), there is currently no prior month to compare against. The feature is built correctly and will activate automatically once a second month of data exists — until then it shows an explicit "not enough data" state rather than a fabricated or zero-filled comparison.
- **Confidence:** Medium (calculation), but **currently unusable in production** until month 2 of data exists — this is stated on the page itself, not hidden.
- **SQL source:** `tblEnergyMetersData`

## Rejected

None of the 12 requested items required outright rejection on data-availability grounds — all either already exist or are Class A with the dataset as-is. The only rejection is **Area Comparison**, rejected as duplicate (not as infeasible).
