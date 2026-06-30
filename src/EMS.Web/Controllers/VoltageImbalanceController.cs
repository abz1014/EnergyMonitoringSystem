namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class VoltageImbalanceController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ILogger<VoltageImbalanceController> _logger;

    public VoltageImbalanceController(IEnergyMeterRepository meterRepo, ILogger<VoltageImbalanceController> logger)
    {
        _meterRepo = meterRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "7d")
    {
        try
        {
            var days = range switch { "30d" => 30, "90d" => 90, _ => 7 };
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

            // Need all three phase voltages present to compute imbalance
            var validData = data.Where(d => d.DateTime.HasValue && d.VoltL1N.HasValue && d.VoltL2N.HasValue && d.VoltL3N.HasValue)
                .OrderBy(d => d.DateTime)
                .ToList();

            ViewBag.HasData = validData.Count > 0;
            if (validData.Count == 0) return View();

            var points = validData.Select(d =>
            {
                var v1 = d.VoltL1N!.Value;
                var v2 = d.VoltL2N!.Value;
                var v3 = d.VoltL3N!.Value;
                var vMax = Math.Max(v1, Math.Max(v2, v3));
                var vMin = Math.Min(v1, Math.Min(v2, v3));
                var vAvg = (v1 + v2 + v3) / 3.0;
                var imbalance = vAvg > 0 ? (vMax - vMin) / vAvg * 100.0 : 0;
                return new { d.DateTime, Imbalance = Math.Round(imbalance, 2) };
            }).ToList();

            var avgImbalance = points.Average(p => p.Imbalance);
            var maxImbalance = points.Max(p => p.Imbalance);
            var peakAt = points.First(p => p.Imbalance == maxImbalance).DateTime;

            // NEMA MG1 standard: 1% is good, >2% requires attention, >5% is severe (derating required)
            var withinLimit = points.Count(p => p.Imbalance <= 2.0);
            var pctWithinLimit = Math.Round((double)withinLimit / points.Count * 100, 1);
            var violationCount = points.Count(p => p.Imbalance > 2.0);

            ViewBag.AvgImbalance = Math.Round(avgImbalance, 2);
            ViewBag.MaxImbalance = Math.Round(maxImbalance, 2);
            ViewBag.PeakAt = peakAt?.ToString("MMM dd, HH:mm");
            ViewBag.PctWithinLimit = pctWithinLimit;
            ViewBag.ViolationCount = violationCount;
            ViewBag.TotalReadings = points.Count;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            // Sample for chart (max 200 points)
            var sampleEvery = Math.Max(1, points.Count / 200);
            var sampled = points.Where((p, i) => i % sampleEvery == 0).ToList();

            ViewBag.TrendLabels = JsonSerializer.Serialize(sampled.Select(p => p.DateTime!.Value.ToString("MM/dd HH:mm")));
            ViewBag.TrendValues = JsonSerializer.Serialize(sampled.Select(p => p.Imbalance));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading voltage imbalance analysis");
            return View("Error");
        }
    }
}
