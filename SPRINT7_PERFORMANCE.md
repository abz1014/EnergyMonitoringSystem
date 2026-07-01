# Sprint 7 — Performance Optimization

## Methodology

Full codebase audit via automated agent (63+ GetByDateRange call sites, 44 controllers) followed by targeted fixes. Benchmarks are DB-call counts per request (cold path, before cache), verified by code inspection. The app currently has 7 days of data and 6 devices — these numbers will grow in production, so per-call overhead matters more than raw timings on a dev machine.

---

## Before vs After — DB Calls Per Request

### Dashboard (cold, cache miss)

| Step | Before | After |
|---|---|---|
| GetAllDevices() | 3× (lines 72, 191, 247) | **1×** (fetched once, passed to helpers) |
| GetByDateRange — today | 2× (GetExecutiveDashboard + BuildLoadProfile) | **1×** |
| GetByDateRange — week (7d) | 2× (score calc + load profile) | **1×** (shared weekData) |
| GetByDateRange — month (MTD) | 1× | 1× |
| GetByDateRange — 12 months | 1× | 1× |
| GetByDateRange — yesterday | 1× (inside BuildLoadProfile) | **0×** (derived from weekData) |
| GetOnlineDeviceCount() | 1× (separate COUNT query) | **0×** (computed from allDevices list) |
| GetActiveAlarmCount() | 1× | 1× |
| **Total** | **~11 calls** | **~6 calls** |

After cache warms (30s TTL): 0 calls for subsequent requests.

### Briefing (per request, no cache)

| Step | Before | After |
|---|---|---|
| GetByDateRange (yesterday data) | 1× | **0×** |
| GetByDateRange (30d fallback) | 1× (conditional) | **0×** |
| GetByDateRange (7d avg) | 1× | **0×** |
| GetByDateRange (weekly health — this week) | 1× | **0×** |
| GetByDateRange (weekly health — prior week) | 1× | **0×** |
| GetByDateRange (monthly — this month) | 1× | **0×** |
| GetByDateRange (monthly — prior month) | 1× | **0×** |
| **Wide single fetch (prior month → today)** | — | **1×** |
| GetAllAlarms() for alarm count | 1× (full table) | **0×** |
| GetAlarmCountInRange() | — | **1×** (COUNT query) |
| GetActiveAlarmCount() | 1× | 1× |
| GetAllDevices() | 1× | 1× |
| **Total** | **~8–9 calls** | **~4 calls** |

### Load Analytics (per request)

| Step | Before | After |
|---|---|---|
| GetByDateRange — selected range | 1× | 1× |
| GetByDateRange — 2-year fetch (monthly variance) | **1× every request** | **0× (cached 5 min)** |
| **Total** | **2 calls + full 2yr rescan every click** | **2 calls first visit, 1 call after** |

---

## What Was Changed

### 1. `MonitoringDeviceRepository.GetAllDevices()` — 10-minute cache
- **File**: `EMS.Infrastructure/Repositories/MonitoringDeviceRepository.cs`
- **Reason**: 6-row table queried by 13 different controllers. Changes only when hardware is added (never during normal runtime). Caching eliminates a round trip on every page that shows device names.
- **Risk**: None. Cache is invalidated automatically after 10 minutes. No write path touches this cache.

### 2. `IAlarmRepository` + `AlarmRepository` — new `GetAlarmCountInRange(from, to)`
- **Files**: `EMS.Core/Interfaces/IAlarmRepository.cs`, `EMS.Infrastructure/Repositories/AlarmRepository.cs`
- **Reason**: `BriefingService.BuildWeeklyHealthAsync` previously called `GetAllAlarms()` (full table scan + in-memory filter) to count alarms in a 7-day window. Replaced with a targeted `COUNT` query.
- **SQL generated**: `SELECT COUNT(*) FROM Alarms WHERE CreatedAt >= @from AND CreatedAt <= @to`

