namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class PowerQualityController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<PowerQualityController> _logger;

    public PowerQualityController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, ILogger<PowerQualityController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? meterId = null, string timeframe = "24h")
    {
        try
        {
            var devices = await _deviceRepo.GetAllDevices();
            var energyMeters = devices
                .Where(d => d.DeviceID.HasValue && d.IsActive == 1 && (d.DeviceType ?? "").Contains("Energy", StringComparison.OrdinalIgnoreCase))
                .GroupBy(d => d.DeviceID!.Value).Select(g => g.First()).OrderBy(d => d.DeviceName).ToList();

            ViewBag.Meters = energyMeters;
            ViewBag.SelectedMeterId = meterId;
            ViewBag.Timeframe = timeframe;
            ViewBag.HasData = false;

            if (!meterId.HasValue && energyMeters.Count > 0)
                meterId = energyMeters.First().DeviceID;

            if (!meterId.HasValue) return View();

            var hours = timeframe switch { "6h" => 6, "24h" => 24, "7d" => 168, "30d" => 720, _ => 24 };
            var from = DateTime.Now.AddHours(-hours);
            var to = DateTime.Now.AddDays(1);

            var data = await _meterRepo.GetConsumptionRange(meterId.Value, from, to);

            if (data.Count == 0)
            {
                data = await _meterRepo.GetConsumptionRange(meterId.Value, DateTime.Now.AddDays(-30), to);
            }

            if (data.Count == 0) return View();

            var sorted = data.Where(d => d.DateTime.HasValue).OrderBy(d => d.DateTime).ToList();
            ViewBag.HasData = true;

            var meterDevice = energyMeters.FirstOrDefault(d => d.DeviceID == meterId);
            ViewBag.MeterName = sorted.First().MeterName ?? meterDevice?.DeviceName ?? $"Meter-{meterId}";
            ViewBag.SelectedMeterId = meterId;

            // Time labels
            var timeLabels = sorted.Select(d => d.DateTime!.Value.ToString(hours <= 24 ? "HH:mm" : "MMM dd HH:mm")).ToList();
            ViewBag.TimeLabels = JsonSerializer.Serialize(timeLabels);

            // PF Trend
            ViewBag.PFL1 = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.PFL1 ?? 0, 3)).ToList());
            ViewBag.PFL2 = JsonSerializer.Serialize(sorted.Select(d => Math.Round((double)(d.PFL2 ?? 0), 3)).ToList());
            ViewBag.PFL3 = JsonSerializer.Serialize(sorted.Select(d => Math.Round((double)(d.PFL3 ?? 0), 3)).ToList());

            // Voltage Trend
            ViewBag.VoltL1N = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.VoltL1N ?? 0, 1)).ToList());
            ViewBag.VoltL2N = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.VoltL2N ?? 0, 1)).ToList());
            ViewBag.VoltL3N = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.VoltL3N ?? 0, 1)).ToList());

            // Current Trend
            ViewBag.CurrentL1 = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.CurrentL1 ?? 0, 1)).ToList());
            ViewBag.CurrentL2 = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.CurrentL2 ?? 0, 1)).ToList());
            ViewBag.CurrentL3 = JsonSerializer.Serialize(sorted.Select(d => Math.Round(d.CurrentL3 ?? 0, 1)).ToList());

            // Frequency
            ViewBag.Frequency = JsonSerializer.Serialize(sorted.Select(d => Math.Round((double)(d.MFreq ?? 0), 2)).ToList());

            // Harmonics (latest reading)
            var latest = sorted.Last();
            ViewBag.HarmonicV = JsonSerializer.Serialize(new[] { Math.Round((double)(latest.HarmonicV1 ?? 0), 1), Math.Round((double)(latest.HarmonicV2 ?? 0), 1), Math.Round((double)(latest.HarmonicV3 ?? 0), 1) });
            ViewBag.HarmonicI = JsonSerializer.Serialize(new[] { Math.Round((double)(latest.HarmonicI1 ?? 0), 1), Math.Round((double)(latest.HarmonicI2 ?? 0), 1), Math.Round((double)(latest.HarmonicI3 ?? 0), 1) });

            // Voltage Imbalance trend
            var voltImbalance = sorted.Select(d =>
            {
                var v1 = d.VoltL1N ?? 0; var v2 = d.VoltL2N ?? 0; var v3 = d.VoltL3N ?? 0;
                var vals = new[] { v1, v2, v3 }.Where(v => v > 0).ToArray();
                if (vals.Length < 2) return 0.0;
                var avg = vals.Average();
                return avg > 0 ? Math.Round((vals.Max() - vals.Min()) / avg * 100, 2) : 0;
            }).ToList();
            ViewBag.VoltImbalance = JsonSerializer.Serialize(voltImbalance);

            // Current Imbalance trend
            var currImbalance = sorted.Select(d =>
            {
                var i1 = d.CurrentL1 ?? 0; var i2 = d.CurrentL2 ?? 0; var i3 = d.CurrentL3 ?? 0;
                var vals = new[] { i1, i2, i3 }.Where(v => v > 0).ToArray();
                if (vals.Length < 2) return 0.0;
                var avg = vals.Average();
                return avg > 0 ? Math.Round((vals.Max() - vals.Min()) / avg * 100, 2) : 0;
            }).ToList();
            ViewBag.CurrImbalance = JsonSerializer.Serialize(currImbalance);

            // Summary stats
            // 3-phase average PF (PFL1+PFL2+PFL3) for the KPI card -- see PowerFactorHelper.
            // Note: the per-phase chart above (PFL1/PFL2/PFL3 series) intentionally stays
            // separate per phase, since that's the more useful view for spotting single-phase issues.
            var pfVals = sorted.Select(EMS.Web.Services.PowerFactorHelper.ThreePhaseAverage).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            ViewBag.AvgPF = pfVals.Count > 0 ? Math.Round(pfVals.Average(), 3) : 0;
            ViewBag.MaxVoltImbalance = voltImbalance.Count > 0 ? Math.Round(voltImbalance.Max(), 2) : 0;
            ViewBag.MaxCurrImbalance = currImbalance.Count > 0 ? Math.Round(currImbalance.Max(), 2) : 0;
            ViewBag.MaxTHDV = Math.Max(Math.Max((double)(latest.HarmonicV1 ?? 0), (double)(latest.HarmonicV2 ?? 0)), (double)(latest.HarmonicV3 ?? 0));
            ViewBag.AvgFreq = sorted.Where(d => d.MFreq.HasValue && d.MFreq > 0).Select(d => (double)d.MFreq!.Value).DefaultIfEmpty(0).Average();
            ViewBag.DataPoints = sorted.Count;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading power quality for meter {MeterId}", meterId);
            return View("Error");
        }
    }
}
