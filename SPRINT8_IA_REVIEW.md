# Sprint 8 — Information Architecture & Productivity Review

## Verdict First

This application currently has **35 pages** serving **3 energy meters, 2 flow meters, and 1 PLC**.

That ratio is the first warning sign. The data does not justify 35 pages. Most industrial facilities with the same hardware footprint operate from 4–6 screens. What happened here is the classic trap: every column in `tblEnergyMetersData` became a page. Meter has `MFreq`? Build a frequency page. Meter has `kVAh`? Build a reactive power page. The database drove the IA instead of user workflows.

The result is an application that contains a lot of information but answers very few questions quickly.

**Target state**: 20 pages, every surviving page answerable in ≤30 seconds.

---

## The Three User Workflows This Application Must Serve

Before reviewing pages, define who uses this and what decisions they make:

**Workflow 1 — Shift Engineer (daily, 3×/day)**
- Is everything online and normal?
- Is consumption unusually high or low vs yesterday?
- Are there any active alarms?
- Which floor/area is consuming the most?
- *Decision: Do I need to investigate anything right now?*

**Workflow 2 — Energy Manager (weekly)**
- What did we consume this week vs last week?
- What is the projected monthly cost?
- Are we at risk of exceeding contracted demand?
- What is the trend — improving or worsening?
- *Decision: Do I need to brief management, adjust setpoints, or commission a study?*

**Workflow 3 — Plant Manager / Finance (monthly)**
- What did we spend on energy this month?
- How does it compare to budget?
- What is our carbon footprint for ESG reporting?
- Is our power factor penalty risk rising?
- *Decision: What do I put in the board report?*

Every page that does not serve one of these three workflows is noise.

---

## Page-by-Page Classification

### TIER 1: ESSENTIAL — Keep exactly as-is

---

**BRIEFING** ✅ Essential
Decision served: "What happened since I was last here and do I need to act?"
This is the application's best page. It synthesizes — yesterday vs average, top consumer, alarms, plant score, weekly comparison. A shift engineer reading this at the start of a shift has everything they need in one screen.
Productivity gain: replaces 4–5 separate page visits with one.

---

**LIVE MONITORING** ✅ Essential
Decision served: "Is the plant running normally right now?"
Real-time meter status (kW, kWh, PF, voltage per meter), active alarms. No substitution exists for this. Industrial operations staff open this first.
Note: confirm the refresh interval is fast enough (< 60s). If it requires manual page reload, add an auto-refresh indicator.

---

**METER FACEPLATE** ✅ Essential
Decision served: "What is this specific meter doing and has it always behaved this way?"
Per-meter deep dive: current readings, today's profile, historical trend, alarm history. Correctly implemented as a drill-down destination from multiple pages. The "View meter →" pattern from Sprint 3 was the right call.

---

**ALARM LIST** ✅ Essential
Decision served: "What needs attention right now?"
Direct — show active alarms, severity, time. No change needed.

---

**ROOT CAUSE** ✅ Essential
Decision served: "Why did this alarm fire?"
Contextual analysis for a specific alarm. The N+1 query issue noted in Sprint 7 should be addressed when alarm volume grows.

---

**ALARM RESPONSE** ✅ Essential
Decision served: "Are we acknowledging alarms fast enough, and are some alarms recurring?"
SLA tracking. Essential for maintenance management. The slowest-response metric and recurring alarm identification are genuine operational insights.

---

**FLOW MONITORING** ✅ Essential
Decision served: "How are fuel tank levels and do I need to order fuel?"
Completely separate system (fuel tanks 4 & 5). This data exists nowhere else in the app. Keep.

---

**BUDGET vs ACTUAL** ✅ Essential
Decision served: "Are we within our energy budget this month?"
Financial accountability. Direct comparison of target vs actual spend. Energy managers and finance need this every month.

---

**EQUIPMENT HEALTH** ✅ Essential
Decision served: "Which meters are behaving abnormally and need investigation?"
Health score per meter based on PF, voltage, alarm history. Good synthetic view. The drill-down to Faceplate makes it actionable.

---

**TRANSFORMER LOADING** ✅ Essential
Decision served: "Are our transformers being overloaded and when is the peak?"
kVA vs nameplate capacity. Direct operational and safety value. The admin-editable nameplate input is the right design.

