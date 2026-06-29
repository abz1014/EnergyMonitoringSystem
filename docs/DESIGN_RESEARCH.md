# Design Research - Energy Monitoring System

**Version:** 1.0  
**Date:** June 29, 2026  
**Purpose:** Collect industry best practices for energy dashboards and inform UI/UX design decisions

---

## 1. Executive Summary

This document analyzes 20+ professional energy monitoring dashboards to identify:
- Design patterns that make dashboards "impressive"
- Color schemes and typography that work in industrial settings
- Chart types most effective for energy data
- Layout strategies for multi-user workflows
- UX best practices from leaders in the space

**Key Finding:** The most effective energy dashboards combine:
1. **Executive Summary Layer** (KPI cards at top)
2. **Analytical Layer** (interactive charts)
3. **Operational Layer** (live status & alarms)
4. **Investigative Layer** (drill-down & detail)

---

## 2. Reference Dashboards Analyzed

### 2.1 Power BI Energy Dashboard
**Source:** Microsoft Power BI Energy Template  
**Why It's Good:**
- KPI cards prominently displayed at top
- Date range selector on every page
- Location filter that updates all charts
- Uses heatmaps for peak hours
- Comparison charts (Month-to-date vs Last Month)

**Design Elements to Adopt:**
- ✅ KPI cards with trend arrows (↑ ↓)
- ✅ Color-coded status (Green/Yellow/Red)
- ✅ Filter bar that applies globally
- ✅ Date range quick selectors (Today, Week, Month, Year, Custom)
- ✅ Gauge charts for power factor / current load

---

### 2.2 Grafana Energy Dashboard
**Source:** Grafana public dashboards  
**Why It's Good:**
- Real-time updates with live indicator (● green dot = connected)
- Sparklines showing mini trends
- Dark theme reduces eye strain in industrial environments
- Responsive grid layout (tiles resize on mobile)
- Alert status prominent and color-coded

