# Team Setup Guide - Running Phase 1 Week 2 Locally

**Goal:** Get the Energy Monitoring System dashboard running on your machine in 5 minutes  
**Prerequisites:** Windows 10+, .NET 8 SDK, Visual Studio 2022, SQL Server  
**Time Required:** ~5 minutes

---

## Quick Start (5 Minutes)

### Step 1: Clone the Repository
```bash
git clone https://github.com/abz1014/EnergyMonitoringSystem.git
cd EnergyMonitoringSystem
git checkout abdullahs-branch
```

### Step 2: Verify SQL Server Connection
```bash
sqlcmd -S (local)\SQLEXPRESS -Q "SELECT DB_NAME()"
```
✅ Should return: `master`

If this fails:
- Start SQL Server: Services → SQL Server (SQLEXPRESS)
- Run "SQL Server Configuration Manager"
- Verify instance is running

### Step 3: Build Solution
```bash
cd src
dotnet build
```
✅ Should see: `Build succeeded. 0 Error(s)`

### Step 4: Run the Web Application
```bash
cd EMS.Web
dotnet run
```
✅ Output will show:
```
info: Microsoft.Hosting.Lifetime
      Now listening on: https://localhost:5051
      Now listening on: http://localhost:5050
```

### Step 5: Open in Browser
- **URL:** http://localhost:5050
- **Click:** "📊 Dashboard" in navigation bar
- **See:** Executive Dashboard with KPI cards and charts

---

## Detailed Setup (With Screenshots)

### 1. Repository Setup

#### Via Git Command Line:
```bash
git clone https://github.com/abz1014/EnergyMonitoringSystem.git
cd EnergyMonitoringSystem
git checkout abdullahs-branch
```

#### Via Visual Studio:
1. Open Visual Studio 2022
2. File → Clone Repository
3. URL: `https://github.com/abz1014/EnergyMonitoringSystem.git`
4. Click "Clone"
5. Git → Branches → Remote → `abdullahs-branch`
6. Right-click → Checkout

### 2. Database Verification

**Check if SQL Server is running:**
```powershell
Get-Service | Where-Object {$_.Name -like "*SQL*"}
```

Should show:
```
Status   Name                                             DisplayName
------   ----                                             -----------
Running  MSSQLSERVER                                      SQL Server (SQLEXPRESS)
```

**Test connection:**
```bash
sqlcmd -S (local)\SQLEXPRESS -U sa -P YourPassword
1> SELECT @@VERSION
2> GO
```

### 3. .NET Version Check

```bash
dotnet --version
```
✅ Should show: `8.0.421` or later

If not installed, download from: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

### 4. Open in Visual Studio

1. File → Open → Folder
2. Navigate to: `C:\path\to\EnergyMonitoringSystem\src`
3. Visual Studio will detect the solution
4. Solution Explorer shows 4 projects:
   - EMS.Core
   - EMS.Infrastructure
   - EMS.API
   - EMS.Web

### 5. Build & Run

**Option A: Visual Studio**
1. Right-click `EMS.Web` → Set as Startup Project
2. Press `F5` to run
3. Browser opens to http://localhost:5050

**Option B: Command Line**
```bash
cd src/EMS.Web
dotnet run
# Then open http://localhost:5050
```

**Option C: Both API and Web (Advanced)**
```bash
# Terminal 1:
cd src/EMS.API
dotnet run
# Outputs: Now listening on https://localhost:5001

# Terminal 2:
cd src/EMS.Web
dotnet run
# Opens http://localhost:5050
```

---

## What You'll See