---

**FLOOR MAP** ✅ Essential
Decision served: "Which physical area of the plant is consuming what?"
Spatial context reduces the abstraction of "Meter 1/2/3" into "the injection molding floor is the problem." This is not available in any other format.

---

### TIER 2: USEFUL — Keep but consolidate or simplify

---

**DASHBOARD** 🟡 Useful → Needs surgery
Decision served: "Executive overview of the plant's current energy state."

Current KPIs: 8 cards. Problems:
- **CO₂ Emissions** (daily equivalent): This is `todaysConsumption × 0.82`. It adds zero information beyond what "Today's Consumption" already tells you — it is the same number in different units. **Remove it.**
- **Online Meters**: Currently 3/3 in normal operation. A KPI that is always 100% is not a KPI — it is a heartbeat indicator that belongs in a status bar, not a prominent card. **Demote to a small status indicator.**
- **Current Load (kW)**: Essential. Keep.
- **Today's Consumption (kWh)**: Essential. Keep.
- **Peak Demand Today (kW)**: Essential. Keep.
- **Monthly Total (kWh)**: Essential for cost context. Keep.
- **Est. Monthly Cost (Lakh Rs.)**: Essential for financial awareness. Keep.
- **Avg Power Factor**: Essential. Keep.

Current charts: 4.
- **Load Profile (today/yesterday/weekly avg)**: Essential. Shows temporal pattern. Keep.
- **Location Breakdown (donut)**: With only 3 meters (Floor 1/2/3), this donut chart has 3 segments. A donut with 3 segments is a bar chart with extra steps. **Replace with a simple 3-row bar/table showing floor name + kWh + %.**
- **Top Consumers (horizontal bar)**: With 3 meters, this chart shows 3 bars. Same problem as above — it's identical to Location Breakdown with different column headings. **This is a duplicate of Location Breakdown. Remove one or merge into a single 3-row table.**
- **Monthly Trend (12-month bar)**: Useful for executives. Keep but be honest — with only 1 month of real data, 11 of the 12 bars are zeros. Show only bars with actual data or explicitly mark the rest as "projected."

**Plant Health Score**: Essential. The synthesized score with subscores is the right level of abstraction for this page.

Net result after surgery: 6 KPI cards, 3 charts (Load Profile + merged Location/Consumer table + Monthly Trend), 1 score. Substantially less noise.

---

**ENERGY ANALYSIS** 🟡 Useful → Audit for duplication
Decision served: "What are our consumption trends over a custom time range?"
Check for overlap with Load Analytics Daily Variance. If they show the same thing (day-by-day consumption trend), one must be removed.

---

**COMPARISON** 🟡 Useful → Scope confirmed correct
Decision served: "Which meter/floor is consuming more than the others?"
With 3 meters, floor comparison is one of the primary questions. This is different from Load Analytics Meter Ranking because it allows temporal range selection and side-by-side charting. Keep.

---

**TIME OF USE** 🟡 Useful — already consolidated correctly
Decision served: "When during the day are we consuming energy, and how much is off-hours waste?"
Shift tab, Baseload tab, Weekday/Weekend tab — three related questions in one place. This was already the right consolidation from Sprint 3. Keep.

---

**HEATMAP** 🟡 Useful — but only for managers
Decision served: "Are there visible patterns in energy use over weeks/months?"
A calendar heatmap is excellent for spotting recurring anomalies (always high on Monday mornings, always low on Fridays). Shift engineers don't need this daily. Energy managers use it monthly. Keep but be clear about audience — this is a management tool, not an operational one.

---

**POWER QUALITY** 🟡 Useful → Consolidation opportunity (see below)
Decision served: "Is our power quality within acceptable limits?"
PF distribution, harmonic indicators, voltage quality summary. This page is doing the right job — synthesizing multiple quality indicators. The problem is that Voltage Imbalance, Voltage Range, Line Voltage Balance, and Reactive Power are ALSO separate pages covering subsets of this information.

---

