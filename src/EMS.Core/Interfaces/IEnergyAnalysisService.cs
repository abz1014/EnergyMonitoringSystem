namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IEnergyAnalysisService
{
    Task<EnergyAnalysisResultDto> GetAnalysisAsync(string timeframe, string metric, string compareWith);
}

public class EnergyAnalysisResultDto
{
    public string Timeframe { get; set; } = "daily";
    public string Metric { get; set; } = "kwh";
    public string CompareWith { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<(DateTime, double)> ConsumptionData { get; set; } = new();
    public List<(DateTime, double)> ComparisonData { get; set; } = new();
    public Dictionary<string, double> Statistics { get; set; } = new();
    public double Peak { get; set; }
    public double Average { get; set; }
    public double Minimum { get; set; }
}
