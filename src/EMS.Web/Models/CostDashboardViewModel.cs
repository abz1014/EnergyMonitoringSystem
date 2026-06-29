namespace EMS.Web.Models;

public class CostDashboardViewModel
{
    public string Currency { get; set; } = "₹";
    public double DefaultRate { get; set; }
    public double PeakRate { get; set; }
    public double OffPeakRate { get; set; }

    // Summary
    public double TodayCost { get; set; }
    public double WeekCost { get; set; }
    public double MonthCost { get; set; }
    public double ProjectedMonthlyCost { get; set; }

    // Cost by Meter
    public List<MeterCostItem> CostByMeter { get; set; } = new();

    // Cost by Location
    public List<LocationCostItem> CostByLocation { get; set; } = new();

    // Daily Trend (30 days)
    public List<DailyCostItem> DailyTrend { get; set; } = new();

    // Peak vs Off-Peak
    public double PeakCost { get; set; }
    public double OffPeakCost { get; set; }
    public double PeakKwh { get; set; }
    public double OffPeakKwh { get; set; }

    public bool HasData { get; set; }
    public string DataPeriod { get; set; } = "";
}

public class MeterCostItem
{
    public string MeterName { get; set; } = "";
    public string Location { get; set; } = "";
    public double Kwh { get; set; }
    public double Cost { get; set; }
}

public class LocationCostItem
{
    public string Location { get; set; } = "";
    public double Cost { get; set; }
    public double Percentage { get; set; }
}

public class DailyCostItem
{
    public string Date { get; set; } = "";
    public double Cost { get; set; }
    public double Kwh { get; set; }
}
