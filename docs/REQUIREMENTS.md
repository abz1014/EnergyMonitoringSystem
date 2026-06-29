# Energy Monitoring System - Software Requirements Specification (SRS)

**Version:** 1.0  
**Date:** June 29, 2026  
**Status:** Draft for Review  
**Prepared for:** Senior Management / Project Lead

---

## 1. Executive Summary

The Energy Monitoring System (EMS) is a modern web-based dashboard application that reads historical energy meter data from an existing SQL Server database (`db_SCADA`) and presents it through intuitive, visually impressive dashboards. The system is NOT a replacement for the existing SCADA desktop application—it is a complementary analytics and reporting platform.

**Primary Goal:** Enable operators, supervisors, and executives to visualize, analyze, and report on energy consumption across multiple locations, buildings, and time periods.

---

## 2. User Personas & Use Cases

### Persona 1: Production Supervisor
- **Goal:** Monitor real-time energy consumption by floor/area
- **Actions:** View dashboards, filter by location, check alerts
- **Frequency:** Daily, multiple times
- **Needs:** Quick overview, alerts for anomalies

### Persona 2: Energy Manager
- **Goal:** Analyze trends, identify inefficiencies, generate reports
- **Actions:** Compare locations, export monthly reports, drill-down into meters
- **Frequency:** Weekly/Monthly
- **Needs:** Detailed analytics, custom date ranges, export capabilities

### Persona 3: Plant Manager / Executive
- **Goal:** High-level KPI visibility, cost tracking
- **Actions:** View dashboard KPIs, cost estimates, peak demand
- **Frequency:** Daily, briefing level
- **Needs:** Executive summary, no technical details

### Persona 4: System Administrator
- **Goal:** Configure users, permissions, device settings
- **Actions:** Manage operator accounts, enable/disable meters, set alert thresholds
- **Frequency:** As needed
- **Needs:** Admin console, permission management

### Persona 5: Maintenance Technician
- **Goal:** Troubleshoot meter issues, verify data quality
- **Actions:** View individual meter readings, check for data anomalies, device status
- **Frequency:** On-demand, during troubleshooting
- **Needs:** Detailed meter information, historical trends

---

## 3. Functional Requirements

### 3.1 Dashboard Views

