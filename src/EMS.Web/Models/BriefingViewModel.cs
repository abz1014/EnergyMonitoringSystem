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
    public string TopConsumerName { get; set; } = "";
    public double TopConsumerKwh { get; set; }

    // Auto-generated Insights
    public List<BriefingInsight> Insights { get; set; } = new();

    // Data availability
    public bool HasData { get; set; }
    public string DataDateLabel { get; set; } = "Yesterday";
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
