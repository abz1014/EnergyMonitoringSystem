# Energy Monitoring System (EMS) - Web Dashboard

**Status:** 🟡 Design Phase Complete | Development Phase Starting  
**Version:** 1.0  
**Last Updated:** June 29, 2026

---

## 📋 Project Overview

A modern web-based energy monitoring platform that reads historical meter data from an existing SQL Server database and presents it through interactive, visually impressive dashboards.

**Key Features:**
- 📊 Executive Dashboard (KPI cards + consumption trends)
- 📡 Live Monitoring (real-time meter status)
- 📈 Energy Analysis (daily/weekly/monthly/yearly trends)
- 📍 Location Drill-Down (hierarchical view by building/floor)
- 🔧 Meter Details (individual meter deep-dive)
- 🚨 Alarm Management (active alerts + acknowledgement)
- 📄 Reports (PDF/Excel export + scheduled email)
- ⚙️ Admin Panel (user management, device config)

---

## 🏗️ Project Structure

```
EnergyMonitoringSystem/
│
├── docs/                           # Design & requirements documents
│   ├── REQUIREMENTS.md             # Complete Software Requirements Specification (SRS)
│   ├── DESIGN_RESEARCH.md          # Design patterns, references, UX best practices
│   └── DASHBOARD_BLUEPRINT.md      # Detailed wireframes, API specs, DB queries
│
├── src/                            # Source code (will be populated)
│   ├── EMS.API/                    # ASP.NET Core 8 Web API backend
│   │   ├── Controllers/            # REST endpoints
│   │   ├── Services/               # Business logic
│   │   └── Program.cs              # Configuration
│   ├── EMS.Web/                    # ASP.NET Core MVC frontend
│   │   ├── Controllers/            # Page controllers
│   │   ├── Views/                  # Razor HTML templates
│   │   └── wwwroot/                # Static files (CSS, JS, images)
│   ├── EMS.Core/                   # Shared domain logic
│   │   ├── Models/                 # Domain entities
│   │   ├── Interfaces/             # Abstractions
│   │   └── Enums/                  # Enumerations
│   ├── EMS.Infrastructure/         # Data access layer
│   │   ├── Repositories/           # Database queries
│   │   ├── Configurations/         # EF Core mappings
│   │   └── DbContext.cs            # Database context
│   ├── EMS.Tests/                  # Unit & integration tests
│   │   ├── UnitTests/              # Business logic tests
│   │   ├── IntegrationTests/       # API endpoint tests
│   │   └── Fixtures/               # Test data
│   └── EnergyMonitoringSystem.sln  # Visual Studio solution file
│
├── database/                       # Database files & scripts
│   ├── db_SCADA.mdf                # SQL Server data file
│   ├── db_SCADA_log.ldf            # SQL Server log file
│   ├── SetupLocalDB.sql            # Initial schema + sample data
│   └── Queries/                    # Reference queries
│
├── .gitignore                      # Git ignore patterns
├── CLAUDE.md                       # Project guide for Claude & developers
└── README.md                       # This file
```

---

## 🚀 Quick Start

