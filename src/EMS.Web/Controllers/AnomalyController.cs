namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class AnomalyController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ILogger<AnomalyController> _logger;

    public AnomalyController(IEnergyMeterRepository meterRepo, ILogger<AnomalyController> logger)
    {
        _meterRepo = meterRepo;
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

            // Aggregate to hourly totals across all meters (system-wide), since anomaly detection
            // is most meaningful on a consistent, complete time series rather than per-meter gaps
            var hourly = data
                .Where(d => d.DateTime.HasValue && d.kWtotal.HasValue)
                .GroupBy(d => new DateTime(d.DateTime!.Value.Year, d.DateTime.Value.Month, d.DateTime.Value.Day, d.DateTime.Value.Hour, 0, 0))
                .Select(g => new { Hour = g.Key, Total = g.Sum(x => x.kWtotal!.Value) })
                .OrderBy(g => g.Hour)
                .ToList();

            ViewBag.HasData = hourly.Count >= 5; // need a reasonable sample for stddev to be meaningful
            if (hourly.Count < 5) return View();

            var values = hourly.Select(h => (double)h.Total).ToList();
            var mean = values.Average();
            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            var stdDev = Math.Sqrt(variance);
            var threshold = mean + 2 * stdDev;
            var lowerThreshold = Math.Max(0, mean - 2 * stdDev);

            var anomalies = hourly
                .Where(h => (double)h.Total > threshold || (double)h.Total < lowerThreshold)
                .Select(h => new
                {
                    h.Hour,
                    Value = Math.Round((double)h.Total, 1),
                    Type = (double)h.Total > threshold ? "Spike" : "Drop",
                    DeviationPct = stdDev > 0 ? Math.Round(((double)h.Total - mean) / mean * 100, 1) : 0
                })
                .OrderByDescending(a => Math.Abs(a.DeviationPct))
                .ToList();

            ViewBag.Mean = Math.Round(mean, 1);
            ViewBag.StdDev = Math.Round(stdDev, 1);
            ViewBag.UpperThreshold = Math.Round(threshold, 1);
            ViewBag.LowerThreshold = Math.Round(lowerThreshold, 1);
            ViewBag.AnomalyCount = anomalies.Count;
            ViewBag.TotalHours = hourly.Count;
            ViewBag.AnomalyPct = Math.Round((double)anomalies.Count / hourly.Count * 100, 1);
            ViewBag.Anomalies = anomalies.Take(20).Select(a => new
            {
                Time = a.Hour.ToString("MMM dd, HH:mm"),
                a.Value,
                a.Type,
                a.DeviationPct
            }).ToList();

            // Full series for chart, sampled if too large
            var sampleEvery = Math.Max(1, hourly.Count / 300);
            var sampled = hourly.Where((h, i) => i % sampleEvery == 0).ToList();
            var anomalyHourSet = anomalies.Select(a => a.Hour).ToHashSet();

            ViewBag.ChartLabels = JsonSerializer.Serialize(sampled.Select(h => h.Hour.ToString("MM/dd HH:mm")));
            ViewBag.ChartValues = JsonSerializer.Serialize(sampled.Select(h => Math.Round((double)h.Total, 1)));
            ViewBag.ChartAnomalyFlags = JsonSerializer.Serialize(sampled.Select(h => anomalyHourSet.Contains(h.Hour)));

            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading anomaly detection");
            return View("Error");
        }
    }
}
