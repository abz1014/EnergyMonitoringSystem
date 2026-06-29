namespace EMS.Core.Interfaces;

public interface IDashboardService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DashboardFilterDto filter);
}

public class DashboardFilterDto
{
    public string Plant { get; set; } = "All";
    public string Building { get; set; } = "All";
    public string Area { get; set; } = "All";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class ExecutiveDashboardDto
{
    public KpiCardsDto KpiCards { get; set; }
    public ChartDataDto Charts { get; set; }
}

public class KpiCardsDto
{
    public KpiCardDto TodayConsumption { get; set; }
    public KpiCardDto CurrentLoad { get; set; }
    public KpiCardDto PeakDemand { get; set; }
    public KpiCardDto MonthlyTotal { get; set; }
    public KpiCardDto OnlineMeters { get; set; }
    public KpiCardDto EstimatedCost { get; set; }
    public KpiCardDto AvgPowerFactor { get; set; }
    public KpiCardDto Co2Emissions { get; set; }
}

public class KpiCardDto
{
    public string Title { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; }
    public string? Trend { get; set; }
    public string? Status { get; set; }
    public string? Subtitle { get; set; }
}

public class ChartDataDto
{
    public List<ConsumptionChartPointDto> ConsumptionTrend { get; set; }
    public List<LocationBreakdownDto> LocationBreakdown { get; set; }
    public List<TopConsumerDto> TopConsumers { get; set; }
}

public class ConsumptionChartPointDto
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class LocationBreakdownDto
{
    public string Location { get; set; }
    public double Consumption { get; set; }
    public double Percentage { get; set; }
}

public class TopConsumerDto
{
    public int Rank { get; set; }
    public string Name { get; set; }
    public double Consumption { get; set; }
}
