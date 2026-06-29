# Energy Monitoring System (EMS) - Web Application

**Version:** 1.0  
**Date:** June 29, 2026  
**Project Lead:** Senior Management  
**Technical Lead:** To be assigned

---

## Project Context

You are the lead architect/developer for the **Energy Monitoring System (EMS)** — a modern web application that reads from an existing SQL Server database and presents industrial energy dashboards.

### What This Is
- ✅ A **web-based analytics platform** for energy monitoring
- ✅ Complementary to the existing Sis.EdgeSCADA desktop application
- ✅ Modern stack: ASP.NET Core 8, Avalonia/React frontend, ApexCharts
- ✅ Read-only access to db_SCADA (no modifications to production data)

### What This Is NOT
- ❌ A replacement for Sis.EdgeSCADA (that stays unchanged)
- ❌ A redesign of the SCADA polling logic
- ❌ A new database or data collection system

---

## Related Systems

**Legacy Desktop App:**
- Location: `../Sis/Sis.EdgeSCADA/17-6-2026 Before working for TP1/Sis.EdgeSCADA/`
- Technology: .NET Framework, WinForms
- Purpose: Reads Modbus meters, stores in SQL
- Status: Functional, will continue running

**Shared Database:**
- Name: `db_SCADA`
- Server: `(local)\SQLEXPRESS`
- Connection: Windows Authentication
- Tables: tblEnergyMetersData, tblMonitoringDevices, tbFlowmetersData, Alarms, etc.
- Access: Read-only (EMS does not modify)

---

## Technology Stack

**Backend:**
- Runtime: .NET 8
- Framework: ASP.NET Core 8 Web API
- ORM: Entity Framework Core 8
- Authentication: ASP.NET Identity (or Azure AD)
- Logging: Serilog
- Testing: xUnit, Moq

**Frontend:**
- Framework: ASP.NET Core MVC 8 (or React, if approved)
- UI Framework: Bootstrap 5
- Charts: ApexCharts
- Styling: CSS/SCSS (dark industrial theme)

**Database:**
- Type: SQL Server
- Version: Any (local SQLEXPRESS for dev)
- Access Pattern: Read-only to db_SCADA

**DevOps:**
- Version Control: Git
- Hosting: IIS or Azure App Service (TBD)
- CI/CD: GitHub Actions or Azure DevOps (TBD)

---

## Architecture Principles

### Clean Architecture
```
EMS.API/              → Controllers, Middleware, Dependencies
EMS.Application/      → Use Cases, DTOs, Validation
EMS.Domain/           → Entities, Business Logic
EMS.Infrastructure/   → Data Access, External Services
EMS.Tests/            → Unit & Integration Tests
```

### Design Patterns
- **Repository Pattern:** Abstract database access
- **Dependency Injection:** Loose coupling, testability
- **SOLID Principles:** Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
- **CQRS** (optional): Separate read/write models if performance requires

### Database Access
- Entity Framework Core for queries
- No raw SQL (except for complex aggregations)
- Parameterized queries (prevent SQL injection)
- Connection pooling

---

## Core Requirements

