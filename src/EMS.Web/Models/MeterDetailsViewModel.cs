namespace EMS.Web.Models;

public class MeterDetailsViewModel
{
    public int MeterId { get; set; }
    public string MeterName { get; set; }
    public string Timeframe { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public DateTime LastUpdated { get; set; }
    public MeterReadingViewModel CurrentValues { get; set; }
    public Dictionary<string, double> Statistics { get; set; }
    public double TotalConsumption { get; set; }
    public double AverageConsumption { get; set; }
    public double PeakConsumption { get; set; }
    public double MinConsumption { get; set; }
}

public class MeterReadingViewModel
{
    public double VoltageL1 { get; set; }
    public double VoltageL2 { get; set; }
    public double VoltageL3 { get; set; }
    public double CurrentL1 { get; set; }
    public double CurrentL2 { get; set; }
    public double CurrentL3 { get; set; }
    public double PowerkW { get; set; }
    public double PowerFactor { get; set; }
    public double Frequency { get; set; }
}