### 3. `DashboardService` — eliminated 5 duplicate calls on cold path
- **File**: `EMS.Web/Services/DashboardService.cs`
- **Changes**:
  - `GetAllDevices()` called once at the top of `GetExecutiveDashboardAsync`, passed into `BuildChartDataAsync` (was called again inside)
  - `GetOnlineDeviceCount()` eliminated — computed in-memory from the already-fetched device list
  - The second `GetAllDevices()` call (was line 191 for score calc) eliminated — uses same list
  - `BuildLoadProfileAsync` (was 4 separate DB calls): replaced with synchronous `BuildLoadProfileFromData` that receives `todayData` and `weekData` from the caller — no DB calls
  - Yesterday data inside load profile: derived by filtering `weekData` — no extra call
  - `weekData` (7-day trailing) fetched once, shared between score calc and load profile

### 4. `BriefingService` — 6 DB calls collapsed to 1 wide fetch
- **File**: `EMS.Web/Services/BriefingService.cs`
- **Change**: Single `GetByDateRange(priorMonthStart, today)` covers ~60 days. All downstream calculations (yesterday summary, 7-day average, weekly health this/prior week, monthly report this/prior month) filter from that in-memory list. Result: 6 date-range queries → 1.
- **`BuildWeeklyHealthAsync` / `BuildMonthlyReportAsync`**: now accept `List<EnergyMeterData> allData` parameter, filter in memory.
- **`BuildMonthlyReportAsync`**: remains `async` because it awaits `AppSettingsService.GetDoubleAsync` (tariff rate — already cached by AppSettingsService).

### 5. `LoadAnalyticsController` — cache 2-year monthly variance fetch
- **File**: `EMS.Web/Controllers/LoadAnalyticsController.cs`
- **Change**: `BuildMonthlyVarianceTab` fetches 2 years of data to compute month-over-month variance. This was re-fetched on every page load and every tab switch. Now cached for 5 minutes. Subsequent requests within that window skip the DB call entirely.

---

## What Was NOT Changed (and Why)

### `RootCauseController` — N+1 per alarm (1 GetByDateRange per alarm in loop)
- Documented by audit agent (line 79-86 with in-code comment acknowledging it)
- Current alarm count: single digits. N+1 impact is negligible at this scale.
- Fix (batch fetch + partition by alarm) is non-trivial and would change displayed output. Deferred.

### `AlarmController` / `AlarmResponseController` / `QbrController` — `GetAllAlarms()` full table fetch
- Alarms table is small. The full-table scan is fine until alarm volume grows to thousands.
- Proper fix: add `GetAlarmsSince(DateTime cutoff)` and convert filtering to SQL.
- Deferred — not on the critical path for page load speed today.

### Chart rendering (ApexCharts inline JSON)
- All chart data is server-serialized into the HTML response (not AJAX). This is the correct approach for initial page load — eliminates a second round trip that client-side chart fetch would require.
- No change needed.

### `AppSettingsService`
- Already caches `GetAllAsync` (5-min TTL), which backs every `GetAsync`/`GetDoubleAsync`/`GetIntAsync` call. Already optimized.

### SQL indexes
- Already correct: `IX_EnergyMetersData_DateTime` (single) and `IX_EnergyMetersData_MeterNo_DateTime` (composite) are both present from the prior `AddCompositeIndexes` migration. `GetByDateRange` (filters on DateTime only) uses the single-column index. No new indexes needed.

### Response compression
- Already enabled in Program.cs (Brotli + Gzip for HTTPS). No change needed.

---

## After-State DB Call Summary

| Page | Before (cold) | After (cold) | After (warm cache) |
|---|---|---|---|
| Dashboard | ~11 | ~6 | 0 |
| Briefing | ~9 | ~4 | — (no cache; data changes daily) |
| Load Analytics | 2 + 2yr full scan | 2 | 1 |
| Time of Use | 1–2 | 1–2 | — (unchanged; already efficient) |
| Any page calling GetAllDevices | 1 DB round trip | **from cache** | from cache |

---

## Build & Test Status

- Build: **0 errors** (10 pre-existing warnings, unchanged)
- Tests: **51/51 passed**