**ANOMALY DETECTION** 🟡 Useful — provided it actually fires
Decision served: "Is there unusual consumption I didn't manually notice?"
If the algorithm works correctly, this page reduces investigation time by surfacing surprises. The risk: if it has never flagged anything in 7 days of data, users will stop checking it. Add an "All clear since [date]" message when no anomalies exist rather than just an empty state — "No anomalies found" is different from "I looked and found nothing."

---

**COST DASHBOARD** 🟡 Useful → Possible merge with Budget
Decision served: "What are we spending and what will we spend?"
Monthly cost, per-day cost trend, projected end-of-month, per-kWh analysis. Check whether this duplicates Budget vs Actual. If Budget vs Actual handles target vs actuals and Cost Dashboard handles breakdown and trend, they are complementary. If they show the same totals in different visual formats, merge them.

---

**QBR TEMPLATE** 🟡 Useful — but not a daily tool
Decision served: "Generate a quarterly business review slide pack."
Executive reporting automation. High value for management. Low frequency. Consider hiding it from the main sidebar (move to Reports section) so it doesn't clutter the operational navigation.

---

**REPORTS** 🟡 Useful
Decision served: "Generate a downloadable report for a specific period."
Export functionality is table stakes for any EMS. Keep.

---

**PF PENALTY CALCULATOR** 🟡 Useful — one-time/monthly use
Decision served: "What is our estimated PF penalty on this bill, and how much would capacitor banks save?"
This is a financial decision tool. It's not a monitoring page — it's a calculator. Correct placement: financial/cost section. It earns its place for the monthly billing review.

---

**WHAT-IF SCENARIO MODELER** 🟡 Useful — management tool
Decision served: "If we reduce consumption by X% or improve PF to Y, what do we save?"
Scenario planning. Earns its place for energy efficiency proposals and management presentations.

---

**FORECAST** 🟡 Useful — with honest caveats
Decision served: "What will consumption be next week?"
Moving average forecast. With only 7 days of data, the forecast is low-confidence. The current implementation surfaces this correctly (needs `windowDays` of history). The label "Forecast" sets high expectations — consider "Consumption Projection (Moving Average)" to set correct expectations.

---

**DEMAND DURATION CURVE** 🟡 Useful — engineering tool
Decision served: "What percentage of time does demand exceed my contracted level?"
This is a genuine engineering tool for sizing contracted demand with the utility. The `contractedDemand` input + `% time above contract` is exactly what an energy engineer needs for the annual utility contract renegotiation. Keep for engineering users.

Overlap note: DemandCurve (statistical distribution of demand) is fundamentally different from DemandProfile/LoadProfile (time-series of demand). They answer different questions. No consolidation needed.

---

**SETTINGS** 🟡 Useful
Decision served: Tariff rates, shift times, CO2 factor, contracted demand. Admin only.

---

### TIER 3: REDUNDANT — Consolidate into existing pages

---

**VOLTAGE IMBALANCE** 🔴 Redundant — already shown in Power Quality
Decision this page serves: "Is the voltage unbalanced across L1/L2/L3?"
**Critical finding**: Power Quality already renders a "Voltage Imbalance (area chart)" as one of its 6 charts. The Voltage Imbalance page duplicates this chart exactly, adds NEMA 2% limit annotation (which could just as easily be on the Power Quality chart), and adds violation count KPIs. This is the same data in a dedicated page that costs one sidebar slot and one navigation click.
**Recommendation**: The NEMA limit annotation and violation count KPIs belong in Power Quality. Delete this page. Move KPIs to Power Quality's existing voltage imbalance section.

---

**VOLTAGE RANGE** 🔴 Redundant → Merge into Power Quality Voltage section
Decision this page serves: "Are voltages staying within acceptable min/max bounds?"
Power Quality already shows a "Voltage Trend L-N (line 3-phase with high/low annotations)" chart. Voltage Range adds a "worst out-of-range events" table which is genuinely useful — but it belongs as a collapsed details panel under Power Quality's voltage chart, not a separate page.
**Recommendation**: Move the out-of-range events table into Power Quality as an expandable section under the voltage trend chart.

---

