# Sprint 5 — Operational Intelligence Feasibility Review

Checked the requested list against what's already built before writing any code. Most of it
already exists; building it again under a new name would be duplicate UI, not new intelligence.

## Rejected — already exists

| Requested | Already covered by | Why it's a duplicate |
|---|---|---|
| Top 5 Energy Consumers | `/LoadAnalytics` (Meter Ranking tab) | Already ranks every meter by consumption. Only 3 meters exist in this install — "Top 5" can't show anything a "Top 3" doesn't. |
| Today's Highest Demand | `/Briefing` ("Peak demand: X kW at HH:mm" insight) | Already computed daily from `kWtotal`. |
| Abnormal Consumption / Unexpected Energy Spike | `/Anomaly` (hourly mean + std-dev threshold detection, with a flagged-hours table) | Already does exactly this calculation. |
| Daily Insight Card | `/Briefing` | This page *is* a daily insight card — greeting, Plant Score, top consumer, 3 auto-generated insights. |
| Executive Summary | `/Briefing` (daily) + `/Qbr` (quarterly) | Two cadences already covered. No gap between them that a third "Executive Summary" page would fill. |

## Rejected — would recreate a known problem

**Best/Worst Performing Area.** Area maps 1:1 to Meter in this install (confirmed in Sprint 4).
Meter Ranking already orders meters by consumption — the highest and lowest are the first and
last rows. A separate "performance" framing would need a different formula (not just raw kWh),
and the only SQL-backed proxies for "performance" are power factor, alarm count, and THD/imbalance
— which is precisely what `/EquipmentHealth`'s score already computes. Building a third score here
would recreate the exact Plant-Score-vs-Equipment-Health-Score confusion already flagged and
partially mitigated earlier in this engagement. Not implementing.

## Genuine gaps — building these

Both confirmed against current data: **only 6 days of history exist in `tblEnergyMetersData`
(2026-06-23 to 2026-06-29)**. Neither a prior week nor a prior month exists yet to compare
against. Both features are built correctly and will activate their comparison once more history
accumulates — they do not fabricate a baseline to look populated today.

### 1. Weekly Energy Health
- **Where:** added to `/Briefing` as a new insight card (not a new page — Briefing is already
  the daily-insight surface; a second standalone "weekly" page would itself be the kind of
  duplication this sprint is supposed to avoid).
- **Input columns:** `tblEnergyMetersData.kWh`, `kWtotal`, PF columns, `kVAh`/`kVARh`
  (contamination filter), `Alarms.CreatedAt`/`Severity`.
- **Formula:** trailing 7-day `SUM(kWh)`, `AVG(PF)`, alarm count in the window; compared against
  the preceding 7-day window when that window has data.
- **Engineering assumptions:** same kWh contamination exclusion and same open
  cumulative-vs-interval semantics caveat as every other kWh-summing feature in this app — stated
  on the card, not hidden.
- **Confidence:** Medium (same gating as Sprint 4's kWh features). Week-over-week comparison
  currently shows "not enough history yet" since no prior week exists.
- **SQL source:** `tblEnergyMetersData`, `Alarms`

### 2. Monthly Energy Report
- **Where:** same card area on `/Briefing`.
- **Input columns:** same as above, grouped by calendar month, plus `tblAppSettings` tariff rate
  for cost.
- **Formula:** month-to-date `SUM(kWh)`, estimated cost (`kWh × tariff rate`), peak `kWtotal`
  this month; compared against the prior complete calendar month when one exists.
- **Engineering assumptions:** same contamination/semantics caveat. With only 6 days of total
  history, there is no prior month yet — the card states this plainly instead of comparing
  against zero or a fabricated baseline.
- **Confidence:** Medium (calculation), currently has no comparison output until month 2 of data
  exists.
- **SQL source:** `tblEnergyMetersData`, `tblAppSettings`
