namespace EMS.Core.Models;

public class EnergyMeterData
{
    public int Id { get; set; }
    public int MeterNo { get; set; }
    public DateTime DateTime { get; set; }

    // Voltages (Line-to-Neutral)
    public double? VoltL1N { get; set; }
    public double? VoltL2N { get; set; }
    public double? VoltL3N { get; set; }

    // Voltages (Line-to-Line)
    public double? VoltL1L2 { get; set; }
    public double? VoltL2L3 { get; set; }
    public double? VoltL3L1 { get; set; }

    // Currents
    public double? CurrentL1 { get; set; }
    public double? CurrentL2 { get; set; }
    public double? CurrentL3 { get; set; }

    // Active Power (kW)
    public double? kWL1 { get; set; }
    public double? kWL2 { get; set; }
    public double? kWL3 { get; set; }
    public double? kWtotal { get; set; }

    // Reactive Power (kVAR)
    public double? kVARtotal { get; set; }

    // Apparent Power (kVA)
    public double? kVAtotal { get; set; }

    // Power Factors
    public double? PFL1 { get; set; }
    public double? PFL2 { get; set; }
    public double? PFL3 { get; set; }

    // Energy Counters
    public double? kWh { get; set; }
    public double? kVAh { get; set; }
    public double? kVARh { get; set; }

    // Harmonics (THD - Total Harmonic Distortion)
    public double? THD_VoltL1 { get; set; }
    public double? THD_VoltL2 { get; set; }
    public double? THD_VoltL3 { get; set; }
    public double? THD_CurrentL1 { get; set; }
    public double? THD_CurrentL2 { get; set; }
    public double? THD_CurrentL3 { get; set; }

    // Frequency
    public double? MFreq { get; set; }
}

public class EnergyMeterLive
{
    public int Id { get; set; }
    public int MeterNo { get; set; }
    public DateTime DateTime { get; set; }
    public bool IsValid { get; set; }

    public double? VoltL1N { get; set; }
    public double? VoltL2N { get; set; }
    public double? VoltL3N { get; set; }
    public double? CurrentL1 { get; set; }
    public double? CurrentL2 { get; set; }
    public double? CurrentL3 { get; set; }
    public double? kWtotal { get; set; }
    public double? PFL1 { get; set; }
    public double? MFreq { get; set; }
}

public class MonitoringDevice
{
    public int Id { get; set; }
    public int DeviceID { get; set; }
    public string DeviceName { get; set; }
    public string Type { get; set; }
    public string Location { get; set; }
    public string Building { get; set; }
    public string Plant { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Alarm
{
    public int Id { get; set; }
    public int MeterNo { get; set; }
    public string DeviceName { get; set; }
    public string Parameter { get; set; }
    public double? CurrentValue { get; set; }
    public double? Threshold { get; set; }
    public string Severity { get; set; }
    public string Message { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AckBy { get; set; }
    public DateTime? AckTime { get; set; }
}

public class FlowmeterData
{
    public int Id { get; set; }
    public int DeviceID { get; set; }
    public DateTime DateTime { get; set; }
    public double? FlowRate { get; set; }
    public double? TotalFlow { get; set; }
    public string? DeviceName { get; set; }
}

public class DeviceTag
{
    public int Id { get; set; }
    public int DeviceID { get; set; }
    public string TagName { get; set; }
    public string RegisterAddress { get; set; }
    public string DataType { get; set; }
}
