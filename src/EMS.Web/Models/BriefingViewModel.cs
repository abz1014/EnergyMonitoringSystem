namespace EMS.Web.Models;

public class BriefingViewModel
{
    public string UserName { get; set; } = "";
    public string Greeting { get; set; } = "Good morning";
    public DateTime ReportDate { get; set; }

    // Plant Score
    public int PlantScore { get; set; }
    public string ScoreTrend { get; set; } = "stable";
    public int PreviousScore { get; set; }

    // Score Breakdown
    public int PfScore { get; set; }
    public int ConsumptionScore { get; set; }
    public int AlarmScore { get; set; }
    public int PowerQualityScore { get; set; }
    public int DataQualityScore { get; set; }

    // Key Metrics
    public double TotalConsumption { get; set; }
    public double ConsumptionChange { get; set; }
    public double SevenDayAverage { get; set; }
    public double PeakDemand { get; set; }
    public string PeakDemandTime { get; set; } = "";
    public double AveragePowerFactor { get; set; }
    public int ActiveAlarmCount { get; set; }
    public int TotalMeters { get; set; }
    public int ReportingMeters { get; set; }

    // Top Consumer
    public int TopConsumerMeterNo { get; set; }
    public string TopConsumerName { get; set; } = "";
    public double TopConsumerKwh { get; set; }

    // Auto-generated Insights
    public List<BriefingInsight> Insights { get; set; } = new();

    // Data availability
    public bool HasData { get; set; }
    public string DataDateLabel { get; set; } = "Yesterday";

    // Weekly Energy Health (Sprint 5)
    public WeeklyHealth Weekly { get; set; } = new();

    // Monthly Energy Report (Sprint 5)
    public MonthlyReport Monthly { get; set; } = new();
}

public class WeeklyHealth
{
    public string WeekLabel { get; set; } = "";
    public double TotalKwh { get; set; }
    public double AvgPf { get; set; }
    public int AlarmCount { get; set; }
    public bool HasPriorWeek { get; set; }
    public double PriorWeekKwh { get; set; }
    public double? WowChangePct { get; set; }
}

public class MonthlyReport
{
    public string MonthLabel { get; set; } = "";
    public double MonthToDateKwh { get; set; }
    public double EstimatedCost { get; set; }
    public double PeakKw { get; set; }
    public bool HasPriorMonth { get; set; }
    public double PriorMonthKwh { get; set; }
    public double? MomChangePct { get; set; }
}

public class BriefingInsight
{
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Color { get; set; } = "#94A3B8";
    public string ActionUrl { get; set; } = "";
    public string ActionLabel { get; set; } = "";
}