### Dashboards (7 Total)
1. **Executive Dashboard** — High-level KPIs (Today's consumption, peak demand, cost, online meters)
2. **Live Monitoring** — Real-time meter status (voltage, current, power, frequency, PF)
3. **Energy Analysis** — Historical trends (daily/weekly/monthly/yearly with comparisons)
4. **Locations** — Hierarchical drill-down (Plant → Building → Floor → Area → Meter)
5. **Meters** — Individual meter details (live values, charts, history, alarms)
6. **Alarms** — Active alerts, acknowledgement workflow, historical log
7. **Reports** — PDF/Excel generation, scheduled email, custom reports

### Global Features
- **Filtering:** Plant, Building, Location, Meter, Date Range, Comparison Mode
- **Charts:** Line, Area, Stacked, Donut, Heatmap, Gauge, Sparkline (ApexCharts)
- **Export:** PDF, Excel, CSV
- **Authentication:** Username/password + optional Azure AD
- **Authorization:** Role-based (Operator, Supervisor, Admin, Executive)
- **Performance:** Page load < 2s, API response < 500ms, 50+ concurrent users
- **Accessibility:** WCAG 2.1 AA compliance
- **Responsiveness:** Desktop, tablet, mobile (100% responsive)

### Data Metrics
All 36 columns from tblEnergyMetersData:
- Phase voltages (L1-N, L2-N, L3-N, L1-L2, L2-L3, L1-L3)
- Phase currents (L1, L2, L3)
- Power (active, reactive, apparent per phase + total)
- Power factors (L1, L2, L3)
- Frequency (50 Hz nominal)
- Energy counters (kWh, kVAh, kVARh)
- Harmonics (voltage THD L1/L2/L3, current THD L1/L2/L3)

### Alarms
- Display active alarms (IsActive = 1)
- Acknowledgement workflow (AckBy, AckTime)
- Severity levels: Critical (red), Warning (yellow), Info (blue)
- Historical audit trail

---

## Implementation Roadmap

### Phase 1: MVP (Weeks 1-4)
**Goal:** Functional dashboard with live data

Deliverables:
- ✅ Solution structure (API + Web projects)
- ✅ Database connection & repositories
- ✅ Executive Dashboard (KPI cards + consumption chart)
- ✅ Live Monitoring (meter table with status)
- ✅ Basic filters (Plant, Building, Date)
- ✅ Authentication (login/logout)
- ✅ PDF export (simple)
- ✅ Unit tests (80% coverage on business logic)

Milestones:
- Day 7: API scaffolding complete
- Day 14: First dashboard working
- Day 21: All Phase 1 features implemented
- Day 28: Testing & bug fixes complete

### Phase 2: Analytics (Weeks 5-8)
**Goal:** Full reporting and analysis capabilities

Deliverables:
- ✅ Energy Analysis dashboard (hourly/daily/weekly/monthly)
- ✅ Locations hierarchical view
- ✅ Meter Details page (live values + history charts)
- ✅ Alarms dashboard (active + historical)
- ✅ Excel export (with formatting)
- ✅ Comparison mode (A/B charts)
- ✅ Admin panel (basic user management)

### Phase 3: Polish (Weeks 9-12)
**Goal:** Production-ready system

Deliverables:
- ✅ Reports generator (custom templates)
- ✅ Scheduled email reports
- ✅ Full admin console (device config, thresholds)
- ✅ Performance optimization
- ✅ Mobile responsive refinement
- ✅ Security audit (penetration testing)
- ✅ Load testing (50+ concurrent users)
- ✅ Documentation (API, deployment, user guide)

---

## Design Documents

**READ FIRST:** `/docs/REQUIREMENTS.md`
- Complete Software Requirements Specification
- User personas, functional/non-functional requirements
- Success criteria, assumptions, constraints

**RESEARCH:** `/docs/DESIGN_RESEARCH.md`
- 20+ reference dashboards analyzed
- Design patterns, color scheme, typography
- Chart recommendations, UX best practices
- Accessibility & performance guidelines

**IMPLEMENTATION:** `/docs/DASHBOARD_BLUEPRINT.md`
- Detailed wireframes for all 8 dashboards
- Filter bar specification
- API endpoints with request/response examples
- Database queries for each feature
- Interaction flows & animations
- 3-phase implementation plan

---

## Coding Standards

### C# (Backend)
```csharp
// Naming: PascalCase for classes, camelCase for parameters
public class EnergyMeterRepository : IEnergyMeterRepository
{
    public async Task<List<EnergyReading>> GetDailyConsumption(
        int meterId, 
        DateTime startDate, 
        DateTime endDate)
    {
        // Implementation
    }
}

// Use async/await for I/O
// Use LINQ for queries
// Use dependency injection for services
// XML comments for public methods
```

### HTML/CSS (Frontend)
```html
<!-- Use semantic HTML (header, nav, main, footer) -->
<!-- Bootstrap classes for layout -->
<!-- Dark theme colors: #0F172A bg, #1E293B cards, #2563EB accent -->
<!-- Accessibility: ARIA labels, alt text, focus states -->
```

### Git Commits
```
feat: Add executive dashboard with KPI cards
fix: Correct voltage calculation in meter details
refactor: Extract chart rendering to component
docs: Update API endpoint documentation
test: Add unit tests for alarm engine

Format: [type]: [description]
Types: feat, fix, refactor, docs, test, chore, ci
```

---

## Performance Targets

| Metric | Target | How We'll Achieve |
|--------|--------|-------------------|
| Page Load | < 2s | Lazy-load charts, cache API responses, minify assets |
| API Response | < 500ms | Query optimization, database indexing, caching |
| Live Update | Every 30s | Efficient WebSocket or polling, debounce filters |
| Mobile Load | < 3s | Responsive images, lightweight CSS, mobile-first |
| Database Queries | < 200ms | Indexed columns (DateTime, MeterNo, DeviceID) |
| Concurrent Users | 50+ | Connection pooling, load balancing, horizontal scaling |

---

## Security

### Authentication
- **Method:** ASP.NET Identity (or Azure AD Single Sign-On)
- **Session:** 30-minute inactivity timeout
- **Password:** Minimum 8 chars, complex
- **Forgot Password:** Secure email reset link

### Authorization
- **Role-Based Access Control (RBAC):**
  - Operator: View dashboards, acknowledge alarms
  - Supervisor: + Generate reports, configure alerts
  - Admin: + User management, device configuration
  - Executive: Dashboard only (read-only)

### Data Protection
- **HTTPS/TLS:** All communication encrypted
- **SQL Injection Prevention:** Parameterized queries, Entity Framework
- **XSS Prevention:** HTML encoding, Content Security Policy (CSP)
- **CSRF Protection:** Token-based (ASP.NET Core built-in)
- **Audit Trail:** Log all user actions (login, exports, acknowledgements)

### Database
- **Read-Only:** EMS never modifies db_SCADA
- **Connection String:** Stored in secure config (Azure Key Vault, not in code)
- **Backup:** Managed by existing SCADA system

---

## Testing Strategy

### Unit Tests (80% coverage target)
- Domain models (calculations, validations)
- Business logic (alarms, aggregations)
- Repositories (data access)
- Services (orchestration)

### Integration Tests
- API endpoints (full request/response cycle)
- Database queries (against test database)
- Authentication/Authorization flows

### Manual Testing
- Dashboard functionality (all 8 pages)
- Filters & drill-downs
- Export (PDF, Excel)
- Mobile responsiveness
- Cross-browser (Chrome, Firefox, Safari, Edge)

### Performance Testing
- Load testing (50+ concurrent users)
- Response time profiling
- Database query optimization

---

## Deployment

### Development Environment
- Local machine: Visual Studio 2022
- Database: Local SQLEXPRESS
- Debugging: Visual Studio debugger

### Staging Environment
- Server: TBD
- Database: Copy of db_SCADA (or staging database)
- Testing: Manual UAT + automated tests

### Production Environment
- Server: IIS or Azure App Service (TBD)
- Database: Production db_SCADA (read-only)
- Monitoring: Application Insights or similar
- Backup: Existing SCADA system manages database

### Deployment Process
```
1. Code review on GitHub (PR with 2 approvals)
2. CI/CD pipeline runs tests & builds
3. Deploy to staging environment
4. Manual testing & sign-off
5. Deploy to production (blue-green if possible)
6. Monitor logs & performance
7. Rollback if issues (database is untouched, so safe)
```

---

## Collaboration Guidelines

### With Me (Claude Code)
- I'll provide code reviews, architectural guidance, implementation help
- I can generate boilerplate (controllers, repositories, DTOs)
- I can debug issues and optimize performance
- I can help with SQL queries and database design

### With Your Team
- Code: Daily standup on progress
- Design: Weekly design review meetings
- Testing: QA team responsible for manual testing
- Deployment: DevOps/System admin handles servers

### Documentation
- Keep README.md updated with setup instructions
- Update API documentation (Swagger) with each endpoint
- Maintain CLAUDE.md as source of truth for architecture
- Document any deviations from this plan

---

## What To Do Next

1. **Review the 3 design documents** (REQUIREMENTS, DESIGN_RESEARCH, BLUEPRINT)
2. **Get senior approval** on dashboard designs and feature list
3. **Set up development environment** (Visual Studio, .NET 8 SDK, SQL Server)
4. **Initialize solution structure** (projects, dependencies, folder layout)
5. **Start Phase 1:** API scaffolding + Executive Dashboard

**First question to resolve:** ASP.NET Core MVC (simpler) vs React (more powerful)?
- My recommendation: MVC for MVP speed, React for Phase 2 if needed

---

## Important Notes

⚠️ **This is NOT:**
- A complete rewrite of SCADA system
- A data collection system (existing app handles that)
- A replacement for the desktop app
- A control/automation system

✅ **This IS:**
- An analytics & reporting system
- A modern UI for existing data
- A dashboard for decision-making
- A complement to existing infrastructure

📚 **Always reference:**
- `/docs/REQUIREMENTS.md` for requirements
- `/docs/DESIGN_RESEARCH.md` for design patterns
- `/docs/DASHBOARD_BLUEPRINT.md` for implementation details
- Legacy system docs in parent folder for context

---

**Version:** 1.0  
**Last Updated:** June 29, 2026  
**Status:** Ready for Development

When you're ready to start coding, let me know which aspect first:
- Backend solution structure?
- Frontend MVC template?
- Database repository layer?
- Authentication setup?

I'm ready to help! 🚀
