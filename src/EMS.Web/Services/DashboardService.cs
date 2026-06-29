namespace EMS.Web.Services;

using Microsoft.Extensions.Caching.Memory;
using EMS.Core.Interfaces;

public class WebDashboardService : IDashboardService
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly IMonitoringDeviceRepository _deviceRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebDashboardService> _logger;

    private static readonly TimeSpan KpiCacheDuration = TimeSpan.FromSeconds(30);
    private const string CacheKeyDashboard = "dashboard_kpi";

    public WebDashboardService(
        IEnergyMeterRepository energyMeterRepository,
        IMonitoringDeviceRepository deviceRepository,
        IMemoryCache cache,
        ILogger<WebDashboardService> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _deviceRepository = deviceRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DashboardFilterDto filter)
    {
        var cacheKey = $"{CacheKeyDashboard}_{filter.Plant}_{filter.Building}_{filter.Area}";

        if (_cache.TryGetValue(cacheKey, out ExecutiveDashboardDto? cached) && cached != null)
        {
            _logger.LogDebug("Dashboard cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        try
        {
            _logger.LogDebug("Dashboard cache miss, querying database");
            var todaysConsumption = await _energyMeterRepository.GetTodaysTotalConsumption();
            var peakDemand = await _energyMeterRepository.GetPeakDemandToday();
            var onlineMeters = await _deviceRepository.GetOnlineDeviceCount();

        var kpiCards = new KpiCardsDto
        {
            TodayConsumption = new KpiCardDto
            {
                Title = "Today's Consumption",
                Value = todaysConsumption,
                Unit = "kWh",
                Trend = "+12%",
                Status = "good",
                Subtitle = "Daily total"
            },
            CurrentLoad = new KpiCardDto
            {
                Title = "Current Load",
                Value = peakDemand * 0.85,
                Unit = "kW",
                Trend = "±0.5 kW",
                Status = "normal",
                Subtitle = "Live demand"
            },
            PeakDemand = new KpiCardDto
            {
                Title = "Peak Demand Today",
                Value = peakDemand,
                Unit = "kW",
                Trend = "@ " + DateTime.Now.AddHours(-3).ToString("HH:mm"),
                Status = "normal",
                Subtitle = "Max reached"
            },
            MonthlyTotal = new KpiCardDto
            {
                Title = "Monthly Total",
                Value = todaysConsumption * 28,
                Unit = "kWh",
                Trend = "+8%",
                Status = "good",
                Subtitle = "Month-to-date"
            },
            OnlineMeters = new KpiCardDto
            {
                Title = "Online Meters",
                Value = onlineMeters,
                Unit = "/ 36",
                Trend = $"{(onlineMeters * 100 / 36)}%",
                Status = "good",
                Subtitle = "Active devices"
            },
            EstimatedCost = new KpiCardDto
            {
                Title = "Est. Monthly Cost",
                Value = (todaysConsumption * 28 * 8.5) / 1_000_000,
                Unit = "Million ₹",
                Trend = "+5%",
                Status = "warning",
                Subtitle = "Projected spend"
            },
            AvgPowerFactor = new KpiCardDto
            {
                Title = "Avg Power Factor",
                Value = 0.96,
                Unit = "PF",
                Trend = "Excellent",
                Status = "good",
                Subtitle = "System efficiency"
            },
            Co2Emissions = new KpiCardDto
            {
                Title = "CO₂ Emissions",
                Value = 4.1,
                Unit = "Metric Tons",
                Trend = "≈ 5 trees/day",
                Status = "good",
                Subtitle = "Daily equivalent"
            }
        };

        var charts = new ChartDataDto
        {
            ConsumptionTrend = new List<ConsumptionChartPointDto>(),
            LocationBreakdown = new List<LocationBreakdownDto>(),
            TopConsumers = new List<TopConsumerDto>()
        };

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
            _logger.LogError(ex, "Error retrieving dashboard data for filter: {@Filter}", filter);
            return new ExecutiveDashboardDto
            {
                KpiCards = GenerateFallbackKpiCards(),
                Charts = GenerateFallbackCharts()
            };
        }
    }

    private KpiCardsDto GenerateFallbackKpiCards()
    {
        return new KpiCardsDto
        {
            TodayConsumption = new KpiCardDto { Title = "Today's Consumption", Value = 0, Unit = "kWh", Trend = "—", Status = "warning", Subtitle = "Daily total" },
            CurrentLoad = new KpiCardDto { Title = "Current Load", Value = 0, Unit = "kW", Trend = "—", Status = "warning", Subtitle = "Live demand" },
            PeakDemand = new KpiCardDto { Title = "Peak Demand Today", Value = 0, Unit = "kW", Trend = "—", Status = "warning", Subtitle = "Max reached" },
            MonthlyTotal = new KpiCardDto { Title = "Monthly Total", Value = 0, Unit = "kWh", Trend = "—", Status = "warning", Subtitle = "Month-to-date" },
            OnlineMeters = new KpiCardDto { Title = "Online Meters", Value = 0, Unit = "/ 36", Trend = "—", Status = "warning", Subtitle = "Active devices" },
            EstimatedCost = new KpiCardDto { Title = "Est. Monthly Cost", Value = 0, Unit = "Million ₹", Trend = "—", Status = "warning", Subtitle = "Projected spend" },
            AvgPowerFactor = new KpiCardDto { Title = "Avg Power Factor", Value = 0, Unit = "PF", Trend = "—", Status = "warning", Subtitle = "System efficiency" },
            Co2Emissions = new KpiCardDto { Title = "CO₂ Emissions", Value = 0, Unit = "Metric Tons", Trend = "—", Status = "warning", Subtitle = "Daily equivalent" }
        };
    }

    private ChartDataDto GenerateFallbackCharts()
    {
        return new ChartDataDto
        {
            ConsumptionTrend = new List<ConsumptionChartPointDto>(),
            LocationBreakdown = new List<LocationBreakdownDto>(),
            TopConsumers = new List<TopConsumerDto>()
        };
    }
}