#### 3.1.1 Executive Dashboard
**Purpose:** High-level overview for management  
**Content:**
- KPI Cards (Today's Consumption, Current Demand, Peak Demand, Monthly Total, Cost Estimate, Online Meters, CO₂ Emissions)
- Daily consumption chart with trend indicator
- Consumption by location (bar chart)
- Top 10 consumers (ranking)
- Meter health status (online/offline/warning count)
- Energy distribution (donut chart)

**Update Frequency:** Every 5 minutes (summary data)

#### 3.1.2 Live Monitoring Dashboard
**Purpose:** Real-time meter status (from `tblEnergyMeterLive`)  
**Content:**
- Live meter table (Meter Name, Status, Voltage, Current, Power, Frequency, Power Factor)
- Mini trend sparklines for each meter
- Status colors (Green=Normal, Yellow=Warning, Red=Offline)
- Last updated timestamp

**Update Frequency:** Every 30 seconds

#### 3.1.3 Energy Analysis Dashboard
**Purpose:** Historical trend analysis (from `tblEnergyMetersData`)  
**Content:**
- Selectable timeframe (Daily/Weekly/Monthly/Yearly/Custom)
- Consumption line chart with comparison capability
- Daily breakdown by location (stacked area chart)
- Peak hours heatmap (which hours consume most energy)
- Monthly trend with min/max/avg indicators

**Update Frequency:** Static (user-driven)

#### 3.1.4 Location Dashboard
**Purpose:** Drill-down by location hierarchy  
**Structure:**
```
Plant
├─ Building A
│  ├─ Floor 1
│  │  ├─ Area 1 (Meters)
│  │  └─ Area 2 (Meters)
│  └─ Floor 2
└─ Building B
   └─ Warehouse
```
**Content:** Clicking any level shows KPIs, charts, and meters for that location

**Update Frequency:** Dynamic on selection

#### 3.1.5 Meter Details Dashboard
**Purpose:** Individual meter deep-dive  
**Content:**
- Meter info (Name, Location, Model, Serial, Status)
- Live values (Voltage L1/L2/L3, Current L1/L2/L3, Power, PF, Frequency, THD)
- Historical chart selector (Voltage/Current/Power/Frequency/Power Factor)
- Weekly/Monthly consumption trend
- Energy counters (kWh, kVAh, kVARh)
- Harmonics analysis (if available)

**Update Frequency:** Dynamic on load

#### 3.1.6 Alarms Dashboard
**Purpose:** Active and historical alerts  
**Content:**
- Active alarms list (DeviceName, Parameter, Value, Threshold, Severity, Time)
- Alarm acknowledgement (AckBy, AckTime)
- Historical alarm log with search/filter
- Severity indicators (Critical=Red, Warning=Yellow, Info=Blue)

**Update Frequency:** Real-time for active alarms

#### 3.1.7 Reports Dashboard
**Purpose:** Generate, schedule, and export reports  
**Content:**
- Report templates (Daily, Weekly, Monthly, Yearly, Custom)
- Export formats (PDF, Excel, CSV)
- Email scheduling (daily/weekly/monthly)
- Comparison reports (Building A vs Building B)
- Custom report builder (select locations, timeframe, metrics)

**Update Frequency:** On-demand

---

### 3.2 Filter & Navigation Features

#### 3.2.1 Global Filter Bar
Appears on every page:
- **Location** dropdown (hierarchical: Plant → Building → Floor → Area)
- **Meter** dropdown (All or specific)
- **Date Range** (Today / Yesterday / This Week / Last Week / This Month / Last Month / This Year / Custom with Start/End)
- **Comparison Toggle** (enable side-by-side comparison with previous period)
- **Refresh Button** (manual data refresh)
- **Export Button** (PDF/Excel quick export)

#### 3.2.2 Main Navigation Menu
- Dashboard (executive overview)
- Live Monitoring
- Energy Analysis
- Locations (hierarchical view)
- Meters (individual meter list)
- Alarms
- Reports
- Settings (admin only)

---

### 3.3 Data Visualization Requirements

#### 3.3.1 Chart Types Required
✅ KPI Cards (number + trend indicator)  
✅ Line Charts (consumption over time)  
✅ Area Charts (stacked for location comparison)  
✅ Bar Charts (horizontal for rankings)  
✅ Donut/Pie Charts (energy distribution)  
✅ Gauge Charts (power factor, current load)  
✅ Heatmap (peak hours, daily patterns)  
✅ Sparklines (mini trends)  
✅ Table (meter list with live values)  

#### 3.3.2 Data Metrics to Display
**Energy Metrics** (all from `tblEnergyMetersData`):
- Voltage (L1, L2, L3 line-to-neutral; L1-L2, L2-L3, L1-L3 line-to-line)
- Current (L1, L2, L3)
- Power (L1, L2, L3; total active, reactive, apparent)
- Power Factor (L1, L2, L3; overall)
- Frequency (50 Hz nominal)
- Energy Counters (kWh, kVAh, kVARh)
- Harmonics (voltage THD L1/L2/L3, current THD L1/L2/L3)

**Fuel Metrics** (from `tbFlowmetersData`):
- Fuel Level (%)
- Volume (Liters)
- Temperature (°C)

**Calculated Metrics:**
- Daily consumption (kWh)
- Peak demand (kW)
- Average power factor
- Cost (estimated based on consumption)
- CO₂ emissions (estimated)

---

### 3.4 Aggregation & Calculation Rules

| Timeframe | Aggregation | Example |
|-----------|-------------|---------|
| **Daily** | Sum of all readings in 24h | Today's consumption = Σ(kWh) from 00:00-23:59 |
| **Weekly** | Sum of daily totals (7 days) | This week = Σ(daily consumption) |
| **Monthly** | Sum of daily totals (30/31 days) | This month = Σ(daily consumption) |
| **Yearly** | Sum of monthly totals (12 months) | This year = Σ(monthly consumption) |
| **Peak Demand** | MAX(kW) in period | Peak = MAX(kWtotal) in timeframe |
| **Average Power Factor** | AVG(PFL1, PFL2, PFL3) | Mean of all PF readings |
| **Cost Estimate** | Consumption × Unit Rate | Configurable rate (admin setting) |

---

### 3.5 Filtering & Comparison

#### 3.5.1 Location Filtering
- Filter by Plant, Building, Floor, Area, or specific Meter
- Dashboard recalculates all charts based on selected location
- Show total consumption for selected location only
- Drill-down from top level (Plant) to individual meter

#### 3.5.2 Date Range Filtering
- Pre-defined ranges (Today, Week, Month, Year)
- Custom date range picker (Start/End dates)
- Compare with previous period (show trend %)
- Support for fiscal year or calendar year

#### 3.5.3 Comparison Mode
- Side-by-side comparison: Location A vs Location B
- Side-by-side comparison: This Month vs Last Month
- Show % difference and trend indicators
- Display delta in charts (e.g., Building A: 1,200 kWh, Building B: 950 kWh, Diff: +26%)

---

### 3.6 Alarm Management

#### 3.6.1 Alarm Display
- Show active alarms (IsActive = 1)
- Display: DeviceName, Parameter (TagName), Current Value, Threshold, Severity, Time
- Severity levels (from Alarms table):
  - **Critical** (3): Red background
  - **Warning** (2): Yellow background
  - **Info** (1): Blue background

#### 3.6.2 Alarm Acknowledgement
- Operator can acknowledge alarm (click "Acknowledge")
- System updates: AckBy (username), AckTime (current time), IsActive → 0
- Alarm disappears from active list, appears in historical log
- Show list of all acknowledged alarms with acknowledgement timestamp

#### 3.6.3 Alarm History
- View all alarms (active + acknowledged) with filters
- Sort by: Time, Severity, Device, Status
- Export alarm log as PDF/Excel

---

### 3.7 Reporting

#### 3.7.1 Report Types
1. **Daily Report:** Consumption, peak demand, by location
2. **Weekly Report:** Weekly total, daily breakdown, location comparison
3. **Monthly Report:** Monthly total, daily trend, peak analysis, cost estimate
4. **Yearly Report:** Yearly total, monthly trend, location breakdown
5. **Custom Report:** User selects locations, timeframe, metrics

#### 3.7.2 Export Formats
- **PDF:** Formatted with logos, charts, tables
- **Excel:** Multiple sheets (summary, detailed, charts)
- **CSV:** Raw data for external analysis

#### 3.7.3 Scheduled Reporting
- Email daily/weekly/monthly reports automatically
- Configurable recipients (admin setting)
- Scheduled time (e.g., 6 AM every Monday)

#### 3.7.4 Comparison Reports
- Compare two locations (Building A vs Building B)
- Compare two periods (This Month vs Last Month)
- Show metrics side-by-side with trend %

---

## 4. Non-Functional Requirements

### 4.1 Performance
- Dashboard loads in < 2 seconds
- Charts render in < 1 second
- Database queries complete in < 500ms
- Support 50+ concurrent users
- Live monitoring updates every 30 seconds without lag

### 4.2 Security
- User authentication (username/password)
- Role-based access control (Operator, Supervisor, Admin, Executive)
- Secure password reset mechanism
- Session timeout (30 min inactivity)
- Audit trail for critical actions (report generation, alarm acknowledgement)

### 4.3 Usability
- Responsive design (works on desktop, tablet, mobile)
- Dark theme for industrial/SCADA aesthetic
- Intuitive navigation (no more than 2 clicks to reach any dashboard)
- Accessibility (WCAG 2.1 Level AA)
- Tooltips & help text for all filters

### 4.4 Reliability
- 99.5% uptime SLA
- Automatic database connection retry (3 attempts)
- Graceful error handling (user-friendly error messages)
- Backup & recovery strategy

### 4.5 Maintainability
- Clean Architecture (layered: API, Service, Repository, Data)
- Dependency Injection for loose coupling
- Unit tests for business logic (target: 80% coverage)
- Integration tests for API endpoints
- Comprehensive API documentation (Swagger)

---

## 5. Technology Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core 8 Web API |
| **Frontend** | ASP.NET Core MVC 8 + Bootstrap 5 (or React if approved) |
| **Database** | SQL Server (db_SCADA) - read-only |
| **Charts** | ApexCharts (industry-standard for dashboards) |
| **Authentication** | ASP.NET Identity (or Azure AD if needed) |
| **Logging** | Serilog |
| **ORM** | Entity Framework Core 8 |
| **API Documentation** | Swagger/OpenAPI |
| **Version Control** | Git |

---

## 6. Database Schema

### 6.1 Tables Used

**Read-Only Tables** (no modifications):
- `tblEnergyMetersData` (36 columns: meter readings, voltage, current, power, harmonics)
- `tbFlowmetersData` (fuel tank data in EAV format)
- `tblMonitoringDevices` (device registry)
- `Alarms` (alerts and faults)
- `tblDevicesTags` (register map - reference)
- `tblDevices` (device master - reference)

### 6.2 Schema Assumptions
- Location hierarchy is inferred from `MeterLocation` and `Location` strings
- If location hierarchy becomes complex (Plant > Building > Floor > Area), we will create dedicated hierarchy tables later (Phase 2)
- Alarm severity: 1=Info, 2=Warning, 3=Critical

---

## 7. Constraints & Limitations (Current)

### 7.1 Location Hierarchy
- Database stores location as flat string (e.g., "Floor 1", "Ground Floor")
- Dashboard will support hierarchical filtering, but hierarchy will be:
  - **Option A:** Manually defined in configuration (JSON file or database)
  - **Option B:** Inferred from consistent naming convention (Floor 1, Floor 2, etc.)
- **Decision deferred:** Will be determined during implementation based on data analysis

### 7.2 Data Retention
- Historical data in `tblEnergyMetersData` is limited by existing database
- No automatic data archival/deletion policy (assume data is managed by SCADA system)
- Dashboard is read-only (does not write to production database)

### 7.3 Real-Time vs Near-Real-Time
- Dashboard reads from `tblEnergyMetersData` (appended every 10 min)
- "Live Monitoring" tab reads from `tblEnergyMeterLive` (updated every 30 sec)
- No sub-30-second updates

---

## 8. Out of Scope (Phase 2)

❌ Modifying SCADA polling logic  
❌ Creating new devices or tags  
❌ Changing Modbus register mapping  
❌ Predictive analytics or ML-based anomaly detection  
❌ Mobile app (web is responsive, but native app is future)  
❌ IoT device integration beyond existing 6 devices  
❌ Integration with external energy management systems  
❌ Custom alert rule engine (alarms are predefined in DB)  

---

## 9. Success Criteria

✅ **Functional:**
- All 7 dashboard types working and queryable
- Filters (location, date, comparison) functional on all dashboards
- Reports (PDF, Excel) generated successfully
- Alarms displayed with acknowledgement workflow
- Multi-user authentication & role-based access working

✅ **Performance:**
- Dashboard loads in < 2 seconds
- 50+ concurrent users supported
- Live monitoring updates every 30 seconds without delay

✅ **Quality:**
- Zero critical bugs in production
- 80%+ unit test coverage on business logic
- All API endpoints documented in Swagger
- Clean code (no TODOs, proper error handling)

✅ **User Acceptance:**
- Senior management approves dashboard designs
- Operators can use system without training
- Supervisors can generate reports independently

---

## 10. Assumptions

1. ✅ Database `db_SCADA` exists and is accessible
2. ✅ Existing SCADA app continues to populate `tblEnergyMetersData` and `tblEnergyMeterLive`
3. ✅ 6 devices (3 meters, 2 fuel tanks, 1 PLC) are configured and active
4. ✅ Data is clean (no NULL values in critical columns like DateTime, MeterNo, kWtotal)
5. ✅ Users have Windows or Azure AD credentials for authentication
6. ✅ SMTP server is available for scheduled email reports
7. ✅ Hosting environment (IIS or Azure App Service) will be provided
8. ✅ SSL/TLS certificate for HTTPS will be provided
9. ✅ Database will be backed up externally (not dashboard's responsibility)
10. ✅ Location hierarchy will be clarified before UI implementation

---

## 11. Acceptance Criteria

- [ ] All dashboard views render without errors
- [ ] Filters work across all dashboards
- [ ] Charts display correct data (verified against SQL queries)
- [ ] Reports export in PDF and Excel formats
- [ ] Alarms display and acknowledge without data loss
- [ ] Application handles missing data gracefully
- [ ] Performance metrics achieved (< 2s load time)
- [ ] Security audit passed (auth, authorization, SQL injection prevention)
- [ ] User acceptance testing approved by senior management

---

## 12. Sign-Off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Project Lead | | | |
| Senior Management | | | |
| Technical Lead | | | |

---

**Document Version:** 1.0  
**Last Updated:** June 29, 2026  
**Next Review:** After senior approval
