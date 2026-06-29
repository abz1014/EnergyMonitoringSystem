namespace EMS.Core.Models;

public class EnergyMeterData
{
    public long SrNo { get; set; }
    public int? MeterNo { get; set; }
    public string? MeterName { get; set; }
    public string? MeterLocation { get; set; }
    public string? MeterBrand { get; set; }
    public string? MeterModel { get; set; }
    public string? Type1 { get; set; }
    public DateTime? DateTime { get; set; }

    public double? VoltL1N { get; set; }
    public double? VoltL2N { get; set; }
    public double? VoltL3N { get; set; }
    public double? VoltL1L2 { get; set; }
    public double? VoltL2L3 { get; set; }
    public double? VoltL1L3 { get; set; }

    public double? CurrentL1 { get; set; }
    public double? CurrentL2 { get; set; }
    public double? CurrentL3 { get; set; }

    public double? PowerL1 { get; set; }
    public double? PowerL2 { get; set; }
    public double? PowerL3 { get; set; }
    public double? kWtotal { get; set; }
    public double? kVAtotal { get; set; }
    public double? kVARtotal { get; set; }

    public double? PFL1 { get; set; }
    public double? PFL2 { get; set; }
    public double? PFL3 { get; set; }
    public double? MFreq { get; set; }

    public double? kWh { get; set; }
    public double? kVAh { get; set; }
    public double? kVARh { get; set; }

    public double? HarmonicV1 { get; set; }
    public double? HarmonicV2 { get; set; }
    public double? HarmonicV3 { get; set; }
    public double? HarmonicI1 { get; set; }
    public double? HarmonicI2 { get; set; }
    public double? HarmonicI3 { get; set; }
}


public class MonitoringDevice
{
    public long SrNo { get; set; }
    public int? DeviceID { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? Model { get; set; }
    public string? MasterDevice { get; set; }
    public string? IPAddress { get; set; }
    public string? Port { get; set; }
    public string? Protocols { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public int? IsActive { get; set; }
    public string? GroupName { get; set; }
}

public class Alarm
{
    public int AlarmID { get; set; }
    public int DeviceID { get; set; }
    public string DeviceName { get; set; } = "";
    public string? DeviceLocation { get; set; }
    public string TagName { get; set; } = "";
    public double TagValue { get; set; }
    public double Threshold { get; set; }
    public string Condition { get; set; } = "";
    public byte Severity { get; set; }
    public string Message { get; set; } = "";
    public bool IsActive { get; set; }
    public string? AckBy { get; set; }
    public DateTime? AckTime { get; set; }
    public DateTime CreatedAt { get; set; }

    public string SeverityName => Severity switch
    {
        1 => "info",
        2 => "warning",
        3 => "critical",
        _ => "unknown"
    };
}

public class FlowmeterData
{
    public long SrNo { get; set; }
    public string? DeviceName { get; set; }
    public string? IPAddress { get; set; }
    public DateTime? DateTime { get; set; }
    public int? MeterNo { get; set; }
    public string? InformationType { get; set; }
    public double? Data { get; set; }
    public string? DataUnit { get; set; }
    public string? Area { get; set; }
}

public class DeviceTag
{
    public int SrNo { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceModel { get; set; }
    public string TagName { get; set; } = "";
    public string? TagAddress { get; set; }
    public string? DataType { get; set; }
    public int? SizeBits { get; set; }
    public double? ScaleFactor { get; set; }
    public int? RegisterCount { get; set; }
}
