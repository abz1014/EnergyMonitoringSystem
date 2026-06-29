namespace EMS.Core.Interfaces;

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
    public KpiCardsDto KpiCards { get; set; } = new();
    public ChartDataDto Charts { get; set; } = new();
}

public class KpiCardsDto
{
    public KpiCardDto TodayConsumption { get; set; } = new();
    public KpiCardDto CurrentLoad { get; set; } = new();
    public KpiCardDto PeakDemand { get; set; } = new();
    public KpiCardDto MonthlyTotal { get; set; } = new();
    public KpiCardDto OnlineMeters { get; set; } = new();
    public KpiCardDto EstimatedCost { get; set; } = new();
    public KpiCardDto AvgPowerFactor { get; set; } = new();
    public KpiCardDto Co2Emissions { get; set; } = new();
}

public class KpiCardDto
{
    public string Title { get; set; } = "";
    public double Value { get; set; }
    public string Unit { get; set; } = "";
    public string? Trend { get; set; }
    public string? Status { get; set; }
    public string? Subtitle { get; set; }
}

public class ChartDataDto
{
    public List<ConsumptionChartPointDto> ConsumptionTrend { get; set; } = new();
    public List<LocationBreakdownDto> LocationBreakdown { get; set; } = new();
    public List<TopConsumerDto> TopConsumers { get; set; } = new();
    public LoadProfileDto LoadProfile { get; set; } = new();
}

public class LoadProfileDto
{
    public List<double> Today { get; set; } = new();
    public List<double> Yesterday { get; set; } = new();
    public List<double> WeeklyAverage { get; set; } = new();
    public List<string> Hours { get; set; } = new();
    public string TodayLabel { get; set; } = "Today";
    public string YesterdayLabel { get; set; } = "Yesterday";
}

public class ConsumptionChartPointDto
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class LocationBreakdownDto
{
    public string Location { get; set; } = "";
    public double Consumption { get; set; }
    public double Percentage { get; set; }
}

public class TopConsumerDto
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public double Consumption { get; set; }
}