### Dashboard Home Page
```
URL: http://localhost:5050/Dashboard/Index

Layout:
┌─ Header: "Energy Monitoring System"
├─ Filter Bar (Plant, Building, Area, Refresh button)
├─ 8 KPI Cards (3 columns on desktop, stacked on mobile)
│  ├─ Today's Consumption: 1,254 kWh ↑12%
│  ├─ Current Load: 458 kW ±0 kW
│  ├─ Peak Demand: 702 kW @ 18:30
│  ├─ Monthly Total: 35,200 kWh ↑8%
│  ├─ Online Meters: 34 / 36 (94%)
│  ├─ Estimated Cost: ₹2.84M ↑5%
│  ├─ Avg Power Factor: 0.96 ✓
│  └─ CO₂ Emissions: 4.1 Metric Tons
├─ Main Chart: "Daily Energy Consumption"
│  └─ 24-hour area chart with gradient
├─ Location Breakdown: "Consumption by Location"
│  └─ Donut chart (Production 45%, Warehouse 25%, etc.)
├─ Top 10 Consumers: "Top 10 Consumers"
│  └─ Horizontal bar chart
└─ Footer: "Last updated: 2 minutes ago | Data source: db_SCADA"
```

### Interactive Elements
- **Hover over charts** → See data point values
- **Click chart** → Zoom/pan (ApexCharts features)
- **Refresh button** → Re-fetch data
- **Filter dropdowns** → Select different plants/buildings (Week 3 feature)
- **Navigation** → Click "Dashboard" in nav bar

---

## API Testing (For Developers)

### Test API Endpoints

**If running API separately (optional):**

```bash
cd src/EMS.API
dotnet run
# Outputs: Now listening on https://localhost:5001
```

### Test with cURL

```bash
# Get Executive Dashboard
curl -X GET "https://localhost:5001/api/dashboard/executive" \
  -H "accept: application/json" \
  --insecure

# Get Live Meters
curl -X GET "https://localhost:5001/api/meters/live" \
  -H "accept: application/json" \
  --insecure

# Get Meter Details
curl -X GET "https://localhost:5001/api/meters/1/details" \
  -H "accept: application/json" \
  --insecure
```

### Test with Postman

1. Download Postman: https://www.postman.com/downloads/
2. Create new requests:
   - Method: GET
   - URL: https://localhost:5001/api/dashboard/executive
   - Headers: Accept: application/json
   - Click "Send"

### Swagger UI (Auto-generated API docs)

1. Run API: `dotnet run` in EMS.API folder
2. Open: https://localhost:5001/swagger
3. Explore all endpoints
4. Try requests directly from Swagger UI

---

## Troubleshooting

### Issue: "Cannot connect to (local)\SQLEXPRESS"

**Solution:**
```powershell
# Start SQL Server
Get-Service "MSSQLSERVER" | Start-Service

# Or start from Services app:
# Control Panel → Administrative Tools → Services
# Find "SQL Server (SQLEXPRESS)" → Right-click → Start
```

### Issue: "Port 5050 is already in use"

**Solution:**
```bash
# Find process using port 5050
netstat -ano | findstr :5050

# Kill the process (replace PID with actual ID)
taskkill /PID <PID> /F

# Or change port in: src/EMS.Web/Properties/launchSettings.json
# Change "applicationUrl": "http://localhost:5050"
```

### Issue: ".NET SDK 8.0.421 not found"

**Solution:**
```bash
# Download .NET 8 from:
# https://dotnet.microsoft.com/en-us/download/dotnet/8.0

# Or use the version you have (if 8.0.x):
cd src
# Edit global.json to match your version
```

### Issue: "Build failed with warnings"

**Usually safe to ignore** if you see:
```
Non-nullable property 'XXX' must contain a non-null value
```

These are Entity Framework model warnings (normal).

If you see **errors**, run:
```bash
dotnet clean
dotnet restore
dotnet build
```

### Issue: Charts not showing on dashboard

**Causes:**
1. JavaScript console errors (F12 → Console tab)
2. ApexCharts CDN down (rare)
3. Model data null

**Debug:**
1. Open browser DevTools (F12)
2. Go to Network tab
3. Refresh page
4. Check if `apexcharts.min.js` loads (CDN)
5. Check Console for JS errors

---

## File Structure Guide

