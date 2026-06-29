namespace EMS.Web.Services;

using EMS.Core.Interfaces;

public class WebDashboardService : IDashboardService
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly IMonitoringDeviceRepository _deviceRepository;
    private readonly ILogger<WebDashboardService> _logger;

    public WebDashboardService(
        IEnergyMeterRepository energyMeterRepository,
        IMonitoringDeviceRepository deviceRepository,
        ILogger<WebDashboardService> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _deviceRepository = deviceRepository;
        _logger = logger;
    }

    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DashboardFilterDto filter)
    {
        try
        {
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
            ConsumptionTrend = GenerateMockConsumptionTrend(),
            LocationBreakdown = GenerateMockLocationBreakdown(),
            TopConsumers = GenerateMockTopConsumers()
        };

            return new ExecutiveDashboardDto
            {
                KpiCards = kpiCards,
                Charts = charts
            };
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

    private List<ConsumptionChartPointDto> GenerateMockConsumptionTrend()
    {
        var result = new List<ConsumptionChartPointDto>();
        var now = DateTime.Now.Date;

        for (int i = 0; i < 24; i++)
        {
            result.Add(new ConsumptionChartPointDto
            {
                Time = now.AddHours(i),
                Value = 400 + (Math.Sin(i / 24.0 * Math.PI * 2) * 150) + (new Random(i).NextDouble() * 50)
            });
        }

        return result;
    }

    private List<LocationBreakdownDto> GenerateMockLocationBreakdown()
    {
        return new List<LocationBreakdownDto>
        {
            new() { Location = "Production", Consumption = 15840, Percentage = 45 },
            new() { Location = "Warehouse", Consumption = 8800, Percentage = 25 },
            new() { Location = "Utilities", Consumption = 7040, Percentage = 20 },
            new() { Location = "Admin", Consumption = 3520, Percentage = 10 }
        };
    }

    private List<TopConsumerDto> GenerateMockTopConsumers()
    {
        return new List<TopConsumerDto>
        {
            new() { Rank = 1, Name = "Machine A", Consumption = 12.5 },
            new() { Rank = 2, Name = "Machine B", Consumption = 10.8 },
            new() { Rank = 3, Name = "Building 2", Consumption = 8.2 },
            new() { Rank = 4, Name = "HVAC", Consumption = 7.5 },
            new() { Rank = 5, Name = "Pump-A", Consumption = 6.3 },
            new() { Rank = 6, Name = "Compressor", Consumption = 5.8 },
            new() { Rank = 7, Name = "Boiler", Consumption = 5.2 },
            new() { Rank = 8, Name = "Lighting", Consumption = 4.1 },
            new() { Rank = 9, Name = "Process Ctrl", Consumption = 3.8 },
            new() { Rank = 10, Name = "Misc. Load", Consumption = 2.9 }
        };
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
            ConsumptionTrend = GenerateMockConsumptionTrend(),
            LocationBreakdown = GenerateMockLocationBreakdown(),
            TopConsumers = GenerateMockTopConsumers()
        };
    }
}