**LINE VOLTAGE BALANCE** 🔴 Redundant — near-identical to Voltage Imbalance page
Decision this page serves: "Are the line-to-line voltages balanced?"
Shows: Avg Imbalance %, Peak %, NEMA violations — the same KPI set as Voltage Imbalance but computed from VLL instead of VLN. Power Quality already computes both voltage and current imbalance. The difference between VLN imbalance and VLL imbalance is meaningful to an electrical engineer but invisible on the page — both pages show "Avg Imbalance %" and "Within NEMA 2% limit" with identical visual design.
**Recommendation**: Power Quality should show both VLN and VLL imbalance as sub-rows within its Voltage Imbalance section. Delete this page.

---

**REACTIVE POWER** 🔴 Redundant → Merge into Power Quality
Decision this page serves: "What is our reactive power consumption (kVAR)?"
kVAR and PF are two expressions of the same underlying condition. Power Quality already shows PF trend and harmonics. The Reactive Power page adds a "Daily Real vs Reactive (bar)" chart and per-meter reactive breakdown table. The per-meter breakdown is genuinely additive — but it belongs in Power Quality as a table section, not a standalone sidebar page.
**Recommendation**: Add the per-meter kVAR/kVAh breakdown as a section in Power Quality. Delete this page.

---

**DIVERSITY FACTOR** 🔴 Redundant → Merge into Capacity Headroom
Decision this page serves: "What is the diversity factor across our meters?"
Diversity factor (sum of individual peaks / coincident system peak) is a means to an end — it exists to estimate how much more load the system can absorb without exceeding the coincident peak. That is exactly what Capacity Headroom answers. The Capacity Headroom page already shows system peak kVA vs contracted kVA and headroom. Diversity factor is supporting math, not a standalone decision.
Productivity cost of the current state: an engineer has to click two separate pages to answer one question.
**Recommendation**: Add diversity factor calculation as a section within Capacity Headroom.

---

**CARBON** 🟠 Optional → Not essential for operations
Decision this page serves: "What is our CO₂ footprint?"
This is only relevant to ESG reporting — a management/compliance function, not an operational one. The formula is `kWh × fixed_factor`. It adds no information beyond consumption data expressed in different units.
For industrial operations in Pakistan, the immediate business drivers are cost and reliability, not carbon. Carbon reporting is valuable for certain contexts (ESG compliance, ISO 14001) but is not a decision a shift engineer or energy manager makes daily.
**Recommendation**: Move Carbon into the Reports or QBR sections as an exportable metric rather than a standalone navigation page. Remove CO₂ from the Dashboard KPI card entirely — it repackages Today's Consumption into Metric Tons using a fixed multiplier, which tells the engineer nothing they don't already know from the kWh figure.

---

### TIER 4: ENGINEERING NOISE — Delete

---

**FREQUENCY** ❌ Delete — data is already shown in two other places
Decision this page serves: "Is grid frequency within ±0.5 Hz of 50 Hz?"
The answer is: almost certainly yes, and the user cannot do anything about it if the answer is no.

Frequency is controlled by the national grid (NTDC in Pakistan). A factory has zero authority over grid frequency. The only action available when frequency deviates is "call the utility and log a complaint."

**Critical finding from page audit**: The Power Quality page already renders a Frequency Trend line chart with the nominal ±tolerance band. Live Monitoring already shows "Freq (Hz)" as a column in the meters table. Frequency is shown in three places, and the Frequency page is the only one of the three that is hard to find in the sidebar.

The page shows avg frequency (nearly always 50.00 Hz), deviation count (nearly zero), and a trend chart (a nearly flat line). This is the definition of visual noise AND it is already covered elsewhere.

If frequency ever deviates dangerously, the right response is an alarm — which the Alarm system already handles. The solution is a frequency alarm threshold in Settings, not a third page displaying the same trend.

**Delete the page. The data is already visible on Power Quality and Live Monitoring.**

---

## Information Density Assessment

### Can a new user understand the current system in:

**10 seconds**: No.
Current state: the user lands on Briefing, which is the best-designed page, but the sidebar immediately shows 35 items organized into 6 groups. First question: "Where do I go to see if anything is wrong?" Answer requires familiarity with the navigation. There is no visual hierarchy that says "start here."
Fix: Make the Briefing page the unambiguous home. Add a "System Status" header band at the very top of Briefing showing three indicators: ✅ All meters online / ⚠️ 2 active alarms / ✅ Consumption normal. This is readable in 3 seconds.

