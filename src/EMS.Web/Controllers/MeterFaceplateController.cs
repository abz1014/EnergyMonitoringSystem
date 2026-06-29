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
}