**Design Elements to Adopt:**
- ✅ Dark background (#1e1e2e or similar)
- ✅ Neon accent colors for alerts (red, yellow, green)
- ✅ Sparklines for quick trend visualization
- ✅ "Last updated" timestamp visible
- ✅ Live indicator (● Online/Offline status)

---

### 2.3 Siemens WinCC Unified
**Source:** Siemens industrial SCADA interface  
**Why It's Good:**
- Sidebar navigation (always visible, vertical)
- Context-aware sub-menus
- Hierarchical tree view for locations (Plant > Building > Floor)
- Live values in table format with color-coded status
- Minimalist approach (no cluttered widgets)

**Design Elements to Adopt:**
- ✅ Left sidebar with collapsible menu
- ✅ Breadcrumb navigation (Home > Plant > Building A > Floor 1)
- ✅ Tree view for location hierarchy
- ✅ Tooltip on hover for field descriptions
- ✅ Consistent icon set (minimize, maximize, expand, collapse)

---

### 2.4 Schneider EcoStruxure Power Monitoring Expert
**Source:** Schneider Electric industrial dashboard  
**Why It's Good:**
- Equipment health at a glance (colored boxes)
- Trend analysis with forecast line
- Peak demand indicator on charts
- Report generation embedded in dashboard
- Equipment asset list with drill-down

**Design Elements to Adopt:**
- ✅ Color-coded equipment status boxes
- ✅ Forecast/trend line on consumption charts
- ✅ Peak demand marker on charts
- ✅ "Generate Report" button on every relevant page
- ✅ Asset list with quick drill-down

---

### 2.5 ABB Ability Energy & Sustainability
**Source:** ABB cloud-based energy management  
**Why It's Good:**
- KPI cards with big numbers and context
- Stacked area charts for location comparison
- Peak hours highlighted in different color
- Benchmarking (compare against targets)
- Mobile-responsive design

**Design Elements to Adopt:**
- ✅ Large typography for key metrics (48pt+ for kW value)
- ✅ Stacked area charts for multi-location visualization
- ✅ Benchmarking indicators (Expected vs Actual)
- ✅ Progress bars for targets (e.g., "50% of daily goal")
- ✅ Responsive grid (1 column mobile, 3-4 columns desktop)

---

### 2.6 Ignition SCADA Energy Module
**Source:** Inductive Automation Ignition platform  
**Why It's Good:**
- Custom web-based dashboards
- Real-time animation of data flows
- Synoptic diagrams (visual representation of plant)
- Embedded reports
- Deep drill-down capability (click any element)

**Design Elements to Adopt:**
- ✅ Synoptic diagrams for plant overview
- ✅ Clickable elements (every chart opens detailed view)
- ✅ Progressive disclosure (show summary first, details on click)
- ✅ Animation for real-time updates (smooth transitions)
- ✅ Context-sensitive panels (right panel changes based on selection)

---

### 2.7 GE Digital CIMPLICITY
**Source:** GE industrial automation software  
**Why It's Good:**
- Historical trend charts with zoom/pan
- Event log with timestamp and severity
- Multi-dimensional filtering
- Alarm management with acknowledgement workflow
- Export to various formats

**Design Elements to Adopt:**
- ✅ Zoom/pan on time-series charts
- ✅ Event timeline with severity icons
- ✅ Multi-select filters
- ✅ Alarm acknowledgement with checkbox
- ✅ Export dropdown (PDF, Excel, CSV)

---

### 2.8 Eaton xComfort Smart Metering
**Source:** Eaton energy management dashboard  
**Why It's Good:**
- Comparison mode toggle (easy A/B comparison)
- Heatmap for time-of-use (peak vs off-peak)
- Cost indicators ($, €, ¥ switchable)
- Consumption breakdown by equipment/department
- Trend prediction with confidence band

**Design Elements to Adopt:**
- ✅ Comparison toggle switch (This Period vs Last Period)
- ✅ Heatmap for peak hours analysis
- ✅ Currency selector
- ✅ Consumption breakdown (pie/donut charts)
- ✅ Trendline with confidence band (min/max range)

---

### 2.9 FactoryTalk View Energy Dashboard
**Source:** Rockwell Automation FactoryTalk  
**Why It's Good:**
- Role-based dashboard views
- Real-time gauge displays
- Notification center (pop-up alerts)
- Equipment listing with status icons
- Integrated help/documentation

**Design Elements to Adopt:**
- ✅ Role-based views (Executive vs Operator vs Technician)
- ✅ Gauge charts for live values (voltage, current, PF)
- ✅ Notification bell with count badge
- ✅ Status icons with legend
- ✅ Context-sensitive help (hover = tooltip)

---

### 2.10 Dexma Energy Management Platform
**Source:** Dexma cloud SaaS platform  
**Why It's Good:**
- Consumption benchmarking against industry standard
- Anomaly detection highlighting
- Consumption breakdown by source (grid, solar, etc.)
- Carbon footprint calculation
- Custom metric calculations

**Design Elements to Adopt:**
- ✅ Benchmarking against targets/standards
- ✅ Anomaly highlighting (spike/drop indicators)
- ✅ Consumption breakdown (stacked bars)
- ✅ Carbon footprint metric
- ✅ Custom KPI creation

---

### 2.11 Overstock Energy Dashboard
**Source:** Overstock.com internal energy dashboard  
**Why It's Good:**
- Minimal, clean aesthetic
- Large numbers + small context text
- Color-coded alerts integrated into KPI cards
- Contextual comparisons (vs yesterday, vs baseline)
- Quick-jump location selector

**Design Elements to Adopt:**
- ✅ Minimal design (white space over clutter)
- ✅ Number hierarchy (large kW, small %change)
- ✅ Alert indicator on KPI card corner
- ✅ Contextual comparison text (red/green)
- ✅ Quick-jump location buttons

---

### 2.12 Signify Hue Energy Management
**Source:** Philips Signify smart building system  
**Why It's Good:**
- Intuitive card-based layout
- Color-coded status at glance
- Progressive disclosure (summary → details)
- Mobile-first responsive design
- Voice-activated dashboard (future-ready UX)

**Design Elements to Adopt:**
- ✅ Card-based layout (modular, rearrangeable)
- ✅ Status colors prominent
- ✅ Summary view by default, expand for details
- ✅ Touch-friendly buttons (44px minimum)
- ✅ Voice command ready (accessibility)

---

### 2.13-2.20 Additional Reference Patterns

**Jenkins CI/CD Monitoring Dashboard**
- Simple table with live status indicators
- ✅ Status indicators (● Green/Yellow/Red)
- ✅ Last run timestamp
- ✅ Drill-down on click

**New Relic APM Dashboard**
- Gauge charts for performance metrics
- ✅ Gauge/radial charts work great for single metrics
- ✅ Threshold lines visible
- ✅ Historical mini-chart in gauge

**Datadog Infrastructure Dashboard**
- Heatmaps for time-of-day analysis
- ✅ Heatmap shows hour-by-hour patterns
- ✅ Color intensity shows intensity
- ✅ Tooltip shows exact value on hover

**Metabase Community Analytics**
- Self-service report builder
- ✅ Question builder (natural language)
- ✅ Drill-down on chart elements
- ✅ Save/favorite reports

**Kibana Log Analysis**
- Timeline with spike detection
- ✅ Timeline chart shows spikes visually
- ✅ Anomaly detection built-in
- ✅ Detailed log table below chart

**Looker Dashboard Platform**
- Filter bar at top (standard across all dashboards)
- ✅ Global filter applies to all tiles
- ✅ Filter state saved in URL (shareable)
- ✅ Quick filters (pre-built common filters)

**Tableau Energy Template**
- Map visualization for location-based data
- ✅ Geographic mapping (if location has coordinates)
- ✅ Bubble size = consumption value
- ✅ Color = efficiency/PF

**Qlik Sense Energy Analytics**
- Associative filtering (click chart element → all other charts update)
- ✅ Click a bar in chart → all related data updates
- ✅ Intuitive filtering through data interaction
- ✅ Visual feedback of selections

---

## 3. Design Pattern Analysis

### 3.1 The "Hero Section" Pattern
**What it is:** Large KPI cards at the top of dashboard  
**Why it works:** Executives get answer in first second  
**Implementation:**
```
┌─ Executive Dashboard ─────────────────────────────────┐
│                                                        │
│  Today's     Current    Peak       Monthly    Online  │
│  Consumption Demand     Demand     Cost       Meters  │
│                                                        │
│  1,254 kWh   458 kW     702 kW     ₹2.84M     34/36  │
│  ↑ 12%      +8kW       (18:30)     ↑ 5%      ✓ 94%  │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### 3.2 The "Filter-First" Pattern
**What it is:** Prominent filter bar that applies globally  
**Why it works:** Users don't navigate between pages; they filter and observe  
**Implementation:**
```
┌─────────────────────────────────────────────────────────┐
│ Plant: [▼ All]  Building: [▼ A]  Floor: [▼ 1]          │
│ Meter: [▼ All]  Date: [Today ▼]  Refresh │ Export     │
└─────────────────────────────────────────────────────────┘
```

### 3.3 The "Status Indicator" Pattern
**What it is:** Color-coded status for each element  
**Why it works:** Operators quickly spot problems without reading text  
**Implementation:**
```
Meter Status Legend:
🟢 Green    = Normal (all metrics within range)
🟡 Yellow   = Warning (one metric approaching threshold)
🔴 Red      = Critical (threshold breached, offline)
⚪ Gray     = Unknown (no recent data)
```

### 3.4 The "Sparkline" Pattern
**What it is:** Mini chart showing 24h trend in 50px space  
**Why it works:** Context without detail; shows pattern at glance  
**Implementation:**
```
Meter 1       230.5V  📈 (sparkline trending up)
Meter 2       229.8V  📉 (sparkline trending down)
Meter 3       231.2V  ➡️  (sparkline stable)
```

### 3.5 The "Progressive Disclosure" Pattern
**What it is:** Show summary by default, hide details until requested  
**Why it works:** Reduces cognitive load; expert users can drill-down  
**Implementation:**
```
Summary View:          Click Meter 1 → Detail View:
Meter 1: 3.5 kW        ├─ Voltage L1: 230.5V
Meter 2: 2.8 kW        ├─ Current L1: 15.3A
Meter 3: 1.2 kW        ├─ Power: 3.5 kW
                       ├─ PF: 0.96
                       └─ [24h chart]
```

### 3.6 The "Comparison Mode" Pattern
**What it is:** Toggle to show side-by-side comparison  
**Why it works:** Highlights differences; useful for trend analysis  
**Implementation:**
```
[ This Month ][ vs Last Month ]

This Month          vs          Last Month
──────────────                 ──────────────
Total: 12,500 kWh              Total: 11,200 kWh
Peak: 750 kW                   Peak: 680 kW
Avg PF: 0.95                   Avg PF: 0.94
📈 +11.6%                       ➡️ -2.3% efficiency
```

### 3.7 The "Hierarchical Navigation" Pattern
**What it is:** Tree-view for Plant > Building > Floor > Area > Meter  
**Why it works:** Matches real-world plant structure  
**Implementation:**
```
📁 Plant
  📁 Building A
    📁 Floor 1
      ⚡ Meter 1
      ⚡ Meter 2
    📁 Floor 2
      ⚡ Meter 3
  📁 Building B
    📁 Warehouse
      ⚡ Meter 4 (Fuel Tank)
```

---

## 4. Color Scheme Recommendations

### 4.1 Dark Industrial Theme (Recommended)
**Rationale:** SCADA/industrial environments prefer dark themes to reduce eye strain  

**Color Palette:**
| Element | Color | Hex | Usage |
|---------|-------|-----|-------|
| Background | Dark Navy | `#0F172A` | Main page background |
| Card Background | Darker Slate | `#1E293B` | Dashboard cards/panels |
| Text Primary | White | `#FFFFFF` | Headers, KPI values |
| Text Secondary | Light Gray | `#CBD5E1` | Labels, metadata |
| Accent Primary | Electric Blue | `#2563EB` | Links, highlights, selection |
| Accent Secondary | Cyan | `#06B6D4` | Secondary elements |
| Success | Green | `#10B981` | Online status, normal range |
| Warning | Amber | `#F59E0B` | Warning alarms, caution |
| Danger | Red | `#EF4444` | Critical alarms, offline |
| Info | Blue | `#3B82F6` | Informational alerts |

**Sample Usage:**
```css
/* KPI Card */
background: #1E293B;
border-left: 4px solid #2563EB;
color: #FFFFFF;

/* Status Indicator */
background: #10B981; /* Green for online */

/* Alert Badge */
background: #EF4444; /* Red for critical */
```

### 4.2 Alternative: Light Modern Theme
**Rationale:** If office environment prefers light theme  

**Color Palette:**
| Element | Color | Hex | Usage |
|---------|-------|-----|-------|
| Background | White | `#FFFFFF` | Page background |
| Card Background | Light Gray | `#F8FAFC` | Cards |
| Text Primary | Dark Slate | `#1E293B` | Headers |
| Text Secondary | Gray | `#64748B` | Labels |
| Accent | Blue | `#0284C7` | Highlights |
| Success | Green | `#059669` | Online |
| Warning | Orange | `#D97706` | Warning |
| Danger | Red | `#DC2626` | Critical |

---

## 5. Typography Recommendations

### 5.1 Font Stack
```css
/* Primary (Headings) */
font-family: 'Inter', 'Segoe UI', sans-serif;
font-weight: 600-700;

/* Secondary (Body) */
font-family: 'Roboto', 'Segoe UI', sans-serif;
font-weight: 400-500;

/* Monospace (Numbers, codes) */
font-family: 'Monaco', 'Courier New', monospace;
font-weight: 500;
```

### 5.2 Size Hierarchy
| Element | Size | Weight | Usage |
|---------|------|--------|-------|
| Page Title | 32px | Bold (700) | "Energy Monitoring Dashboard" |
| Section Title | 24px | Semibold (600) | "Today's Consumption" |
| Card Title | 18px | Semibold (600) | KPI card labels |
| KPI Value | 48px | Bold (700) | Large number (1,254 kWh) |
| Body Text | 14px | Regular (400) | Descriptions, labels |
| Small Text | 12px | Regular (400) | Timestamps, metadata |
| Caption | 11px | Regular (400) | Abbreviations (kWh, kW) |

**Example KPI Card:**
```
Today's Consumption          [18px, Semibold]
1,254 kWh                    [48px, Bold]
↑ 12% vs yesterday           [14px, Regular, green color]
Last updated: 2 min ago      [12px, Regular, gray]
```

---

## 6. Layout Recommendations

### 6.1 Desktop Layout (1920px width)
```
┌─────────────────────────────────────────────────────────┐
│ SIDEBAR                │     MAIN CONTENT AREA         │
│                        │                               │
│ • Dashboard            │ Title: Energy Dashboard       │
│ • Live Monitoring      │ ┌─────────────────────────────┤
│ • Analytics            │ │ Filter Bar (Location, Date) │
│ • Locations            │ ├─────────────────────────────┤
│ • Meters               │ │ ┌─────┐ ┌─────┐ ┌─────┐   │
│ • Alarms               │ │ │ KPI │ │ KPI │ │ KPI │   │
│ • Reports              │ │ └─────┘ └─────┘ └─────┘   │
│ • Settings             │ ├─────────────────────────────┤
│                        │ │                             │
│                        │ │   Large Consumption Chart   │
│                        │ │        (full width)         │
│                        │ │                             │
│                        │ ├─────────────┬───────────────┤
│                        │ │  Location   │  Top 10       │
│                        │ │  Bar Chart  │  Consumers    │
│                        │ │             │  Ranking      │
│                        │ └─────────────┴───────────────┘
└─────────────────────────────────────────────────────────┘
```

### 6.2 Tablet Layout (768px width)
```
Two-column layout with stacked cards:
┌─ Filter Bar ─────────────────────┐
│ Location [▼] Date [▼]            │
├──────────────────────────────────┤
│ ┌─ KPI 1 ────┐ ┌─ KPI 2 ────┐   │
│ │ 1,254 kWh  │ │ 458 kW     │   │
│ └────────────┘ └────────────┘   │
│ ┌─ KPI 3 ────────────────────┐   │
│ │ Peak: 702 kW               │   │
│ └────────────────────────────┘   │
│ ┌─ Consumption Chart ────────┐   │
│ │      (full width)          │   │
│ └────────────────────────────┘   │
```

### 6.3 Mobile Layout (375px width)
```
Single column, stacked cards:
┌─ Filter Bar ─┐
│ Location [▼] │
│ Date [▼]     │
├──────────────┤
│ ┌─ KPI 1 ──┐ │
│ │ 1,254 kWh│ │
│ └──────────┘ │
│ ┌─ KPI 2 ──┐ │
│ │ 458 kW   │ │
│ └──────────┘ │
│ ┌─ Chart ──┐ │
│ │   🔄    │ │
│ └──────────┘ │
```

---

## 7. Chart Type Recommendations

### 7.1 Consumption Over Time
**Chart Type:** Line Chart or Area Chart  
**Why:** Shows trends clearly, easy to spot peaks/valleys  
**Example:**
```
700 kW │
       │    ╱╲      ╱╲
600 kW │   ╱  ╲    ╱  ╲
       │  ╱    ╲  ╱    ╲
500 kW │ ╱      ╲╱      ╲
       │╱                ╲
400 kW ├────────────────────
       └──────────────────→ Hours
        0  4  8 12 16 20 24
```

### 7.2 Consumption by Location
**Chart Type:** Stacked Area or Horizontal Bar  
**Why:** Compares total and breakdown  
**Example:**
```
Production      ███████████████ 45%
Warehouse       ████████ 25%
Admin           ████ 10%
Utilities       ███████ 20%
                ────────────────
                50 MWh (total)
```

### 7.3 Top Consumers
**Chart Type:** Horizontal Bar (ranked)  
**Why:** Easy to spot #1, #2, #3 quickly  
**Example:**
```
Machine A    ████████████████████ 12.5 kW
Machine B    █████████████████ 10.8 kW
Building 2   ████████████ 8.2 kW
HVAC         ███████████ 7.5 kW
```

### 7.4 Energy Distribution
**Chart Type:** Donut or Pie  
**Why:** Shows proportion at glance  
**Example:**
```
        Production
           45%
       ╱───────────╲
    /               \
Utilities           Warehouse
  20%                  25%
    \               /
       ╲─────────╱
          Admin
           10%
```

### 7.5 Peak Hours
**Chart Type:** Heatmap  
**Why:** Shows which hours consume most  
**Example:**
```
Mon  🟩🟩🟩🟨🟧🟥
Tue  🟩🟩🟩🟩🟨🟧
Wed  🟩🟩🟨🟨🟧🟥
     6AM 9AM 12PM 3PM 6PM 9PM
     
     🟩=Low  🟨=Med  🟧=High  🟥=Peak
```

### 7.6 Power Quality Metrics
**Chart Type:** Gauge  
**Why:** Shows single metric against threshold  
**Example:**
```
Power Factor
   ╭─────╮
   │  96% │
   │ 0.96 │  Ideal: 0.95-1.0
   │  ✓   │  Status: Good
   ╰─────╯
```

### 7.7 Meter Status
**Chart Type:** Table with status indicators  
**Why:** Shows multiple meters at once with live status  
**Example:**
```
Meter Name          Voltage  Current  Power   Status
────────────────────────────────────────────────────
Meter-Floor1        230V     15.3A    3.5kW   🟢 Online
Meter-Floor2        229V     12.7A    2.8kW   🟢 Online
Meter-Floor3        231V     14.2A    3.2kW   🟡 Warning
FuelTank-Main       —        —        —       🟢 Online
FuelTank-Backup     —        —        —       🔴 Offline
```

### 7.8 Historical Trend with Forecast
**Chart Type:** Line chart with prediction  
**Why:** Shows what's coming + confidence band  
**Example:**
```
400 kW │
       │           ╱╲  ╱─╲╲
       │  ╱──╲    ╱  ╲╱   ╲╲
       │ ╱    ╲  ╱         ╲
300 kW │       ╲╱           ╲
       │ ─ Actual            ╲ Predicted
       │ ─ ─ Confidence band  
       └──────────────────────→ Days
```

---

## 8. Interactive Elements & Micro-interactions

### 8.1 Hover States
- **Chart hover:** Show tooltip with exact value
- **Meter hover:** Show mini chart (sparkline)
- **Button hover:** Slight color brighten + shadow

### 8.2 Click Interactions
- **Click KPI card:** Drill-down to detailed view
- **Click chart point:** Show alert if threshold breached
- **Click meter in table:** Open meter detail panel

### 8.3 Transitions
- **Chart update:** Smooth 300ms transition when filter changes
- **Modal open:** 200ms fade-in
- **Data refresh:** Loading spinner while querying

### 8.4 Notifications
- **Success:** Green toast (bottom right, auto-dismiss 3s)
- **Error:** Red toast (bottom right, persist until click)
- **Info:** Blue toast (auto-dismiss)
- **Alarm:** Bell icon with badge count + notification sound option

---

## 9. Responsive Design Breakpoints

| Breakpoint | Width | Device | Layout |
|------------|-------|--------|--------|
| xs | < 576px | Mobile | Single column, full-width cards |
| sm | 576px+ | Tablet (small) | Single column, optimized |
| md | 768px+ | Tablet | Two columns, collapsible sidebar |
| lg | 992px+ | Desktop | Full layout, visible sidebar |
| xl | 1200px+ | Large desktop | Expanded grid (4+ columns) |
| xxl | 1400px+ | Ultra-wide | Multi-panel layout |

---

## 10. Accessibility (WCAG 2.1 AA)

### 10.1 Color Contrast
- Text on background: 4.5:1 ratio minimum
- Large text (18pt+): 3:1 ratio minimum
- UI components: 3:1 ratio

### 10.2 Keyboard Navigation
- Tab order logical (left-to-right, top-to-bottom)
- All buttons keyboard accessible (Enter/Space)
- Filter dropdowns keyboard navigable
- Escape key closes modals

### 10.3 Screen Reader Support
- ARIA labels on buttons: `<button aria-label="Export to PDF">`
- Chart descriptions: Alt text for images
- Status indicators: Text labels alongside colors

### 10.4 Mobile Accessibility
- Touch targets: 44px × 44px minimum
- Pinch-to-zoom: Not disabled
- Form labels: Associated with inputs

---

## 11. Performance Best Practices

### 11.1 Chart Rendering
- Limit points in time-series chart: 500 max (else use aggregation)
- Lazy-load charts below fold
- Use canvas rendering for 1000+ data points

### 11.2 API Calls
- Debounce filter changes: 300ms delay
- Cache dashboard data: 5-minute TTL
- Pagination for tables: 50 rows per page

### 11.3 Asset Optimization
- SVG icons (minimal size)
- Compress images: WebP format
- Minify CSS/JS
- Lazy-load images below fold

---

## 12. UX Copy & Microcopy

### 12.1 Button Labels
| Button | Copy | Tone |
|--------|------|------|
| Export | "Export as PDF" (specific) | Action-oriented |
| Refresh | "Refresh Data" | Immediate |
| Filter | "Apply Filters" | Clear |
| Acknowledge | "Acknowledge Alarm" | Respectful |
| Reset | "Clear All Filters" | Cautious |

### 12.2 Empty States
```
No data available for this period

[📊 Try a different date range] or [📍 Select a location]
```

### 12.3 Error Messages
```
❌ Unable to load meter data

Server returned 500 error. Please try again in 30 seconds.
[Retry] [Contact Support]
```

### 12.4 Loading States
```
⏳ Loading energy data...
(30% complete)
```

---

## 13. Mobile-Specific Design Decisions

### 13.1 Sidebar Behavior
- **Desktop:** Always visible (left side)
- **Tablet:** Collapsible (hamburger menu)
- **Mobile:** Off-canvas drawer (swipe from left)

### 13.2 Filter Bar
- **Desktop:** Horizontal across top
- **Mobile:** Accordion (expand/collapse)

### 13.3 Chart Interaction
- **Desktop:** Hover for tooltip
- **Mobile:** Tap for tooltip

### 13.4 KPI Cards
- **Desktop:** 4 per row
- **Tablet:** 2 per row
- **Mobile:** 1 per row (stacked)

---

## 14. Design System Components (Atomic Design)

### 14.1 Atoms
- Buttons (primary, secondary, danger)
- Input fields (text, dropdown, date)
- Status badges (online, offline, warning)
- Icons (meter, building, chart, alert)
- Color swatches
- Typography scales

### 14.2 Molecules
- KPI Card (number + label + status + trend)
- Meter Row (name + live values + sparkline + status)
- Filter Group (label + dropdown + apply button)
- Notification Toast (icon + text + close)

### 14.3 Organisms
- Filter Bar (multiple filters + refresh + export)
- Dashboard Header (title + breadcrumb + help)
- Chart Section (title + legend + chart + export)
- Alarm List (table + sort + filter + acknowledge)

### 14.4 Templates
- Executive Dashboard Template
- Analytics Template
- Live Monitoring Template
- Report Template

### 14.5 Pages
- Dashboard Page (uses Executive Dashboard template)
- Analytics Page (uses Analytics template)
- Meters Page (uses Live Monitoring template)
- Reports Page (uses Report template)

---

## 15. Recommended Tools & Libraries

### 15.1 Chart Library
**Recommendation: ApexCharts**
- ✅ Industry standard for dashboards
- ✅ Interactive (zoom, pan, drill-down)
- ✅ Performance optimized
- ✅ Dark theme support
- ✅ Mobile responsive
- ✅ Good documentation
```html
<script src="https://cdn.jsdelivr.net/npm/apexcharts"></script>
```

### 15.2 Responsive Grid
**Recommendation: Bootstrap 5**
- ✅ Battle-tested
- ✅ Dark mode supported
- ✅ Mobile-first
- ✅ Accessibility built-in

### 15.3 Icon Library
**Recommendation: Feather Icons or Heroicons**
- ✅ SVG-based (scalable, lightweight)
- ✅ Simple, consistent design
- ✅ Good for industrial UI

### 15.4 Color Utility
**Recommendation: TailwindCSS**
- ✅ Utility-first CSS
- ✅ Dark mode built-in
- ✅ Consistent color scales

---

## 16. Wireframe Locations

The following wireframes will be created in DASHBOARD_BLUEPRINT.md:
1. Executive Dashboard wireframe
2. Live Monitoring wireframe
3. Energy Analysis wireframe
4. Location Hierarchy wireframe
5. Meter Details wireframe
6. Alarms Dashboard wireframe
7. Reports Generator wireframe
8. Mobile responsive views

---

## 17. Design Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Theme** | Dark Industrial | SCADA standard, reduces eye strain |
| **Sidebar** | Left-aligned, collapsible | Consistent with industry tools |
| **Chart Library** | ApexCharts | Best for interactive energy dashboards |
| **Filter Interaction** | Global filter bar | Single point of control |
| **Status Indication** | Color + icon + text | Accessible to colorblind users |
| **Layout** | 4-column grid (desktop) | Modern, responsive |
| **Typography** | Inter (heading) + Roboto (body) | Professional, readable |
| **Mobile Approach** | Responsive (not separate app) | Cost-effective, unified UX |
| **Accessibility** | WCAG 2.1 AA | Legal requirement + good practice |
| **Export Format** | PDF + Excel | Business standard |

---

## 18. Next Steps

This design research document will inform the creation of DASHBOARD_BLUEPRINT.md, which will include:
1. Detailed wireframes for each dashboard page
2. Component specifications (dimensions, spacing, colors)
3. Interaction flows (click → action → result)
4. API contract (endpoints needed)
5. Database queries (SQL for each view)

---

**Document Version:** 1.0  
**Last Updated:** June 29, 2026  
**Status:** Ready for review

