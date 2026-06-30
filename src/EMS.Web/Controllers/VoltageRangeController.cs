namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Columns: VoltL1N, VoltL2N, VoltL3N | Direct measurement
// Formula: % of periodic readings outside ANSI C84.1 Range A (+/-5% of nominal voltage)
// IMPORTANT ENGINEERING DISCLAIMER: this is NOT voltage sag/swell detection. True sag/swell
// per IEEE 1159 requires sub-cycle (sub-20ms) event-triggered RMS sampling, which this system's
// periodic polling architecture cannot provide -- even a 1-second poll interval cannot see a
// 100ms sag. This page only shows how often the *periodic snapshot* readings fall outside the
// nominal voltage range -- a much weaker, slower signal that will miss the vast majority of
// real power quality events. Labeled explicitly to avoid overclaiming capability the system
// does not have.
// Validatable against SCADA: yes, VoltL1N/L2N/L3N match the meter's own readings directly.
// Confidence: High for what it measures (periodic range compliance); explicitly NOT a substitute
// for true sag/swell/transient detection.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class VoltageRangeController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<VoltageRangeController> _logger;

    public VoltageRangeController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<VoltageRangeController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "7d", double rangePct = 5.0)
    {
        try
        {
            rangePct = Math.Clamp(rangePct, 1.0, 20.0);
            var nominalVoltage = await _settings.GetDoubleAsync("General.NominalVoltage", 230.0);

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

            var validData = data
                .Where(d => d.DateTime.HasValue && d.VoltL1N.HasValue && d.VoltL2N.HasValue && d.VoltL3N.HasValue)
                .OrderBy(d => d.DateTime)
                .ToList();

            ViewBag.HasData = validData.Count > 0;
            ViewBag.Range = range;
            ViewBag.RangePct = rangePct;
            ViewBag.NominalVoltage = nominalVoltage;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            if (validData.Count == 0) return View();

            var lowerBound = nominalVoltage * (1 - rangePct / 100.0);
            var upperBound = nominalVoltage * (1 + rangePct / 100.0);

            var outOfRange = validData.Where(d =>
                d.VoltL1N!.Value < lowerBound || d.VoltL1N.Value > upperBound ||
                d.VoltL2N!.Value < lowerBound || d.VoltL2N.Value > upperBound ||
                d.VoltL3N!.Value < lowerBound || d.VoltL3N.Value > upperBound
            ).ToList();

            var outOfRangePct = Math.Round((double)outOfRange.Count / validData.Count * 100, 2);

            // Estimate total out-of-range duration assuming uniform spacing between readings
            var totalSpanHours = validData.Count > 1 ? (validData.Last().DateTime!.Value - validData.First().DateTime!.Value).TotalHours : 0;
            var avgIntervalHours = validData.Count > 1 ? totalSpanHours / (validData.Count - 1) : 0;
            var estimatedOutOfRangeHours = outOfRange.Count * avgIntervalHours;

            ViewBag.OutOfRangeCount = outOfRange.Count;
            ViewBag.OutOfRangePct = outOfRangePct;
            ViewBag.EstimatedHours = Math.Round(estimatedOutOfRangeHours, 1);
            ViewBag.TotalReadings = validData.Count;
            ViewBag.LowerBound = Math.Round(lowerBound, 1);
            ViewBag.UpperBound = Math.Round(upperBound, 1);

            // Worst individual events (top 10 by deviation from nominal)
            var worstEvents = outOfRange
                .Select(d =>
                {
                    var maxDev = new[] { Math.Abs(d.VoltL1N!.Value - nominalVoltage), Math.Abs(d.VoltL2N!.Value - nominalVoltage), Math.Abs(d.VoltL3N!.Value - nominalVoltage) }.Max();
                    return new { d.DateTime, d.VoltL1N, d.VoltL2N, d.VoltL3N, MaxDeviation = Math.Round(maxDev, 1) };
                })
                .OrderByDescending(x => x.MaxDeviation)
                .Take(10)
                .Select(x => new { Time = x.DateTime!.Value.ToString("MMM dd, HH:mm"), x.VoltL1N, x.VoltL2N, x.VoltL3N, x.MaxDeviation })
                .ToList();
            ViewBag.WorstEvents = worstEvents;

            // Sample for chart
            var sampleEvery = Math.Max(1, validData.Count / 300);
            var sampled = validData.Where((d, i) => i % sampleEvery == 0).ToList();
            ViewBag.ChartLabels = JsonSerializer.Serialize(sampled.Select(d => d.DateTime!.Value.ToString(days <= 1 ? "HH:mm" : "MM/dd HH:mm")));
            ViewBag.ChartL1 = JsonSerializer.Serialize(sampled.Select(d => Math.Round(d.VoltL1N!.Value, 1)));
            ViewBag.ChartL2 = JsonSerializer.Serialize(sampled.Select(d => Math.Round(d.VoltL2N!.Value, 1)));
            ViewBag.ChartL3 = JsonSerializer.Serialize(sampled.Select(d => Math.Round(d.VoltL3N!.Value, 1)));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading voltage range analysis");
            return View("Error");
        }
    }
}
