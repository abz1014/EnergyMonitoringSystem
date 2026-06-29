namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Models;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class MeterFaceplateController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly IDeviceTagRepository _tagRepo;
    private readonly ILogger<MeterFaceplateController> _logger;

    public MeterFaceplateController(
        IEnergyMeterRepository meterRepo,
        IMonitoringDeviceRepository deviceRepo,
        IDeviceTagRepository tagRepo,
        ILogger<MeterFaceplateController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _tagRepo = tagRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? meterId = null, string location = "")
    {
        try
        {
            var devices = await _deviceRepo.GetAllDevices();
            var distinctDevices = devices
                .Where(d => d.DeviceID.HasValue && d.IsActive == 1)
                .GroupBy(d => d.DeviceID!.Value)
                .Select(g => g.First())
                .ToList();

            var locations = distinctDevices
                .Where(d => !string.IsNullOrEmpty(d.Location))
                .Select(d => d.Location!)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            var filteredDevices = string.IsNullOrEmpty(location)
                ? distinctDevices
                : distinctDevices.Where(d => d.Location == location).ToList();

            var model = new MeterFaceplateViewModel
            {
                Locations = locations,
                SelectedLocation = location,
                AvailableMeters = filteredDevices.Select(d => new MeterPickerItem
                {
                    DeviceID = d.DeviceID!.Value,
                    DeviceName = d.DeviceName ?? $"Device-{d.DeviceID}",
                    Location = d.Location ?? "",
                    DeviceType = d.DeviceType ?? "",
                    Model = d.Model ?? ""
                }).OrderBy(m => m.DeviceName).ToList()
            };

            if (meterId.HasValue)
            {
                var device = distinctDevices.FirstOrDefault(d => d.DeviceID == meterId.Value);
                var latest = await _meterRepo.GetLatestReading(meterId.Value);
                var tags = device?.Model != null
                    ? await _tagRepo.GetTagsByDeviceModel(device.Model)
                    : new();

                if (latest != null)
                {
                    model.MeterId = meterId.Value;
                    model.MeterName = latest.MeterName ?? device?.DeviceName ?? $"Meter-{meterId}";
                    model.MeterLocation = latest.MeterLocation ?? device?.Location ?? "";
                    model.MeterModel = latest.MeterModel ?? device?.Model ?? "";
                    model.MeterBrand = latest.MeterBrand ?? "";
                    model.DeviceType = device?.DeviceType ?? "";
                    model.LastUpdated = latest.DateTime ?? DateTime.MinValue;
                    model.Status = (DateTime.Now - (latest.DateTime ?? DateTime.MinValue)).TotalMinutes < 5 ? "online" : "offline";

                    model.VoltL1N = latest.VoltL1N ?? 0;
                    model.VoltL2N = latest.VoltL2N ?? 0;
                    model.VoltL3N = latest.VoltL3N ?? 0;
                    model.VoltL1L2 = (double)(latest.VoltL1L2 ?? 0);
                    model.VoltL2L3 = (double)(latest.VoltL2L3 ?? 0);
                    model.VoltL1L3 = (double)(latest.VoltL1L3 ?? 0);

                    model.CurrentL1 = latest.CurrentL1 ?? 0;
                    model.CurrentL2 = latest.CurrentL2 ?? 0;
                    model.CurrentL3 = latest.CurrentL3 ?? 0;

                    model.PowerL1 = latest.PowerL1 ?? 0;
                    model.PowerL2 = latest.PowerL2 ?? 0;
                    model.PowerL3 = latest.PowerL3 ?? 0;
                    model.kWtotal = latest.kWtotal ?? 0;
                    model.kVAtotal = latest.kVAtotal ?? 0;
                    model.kVARtotal = latest.kVARtotal ?? 0;

                    model.PFL1 = latest.PFL1 ?? 0;
                    model.PFL2 = (double)(latest.PFL2 ?? 0);
                    model.PFL3 = (double)(latest.PFL3 ?? 0);
                    model.Frequency = (double)(latest.MFreq ?? 0);

                    model.kWh = (double)(latest.kWh ?? 0);
                    model.kVAh = (double)(latest.kVAh ?? 0);
                    model.kVARh = (double)(latest.kVARh ?? 0);

                    model.HarmonicV1 = (double)(latest.HarmonicV1 ?? 0);
                    model.HarmonicV2 = (double)(latest.HarmonicV2 ?? 0);
                    model.HarmonicV3 = (double)(latest.HarmonicV3 ?? 0);
                    model.HarmonicI1 = (double)(latest.HarmonicI1 ?? 0);
                    model.HarmonicI2 = (double)(latest.HarmonicI2 ?? 0);
                    model.HarmonicI3 = (double)(latest.HarmonicI3 ?? 0);

                    model.Tags = tags.GroupBy(t => t.TagName).Select(g => g.First()).ToList();
                }
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading meter faceplate for MeterId: {MeterId}", meterId);
            return View("Error");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Trend(int meterId, string parameter, int hours = 24)
    {
        try
        {
            if (hours > 720) hours = 720;
            var from = DateTime.Now.AddHours(-hours);
            var to = DateTime.Now.AddDays(1);

            var data = await _meterRepo.GetByDateRange(from, to);
            var meterData = data.Where(d => d.MeterNo == meterId && d.DateTime.HasValue).OrderBy(d => d.DateTime).ToList();

            if (meterData.Count == 0)
            {
                var recentData = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-30), to);
                meterData = recentData.Where(d => d.MeterNo == meterId && d.DateTime.HasValue).OrderBy(d => d.DateTime).ToList();
            }

            var points = meterData.Select(d => new
            {
                time = d.DateTime!.Value.ToString("yyyy-MM-dd HH:mm"),
                value = ExtractParameter(d, parameter)
            }).ToList();

            var values = points.Select(p => p.value).Where(v => v != 0).ToList();
            var thresholds = GetThresholds(parameter);

            return Json(new
            {
                points,
                min = values.Count > 0 ? Math.Round(values.Min(), 2) : 0,
                max = values.Count > 0 ? Math.Round(values.Max(), 2) : 0,
                avg = values.Count > 0 ? Math.Round(values.Average(), 2) : 0,
                unit = GetUnit(parameter),
                thresholdHigh = thresholds.high,
                thresholdLow = thresholds.low,
                parameterName = parameter
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trend for {Parameter} on meter {MeterId}", parameter, meterId);
            return Json(new { points = Array.Empty<object>(), error = ex.Message });
        }
    }

    private static double ExtractParameter(EMS.Core.Models.EnergyMeterData d, string param)
    {
        return param switch
        {
            "VoltL1N" => d.VoltL1N ?? 0,
            "VoltL2N" => d.VoltL2N ?? 0,
            "VoltL3N" => d.VoltL3N ?? 0,
            "CurrentL1" => d.CurrentL1 ?? 0,
            "CurrentL2" => d.CurrentL2 ?? 0,
            "CurrentL3" => d.CurrentL3 ?? 0,
            "kWtotal" => d.kWtotal ?? 0,
            "kVAtotal" => d.kVAtotal ?? 0,
            "kVARtotal" => d.kVARtotal ?? 0,
            "PFL1" => d.PFL1 ?? 0,
            "MFreq" => (double)(d.MFreq ?? 0),
            "kWh" => (double)(d.kWh ?? 0),
            "kVAh" => (double)(d.kVAh ?? 0),
            "kVARh" => (double)(d.kVARh ?? 0),
            "HarmonicV1" => (double)(d.HarmonicV1 ?? 0),
            "HarmonicV2" => (double)(d.HarmonicV2 ?? 0),
            "HarmonicV3" => (double)(d.HarmonicV3 ?? 0),
            "HarmonicI1" => (double)(d.HarmonicI1 ?? 0),
            "HarmonicI2" => (double)(d.HarmonicI2 ?? 0),
            "HarmonicI3" => (double)(d.HarmonicI3 ?? 0),
            "PowerL1" => d.PowerL1 ?? 0,
            "PowerL2" => d.PowerL2 ?? 0,
            "PowerL3" => d.PowerL3 ?? 0,
            _ => 0
        };
    }

    private static string GetUnit(string param)
    {
        if (param.StartsWith("Volt")) return "V";
        if (param.StartsWith("Current")) return "A";
        if (param.StartsWith("Power") || param == "kWtotal") return "kW";
        if (param == "kVAtotal") return "kVA";
        if (param == "kVARtotal") return "kVAR";
        if (param.StartsWith("PF")) return "PF";
        if (param == "MFreq") return "Hz";
        if (param == "kWh") return "kWh";
        if (param == "kVAh") return "kVAh";
        if (param == "kVARh") return "kVARh";
        if (param.StartsWith("Harmonic")) return "%";
        return "";
    }

    private static (double high, double low) GetThresholds(string param)
    {
        if (param.StartsWith("Volt") && param.Contains("N")) return (253, 207);
        if (param.StartsWith("PF")) return (1.0, 0.90);
        if (param == "MFreq") return (50.5, 49.5);
        if (param.StartsWith("Harmonic") && param.Contains("V")) return (5.0, 0);
        if (param.StartsWith("Harmonic") && param.Contains("I")) return (8.0, 0);
        return (0, 0);
    }
}
