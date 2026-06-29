namespace EMS.Web.Services;

using Microsoft.Extensions.Caching.Memory;
using EMS.Core.Interfaces;
using EMS.Core.Models;

public class WebDashboardService : IDashboardService
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly IMonitoringDeviceRepository _deviceRepository;
    private readonly IEnergyMeterLiveRepository _liveRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebDashboardService> _logger;

    private static readonly TimeSpan KpiCacheDuration = TimeSpan.FromSeconds(30);
    private const string CacheKeyDashboard = "dashboard_kpi";

    public WebDashboardService(
        IEnergyMeterRepository energyMeterRepository,
        IMonitoringDeviceRepository deviceRepository,
        IEnergyMeterLiveRepository liveRepository,
        IMemoryCache cache,
        ILogger<WebDashboardService> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _deviceRepository = deviceRepository;
        _liveRepository = liveRepository;
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

            var todaysConsumption = await _energyMeterRepository.GetTodaysTotalConsumption();
            var peakDemand = await _energyMeterRepository.GetPeakDemandToday();
            var onlineMeters = await _deviceRepository.GetOnlineDeviceCount();
            var totalDevices = (await _deviceRepository.GetAllDevices()).Count;

            var monthData = await _energyMeterRepository.GetByDateRange(monthStart, tomorrow);
            var monthlyTotal = monthData.Sum(d => d.kWh ?? 0);

            var liveMeters = await _liveRepository.GetAllLive();
            var avgPowerFactor = liveMeters.Count > 0
                ? liveMeters.Where(m => m.PFL1.HasValue && m.PFL1 > 0).Select(m => m.PFL1!.Value).DefaultIfEmpty(0).Average()
                : 0;

            var todayData = await _energyMeterRepository.GetByDateRange(today, tomorrow);
            var co2Factor = 0.82;
            var co2Emissions = (todaysConsumption / 1000.0) * co2Factor;

            var tariffRate = 8.5;
            var estimatedCost = (monthlyTotal * tariffRate) / 1_000_000;

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
                    Value = liveMeters.Sum(m => m.kWtotal ?? 0),
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
                    Unit = "Million ₹",
                    Trend = $"₹{tariffRate}/kWh",
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

            var dashboard = new ExecutiveDashboardDto
            {
                KpiCards = kpiCards,
                Charts = charts
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
            .GroupBy(d => d.DateTime.Hour)
            .Select(g => new ConsumptionChartPointDto
            {
                Time = today.AddHours(g.Key),
                Value = g.Sum(x => x.kWh ?? 0)
            })
            .OrderBy(c => c.Time)
            .ToList();

        var devices = await _deviceRepository.GetAllDevices();
        var deviceLookup = devices.ToDictionary(d => d.DeviceID, d => d);

        var locationGroups = todayData
            .Where(d => deviceLookup.ContainsKey(d.MeterNo))
            .GroupBy(d => deviceLookup[d.MeterNo].Building)
            .Select(g => new { Location = g.Key, Total = g.Sum(x => x.kWh ?? 0) })
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
            .GroupBy(d => d.MeterNo)
            .Select(g => new
            {
                MeterNo = g.Key,
                Total = g.Sum(x => x.kWh ?? 0)
            })
            .OrderByDescending(g => g.Total)
            .Take(10)
            .Select((g, i) => new TopConsumerDto
            {
                Rank = i + 1,
                Name = deviceLookup.ContainsKey(g.MeterNo) ? deviceLookup[g.MeterNo].DeviceName : $"Meter-{g.MeterNo}",
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

    private KpiCardsDto GenerateFallbackKpiCards()
    {
        return new KpiCardsDto
        {
            TodayConsumption = new KpiCardDto { Title = "Today's Consumption", Value = 0, Unit = "kWh", Trend = "—", Status = "warning", Subtitle = "No data" },
            CurrentLoad = new KpiCardDto { Title = "Current Load", Value = 0, Unit = "kW", Trend = "—", Status = "warning", Subtitle = "No data" },
            PeakDemand = new KpiCardDto { Title = "Peak Demand Today", Value = 0, Unit = "kW", Trend = "—", Status = "warning", Subtitle = "No data" },
            MonthlyTotal = new KpiCardDto { Title = "Monthly Total", Value = 0, Unit = "kWh", Trend = "—", Status = "warning", Subtitle = "No data" },
            OnlineMeters = new KpiCardDto { Title = "Online Meters", Value = 0, Unit = "/ 0", Trend = "—", Status = "warning", Subtitle = "No data" },
            EstimatedCost = new KpiCardDto { Title = "Est. Monthly Cost", Value = 0, Unit = "Million ₹", Trend = "—", Status = "warning", Subtitle = "No data" },
            AvgPowerFactor = new KpiCardDto { Title = "Avg Power Factor", Value = 0, Unit = "PF", Trend = "—", Status = "warning", Subtitle = "No data" },
            Co2Emissions = new KpiCardDto { Title = "CO₂ Emissions", Value = 0, Unit = "Metric Tons", Trend = "—", Status = "warning", Subtitle = "No data" }
        };
    }
}