### Prerequisites
- **OS:** Windows 10/11
- **IDE:** Visual Studio 2022 (Community edition OK)
- **Runtime:** .NET 8 SDK (download from https://dotnet.microsoft.com/download/dotnet/8.0)
- **Database:** SQL Server 2019+ (local SQLEXPRESS)
- **Git:** For version control

### Installation

#### 1. Clone or Navigate to Project
```bash
cd "C:\Users\ABDULLAH SAJID\Desktop\scada software\EnergyMonitoringSystem"
```

#### 2. Verify Database Connection
```bash
# Test connection to db_SCADA
sqlcmd -S (local)\SQLEXPRESS -Q "SELECT DB_NAME()"

# Should output: master
```

#### 3. Restore & Build Solution (when code is ready)
```bash
cd src
dotnet restore
dotnet build
```

#### 4. Run API
```bash
cd EMS.API
dotnet run
# API will start on http://localhost:5000 (HTTPS: 5001)
```

#### 5. Run Web Frontend
```bash
cd EMS.Web
dotnet run
# Frontend will start on http://localhost:5050 (HTTPS: 5051)
```

#### 6. Access Application
```
http://localhost:5050
Username: admin@example.com (default)
Password: Password@123 (default)
```

---

## 📖 Documentation

### For Non-Technical (Managers, Operators)
👉 Start with: `/docs/REQUIREMENTS.md` → Section 3 "Functional Requirements"
- What dashboards look like
- What data is displayed
- What users can do

### For Designers (UI/UX)
👉 Start with: `/docs/DESIGN_RESEARCH.md` → All sections
- Reference dashboards (20+ analyzed)
- Color schemes & typography
- Interaction patterns
- Accessibility guidelines

### For Developers (Backend/Frontend)
👉 Start with: `/docs/DASHBOARD_BLUEPRINT.md` → All sections
- Detailed wireframes
- API endpoint specifications
- Database queries
- Implementation roadmap

### For Project Leads
👉 Start with: `CLAUDE.md` → All sections
- Project context & goals
- Technology stack
- Deployment strategy
- Collaboration guidelines

---

## 🔧 Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 8 |
| **Backend API** | ASP.NET Core | 8 |
| **Frontend** | ASP.NET Core MVC | 8 |
| **UI Framework** | Bootstrap | 5 |
| **Charts** | ApexCharts | 3.x |
| **Authentication** | ASP.NET Identity | Built-in |
| **ORM** | Entity Framework Core | 8 |
| **Logging** | Serilog | Latest |
| **Testing** | xUnit + Moq | Latest |
| **Database** | SQL Server | 2019+ |
| **CSS** | SCSS/CSS3 | Latest |
| **JavaScript** | ES6+ | Latest |

---

## 📊 Database Connection

**Database Name:** `db_SCADA`  
**Server:** `(local)\SQLEXPRESS` (local machine only)  
**Authentication:** Windows Authentication  
**Access Mode:** Read-only (EMS does not modify data)

### Connection String
```
Server=(local)\SQLEXPRESS;Database=db_SCADA;Integrated Security=true;
```

### Key Tables
| Table | Purpose | Rows |
|-------|---------|------|
| `tblEnergyMetersData` | Historical meter readings (36 columns) | 1000s |
| `tblEnergyMeterLive` | Latest snapshot per meter | ~6 |
| `tblMonitoringDevices` | Device registry | ~6 |
| `tbFlowmetersData` | Fuel tank readings | 100s |
| `Alarms` | Active & historical alerts | 10s-100s |
| `tblDevicesTags` | Register map | ~20 |

---

## 🎯 Implementation Roadmap

### Phase 1: MVP (Weeks 1-4) ⏳ Starting Soon
**Target:** Functional dashboard with live data

- [ ] Solution structure (API, Web, Core, Infrastructure, Tests)
- [ ] Database repositories (EF Core)
- [ ] Executive Dashboard (KPI cards + main chart)
- [ ] Live Monitoring (meter table with status)
- [ ] Basic filters (Plant, Building, Date)
- [ ] Authentication (login page)
- [ ] PDF export (single-page)
- [ ] Unit tests (80% coverage)

**Success Criteria:** Executive dashboard working, all Phase 1 features functional, no critical bugs

### Phase 2: Analytics (Weeks 5-8)
**Target:** Full reporting and analysis capabilities

- [ ] Energy Analysis dashboard (trends, heatmaps)
- [ ] Locations hierarchical view (drill-down)
- [ ] Meter Details page (charts, history)
- [ ] Alarms dashboard (acknowledge workflow)
- [ ] Excel export (formatted, multi-sheet)
- [ ] Comparison mode (this month vs last month)
- [ ] Admin panel basics (user management)
- [ ] Integration tests

**Success Criteria:** All analytics dashboards functional, reporting working, admin able to manage users

### Phase 3: Polish (Weeks 9-12)
**Target:** Production-ready system

- [ ] Reports generator (custom templates)
- [ ] Scheduled email reports (SMTP)
- [ ] Full admin console (device config, alert thresholds)
- [ ] Performance optimization (query tuning, caching)
- [ ] Mobile responsive refinement
- [ ] Security hardening (penetration testing)
- [ ] Load testing (50+ concurrent users)
- [ ] Documentation (API, deployment, user guide)
- [ ] Production deployment

**Success Criteria:** All features complete, performance targets met, security audit passed, 50+ concurrent users supported

---

## 🏃 Development Workflow

### Daily Standup
- **When:** 9:00 AM (suggested)
- **Duration:** 15 minutes
- **Topics:** Completed yesterday, planned today, blockers

### Code Review
- **Process:** Create PR → 2 approvals → Merge
- **Standards:** Follow CLAUDE.md coding guidelines
- **Testing:** All tests pass before merge

### Git Workflow
```bash
# Create feature branch
git checkout -b feature/executive-dashboard

# Commit frequently with clear messages
git commit -m "feat: Add KPI cards to executive dashboard"

# Push to origin
git push origin feature/executive-dashboard

# Create pull request on GitHub
# Get 2 approvals, then merge
```

### Testing Before Commit
```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --project EMS.Tests/IntegrationTests

# Check code coverage
dotnet test /p:CollectCoverage=true
```

---

## 🐛 Troubleshooting

### Database Connection Issues
```
Error: "Cannot connect to (local)\SQLEXPRESS"

Solutions:
1. Verify SQL Server is running (Services → SQL Server)
2. Check connection string matches your machine
3. Try: sqlcmd -S (local)\SQLEXPRESS -E -Q "SELECT 1"
```

### Port Already in Use
```
Error: "Port 5000 already in use"

Solution:
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process
taskkill /PID <PID> /F

# Or use different port in launchSettings.json
```

### Build Failures
```
# Clean and rebuild
dotnet clean
dotnet build

# Clear NuGet cache
dotnet nuget locals all --clear
```

### Tests Failing
```
# Ensure database is accessible
# Check test connection string in appsettings.Testing.json
# Run individual test:
dotnet test --filter "TestClassName.TestMethodName"
```

---

## 📞 Support & Escalation

### If You're Stuck On:
| Issue | Contact | Response Time |
|-------|---------|---------------|
| Architecture question | Project Lead | 24 hours |
| Database query | Senior Dev | 4 hours |
| Visual design | UI Designer | Next day |
| Deployment | DevOps | When needed |

### Escalation Path
1. **L1:** Team mate / Google / Stack Overflow (30 min)
2. **L2:** Claude Code / Project Lead (2 hours)
3. **L3:** Senior Management (same day)

---

## 📋 Checklist for Getting Started

- [ ] Read `/docs/REQUIREMENTS.md` (understand requirements)
- [ ] Review `/docs/DESIGN_RESEARCH.md` (understand design)
- [ ] Skim `/docs/DASHBOARD_BLUEPRINT.md` (understand implementation)
- [ ] Read `CLAUDE.md` (understand project context)
- [ ] Install .NET 8 SDK
- [ ] Install Visual Studio 2022
- [ ] Verify SQL Server connection
- [ ] Clone/pull latest code
- [ ] Open `src/EnergyMonitoringSystem.sln` in Visual Studio
- [ ] Build solution (should succeed)
- [ ] Run first API locally
- [ ] Test database connection
- [ ] Attend kickoff meeting

---

## 📊 Project Status

| Component | Status | Owner | ETA |
|-----------|--------|-------|-----|
| Requirements | ✅ Complete | Project Lead | June 29 |
| Design Research | ✅ Complete | Designer | June 29 |
| Dashboard Wireframes | ✅ Complete | Designer | June 29 |
| API Specification | ✅ Complete | Architect | June 29 |
| Solution Structure | ⏳ Pending | Backend Dev | July 1-3 |
| Phase 1 Implementation | ⏳ Pending | Backend Dev | July 4-25 |
| Phase 2 Implementation | ⏳ Pending | Full Team | July 26-Aug 15 |
| Phase 3 Implementation | ⏳ Pending | Full Team | Aug 16-Sep 5 |
| Deployment to Prod | ⏳ Pending | DevOps | ~Sep 5 |

---

## 🔗 Related Projects

**Legacy SCADA Desktop App:**
- Location: `../Sis/Sis.EdgeSCADA/17-6-2026 Before working for TP1/Sis.EdgeSCADA/`
- Technology: .NET Framework, WinForms
- Status: Active (will continue running)
- Database: Same `db_SCADA`

**Sibling Projects:**
- None yet (EMS is primary modern initiative)

---

## 📝 License & Copyright

**Copyright © 2026 [Company Name]**  
All rights reserved. This is proprietary software.

---

## 🎯 Next Steps

### For Developers
1. ✅ Download .NET 8 SDK (if not already done)
2. ✅ Install Visual Studio 2022 (if not already done)
3. ✅ Read CLAUDE.md completely
4. ⏳ Wait for project lead to say "Start Phase 1"
5. ⏳ Initialize solution structure (I'll help)

### For Project Leads
1. ✅ Review all 3 design documents
2. ⏳ Get senior approval on dashboard designs
3. ⏳ Allocate team resources
4. ⏳ Schedule kickoff meeting
5. ⏳ Say "Go!" to start Phase 1

### For Senior Management
1. ✅ Review REQUIREMENTS.md (Section 3 for features)
2. ⏳ Review DESIGN_RESEARCH.md (Section 2 for reference dashboards)
3. ⏳ Review DASHBOARD_BLUEPRINT.md (Section 3 for actual designs)
4. ⏳ Approve or request changes
5. ⏳ Sign off on go-live timeline

---

## 📧 Questions or Feedback?

For clarifications on this README or the design documents:
- **Email:** [your project lead email]
- **Slack:** [your team channel]
- **GitHub Issues:** [create issue in repo]

---

**Version:** 1.0  
**Last Updated:** June 29, 2026  
**Status:** 🟡 Design Complete | Development Starting

Welcome to the Energy Monitoring System! 🚀

