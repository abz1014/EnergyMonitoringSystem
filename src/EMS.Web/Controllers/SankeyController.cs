namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class SankeyController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<SankeyController> _logger;

    public SankeyController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, AppSettingsService settings, ILogger<SankeyController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

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

            var validData = data.Where(d => d.MeterNo.HasValue && d.kWh.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            if (validData.Count == 0) return View();

            var devices = await _deviceRepo.GetAllDevices();
            var deviceNames = devices
                .Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value)
                .ToDictionary(g => g.Key, g => g.First().DeviceName ?? $"Meter {g.Key}");

            var byMeter = validData
                .GroupBy(d => d.MeterNo!.Value)
                .Select(g => new
                {
                    MeterNo = g.Key,
                    Name = deviceNames.GetValueOrDefault(g.Key, $"Meter {g.Key}"),
                    Total = Math.Round(g.Sum(x => (double)x.kWh!.Value), 0)
                })
                .Where(m => m.Total > 0)
                .OrderByDescending(m => m.Total)
                .ToList();

            var grandTotal = byMeter.Sum(m => m.Total);

            // Source -> Meter -> [Peak/OffPeak split]
            var peakStart = await _settings.GetIntAsync("Tariff.PeakStartHour", 18);
            var peakEnd = await _settings.GetIntAsync("Tariff.PeakEndHour", 22);

            var nodes = new List<string> { "Total Supply" };
            var links = new List<object>();

            foreach (var m in byMeter)
            {
                nodes.Add(m.Name);
                links.Add(new { source = "Total Supply", target = m.Name, value = m.Total });

                var meterReadings = validData.Where(d => d.MeterNo == m.MeterNo).ToList();
                var peakTotal = Math.Round(meterReadings
                    .Where(d => d.DateTime.HasValue && IsPeakHour(d.DateTime.Value.Hour, peakStart, peakEnd))
                    .Sum(d => (double)d.kWh!.Value), 0);
                var offPeakTotal = Math.Round(m.Total - peakTotal, 0);

                if (peakTotal > 0)
                {
                    var peakLabel = $"{m.Name} (Peak)";
                    if (!nodes.Contains(peakLabel)) nodes.Add(peakLabel);
                    links.Add(new { source = m.Name, target = peakLabel, value = peakTotal });
                }
                if (offPeakTotal > 0)
                {
                    var offPeakLabel = $"{m.Name} (Off-Peak)";
                    if (!nodes.Contains(offPeakLabel)) nodes.Add(offPeakLabel);
                    links.Add(new { source = m.Name, target = offPeakLabel, value = offPeakTotal });
                }
            }

            ViewBag.GrandTotal = Math.Round(grandTotal, 0);
            ViewBag.MeterCount = byMeter.Count;
            ViewBag.Currency = currency;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            ViewBag.SankeyData = JsonSerializer.Serialize(new { nodes = nodes.Select(n => new { name = n }), links });

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sankey diagram");
            return View("Error");
        }
    }

    private static bool IsPeakHour(int hour, int peakStart, int peakEnd)
    {
        return peakStart > peakEnd
            ? (hour >= peakStart || hour < peakEnd)
            : (hour >= peakStart && hour < peakEnd);
    }
}
