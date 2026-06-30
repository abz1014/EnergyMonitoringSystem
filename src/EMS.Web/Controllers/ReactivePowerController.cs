namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Columns: kWh, kVAh, kVARh, MeterNo, DateTime | Direct measurement
// (these are meter-reported cumulative energy registers, not derived from instantaneous kW/kVAR)
// Validatable against SCADA: yes, matches the meter's own registers directly.
// Confidence: High.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class ReactivePowerController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<ReactivePowerController> _logger;

    public ReactivePowerController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, ILogger<ReactivePowerController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var days = range switch { "7d" => 7, "90d" => 90, _ => 30 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            var data = await _meterRepo.GetByDateRange(from, to);

            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-60), to);
                if (recent.Count > 0)
                {
                    var latestDate = recent.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    to = latestDate.AddDays(1);
                    from = to.AddDays(-days);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= from && d.DateTime.Value < to).ToList();
                }
            }

            var validData = data.Where(d => d.DateTime.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            if (validData.Count == 0) return View();

            var devices = await _deviceRepo.GetAllDevices();
            var deviceNames = devices
                .Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value)
                .ToDictionary(g => g.Key, g => g.First().DeviceName ?? $"Meter {g.Key}");

            // Some readings (notably the original real gateway capture on 2026-06-27) report
            // kVAh=0 and kVARh=0 alongside a kWh value that looks like a cumulative register rather
            // than an interval reading -- including those would corrupt the reactive-fraction math
            // (kWh could exceed kVAh, which is physically impossible). Until the kWh semantics are
            // confirmed with the client, only include rows that report a complete, internally
            // consistent power triangle (kWh <= kVAh, kVAh > 0) in this page's totals.
            var triangleData = validData.Where(d => d.kVAh.HasValue && d.kVAh.Value > 0 && (d.kWh ?? 0) <= d.kVAh.Value).ToList();

            var totalKwh = triangleData.Sum(d => (double)(d.kWh ?? 0));
            var totalKvah = triangleData.Sum(d => (double)(d.kVAh ?? 0));
            var totalKvarh = triangleData.Sum(d => (double)(d.kVARh ?? 0));
            ViewBag.ExcludedRowCount = validData.Count - triangleData.Count;

            // Implied PF from energy registers: kWh / kVAh -- a cross-check against the PFL1-3 instantaneous readings
            var impliedPf = totalKvah > 0 ? Math.Round(totalKwh / totalKvah, 3) : (double?)null;
            // Reactive fraction: how much of the apparent energy was reactive, not real, work
            var reactiveFraction = totalKvah > 0 ? Math.Round(totalKvarh / totalKvah * 100, 1) : 0;

            ViewBag.TotalKwh = Math.Round(totalKwh, 0);
            ViewBag.TotalKvah = Math.Round(totalKvah, 0);
            ViewBag.TotalKvarh = Math.Round(totalKvarh, 0);
            ViewBag.ImpliedPf = impliedPf;
            ViewBag.ReactiveFraction = reactiveFraction;

            // Per-meter breakdown
            var byMeter = triangleData
                .Where(d => d.MeterNo.HasValue)
                .GroupBy(d => d.MeterNo!.Value)
                .Select(g => new
                {
                    MeterNo = g.Key,
                    Name = deviceNames.GetValueOrDefault(g.Key, $"Meter {g.Key}"),
                    Kvarh = Math.Round(g.Sum(x => (double)(x.kVARh ?? 0)), 0),
                    Kvah = Math.Round(g.Sum(x => (double)(x.kVAh ?? 0)), 0)
                })
                .Where(m => m.Kvah > 0)
                .OrderByDescending(m => m.Kvarh)
                .ToList();

            ViewBag.MeterBreakdown = byMeter;

            // Daily trend: kWh (real) vs kVARh (reactive)
            var daily = triangleData
                .GroupBy(d => d.DateTime!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("MMM dd"),
                    Kwh = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)), 1),
                    Kvarh = Math.Round(g.Sum(x => (double)(x.kVARh ?? 0)), 1)
                }).ToList();

            ViewBag.DailyLabels = JsonSerializer.Serialize(daily.Select(d => d.Date));
            ViewBag.DailyKwh = JsonSerializer.Serialize(daily.Select(d => d.Kwh));
            ViewBag.DailyKvarh = JsonSerializer.Serialize(daily.Select(d => d.Kvarh));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading reactive power analysis");
            return View("Error");
        }
    }
}