**30 seconds**: Partially.
On Briefing, a user who reads the insight cards and the Plant Score can understand the overall situation. But if something is wrong (e.g., alarm active), the drill-down path is: Briefing → see alarm insight → click "View Alarms" → Alarm list → click device → Root Cause. That's 3 clicks minimum. Should be 1 click from Briefing to see the active alarm and its context.

**2 minutes**: Yes — for a skilled user.
A trained operator can review Briefing, jump to Live Monitoring, and check Alarms in under 2 minutes. For a new user, 2 minutes is not enough to understand the sidebar structure, let alone find the right page.

---

## Cognitive Load Assessment

### High cognitive load pages (require active thinking to interpret):

**Diversity Factor**: Requires understanding of a ratio concept (sum of individual peaks / coincident peak). The number means nothing without the context of Capacity Headroom. Users who don't understand the formula cannot use this page. Not self-explaining.

**Demand Duration Curve**: The X-axis (% of time) is not intuitive without explanation. The concept of "load duration" is familiar to electrical engineers but not to plant managers or operators. Current implementation has KPIs (Peak, Avg, Load Factor, P10/P50/P90) but no plain-language explanation of what to do with them.

**Forecast**: A line chart that transitions from solid (actual) to dashed (projected) is standard, but the moving average method is not explained. A user seeing a forecast that is identical every day (flat line at average) may not understand this is expected behavior, not a software bug.

**Load Analytics (Monthly Variance tab)**: The honest "only X months of data available" empty state is correct but creates a page that shows almost nothing for new installations. This is unavoidable — just ensure the explanation is prominent.

### Low cognitive load pages (information is immediately parseable):

**Briefing**: The insight cards with color coding (green/yellow/red), plain-language descriptions, and action buttons minimize cognitive work.
**Alarm List**: Tabular, severity-coded, sortable. Self-explaining.
**Equipment Health**: Traffic-light health scores per meter. Immediately parseable.
**Transformer Loading**: Progress bar for loading percentage. The "Critical / Warning / Normal" label does the interpretation for the user.

---

## Cognitive Load Reduction Recommendations

**1. Add a 3-number status line to every page header**
Every page should open with: "As of [date]: X kWh consumed · PF [value] · [N] active alarms"
This costs one DB query (already cached) and immediately orients the user without requiring them to navigate away to check system status.

**2. Replace chart X-axis explanations with plain-language summaries above each chart**
Instead of: [chart with unlabeled axes]
Write: "This chart shows average demand (kW) for each hour of the day. Peak demand typically occurs at [time]."
One sentence. Removes the requirement to know what the chart format means.

**3. Add a "What this means" card below non-obvious analytics**
Demand Duration Curve, Diversity Factor, and PF Penalty Calculator should each have a 2-sentence plain-language interpretation of the result: "Your peak demand exceeded contracted limit 3% of the time. This means you may incur X Rs. in demand charges."

**4. Move engineering-only pages behind an "Advanced" toggle or separate sidebar section**
Diversity Factor, Demand Duration Curve, Line Voltage Balance, Frequency Analysis — these are tools for electrical engineers running quarterly studies, not daily operational tools. Putting them in the main sidebar next to Live Monitoring creates a navigation that treats every analysis as equally important to operations. They are not.

---

## Productivity Measurement

### Reduces clicks?
**Current state**: Finding out if everything is OK requires: Briefing (1 page) + Live Monitoring (second click) + Alarms (third click). Three pages for one question.
**Improvement**: A system-status widget on Briefing showing "3/3 meters online · 0 active alarms · Consumption normal" reduces this to 0 additional clicks on a green day.
**Gain**: ~2 clicks eliminated on every normal shift start.

### Reduces investigation time?
**Current state**: An alarm fires on "Floor 2 Meter." The engineer goes: Alarm List → click device → Root Cause → see energy context. 3 clicks + reading time.
**Improvement**: Make the alarm row itself expandable with a mini energy context panel inline. Or auto-open Root Cause with the relevant alarm pre-selected when clicking from Alarm List.
**Gain**: 1–2 clicks, ~30 seconds per investigation.