```
EnergyMonitoringSystem/
├── README.md                          ← Start here
├── CLAUDE.md                          ← Technical architecture guide
├── EXECUTIVE_SUMMARY.md               ← For non-technical stakeholders
├── PHASE1_WEEK2_REVIEW.md             ← Detailed code review
├── TEAM_SETUP_GUIDE.md                ← This file
│
├── docs/                              ← Design & requirements
│   ├── REQUIREMENTS.md                ← Full requirements spec
│   ├── DESIGN_RESEARCH.md             ← Design patterns & research
│   └── DASHBOARD_BLUEPRINT.md         ← Wireframes & API specs
│
└── src/                               ← Source code
    ├── global.json                    ← SDK version (8.0.421)
    ├── EnergyMonitoringSystem.slnx    ← Solution file
    │
    ├── EMS.Core/                      ← Domain models
    │   ├── Models.cs                  ← Entity definitions
    │   └── Interfaces/                ← Repository & service contracts
    │
    ├── EMS.Infrastructure/            ← Data access
    │   ├── ScadaDbContext.cs          ← EF Core DbContext
    │   └── Repositories/              ← 5 data repositories
    │
    ├── EMS.API/                       ← Web API (ASP.NET Core)
    │   ├── Program.cs                 ← API configuration
    │   ├── Controllers/               ← 2 API controllers
    │   ├── Services/                  ← Business logic
    │   └── appsettings.json           ← Configuration
    │
    ├── EMS.Web/                       ← Web UI (ASP.NET MVC)
    │   ├── Program.cs                 ← Web configuration
    │   ├── Controllers/               ← DashboardController
    │   ├── Services/                  ← Dashboard service
    │   ├── Views/                     ← Razor views
    │   │   ├── Dashboard/             ← Dashboard page
    │   │   │   └── Index.cshtml       ← Main dashboard UI
    │   │   └── Shared/                ← Layout & navigation
    │   ├── wwwroot/                   ← Static files (CSS, JS)
    │   └── appsettings.json           ← Configuration
    │
    └── EMS.Tests/                     ← Unit & integration tests
        └── (To be filled in Phase 2)
```

---

## Next Steps

### For Frontend Developers
1. Review: `src/EMS.Web/Views/Dashboard/Index.cshtml`
2. Check styling in _Layout.cshtml
3. Test chart interactivity
4. Verify mobile responsiveness

### For Backend Developers
1. Review: `src/EMS.Infrastructure/Repositories/`
2. Check `EMS.API/Services/DashboardService.cs`
3. Test API endpoints with Postman
4. Review error handling

### For QA/Testing
1. Test all dashboard interactions
2. Check mobile/tablet viewing
3. Test in different browsers
4. Verify data consistency
5. Performance testing

### For DevOps
1. Review Dockerfile (to be created)
2. Check deployment scripts
3. Plan CI/CD pipeline
4. Review infrastructure requirements

---

## Performance Testing (Optional)

### Check API Response Time
```bash
# Using curl with timing
curl -w "@curl-format.txt" -o /dev/null -s https://localhost:5001/api/dashboard/executive

# curl-format.txt should contain:
# time_namelookup:  %{time_namelookup}\n
# time_connect:     %{time_connect}\n
# time_total:       %{time_total}\n
```

### Browser Performance
1. Open Dashboard
2. Press F12 → Performance tab
3. Click Record
4. Refresh page
5. Stop recording
6. Analyze results (target: < 2s total load)

---

## Support & Questions

**Getting Help:**
1. Check CLAUDE.md for architecture questions
2. Check PHASE1_WEEK2_REVIEW.md for code questions
3. Check README.md for deployment questions
4. Check this file for setup questions

**Reporting Issues:**
1. Open issue on GitHub: https://github.com/abz1014/EnergyMonitoringSystem/issues
2. Include:
   - What you tried
   - What error you got
   - Your environment (.NET version, SQL Server version, OS)

**Contact:**
- Email: abdullahsajid772@gmail.com
- GitHub Issues: Use for technical discussions

---

## Celebration! 🎉

You now have the Energy Monitoring System running locally!

Next steps:
1. Explore the dashboard
2. Test the API endpoints
3. Review the code
4. Plan for Week 3 features
5. Share feedback with the team

**Thank you for building this amazing project!**

---

**Prepared:** June 29, 2026  
**For:** Development Team  
**Status:** Ready to Use  
**Questions?** See support section above
