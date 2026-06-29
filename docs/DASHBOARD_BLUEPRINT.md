# Dashboard Blueprint - Energy Monitoring System

**Version:** 1.0  
**Date:** June 29, 2026  
**Purpose:** Detailed specification for dashboard design, layout, components, and interactions  
**Based on:** REQUIREMENTS.md + DESIGN_RESEARCH.md

---

## 1. Navigation Architecture

### 1.1 Application Navigation Map

```
┌─ ENERGY MONITORING SYSTEM ─────────────────────────────┐
│                                                         │
├─ Navigation Sidebar                                    │
│ ├─ Dashboard (Executive Overview)                      │
│ ├─ Live Monitoring (Real-time status)                  │
│ ├─ Energy Analysis (Trends & historical)               │
│ ├─ Locations (Hierarchical drill-down)                 │
│ ├─ Meters (Individual meter details)                   │
│ ├─ Alarms (Active & historical)                        │
│ ├─ Reports (Generate & export)                         │
│ └─ Settings (Admin configuration)                      │
│    ├─ User Management                                  │
│    ├─ Device Configuration                             │
│    ├─ Alert Thresholds                                 │
│    └─ System Settings                                  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 1.2 Breadcrumb Navigation
All pages show breadcrumb:
```
Home > [Current Section] > [Current Page]

Examples:
Home > Dashboard
Home > Energy Analysis > Trends
Home > Locations > Building A > Floor 1
Home > Meters > Meter-Floor1
Home > Reports > Generate Report
```

---

## 2. Global Filter Bar Specification

### 2.1 Filter Bar Layout (Desktop)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  🏢 Plant                      🏗️  Building                  📍 Location      │
│  [Select Plant ▼]              [Select Building ▼]          [Select Area ▼] │
│                                                                               │
│  📊 Metric               📅 Date Range            🔄 Compare Period         │
│  [All Meters ▼]          [Today ▼]                ☐ Enable Comparison     │
│                                                                               │
│  📆 Custom Range (if selected)                                              │
│  From: [________] To: [________]     [Apply] [Reset] │ [Refresh] [Export PDF] │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Filter Bar Sections

#### Section 1: Location Hierarchy
| Field | Type | Options | Default |
|-------|------|---------|---------|
| Plant | Dropdown | All, Plant-1, Plant-2 | All |
| Building | Dropdown | All, Building A, Building B | All |
| Location/Area | Dropdown | All, Floor 1, Floor 2, Warehouse | All |
| Meter | Dropdown | All, Meter-Floor1, Meter-Floor2, ... | All |

**Cascading Logic:**
```
IF Plant = "All" THEN Building dropdown shows all buildings
IF Plant = "Plant-1" THEN Building dropdown shows only buildings in Plant-1
IF Building = "Building A" THEN Location shows only locations in Building A
```

#### Section 2: Time Range
| Field | Type | Options | Default |
|-------|------|---------|---------|
| Quick Select | Buttons | Today, Yesterday, This Week, Last Week, This Month, Last Month, This Year, Custom | Today |
| Custom Start | Date Picker | Any date | Today's date |
| Custom End | Date Picker | Any date ≥ Start | Today's date |

#### Section 3: Comparison
| Field | Type | Options | Default |
|-------|------|---------|---------|
| Enable Comparison | Toggle | ON / OFF | OFF |
| Compare With | Dropdown (if ON) | Previous Day, Previous Week, Previous Month, Previous Year | Previous Day |

#### Section 4: Actions
| Button | Action | Icon |
|--------|--------|------|
| Refresh | Re-query data from API | 🔄 |
| Export | Download dashboard as PDF | ⬇️ |
| Reset | Clear all filters, return to defaults | ↺ |

### 2.3 Filter Bar Responsiveness

**Desktop (lg 992px+):**
```
All fields on one horizontal row
Adequate spacing between sections
Dropdown menus below field
```

**Tablet (md 768px+):**
```
Two rows:
Row 1: Location filters (Plant, Building, Area, Meter)
Row 2: Date, Comparison, Actions
Dropdowns collapse into modal on mobile
```

**Mobile (xs-sm < 768px):**
```
Single accordion / collapsible panel
Header: "🔽 Filters (Active: 3)"
Click to expand full filter panel
All dropdowns become bottom sheets (iOS-style)
```

---

## 3. Dashboard Pages - Detailed Wireframes

### 3.1 EXECUTIVE DASHBOARD

#### 3.1.1 Page Purpose
High-level overview for plant manager and executives. Show energy consumption at a glance with trend indicators and alerts.

#### 3.1.2 Page Layout

```
┌─ Header ─────────────────────────────────────────────────────────────────┐
│ Energy Monitoring System › Dashboard           User Profile ▼ Settings   │
└──────────────────────────────────────────────────────────────────────────┘

┌─ Filter Bar ──────────────────────────────────────────────────────────────┐
│ 🏢 Plant [All▼]  Building [A▼]  Area [1▼]  Date [Today▼]  Comparison ☐  │
│ [Refresh] [Export PDF]                                                   │
└──────────────────────────────────────────────────────────────────────────┘

┌─ KPI SECTION ─────────────────────────────────────────────────────────────┐
│                                                                            │
│  ┌─ Card 1 ───────────┐  ┌─ Card 2 ───────────┐  ┌─ Card 3 ──────────┐  │
│  │ Today's           │  │ Current            │  │ Peak Demand       │  │
│  │ Consumption       │  │ Load               │  │ Today             │  │
│  │                   │  │                    │  │                   │  │
│  │ 1,254 kWh         │  │ 458 kW             │  │ 702 kW            │  │
│  │ ↑ 12%             │  │ ±0 kW              │  │ @ 18:30           │  │
│  │ (Green)           │  │ (Normal)           │  │ (Normal)          │  │
│  └───────────────────┘  └────────────────────┘  └───────────────────┘  │
│                                                                            │
│  ┌─ Card 4 ───────────┐  ┌─ Card 5 ───────────┐  ┌─ Card 6 ──────────┐  │
│  │ Monthly Total     │  │ Online Meters      │  │ Est. Monthly Cost │  │
│  │                   │  │                    │  │                   │  │
│  │ 35,200 kWh        │  │ 34 / 36            │  │ ₹ 2.84 Million    │  │
│  │ ↑ 8%              │  │ ✓ 94% Healthy      │  │ ↑ 5%              │  │
│  │ (Green)           │  │ (Green)            │  │ (Amber)           │  │
│  └───────────────────┘  └────────────────────┘  └───────────────────┘  │
│                                                                            │
│  ┌─ Card 7 ───────────┐  ┌─ Card 8 ───────────┐                         │
│  │ Avg Power Factor  │  │ CO₂ Emissions      │                         │
│  │                   │  │                    │                         │
│  │ 0.96              │  │ 4.1 Metric Tons    │                         │
│  │ ✓ Excellent       │  │ Equivalent to      │                         │
│  │ (Green)           │  │ 5 trees/day        │                         │
│  └───────────────────┘  └────────────────────┘                         │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘

┌─ MAIN CHART SECTION ──────────────────────────────────────────────────────┐
│                                                                            │
│ Daily Energy Consumption (Today's Breakdown)                  [24h] [48h] │
│                                                                            │
│  700 kW │                                                                  │
│         │    ╱╲      ╱╲                                                    │
│  600 kW │   ╱  ╲    ╱  ╲                                                   │
│         │  ╱    ╲  ╱    ╲                                                  │
│  500 kW │ ╱      ╲╱      ╲                                                 │
│         │╱                ╲                                                │
│  400 kW ├─────────────────────                                            │
│         └──────────────────────────→ Hours                                 │
│          0  4  8 12 16 20 24                                               │
│                                                                            │
│ Peak: 702 kW (18:30)  Avg: 512 kW  Min: 405 kW                            │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘

┌─ BOTTOM SECTION (Two Columns) ────────────────────────────────────────────┐
│                                                                            │
│  ┌─ Consumption by Location ────────┐  ┌─ Top 10 Consumers ────────────┐  │
│  │                                   │  │                               │  │
│  │ Production   ████████████████ 45% │  │ 1. Machine A    12.5 kW       │  │
│  │ Warehouse    ████████ 25%         │  │ 2. Machine B    10.8 kW       │  │
│  │ Utilities    ███████ 20%          │  │ 3. Building 2    8.2 kW       │  │
│  │ Admin        ████ 10%             │  │ 4. HVAC          7.5 kW       │  │
│  │              ─────────            │  │ 5. Pump-A        6.3 kW       │  │
│  │              50 MWh (total)       │  │ 6. Compressor    5.8 kW       │  │
│  │                                   │  │ 7. Boiler        5.2 kW       │  │
│  └───────────────────────────────────┘  │ 8. Lighting      4.1 kW       │  │
│                                          │ 9. Process Ctrl  3.8 kW       │  │
│                                          │ 10. Misc. Load   2.9 kW       │  │
│  ┌─ Energy Distribution ─────────────┐  │                               │  │
│  │                                   │  │ [View Full List]              │  │
│  │        Production                 │  └───────────────────────────────┘  │
│  │           45%                     │                                     │
│  │       ╱───────────╲               │                                     │
│  │    /               \              │                                     │
│  │ Utilities           Warehouse     │                                     │
│  │   20%                  25%        │                                     │
│  │    \               /              │                                     │
│  │       ╲─────────╱                 │                                     │
│  │          Admin                    │                                     │
│  │           10%                     │                                     │
│  │                                   │                                     │
│  └───────────────────────────────────┘                                     │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘

┌─ FOOTER ──────────────────────────────────────────────────────────────────┐
│ Last updated: 2 minutes ago  │  Data source: db_SCADA  │  © 2026 Company  │
└──────────────────────────────────────────────────────────────────────────┘
```

#### 3.1.3 Component Specifications

**KPI Cards:**
- Dimensions: 280px × 140px (desktop)
- Background: #1E293B
- Border-left: 4px solid (green #10B981 if positive, red if negative)
- Title: 16px, #CBD5E1, uppercase with spacing
- Value: 48px, #FFFFFF, bold, monospace font
- Trend: 14px, Green ↑ or Red ↓, with percentage
- Subtitle: 12px, #64748B

**Main Chart:**
- Library: ApexCharts
- Type: Area chart with gradient
- Color: Blue gradient (#2563EB → #0284C7)
- Height: 400px
- Interactive: Hover shows tooltip, click shows detail view

**Secondary Charts:**
- Consumption by Location: Horizontal bar chart, stacked
- Top 10 Consumers: Vertical bar chart, ranked
- Energy Distribution: Donut chart with labels

#### 3.1.4 Interaction Flows

1. **Click KPI Card:** Drill-down to related dashboard
   - Click "Today's Consumption" → Navigate to Energy Analysis, filter to TODAY
   - Click "Peak Demand" → Show peak hours detail chart
   - Click "Online Meters" → Show Live Monitoring dashboard

2. **Hover on Chart Point:** Show tooltip
   - Time: "18:30"
   - Value: "702 kW"
   - Status: "🔴 Peak demand"

3. **Click Compare Period (if enabled):** Show comparison lines
   - Add overlay line showing same time last period
   - Add legend toggle for each line

4. **Export PDF:** Generate formatted report
   - Include all KPI cards
   - Include main chart
   - Include location breakdown
   - Filename: "Energy_Dashboard_2026-06-29.pdf"

---

### 3.2 LIVE MONITORING DASHBOARD

#### 3.2.1 Page Purpose
Real-time meter status for operators. Shows current readings, status, and quick alerts.

#### 3.2.2 Page Layout

```
┌─ Header & Filters (same as Executive Dashboard) ────────────────────────┐
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘

┌─ STATUS SUMMARY ──────────────────────────────────────────────────────────┐
│                                                                           │
│  🟢 Online: 34    🟡 Warning: 2    🔴 Offline: 0    ⚪ Unknown: 0         │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ LIVE METERS TABLE ───────────────────────────────────────────────────────┐
│                                                                           │
│ Meter Name          │Status│Voltage│Current│Power │PF  │Freq│Trend      │
│─────────────────────┼──────┼───────┼───────┼──────┼────┼────┼───────────│
│ Meter-Floor1        │  🟢  │230.5V │15.3A  │3.5kW │0.96│50Hz│ ↗ (trend) │
│ Meter-Floor2        │  🟢  │229.8V │12.7A  │2.8kW │0.94│50Hz│ ➜ (stable)│
│ Meter-Floor3        │  🟡  │231.2V │14.2A  │3.2kW │0.92│50Hz│ ↘ (down)  │
│ FuelTank-Main       │  🟢  │  —    │  —    │  —   │ —  │ —  │ ↗ (trend) │
│ FuelTank-Backup     │  🔴  │  —    │  —    │  —   │ —  │ —  │ ❌ Offline│
│ PLC-Main            │  🟢  │  —    │  —    │  —   │ —  │ —  │ ➜ (stable)│
│                     │      │       │       │      │    │    │           │
│ [Scroll for more...] Last updated: 30 seconds ago                        │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ ALERTS SECTION (if any active) ──────────────────────────────────────────┐
│                                                                           │
│ ⚠️  Active Alerts: 2                                                      │
│                                                                           │
│ 🔴 [CRITICAL] Fuel Tank 2 Level 45% - Critical Low              ← Ack    │
│ 🟡 [WARNING] Meter Floor3 PF 0.92 - Below 0.93 Threshold        ← Ack    │
│                                                                           │
│ [View All Alerts]                                                        │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ MINI CHARTS (Optional - Bottom Section) ─────────────────────────────────┐
│                                                                           │
│ Quick 24-Hour Trends (Click for detail):                                 │
│                                                                           │
│  ┌─ Meter-Floor1 ──┐  ┌─ Meter-Floor2 ──┐  ┌─ Meter-Floor3 ──┐         │
│  │     📈           │  │     ➜            │  │     📉           │         │
│  │  Consumption    │  │  Consumption    │  │  Consumption    │         │
│  │  3.5 kW (now)   │  │  2.8 kW (now)   │  │  3.2 kW (now)   │         │
│  │  Avg: 3.4 kW    │  │  Avg: 2.6 kW    │  │  Avg: 3.1 kW    │         │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘         │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

#### 3.2.3 Table Column Specifications

| Column | Data Type | Format | Editable | Notes |
|--------|-----------|--------|----------|-------|
| Meter Name | String | Text | No | Links to detail page |
| Status | Enum | 🟢/🟡/🔴/⚪ | No | Green=Online, Yellow=Warning, Red=Offline |
| Voltage | Float | "230.5V" | No | Last reading, line-to-neutral L1 |
| Current | Float | "15.3A" | No | Last reading, L1 phase |
| Power | Float | "3.5 kW" | No | Total active power |
| PF | Float | "0.96" | No | Power factor (ideal 0.95-1.0) |
| Freq | Float | "50Hz" | No | Grid frequency |
| Trend | Sparkline | Visual | No | 24-hour mini chart |

#### 3.2.4 Interaction Flows

1. **Click Meter Row:** Open meter detail panel (side drawer)
   - Show full meter info + 24h chart
   - Show last 10 readings table
   - Link to daily consumption chart

2. **Hover Trend Sparkline:** Show tooltip with value at that time
   - Time: "12:00"
   - Value: "3.2 kW"

3. **Acknowledge Alert:** Update alarm status
   - Click [Ack] button → Confirm dialog → Update DB → Remove from list
   - Show toast: "✓ Alert acknowledged"

4. **Auto-refresh:** Every 30 seconds
   - Query /api/meters/live endpoint
   - Update table rows with smooth transition
   - Flash indicator on changed values (brief yellow highlight)

---

### 3.3 ENERGY ANALYSIS DASHBOARD

#### 3.3.1 Page Purpose
Historical trend analysis and detailed consumption reports. For energy managers and analysts.

#### 3.3.2 Page Layout

```
┌─ Header & Filters ────────────────────────────────────────────────────────┐
│ Additional controls:                                                      │
│ [Timeframe: Daily▼] [Comparison: Off▼] [Metrics: kWh▼] [Avg/Peak/Min]    │
└──────────────────────────────────────────────────────────────────────────┘

┌─ MAIN CONSUMPTION CHART ──────────────────────────────────────────────────┐
│                                                                           │
│ Consumption Trend (Last 30 Days)                                         │
│                                                                           │
│ 1200 kWh │                                                                 │
│          │      ╱╲        ╱╲        ╱╲                                     │
│ 1000 kWh │     ╱  ╲      ╱  ╲      ╱  ╲                                    │
│          │    ╱    ╲    ╱    ╲    ╱    ╲                                   │
│  800 kWh │   ╱      ╲  ╱      ╲  ╱      ╲                                  │
│          │  ╱        ╲╱        ╲╱        ╲                                 │
│  600 kWh ├─────────────────────────────────                               │
│          │ ─ This Month                                                    │
│          │ ─ ─ Last Month (for comparison if enabled)                     │
│          │ ─ ─ ─ Forecast (if enabled)                                    │
│          │                                                                 │
│          └─────────────────────────────→ Days                              │
│           1   5   10  15  20  25  30                                       │
│                                                                           │
│ Stats: Peak: 1,150 kWh/day (Day 18)  Avg: 850 kWh/day  Min: 620 kWh/day  │
│ Trend: ↑ +8% vs Last Month                                                │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ SECONDARY ANALYSIS (Split View) ─────────────────────────────────────────┐
│                                                                           │
│  ┌─ Daily Breakdown ─────────────────────┐  ┌─ Peak Hours Heatmap ────┐ │
│  │ (Stacked area by location)            │  │                        │ │
│  │                                        │  │ Mon 🟩🟩🟨🟧🟥         │ │
│  │ Production (orange)                   │  │ Tue 🟩🟩🟨🟨🟧          │ │
│  │ Warehouse (blue)                      │  │ Wed 🟩🟨🟧🟧🟥          │ │
│  │ Admin (purple)                        │  │ Thu 🟩🟨🟨🟧🟥          │ │
│  │ Utilities (green)                     │  │ Fri 🟩🟨🟧🟥🟥          │ │
│  │                                        │  │ Sat 🟩🟨🟨🟨🟧          │ │
│  │ [Grouped View] [Stacked View]          │  │ Sun 🟩🟩🟨🟧🟥          │ │
│  │                                        │  │                        │ │
│  │                                        │  │ 🟩=Low  🟧=High 🟥=Peak │ │
│  │                                        │  │ 6AM 12PM 6PM Hrs       │ │
│  └────────────────────────────────────────┘  └────────────────────────┘ │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ DETAILED TABLE ──────────────────────────────────────────────────────────┐
│                                                                           │
│ Daily Breakdown Table (Sortable, Filterable)                             │
│                                                                           │
│ Date       │ Production │ Warehouse │ Admin │ Utilities │ Total  │ Trend │
│────────────┼────────────┼───────────┼───────┼───────────┼────────┼───────│
│ 2026-06-29 │  450 kWh   │  280 kWh  │60 kWh │  215 kWh  │1005 kWh│ ↑ 8% │
│ 2026-06-28 │  430 kWh   │  265 kWh  │58 kWh │  200 kWh  │  953 kWh│ ↑12% │
│ 2026-06-27 │  420 kWh   │  250 kWh  │55 kWh │  190 kWh  │  915 kWh│ ↓ 3% │
│ ... (more rows)                                                           │
│                                                                           │
│ [Scroll] Page 1 of 12                [Download as CSV] [Print]           │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

#### 3.3.3 Timeframe Controls

| Control | Type | Options | Updates |
|---------|------|---------|---------|
| Timeframe | Button group | Daily, Weekly, Monthly, Yearly, Custom | Chart aggregation |
| Comparison | Dropdown | Off, Prev Day, Prev Week, Prev Month, Prev Year | Overlay second line |
| Metrics | Dropdown | kWh (total), kW (peak), kVA (apparent), kVAR (reactive) | Y-axis metric |
| Statistics | Checkboxes | Peak, Avg, Min, Forecast | Display indicators |

#### 3.3.4 Export Options

```
[Export PDF] [Export Excel] [Export CSV]

PDF exports:
- Chart image
- Daily breakdown table
- Summary statistics
- Comparison data (if enabled)
- Filename: "Energy_Analysis_2026-06-01_to_2026-06-29.pdf"

Excel exports:
- Sheet 1: Summary (KPIs, peak, avg, min)
- Sheet 2: Daily Data (all locations, all dates)
- Sheet 3: Charts (embedded)
- Filename: "Energy_Analysis_2026-06-01_to_2026-06-29.xlsx"
```

---

### 3.4 LOCATIONS DASHBOARD

#### 3.4.1 Page Purpose
Hierarchical drill-down by plant/building/floor/area. For supervisors and facility managers.

#### 3.4.2 Page Layout

```
┌─ Header & Filters ────────────────────────────────────────────────────────┐
│ Hierarchy: [Collapse All] [Expand All]  Date: [Today▼]                   │
└──────────────────────────────────────────────────────────────────────────┘

┌─ LOCATION TREE + KPI CARDS (Split View) ──────────────────────────────────┐
│                                                                           │
│ Left Panel (Tree):          │ Right Panel (Selected Location Details):    │
│                             │                                           │
│ 📁 Plant-1                  │ 📍 Building A                              │
│   📁 Building A ← Selected  │                                           │
│   │   📁 Floor 1            │ Consumption: 850 kWh (Today)              │
│   │   │   ⚡ Meter-Floor1   │ Peak: 280 kW  |  Avg: 218 kW              │
│   │   │   ⚡ Meter-Floor2   │ Online: 4/4 Meters (✓ 100%)               │
│   │   │ 📁 Floor 2          │ Avg Power Factor: 0.95                    │
│   │   │   ⚡ Meter-Floor3   │                                           │
│   │   │   ⚡ Meter-Floor4   │ ┌─ Consumption Trend ────────────┐         │
│   │   │ 📁 Floor 3          │ │  400 kWh │        ╱╲           │         │
│   │   │   ⚡ Tank-Main      │ │  300 kWh │       ╱  ╲          │         │
│   │   │   ⚡ Tank-Backup    │ │  200 kWh │      ╱    ╲         │         │
│   │                         │ │  100 kWh │     ╱      ╲        │         │
│   📁 Building B             │ │        0 │    ╱        ╲       │         │
│   │   📁 Warehouse          │ │         └─────────────────     │         │
│   │   │   ⚡ Meter-W1       │ │          0  4  8 12 16 20 24  │         │
│   │   │   ⚡ Meter-W2       │ └─────────────────────────────────┘         │
│   │   ⚡ Tank-Warehouse     │                                           │
│   │                         │ ┌─ Meters in this Location ────────┐      │
│   📁 Building C             │ │                                  │      │
│   │   ⚡ PLC-Main          │ │ Meter-Floor1    🟢 3.5 kW ↗       │      │
│   │   ⚡ PLC-Backup        │ │ Meter-Floor2    🟢 2.8 kW ➜       │      │
│   │                         │ │ Meter-Floor3    🟡 3.2 kW ↘       │      │
│   📁 Plant-2               │ │ Meter-Floor4    🟢 2.9 kW ↗       │      │
│   │   ...                  │ │                                  │      │
│                             │ └──────────────────────────────────┘      │
│ [↕ Resize]                  │                                           │
│                             │ [View Detail] [Compare with...]           │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

#### 3.4.3 Interaction Flows

1. **Click Location Node:** Right panel updates
   - Show KPIs for selected location
   - Show meter list for that location
   - Update chart to show consumption breakdown

2. **Expand/Collapse Folders:**
   - Show children locations and meters
   - Remember expansion state in local storage

3. **Click Meter in Right Panel:**
   - Navigate to Meter Details page
   - Filter shows context (Building A > Floor 1 > Meter-Floor1)

4. **Compare Locations:**
   - Click [Compare with...] button
   - Modal shows list of same-level siblings (other floors, other buildings)
   - Select another location → Show side-by-side consumption comparison

---

### 3.5 METERS DASHBOARD

#### 3.5.1 Page Purpose
Individual meter deep-dive. Shows all electrical parameters, trends, and history.

#### 3.5.2 Page Layout

```
┌─ Header ──────────────────────────────────────────────────────────────────┐
│ Breadcrumb: Home > Meters > Meter-Floor1                                  │
│                                                                           │
│ Meter Information                                                         │
│ ├─ Name: Meter-Floor1                                                    │
│ ├─ Location: Building A, Floor 1, Area Production                        │
│ ├─ Model: Janitza UMG 96 RM                                              │
│ ├─ Status: 🟢 Online (Last reading: 2 min ago)                           │
│ └─ Serial: JAN-2024-00127                                                │
│                                                                           │
│ [Detail View] [Charts] [History] [Alarms]                                │
│                                                                           │
└──────────────────────────────────────────────────────────────────────────┘

┌─ TAB 1: LIVE VALUES (Default) ────────────────────────────────────────────┐
│                                                                           │
│ ┌─ Voltage Metrics ─────────┐  ┌─ Current Metrics ─────────┐             │
│ │ L1-N: 230.5V  ✓ Normal    │  │ L1:  15.3A   ✓ Normal    │             │
│ │ L2-N: 230.1V  ✓ Normal    │  │ L2:  15.1A   ✓ Normal    │             │
│ │ L3-N: 229.8V  ✓ Normal    │  │ L3:  15.2A   ✓ Normal    │             │
│ │                            │  │                          │             │
│ │ L1-L2: 398.4V ✓ Normal    │  │ Avg: 15.2A   ✓ Balanced  │             │
│ │ L2-L3: 398.2V ✓ Normal    │  │ Max: 15.3A               │             │
│ │ L1-L3: 398.1V ✓ Normal    │  │ Min: 15.1A               │             │
│ └───────────────────────────┘  └──────────────────────────┘             │
│                                                                           │
│ ┌─ Power Metrics ────────────────────┐  ┌─ Quality Metrics ──────────┐   │
│ │ Active Power (kW):    3.5 kW       │  │ Frequency: 50.0 Hz   ✓    │   │
│ │ Reactive Power (kVAR): 0.28 kVAR   │  │ Power Factor (avg): 0.96  │   │
│ │ Apparent Power (kVA):  3.65 kVA    │  │  ├─ L1: 0.96              │   │
│ │                                    │  │  ├─ L2: 0.95              │   │
│ │ Power Factor:         0.96  ✓      │  │  └─ L3: 0.97              │   │
│ │ Phase Balance:        98%   ✓      │  │                           │   │
│ └────────────────────────────────────┘  │ THD (Voltage): 2.3%  ✓    │   │
│                                         │ THD (Current):  5.1%  ✓    │   │
│ ┌─ Energy Counters (Lifetime) ────────┐ └───────────────────────────┘   │
│ │ Import Energy (kWh): 45,230         │                                 │
│ │ Export Energy (kWh): 0              │                                 │
│ │ Reactive Energy (kVAh): 3,450       │                                 │
│ │ Apparent Energy (kVAh): 46,120      │                                 │
│ └─────────────────────────────────────┘                                 │
│                                                                           │
│ Last Updated: 2026-06-29 16:42:30  |  Next Update: 16:42:32             │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ TAB 2: CHARTS (Multi-selector) ──────────────────────────────────────────┐
│                                                                           │
│ [Voltage] [Current] [Power] [Frequency] [Power Factor] [Harmonics]       │
│                                                                           │
│ Chart Type: [Line ▼]  Timeframe: [Last 24h ▼]  [Custom Range]           │
│                                                                           │
│ ┌─ Voltage Trend (Last 24 Hours) ──────────────────────────────────────┐ │
│ │  232 V │                                                              │ │
│ │  231 V │  ╱───╲      ╱───╲      ╱───╲                                 │ │
│ │  230 V │ ╱     ╲    ╱     ╲    ╱     ╲                                │ │
│ │  229 V │╱       ╲  ╱       ╲  ╱       ╲                               │ │
│ │  228 V │         ╲╱         ╲╱         ╲                              │ │
│ │        └──────────────────────────────────→ Time                      │ │
│ │        0:00     6:00    12:00   18:00   24:00                         │ │
│ │                                                                        │ │
│ │ ─ L1  ─ L2  ─ L3  [Toggle phases]                                     │ │
│ │                                                                        │ │
│ │ Stats: Peak: 231.8V  Avg: 230.2V  Min: 228.5V  Std Dev: 0.8V         │ │
│ └────────────────────────────────────────────────────────────────────────┘ │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ TAB 3: HISTORY (Readings Table) ─────────────────────────────────────────┐
│                                                                           │
│ [Export as CSV]  Showing: 1-50 of 1,440  [< Prev] [Next >]              │
│                                                                           │
│ DateTime            │VL1│VL2│VL3│IL1│IL2│IL3│kW  │kVAR│PF │Freq        │
│─────────────────────┼───┼───┼───┼───┼───┼───┼────┼────┼───┼─────       │
│2026-06-29 16:42:00  │230│230│229│15 │15 │15 │3.5 │0.28│0.96│50.0       │
│2026-06-29 16:37:00  │231│231│230│15 │15 │15 │3.5 │0.27│0.96│50.0       │
│2026-06-29 16:32:00  │230│229│230│15 │15 │15 │3.4 │0.29│0.95│50.0       │
│... (1,437 more rows)                                                    │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ TAB 4: ALARMS ───────────────────────────────────────────────────────────┐
│                                                                           │
│ Active Alarms for this Meter: None  ✓                                    │
│                                                                           │
│ Recent Alarms (Last 7 Days):                                             │
│                                                                           │
│ DateTime            │Parameter   │Value  │Threshold│Severity│Status     │
│─────────────────────┼────────────┼───────┼─────────┼────────┼──────────│
│2026-06-28 14:22:00  │Voltage_L3  │228.2V │< 220V   │Warning │✓ Ack'd  │
│2026-06-27 09:15:00  │Frequency   │49.8Hz │< 49.5Hz │Info    │✓ Ack'd  │
│                                                                           │
│ [View All Alarms]                                                        │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

#### 3.5.3 Tabs & Their Content

| Tab | Content | Update Frequency | Purpose |
|-----|---------|------------------|---------|
| Live Values | Current readings in colored boxes | Every 30 sec (auto-refresh) | Operator quick check |
| Charts | Time-series charts (switchable) | On demand | Engineer analysis |
| History | Table of all readings | Paginated, static | Data audit trail |
| Alarms | Active + recent alarms for meter | Auto-refresh | Alert history |

---

### 3.6 ALARMS DASHBOARD

#### 3.6.1 Page Purpose
Alarm management for operators and supervisors. Show active alarms, history, and acknowledgement workflow.

#### 3.6.2 Page Layout

```
┌─ ACTIVE ALARMS SECTION ───────────────────────────────────────────────────┐
│                                                                           │
│ Active Alarms: 2  [🔔 Enable Sound] [Auto-refresh: ON]                   │
│                                                                           │
│ ┌─ CRITICAL (1) ─────────────────────────────────────────────────────┐   │
│ │ 🔴 Fuel Tank 2 - Level 45.2% < 50%                   [🔊 Alert]   │   │
│ │    Created: 2026-06-29 14:00  |  Duration: 2h 42m                 │   │
│ │    Severity: CRITICAL  |  Status: UNACKNOWLEDGED                  │   │
│ │    Message: "Fuel tank level critically low, refill urgent"       │   │
│ │    [Acknowledge] [Snooze 1h] [View Device]                        │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│ ┌─ WARNING (1) ──────────────────────────────────────────────────────┐   │
│ │ 🟡 Meter-Floor3 - PF 0.92 < 0.93                                  │   │
│ │    Created: 2026-06-29 15:30  |  Duration: 1h 12m                 │   │
│ │    Severity: WARNING  |  Status: UNACKNOWLEDGED                   │   │
│ │    Message: "Power factor below optimal, check load balance"      │   │
│ │    [Acknowledge] [Snooze 30m] [View Device]                       │   │
│ └────────────────────────────────────────────────────────────────────┘   │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ ACKNOWLEDGED ALARMS (Last 24 Hours) ──────────────────────────────────────┐
│                                                                           │
│ Showing: 5 alarms  [Show older than 24h]                                 │
│                                                                           │
│ DateTime            │Severity│Device        │Parameter│Value│Ack By│Time│
│─────────────────────┼────────┼──────────────┼─────────┼─────┼──────┼────│
│2026-06-29 13:45:00  │Warning │Meter-Floor1  │Voltage_L2│228V │Op1   │13:46
│2026-06-29 12:00:00  │Info    │Meter-Floor2  │Freq    │49.8Hz│Op2   │12:05
│2026-06-28 18:30:00  │Critical│Tank-Main    │Level   │15%   │Op1   │18:35
│2026-06-28 14:22:00  │Warning │Meter-Floor3  │Current_L1│22A │Op3   │14:25
│2026-06-27 09:15:00  │Info    │PLC-Main     │Status  │Warn  │Op2   │09:20
│                                                                           │
│ [Export Alarm Log]                                                       │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

#### 3.6.3 Alarm State Machine

```
CREATED (User unaware)
    ↓
ACTIVE (Displayed in "Active Alarms" list)
    ├─ [Acknowledge] → ACKNOWLEDGED (Moved to history)
    ├─ [Snooze 1h] → SNOOZED (Hidden for 1 hour, then re-appears)
    └─ [Auto-clear] → CLEARED (Auto-cleared when condition normalizes)
```

#### 3.6.4 Interaction Flows

1. **Acknowledge Alarm:**
   - Click [Acknowledge] → Dialog appears
   - Auto-fills "Acknowledged By: Current User"
   - Click [Confirm] → POST /api/alarms/{id}/acknowledge
   - Alarm disappears from active list, appears in history with timestamp
   - Toast notification: "✓ Alarm acknowledged"

2. **Snooze Alarm:**
   - Click [Snooze] → Dropdown shows options (30m, 1h, 4h, custom)
   - Select timeframe → Alarm hidden
   - When time elapsed, alarm re-appears in active list
   - Toast notification: "🔔 Snoozed for 1 hour"

3. **View Device:**
   - Click [View Device] button → Navigate to meter detail page
   - Highlight relevant chart (e.g., if PF alarm, show PF chart)

4. **Export Log:**
   - Click [Export Alarm Log] → Download PDF or Excel
   - Includes all alarms from date range
   - Columns: DateTime, Severity, Device, Parameter, Value, AckBy, AckTime

---

### 3.7 REPORTS DASHBOARD

#### 3.7.1 Page Purpose
Generate, customize, and export energy reports. For management and record-keeping.

#### 3.7.2 Page Layout

```
┌─ REPORT BUILDER ──────────────────────────────────────────────────────────┐
│                                                                           │
│ ┌─ Report Type ─────────────────────────────────────────────────────┐    │
│ │ ☐ Daily Report      ☐ Weekly Report     ☐ Monthly Report        │    │
│ │ ☐ Yearly Report     ☐ Custom Report     ☐ Comparison Report     │    │
│ └────────────────────────────────────────────────────────────────────┘    │
│                                                                           │
│ ┌─ Report Parameters ────────────────────────────────────────────────┐    │
│ │ Date Range:                                                       │    │
│ │  Start: [📅 2026-06-01]  End: [📅 2026-06-29]                    │    │
│ │                                                                   │    │
│ │ Locations:                                                        │    │
│ │  ☑ Building A  ☑ Building B  ☐ Building C  [Select All] [Clear] │    │
│ │                                                                   │    │
│ │ Metrics to Include:                                               │    │
│ │  ☑ Consumption (kWh)     ☑ Peak Demand (kW)     ☑ Cost           │    │
│ │  ☑ Power Factor          ☑ Frequency            ☑ CO₂ Emissions │    │
│ │  ☑ Harmonics             ☑ Phase Balance        [Select All]    │    │
│ │                                                                   │    │
│ │ Analysis Type:                                                    │    │
│ │  ☉ Summary Only          ○ Summary + Daily Detail  ○ Full Detail │    │
│ │                                                                   │    │
│ │ Include Charts:                                                   │    │
│ │  ☑ Consumption Trend  ☑ Location Breakdown  ☑ Peak Hours       │    │
│ │  ☑ Top Consumers      ☑ Comparison Chart (if enabled)           │    │
│ └────────────────────────────────────────────────────────────────────┘    │
│                                                                           │
│ [Preview] [Generate PDF] [Generate Excel] [Email Report]                │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ RECENT REPORTS ──────────────────────────────────────────────────────────┐
│                                                                           │
│ File Name                          │ Type  │ Date Generated │ Actions    │
│────────────────────────────────────┼───────┼────────────────┼────────────│
│Energy_Report_2026-06-01_to_06-29   │ PDF   │ 2026-06-29 14:00 │📥🗑️     │
│Energy_Report_Building_A_June       │ Excel │ 2026-06-28 16:30 │📥🗑️     │
│Comparison_Building_A_vs_B_June     │ PDF   │ 2026-06-27 10:15 │📥🗑️     │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

┌─ SCHEDULED REPORTS ───────────────────────────────────────────────────────┐
│                                                                           │
│ Schedule new report to email automatically:                              │
│                                                                           │
│ Report Name: [______________________________]                            │
│ Schedule: ☉ Daily  ○ Weekly  ○ Monthly                                  │
│ Time: [09:00]  Timezone: [UTC+05:30]                                     │
│ Recipients: [email1@company.com] [email2@company.com] [+ Add]            │
│ Report Type: [Custom Report ▼]                                           │
│ Status: [Enable]                                                         │
│                                                                           │
│ [Save Schedule]                                                          │
│                                                                           │
│ Active Schedules:                                                        │
│  ✓ Daily Energy Report        Every day @ 06:00 UTC+05:30  [Edit][Disable]
│  ✓ Weekly Management Summary   Every Monday @ 09:00 UTC+05:30 [Edit][Disable]
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

#### 3.7.3 Report PDF Layout

```
┌─────────────────────────────────────────────────────────┐
│           [COMPANY LOGO]                                │
│       ENERGY MONITORING REPORT                          │
│                                                         │
│ Report Period: June 1 - June 29, 2026                  │
│ Generated: June 29, 2026 at 14:30                      │
│ Generated By: operator@company.com                     │
└─────────────────────────────────────────────────────────┘

EXECUTIVE SUMMARY
─────────────────
Total Consumption:    35,200 kWh
Peak Demand:          702 kW (June 29, 18:30)
Average Demand:       512 kW
Estimated Cost:       ₹ 2,840,000
CO₂ Emissions:        4.1 Metric Tons
Average Power Factor: 0.95 (Excellent)

LOCATION BREAKDOWN
──────────────────
Production:  15,840 kWh (45%)  │ Peak: 340 kW
Warehouse:   8,800 kWh (25%)   │ Peak: 190 kW
Admin:       3,520 kWh (10%)   │ Peak: 80 kW
Utilities:   7,040 kWh (20%)   │ Peak: 92 kW

[Charts: Consumption Trend, Location Breakdown, Peak Hours Heatmap]

DETAILED DAILY DATA
───────────────────
[Table: Date | Production | Warehouse | Admin | Utilities | Total | Peak | Avg PF]

RECOMMENDATIONS
───────────────
1. Peak demand occurs 18:00-20:00. Consider load shifting.
2. Warehouse power factor is 0.92. Check for motor issues.
3. Overall system health: Good ✓

═══════════════════════════════════════════════════════════
Page 1 of 3  │  Company: ABC Manufacturing  │  © 2026 EMS
```

---

### 3.8 SETTINGS DASHBOARD (Admin Only)

#### 3.8.1 Page Purpose
System configuration, user management, and advanced settings.

#### 3.8.2 Tabs

| Tab | Content | Accessible By |
|-----|---------|---------------|
| Users | Manage operator accounts, permissions | Admin |
| Devices | Enable/disable meters, configure tags | Admin |
| Alerts | Set alarm thresholds, alert rules | Admin + Supervisor |
| System | Email config, data retention, backups | Admin |
| About | Version info, documentation links | Everyone |

---

## 4. API Endpoint Specifications

### 4.1 Dashboard API Endpoints

#### Executive Dashboard
```
GET /api/dashboard/executive
  Params:
    - plant: string (default: "All")
    - building: string (default: "All")
    - area: string (default: "All")
    - dateFrom: date (default: today)
    - dateTo: date (default: today)
    - compareWith: string (default: null) // "yesterday" | "lastMonth"

  Response:
  {
    "kpis": {
      "todayConsumption": { "value": 1254, "unit": "kWh", "trend": "+12%" },
      "currentDemand": { "value": 458, "unit": "kW", "trend": "+8kW" },
      "peakDemand": { "value": 702, "unit": "kW", "time": "18:30" },
      ...
    },
    "charts": {
      "consumptionTrend": [...],
      "locationBreakdown": [...],
      "topConsumers": [...]
    }
  }
```

#### Live Monitoring
```
GET /api/meters/live
  Params:
    - plant: string
    - building: string
    - status: string (default: "all") // "online" | "offline" | "warning"
    - includeSparklines: boolean (default: true)

  Response: Array of meters with live values
  [
    {
      "meterId": 1,
      "name": "Meter-Floor1",
      "status": "online",
      "voltage": { "L1": 230.5, "L2": 230.1, "L3": 229.8, "unit": "V" },
      "current": { "L1": 15.3, "L2": 15.1, "L3": 15.2, "unit": "A" },
      "power": { "kW": 3.5, "kVAR": 0.28, "kVA": 3.65 },
      "powerFactor": 0.96,
      "frequency": 50.0,
      "sparkline": [3.4, 3.45, 3.48, 3.42, ...], // 24-point array
      "lastUpdated": "2026-06-29T16:42:30Z"
    },
    ...
  ]
```

#### Energy Analysis
```
GET /api/energy/analysis
  Params:
    - plant: string
    - building: string
    - area: string
    - timeframe: string (default: "daily") // "daily" | "weekly" | "monthly" | "yearly"
    - dateFrom: date
    - dateTo: date
    - compareWith: string (null) // "previousDay" | "previousMonth"
    - metric: string (default: "kWh") // "kWh" | "kW" | "kVA" | "kVAR"

  Response:
  {
    "data": [
      { "date": "2026-06-01", "value": 850, "peak": 280, "avg": 215, "min": 145 },
      { "date": "2026-06-02", "value": 920, "peak": 310, "avg": 238, "min": 160 },
      ...
    ],
    "comparison": [ // if compareWith is set
      { "date": "2026-05-01", "value": 780, ... },
      ...
    ],
    "stats": {
      "peak": 1150,
      "avg": 850,
      "min": 620,
      "trend": "+8%"
    }
  }
```

#### Meter Details
```
GET /api/meters/{meterId}/details
  Params:
    - timeframe: string (default: "24h") // "24h" | "7d" | "30d"

  Response:
  {
    "info": {
      "id": 1,
      "name": "Meter-Floor1",
      "location": "Building A, Floor 1",
      "model": "Janitza",
      "status": "online"
    },
    "liveValues": {
      "voltage": { L1N: 230.5, L2N: 230.1, L3N: 229.8, ... },
      "current": { L1: 15.3, L2: 15.1, L3: 15.2 },
      ...
    },
    "charts": {
      "voltage": [ { time: "00:00", L1: 230, L2: 229, L3: 231 }, ... ],
      "current": [ ... ],
      ...
    },
    "history": [
      { "dateTime": "2026-06-29 16:42:00", "VL1N": 230.5, "IL1": 15.3, ... },
      ...
    ]
  }
```

#### Alarms
```
GET /api/alarms
  Params:
    - status: string (default: "active") // "active" | "acknowledged" | "all"
    - severity: string (null) // "critical" | "warning" | "info"
    - meterId: int (null)
    - limit: int (default: 50)

  Response:
  {
    "total": 2,
    "alarms": [
      {
        "id": 1,
        "meterId": 4,
        "deviceName": "FuelTank-Main",
        "parameter": "Level",
        "currentValue": 45.2,
        "threshold": 50,
        "severity": "critical",
        "message": "Fuel level critically low",
        "isActive": true,
        "createdAt": "2026-06-29T14:00:00Z",
        "ackBy": null,
        "ackTime": null
      },
      ...
    ]
  }

POST /api/alarms/{id}/acknowledge
  Body:
  {
    "acknowledgedBy": "operator@company.com",
    "note": "Checked, will refill today"
  }
```

#### Reports
```
GET /api/reports/generate
  Params:
    - type: string // "pdf" | "excel" | "csv"
    - reportTemplate: string // "daily" | "weekly" | "monthly" | "custom"
    - dateFrom: date
    - dateTo: date
    - locations: string[] // ["Building A", "Floor 1"]
    - metrics: string[] // ["consumption", "peakDemand", "cost"]

  Response: (file download with Content-Disposition header)
  Binary file: Energy_Report_2026-06-01_to_06-29.pdf
```

### 4.2 Common Query Parameters

All endpoints support:
```
- plant: string (hierarchical filter)
- building: string
- area: string
- meter: string
- dateFrom: ISO8601 date
- dateTo: ISO8601 date
- compareWith: string
- limit: int (pagination)
- offset: int (pagination)
```

### 4.3 Response Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad request (missing params, invalid format) |
| 401 | Unauthorized (not logged in) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not found (meter/device doesn't exist) |
| 500 | Server error |

---

## 5. Database Queries by Feature

### 5.1 Executive Dashboard KPIs

```sql
-- Today's Consumption
SELECT SUM(kWh) AS consumption 
FROM tblEnergyMetersData 
WHERE CAST(DateTime AS DATE) = CAST(GETDATE() AS DATE)
  AND MeterNo IN (SELECT DeviceID FROM tblMonitoringDevices WHERE Type = 'EnergyMeter')

-- Peak Demand (Max kW reached)
SELECT MAX(kWtotal) AS peakDemand, DateTime AS peakTime
FROM tblEnergyMetersData
WHERE CAST(DateTime AS DATE) = CAST(GETDATE() AS DATE)
GROUP BY CAST(DateTime / 10, INT) -- 30-min intervals

-- Current Load (Latest reading)
SELECT TOP 1 kWtotal FROM tblEnergyMetersData 
ORDER BY DateTime DESC

-- Online Meter Count
SELECT COUNT(*) AS onlineCount
FROM tblMonitoringDevices d
WHERE d.IsActive = 1 
  AND EXISTS (SELECT 1 FROM tblEnergyMeterLive l WHERE l.MeterNo = d.DeviceID)
```

### 5.2 Live Monitoring Table

```sql
SELECT 
  d.DeviceName,
  CASE 
    WHEN l.IsValid = 1 THEN 'online'
    WHEN l.IsValid = 0 THEN 'offline'
    ELSE 'unknown'
  END AS status,
  l.VoltL1N, l.VoltL2N, l.VoltL3N,
  l.CurrentL1, l.CurrentL2, l.CurrentL3,
  l.kWtotal,
  l.PFL1,
  l.MFreq,
  l.DateTime
FROM tblMonitoringDevices d
LEFT JOIN tblEnergyMeterLive l ON d.DeviceID = l.MeterNo
WHERE d.IsActive = 1
ORDER BY d.DeviceID
```

### 5.3 Energy Analysis - Daily Totals

```sql
SELECT 
  CAST(DateTime AS DATE) AS Date,
  SUM(kWh) AS dailyConsumption,
  MAX(kWtotal) AS peakDemand,
  AVG(kWtotal) AS avgDemand,
  AVG(PFL1) AS avgPowerFactor
FROM tblEnergyMetersData
WHERE DateTime BETWEEN @dateFrom AND @dateTo
  AND MeterNo = @meterId -- or aggregated if @meterId = 0
GROUP BY CAST(DateTime AS DATE)
ORDER BY Date DESC
```

### 5.4 Alarms - Active Only

```sql
SELECT *
FROM Alarms
WHERE IsActive = 1
  AND CreatedAt >= DATEADD(DAY, -7, GETDATE()) -- Last 7 days
ORDER BY Severity DESC, CreatedAt DESC
```

---

## 6. Interaction & Animation Specifications

### 6.1 Chart Transitions
```
Duration: 300ms
Easing: ease-in-out
On: Filter change, date range change, comparison toggle
Effect: Smooth line/area path animation
```

### 6.2 Modal Transitions
```
Open: 200ms fade-in, 100ms scale-up
Close: 150ms fade-out
Backdrop: Blur background (10px), opacity 50%
```

### 6.3 Loading States
```
On data fetch: Show spinner + skeleton loading
Duration: Until API responds
Spinner: Rotating icon, centered
```

### 6.4 Error States
```
Display: Red toast at bottom-right
Icon: ⚠️ or ❌
Message: User-friendly error text
Auto-dismiss: No (user clicks to close)
Retry button: Available if applicable
```

---

## 7. Accessibility Specifications

### 7.1 Color Contrast
- All text: 4.5:1 minimum (WCAG AAA)
- Large text (18pt+): 3:1 minimum

### 7.2 Keyboard Navigation
- Tab order: Left-to-right, top-to-bottom
- All interactive: Focusable with Tab key
- Dropdowns: Arrow keys to navigate, Enter to select

### 7.3 Screen Reader Support
- `aria-label="Export to PDF"` on buttons
- `role="status"` on live-updating sections
- Charts: Provide `<table>` alternative or text summary

### 7.4 Mobile Touch
- All buttons: 44px × 44px minimum
- Tap targets: 8px padding
- No hover-only functions

---

## 8. Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| Page Load | < 2s | Initial render |
| Chart Render | < 1s | ApexCharts initialization |
| API Response | < 500ms | Dashboard endpoint |
| Live Update | < 30s | Real-time refresh cycle |
| Mobile Load | < 3s | On 4G network |

---

## 9. Implementation Priority

### Phase 1 (MVP - Weeks 1-4)
✅ Executive Dashboard  
✅ Live Monitoring  
✅ Basic Filters (Plant, Date)  
✅ Login / Authentication  
✅ PDF Export  

### Phase 2 (Weeks 5-8)
✅ Energy Analysis with full timeframes  
✅ Meter Details page  
✅ Locations hierarchical view  
✅ Alarms dashboard  
✅ Excel export  

### Phase 3 (Weeks 9-12)
✅ Reports generator  
✅ Scheduled email reports  
✅ Settings / Admin console  
✅ Performance optimization  
✅ Mobile responsive refinement  

---

## 10. Summary

This blueprint specifies:
- **8 dashboard pages** with detailed layouts
- **30+ interactive features** (filters, charts, drill-downs)
- **20+ API endpoints** with query specifications
- **Database queries** for each major feature
- **Accessibility & performance** targets
- **Animation & UX** specifications

**Next Step:** Backend API development (ASP.NET Core), then Frontend (MVC + ApexCharts)

---

**Document Version:** 1.0  
**Last Updated:** June 29, 2026  
**Status:** Ready for Development