### Reduces report generation time?
**Current state**: Monthly report for management requires visiting: Cost Dashboard + Budget vs Actual + Carbon + QBR.
**Improvement**: QBR template already pulls this together. The gap is that the user has to navigate there — it should be a prominent action ("Generate Monthly Report") on the Briefing page for the last day of the month.
**Gain**: 5–10 minutes per month report.

### Improves operational awareness?
**Current state**: Yes, via Live Monitoring and Equipment Health.
**Gap**: There is no "nothing to do" positive confirmation. If a shift engineer checks the app and everything is fine, the current design offers no clear signal that says "all clear — no action required." The engineer has to visit 3–4 pages to confirm absence of problems.
**Improvement**: Dashboard / Briefing should have an explicit "All Systems Normal" state that is just as prominent as the alarm state. Green is as important as red.

---

## Consolidation Action Plan

This is the concrete implementation recommendation, not a philosophy statement.

### Immediate (delete, no rebuild needed)

1. **Delete Frequency page** from sidebar. Keep the controller and MFreq monitoring — just add a frequency-deviation alarm threshold in Settings instead of a dedicated analysis page.

2. **Remove CO₂ card from Dashboard**. The number is `kWh × 0.82`. Anyone who needs CO₂ figures knows how to multiply. It reduces signal-to-noise on the most-visited page.

3. **Remove "Online Meters" KPI card from Dashboard**. Demote to a small indicator in the page header or topbar. A 3/3 KPI card that never changes is using prime real estate on the most important page.

### Medium-term (consolidate, some rebuild)

4. **Merge Voltage Imbalance + Voltage Range + Line Voltage Balance → Power Quality "Voltage" tab**
Power Quality becomes a 3-tab page: PF & Reactive Power | Voltage | Harmonics.
Result: 3 sidebar items → 1. No information lost.

5. **Merge Reactive Power → Power Quality "PF & Reactive Power" tab**
Result: 4 sidebar items → 1 (Power Quality).

6. **Merge Diversity Factor → Capacity Headroom as a lower section**
Result: 2 sidebar items → 1 (Capacity Headroom).

7. **Move Carbon → Reports section or QBR appendix**
Not a daily operational page. Remove from main sidebar.

### Sidebar restructure (after consolidations)

**Before (35 items, 6 groups)**

**After target (20 items, 4 groups)**

```
Operations          Engineering Tools    Financial           Reports & Admin
────────────        ─────────────────    ─────────           ───────────────
Briefing            Power Quality *      Cost Dashboard       Reports
Live Monitoring     Transformer Loading  Budget vs Actual     QBR Template
Meter Faceplate     Capacity Headroom *  PF Penalty Calc      Data Quality
Floor Map           Demand Curve         What-If Modeler      Settings
Alarms              Time of Use
Root Cause          Load Analytics
Alarm Response      Heatmap
Equipment Health    Anomaly Detection
Flow Monitoring     Energy Analysis
                    Comparison
                    Forecast
```

`*` = consolidated (Power Quality absorbs 4 pages; Capacity Headroom absorbs 1 page)

Removed from navigation: Frequency, Carbon (→ Reports), Diversity Factor (→ Capacity Headroom), Voltage Imbalance (→ Power Quality), Voltage Range (→ Power Quality), Line Voltage Balance (→ Power Quality), Reactive Power (→ Power Quality).

---

## Philosophy Challenge — One Question Per Page

Every page must answer: "What decision does this page help someone make?"

