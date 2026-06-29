namespace EMS.Web.Models;

using EMS.Core.Models;

public class MeterFaceplateViewModel
{
    public int MeterId { get; set; }
    public string MeterName { get; set; } = "";
    public string MeterLocation { get; set; } = "";
    public string MeterModel { get; set; } = "";
    public string MeterBrand { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Status { get; set; } = "unknown";
    public DateTime LastUpdated { get; set; }

    // 3-Phase Voltages
    public double VoltL1N { get; set; }
    public double VoltL2N { get; set; }
    public double VoltL3N { get; set; }
    public double VoltL1L2 { get; set; }
    public double VoltL2L3 { get; set; }
    public double VoltL1L3 { get; set; }

    // 3-Phase Currents
    public double CurrentL1 { get; set; }
    public double CurrentL2 { get; set; }
    public double CurrentL3 { get; set; }

    // Power
    public double PowerL1 { get; set; }
    public double PowerL2 { get; set; }
    public double PowerL3 { get; set; }
    public double kWtotal { get; set; }
    public double kVAtotal { get; set; }
    public double kVARtotal { get; set; }

    // Power Factor
    public double PFL1 { get; set; }
    public double PFL2 { get; set; }
    public double PFL3 { get; set; }

    // Frequency
    public double Frequency { get; set; }

    // Energy Counters
    public double kWh { get; set; }
    public double kVAh { get; set; }
    public double kVARh { get; set; }

    // Harmonics
    public double HarmonicV1 { get; set; }
    public double HarmonicV2 { get; set; }
    public double HarmonicV3 { get; set; }
    public double HarmonicI1 { get; set; }
    public double HarmonicI2 { get; set; }
    public double HarmonicI3 { get; set; }

    // Device Tags
    public List<DeviceTag> Tags { get; set; } = new();

    // Available meters for picker
    public List<MeterPickerItem> AvailableMeters { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public string SelectedLocation { get; set; } = "";
}

public class MeterPickerItem
{
    public int DeviceID { get; set; }
    public string DeviceName { get; set; } = "";
    public string Location { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Model { get; set; } = "";
}
