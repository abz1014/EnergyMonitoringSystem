namespace EMS.Web.Models;

public class EnergyAnalysisViewModel
{
    public string Timeframe { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<(DateTime, double)> ConsumptionData { get; set; }
    public List<(DateTime, double)> ComparisonData { get; set; }
    public Dictionary<string, double> Statistics { get; set; }
    public double Peak { get; set; }
    public double Average { get; set; }
    public double Minimum { get; set; }
}
