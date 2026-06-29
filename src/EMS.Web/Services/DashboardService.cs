namespace EMS.Web.Services;

using Microsoft.Extensions.Caching.Memory;
using EMS.Core.Interfaces;
using EMS.Core.Models;

public class WebDashboardService : IDashboardService
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly IMonitoringDeviceRepository _deviceRepository;
    private readonly IAlarmRepository _alarmRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebDashboardService> _logger;

    private static readonly TimeSpan KpiCacheDuration = TimeSpan.FromSeconds(30);
    private const string CacheKeyDashboard = "dashboard_kpi";

    public WebDashboardService(
        IEnergyMeterRepository energyMeterRepository,
        IMonitoringDeviceRepository deviceRepository,
        IAlarmRepository alarmRepository,
        IMemoryCache cache,
        ILogger<WebDashboardService> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _deviceRepository = deviceRepository;
        _alarmRepository = alarmRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DashboardFilterDto filter)
    {
        var cacheKey = $"{CacheKeyDashboard}_{filter.Plant}_{filter.Building}_{filter.Area}";

        if (_cache.TryGetValue(cacheKey, out ExecutiveDashboardDto? cached) && cached != null)
            return cached;

        try
        {
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var todayData = await _energyMeterRepository.GetByDateRange(today, tomorrow);

            if (todayData.Count == 0)
            {
                var latestReading = await _energyMeterRepository.GetByDateRange(DateTime.Now.AddDays(-30), tomorrow);
                if (latestReading.Count > 0)
                {
                    var latestDate = latestReading.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    todayData = latestReading.Where(d => d.DateTime.HasValue && d.DateTime.Value.Date == latestDate).ToList();
                    today = latestDate;
                    tomorrow = latestDate.AddDays(1);
                }
            }

            var todaysConsumption = todayData.Sum(d => (double)(d.kWh ?? 0));
            var peakDemand = todayData.Count > 0 ? todayData.Max(d => (double)(d.kWtotal ?? 0)) : 0;
            var onlineMeters = await _deviceRepository.GetOnlineDeviceCount();
            var totalDevices = (await _deviceRepository.GetAllDevices()).Count;

            var monthData = await _energyMeterRepository.GetByDateRange(monthStart, tomorrow);
            var monthlyTotal = monthData.Sum(d => (double)(d.kWh ?? 0));

            var avgPowerFactor = todayData.Count > 0
                ? todayData.Where(m => m.PFL1.HasValue && m.PFL1 > 0).Select(m => (double)m.PFL1!.Value).DefaultIfEmpty(0).Average()
                : 0;

            var co2Factor = 0.82;
            var co2Emissions = (todaysConsumption / 1000.0) * co2Factor;

            var tariffRate = 52.0;
            var estimatedCost = (monthlyTotal * tariffRate) / 100_000;

            var latestReadings = todayData.Where(d => d.MeterNo.HasValue).GroupBy(d => d.MeterNo!.Value).Select(g => g.OrderByDescending(d => d.DateTime).First()).ToList();
            var currentLoad = latestReadings.Sum(m => (double)(m.kWtotal ?? 0));

            var kpiCards = new KpiCardsDto
            {
                TodayConsumption = new KpiCardDto
                {
                    Title = "Today's Consumption",
                    Value = todaysConsumption,
                    Unit = "kWh",
                    Trend = "Live",
                    Status = "good",
                    Subtitle = "Daily total"
                },
                CurrentLoad = new KpiCardDto
                {
                    Title = "Current Load",
                    Value = currentLoad,
                    Unit = "kW",
                    Trend = "Real-time",
                    Status = "normal",
                    Subtitle = "Live demand"
                },
                PeakDemand = new KpiCardDto
                {
                    Title = "Peak Demand Today",
                    Value = peakDemand,
                    Unit = "kW",
                    Trend = "Today",
                    Status = peakDemand > 500 ? "warning" : "normal",
                    Subtitle = "Max reached"
                },
                MonthlyTotal = new KpiCardDto
                {
                    Title = "Monthly Total",
                    Value = monthlyTotal,
                    Unit = "kWh",
                    Trend = "Month-to-date",
                    Status = "good",
                    Subtitle = $"{monthStart:MMM yyyy}"
                },
                OnlineMeters = new KpiCardDto
                {
                    Title = "Online Meters",
                    Value = onlineMeters,
                    Unit = $"/ {totalDevices}",
                    Trend = totalDevices > 0 ? $"{onlineMeters * 100 / totalDevices}%" : "—",
                    Status = onlineMeters == totalDevices ? "good" : "warning",
                    Subtitle = "Active devices"
                },
                EstimatedCost = new KpiCardDto
                {
                    Title = "Est. Monthly Cost",
                    Value = estimatedCost,
                    Unit = "Lakh Rs.",
                    Trend = $"Rs.52/kWh (B2)",
                    Status = "warning",
                    Subtitle = "Projected spend"
                },
                AvgPowerFactor = new KpiCardDto
                {
                    Title = "Avg Power Factor",
                    Value = avgPowerFactor,
                    Unit = "PF",
                    Trend = avgPowerFactor >= 0.95 ? "Excellent" : avgPowerFactor >= 0.85 ? "Good" : "Poor",
                    Status = avgPowerFactor >= 0.9 ? "good" : "warning",
                    Subtitle = "System efficiency"
                },
                Co2Emissions = new KpiCardDto
                {
                    Title = "CO₂ Emissions",
                    Value = co2Emissions,
                    Unit = "Metric Tons",
                    Trend = $"{co2Factor} kg/kWh",
                    Status = "good",
                    Subtitle = "Daily equivalent"
                }
            };

            var charts = await BuildChartDataAsync(today, tomorrow, todayData);
            charts.LoadProfile = await BuildLoadProfileAsync(today);
            charts.MonthlyTrend = await BuildMonthlyTrendAsync();

            var energyScore = BuildEnergyScore(todaysConsumption, peakDemand, avgPowerFactor, await _alarmRepository.GetActiveAlarmCount(), onlineMeters, totalDevices);

            var dashboard = new ExecutiveDashboardDto
            {
                KpiCards = kpiCards,
                Charts = charts,
                EnergyScore = energyScore
            };

            _cache.Set(cacheKey, dashboard, KpiCacheDuration);
            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            return new ExecutiveDashboardDto
            {
                KpiCards = GenerateFallbackKpiCards(),
                Charts = new ChartDataDto
                {
                    ConsumptionTrend = new(),
                    LocationBreakdown = new(),
                    TopConsumers = new()
                }
            };
        }
    }

    private async Task<ChartDataDto> BuildChartDataAsync(DateTime today, DateTime tomorrow, List<EnergyMeterData> todayData)
    {
        var consumptionTrend = todayData
            .Where(d => d.DateTime.HasValue)
            .GroupBy(d => d.DateTime!.Value.Hour)
            .Select(g => new ConsumptionChartPointDto
            {
                Time = today.AddHours(g.Key),
                Value = g.Sum(x => (double)(x.kWh ?? 0))
            })
            .OrderBy(c => c.Time)
            .ToList();

        var devices = await _deviceRepository.GetAllDevices();
        var deviceLookup = devices.Where(d => d.DeviceID.HasValue).GroupBy(d => d.DeviceID!.Value).ToDictionary(g => g.Key, g => g.First());

        var locationGroups = todayData
            .Where(d => d.MeterNo.HasValue && deviceLookup.ContainsKey(d.MeterNo.Value))
            .GroupBy(d => deviceLookup[d.MeterNo!.Value].Location)
            .Select(g => new { Location = g.Key, Total = g.Sum(x => (double)(x.kWh ?? 0)) })
            .OrderByDescending(g => g.Total)
            .ToList();

        var grandTotal = locationGroups.Sum(g => g.Total);
        var locationBreakdown = locationGroups.Select(g => new LocationBreakdownDto
        {
            Location = g.Location ?? "Unknown",
            Consumption = g.Total,
            Percentage = grandTotal > 0 ? Math.Round(g.Total / grandTotal * 100, 1) : 0
        }).ToList();

        var topConsumers = todayData
            .Where(d => d.MeterNo.HasValue)
            .GroupBy(d => d.MeterNo!.Value)
            .Select(g => new
            {
                MeterNo = g.Key,
                Total = g.Sum(x => (double)(x.kWh ?? 0))
            })
            .OrderByDescending(g => g.Total)
            .Take(10)
            .Select((g, i) => new TopConsumerDto
            {
                Rank = i + 1,
                Name = deviceLookup.ContainsKey(g.MeterNo) ? deviceLookup[g.MeterNo].DeviceName ?? $"Meter-{g.MeterNo}" : $"Meter-{g.MeterNo}",
                Consumption = Math.Round(g.Total, 1)
            })
            .ToList();

        return new ChartDataDto
        {
            ConsumptionTrend = consumptionTrend,
            LocationBreakdown = locationBreakdown,
            TopConsumers = topConsumers
        };
    }

    private async Task<MonthlyTrendDto> BuildMonthlyTrendAsync()
    {
        var result = new MonthlyTrendDto();
        var tariffRate = 52.0;
        var now = DateTime.Now;

        var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
        var data = await _energyMeterRepository.GetByDateRange(twelveMonthsAgo, now.AddDays(1));

        var grouped = data.Where(d => d.DateTime.HasValue)
            .GroupBy(d => new { d.DateTime!.Value.Year, d.DateTime.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Kwh = g.Sum(x => (double)(x.kWh ?? 0)) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToList();

        for (int i = 0; i < 12; i++)
        {
            var monthDate = twelveMonthsAgo.AddMonths(i);
            var match = grouped.FirstOrDefault(g => g.Year == monthDate.Year && g.Month == monthDate.Month);
            var kwh = match?.Kwh ?? 0;

            result.Months.Add(monthDate.ToString("MMM yy"));
            result.Values.Add(Math.Round(kwh, 0));
            result.Costs.Add(Math.Round(kwh * tariffRate, 0));
        }

        result.Average = result.Values.Count > 0 ? Math.Round(result.Values.Where(v => v > 0).DefaultIfEmpty(0).Average(), 0) : 0;
        return result;
    }

    private static EnergyScoreDto BuildEnergyScore(double consumption, double peakDemand, double avgPf, int alarmCount, int onlineMeters, int totalDevices)
    {
        var pfScore = avgPf >= 0.95 ? 30 : avgPf >= 0.90 ? 20 : avgPf >= 0.85 ? 10 : 0;
        var consumptionScore = 25; // baseline — no historical target yet
        var alarmScore = alarmCount == 0 ? 20 : alarmCount <= 3 ? 15 : alarmCount <= 10 ? 5 : 0;
        var pqScore = avgPf >= 0.92 ? 15 : avgPf >= 0.85 ? 10 : 5;
        var dataRatio = totalDevices > 0 ? (double)onlineMeters / totalDevices : 0;
        var dataScore = dataRatio >= 0.95 ? 10 : dataRatio >= 0.80 ? 5 : 0;

        return new EnergyScoreDto
        {
            Score = pfScore + consumptionScore + alarmScore + pqScore + dataScore,
            PfScore = pfScore,
            ConsumptionScore = consumptionScore,
            AlarmScore = alarmScore,
            PqScore = pqScore,
            DataScore = dataScore,
            Trend = alarmCount == 0 && avgPf >= 0.90 ? "improving" : alarmCount > 5 ? "declining" : "stable"
        };
    }

    private async Task<LoadProfileDto> BuildLoadProfileAsync(DateTime today)
    {
        var profile = new LoadProfileDto();
        var hours = Enumerable.Range(0, 24).ToList();
        profile.Hours = hours.Select(h => $"{h:D2}:00").ToList();

        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var weekAgo = today.AddDays(-7);

        var todayData = await _energyMeterRepository.GetByDateRange(today, tomorrow);
        var yesterdayData = await _energyMeterRepository.GetByDateRange(yesterday, today.AddTicks(-1));

        if (todayData.Count == 0 && yesterdayData.Count == 0)
        {
            var recentData = await _energyMeterRepository.GetByDateRange(today.AddDays(-30), tomorrow);
            if (recentData.Count > 0)
            {
                var dates = recentData.Where(d => d.DateTime.HasValue).Select(d => d.DateTime!.Value.Date).Distinct().OrderByDescending(d => d).ToList();
                if (dates.Count >= 1)
                {
                    var latestDate = dates[0];
                    today = latestDate;
                    tomorrow = latestDate.AddDays(1);
                    yesterday = latestDate.AddDays(-1);
                    weekAgo = latestDate.AddDays(-7);

                    todayData = recentData.Where(d => d.DateTime.HasValue && d.DateTime.Value.Date == latestDate).ToList();
                    yesterdayData = dates.Count >= 2
                        ? recentData.Where(d => d.DateTime.HasValue && d.DateTime.Value.Date == dates[1]).ToList()
                        : new();

                    profile.TodayLabel = latestDate.ToString("MMM dd");
                    profile.YesterdayLabel = dates.Count >= 2 ? dates[1].ToString("MMM dd") : "No data";
                }
            }
        }

        var todayByHour = todayData
            .Where(d => d.DateTime.HasValue)
            .GroupBy(d => d.DateTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => g.Sum(x => (double)(x.kWh ?? 0)));

        var yesterdayByHour = yesterdayData
            .Where(d => d.DateTime.HasValue)
            .GroupBy(d => d.DateTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => g.Sum(x => (double)(x.kWh ?? 0)));

        var weekData = await _energyMeterRepository.GetByDateRange(weekAgo, tomorrow);
        var weekDays = weekData
            .Where(d => d.DateTime.HasValue)
            .GroupBy(d => d.DateTime!.Value.Date)
            .Count();
        var weekByHour = weekData
            .Where(d => d.DateTime.HasValue)
            .GroupBy(d => d.DateTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => weekDays > 0 ? g.Sum(x => (double)(x.kWh ?? 0)) / weekDays : 0);

        foreach (var h in hours)
        {
            profile.Today.Add(Math.Round(todayByHour.GetValueOrDefault(h, 0), 1));
            profile.Yesterday.Add(Math.Round(yesterdayByHour.GetValueOrDefault(h, 0), 1));
            profile.WeeklyAverage.Add(Math.Round(weekByHour.GetValueOrDefault(h, 0), 1));
        }

        return profile;
    }

    private KpiCardsDto GenerateFallbackKpiCards()
    {
        return new KpiCardsDto
        {
            TodayConsumption = new KpiCardDto { Title = "Today's Consumption", Value = 0, Unit = "kWh", Trend = "—", Status = "warning", Subtitle = "No data" },
            CurrentLoad = new KpiCardDto { Title = "Current Load", Value = 0, Unit = "kW", Trend = "—", Status = "warning", Subtitle = "No data" },
            PeakDemand = new KpiCardDto { Title = "Peak Demand Today", Value = 0, Unit = "kW", Trend = "—", Status = "warning", Subtitle = "No data" },
            MonthlyTotal = new KpiCardDto { Title = "Monthly Total", Value = 0, Unit = "kWh", Trend = "—", Status = "warning", Subtitle = "No data" },
            OnlineMeters = new KpiCardDto { Title = "Online Meters", Value = 0, Unit = "/ 0", Trend = "—", Status = "warning", Subtitle = "No data" },
            EstimatedCost = new KpiCardDto { Title = "Est. Monthly Cost", Value = 0, Unit = "Lakh Rs.", Trend = "—", Status = "warning", Subtitle = "No data" },
            AvgPowerFactor = new KpiCardDto { Title = "Avg Power Factor", Value = 0, Unit = "PF", Trend = "—", Status = "warning", Subtitle = "No data" },
            Co2Emissions = new KpiCardDto { Title = "CO₂ Emissions", Value = 0, Unit = "Metric Tons", Trend = "—", Status = "warning", Subtitle = "No data" }
        };
    }
}