| Page | Decision it enables | Verdict |
|---|---|---|
| Briefing | "Do I need to act on anything this shift?" | ✅ Keep |
| Live Monitoring | "Is the plant running normally right now?" | ✅ Keep |
| Meter Faceplate | "What is wrong with this specific meter?" | ✅ Keep |
| Dashboard | "What is the plant's current energy state at a glance?" | ✅ Keep (after surgery) |
| Alarms | "What needs attention right now?" | ✅ Keep |
| Root Cause | "Why did this alarm fire?" | ✅ Keep |
| Alarm Response | "Are we handling alarms fast enough?" | ✅ Keep |
| Equipment Health | "Which equipment needs attention before it fails?" | ✅ Keep |
| Transformer Loading | "Are transformers at risk of overload?" | ✅ Keep |
| Floor Map | "Which physical area has the problem?" | ✅ Keep |
| Flow Monitoring | "Do I need to order fuel?" | ✅ Keep |
| Power Quality | "Is our power quality within limits?" | ✅ Keep (consolidated) |
| Capacity Headroom | "Can I add more load without exceeding limits?" | ✅ Keep (consolidated) |
| Time of Use | "When is energy being wasted?" | ✅ Keep |
| Load Analytics | "How does load vary by meter and day?" | ✅ Keep |
| Demand Curve | "How often do we exceed our contracted demand?" | ✅ Keep |
| Heatmap | "Are there visible consumption patterns over weeks?" | ✅ Keep |
| Anomaly Detection | "Is there something unusual I should investigate?" | ✅ Keep |
| Energy Analysis | "What is the consumption trend over a custom period?" | ✅ Keep (verify no duplicate with Load Analytics) |
| Comparison | "Which floor is consuming more than the others?" | ✅ Keep |
| Forecast | "What will consumption be next week?" | ✅ Keep (with labeling caveat) |
| Budget vs Actual | "Are we within energy budget?" | ✅ Keep |
| Cost Dashboard | "What are we spending and how does it trend?" | ✅ Keep (verify merge opportunity with Budget) |
| PF Penalty Calc | "What is our PF penalty and how much would capacitors save?" | ✅ Keep |
| What-If Modeler | "What would we save if we changed consumption by X%?" | ✅ Keep |
| QBR Template | "Generate a quarterly management report." | ✅ Keep (move to Reports group) |
| Reports | "Download data for a period." | ✅ Keep |
| Data Quality | "Is the meter data reliable?" | ✅ Keep |
| Settings | Configuration. | ✅ Keep |
| **Frequency** | ~~"Is grid frequency within 0.5 Hz?"~~ | ❌ **Delete** — user cannot act on this |
| **Carbon** | "What is our CO₂ footprint?" | 🟠 Move to Reports — not operational |
| **Voltage Imbalance** | Already covered by Power Quality | 🔴 Merge → Power Quality |
| **Voltage Range** | Already covered by Power Quality | 🔴 Merge → Power Quality |
| **Line Voltage Balance** | Already covered by Power Quality | 🔴 Merge → Power Quality |
| **Reactive Power** | Already covered by Power Quality | 🔴 Merge → Power Quality |
| **Diversity Factor** | Already answered by Capacity Headroom | 🔴 Merge → Capacity Headroom |

---

## The Standard

The target experience:

> Shift engineer starts their 6am shift. Opens Briefing. Reads: "Consumption was 12% below 7-day average. All 3 meters online. 0 active alarms. Floor 2 was the highest consumer at 41%." Opens Live Monitoring to confirm live status. Closes the laptop. Shift started in under 60 seconds.

> Energy manager on Friday afternoon. Opens Cost Dashboard. Sees monthly spend is 8% over budget. Clicks through to Budget vs Actual. Sees which week drove the overage. Opens Load Analytics Daily Variance — confirms Tuesday 24th was anomalous. Clicks to Anomaly Detection — confirms the spike was flagged. Has everything needed for the Monday report in under 3 minutes.

Neither of those workflows requires Frequency, Voltage Range, Line Voltage Balance, Reactive Power as separate pages, or a CO₂ card that repeats the kWh number in different units.

---

## Summary: What to Build in Sprint 8

Priority order based on impact per effort:

1. **Delete Frequency page** (5 min — remove from sidebar array)
2. **Remove CO₂ KPI from Dashboard, demote Online Meters** (30 min)
3. **Merge Power Quality group** (Voltage Imbalance + Voltage Range + Line Voltage Balance + Reactive Power → tabs in Power Quality) (1 day)
4. **Merge Diversity Factor → Capacity Headroom** (2 hours)
5. **Move Carbon to Reports** (1 hour)
6. **Restructure sidebar into 4 clear groups** (2 hours)
7. **Add "All Systems Normal" state to Briefing** (1 hour)
8. **Add system-status micro-summary to page headers** (half day — shared component)
9. **Inline alarm context on Alarm List** (to reduce clicks to investigation) (half day)
