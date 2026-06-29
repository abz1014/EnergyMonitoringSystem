namespace EMS.Core.Interfaces;

public class LiveMonitoringFilterDto
{
    public string Plant { get; set; } = "All";
    public string Building { get; set; } = "All";
    public string Status { get; set; } = "all";
    public bool IncludeSparklines { get; set; } = true;
}

public class LiveMonitoringResponseDto
{
    public StatusSummaryDto StatusSummary { get; set; } = new();
    public List<MeterLiveDto> Meters { get; set; } = new();
    public List<AlarmDto> ActiveAlarms { get; set; } = new();
}

public class StatusSummaryDto
{
    public int Online { get; set; }
    public int Warning { get; set; }
    public int Offline { get; set; }
    public int Unknown { get; set; }
}

public class MeterLiveDto
{
    public int MeterId { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "unknown";
    public VoltageReadingDto Voltage { get; set; } = new();
    public CurrentReadingDto Current { get; set; } = new();
    public PowerReadingDto Power { get; set; } = new();
    public double PowerFactor { get; set; }
    public double Frequency { get; set; }
    public List<double> Sparkline { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class VoltageReadingDto
{
    public double L1 { get; set; }
    public double L2 { get; set; }
    public double L3 { get; set; }
    public string Unit { get; set; } = "V";
}

public class CurrentReadingDto
{
    public double L1 { get; set; }
    public double L2 { get; set; }
    public double L3 { get; set; }
    public string Unit { get; set; } = "A";
}

public class PowerReadingDto
{
    public double kW { get; set; }
    public double kVAR { get; set; }
    public double kVA { get; set; }
    public string Unit { get; set; } = "kW";
}

public class AlarmDto
{
    public int Id { get; set; }
    public int MeterId { get; set; }
    public string DeviceName { get; set; } = "";
    public string Parameter { get; set; } = "";
    public double? CurrentValue { get; set; }
    public double? Threshold { get; set; }
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
