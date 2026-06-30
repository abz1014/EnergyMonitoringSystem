namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Columns: VoltL1L2, VoltL2L3, VoltL1L3 | Direct measurement
// Formula: NEMA MG1 imbalance % = (Vmax - Vmin) / Vavg * 100, applied to line-to-line
// (delta-side) voltages -- distinct from the existing Voltage Imbalance page, which uses
// line-to-neutral (VoltL1N/L2N/L3N, wye-side) readings. The two can diverge: a wye-side
// imbalance doesn't always show up the same way on the delta side and vice versa, so
// they're kept as separate analyses rather than merged.
// Validatable against SCADA: yes, matches the meter's own line-to-line readings directly.
// Confidence: High.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class LineVoltageBalanceController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ILogger<LineVoltageBalanceController> _logger;

    public LineVoltageBalanceController(IEnergyMeterRepository meterRepo, ILogger<LineVoltageBalanceController> logger)
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

            var validData = data.Where(d => d.DateTime.HasValue && d.VoltL1L2.HasValue && d.VoltL2L3.HasValue && d.VoltL1L3.HasValue)
                .OrderBy(d => d.DateTime)
                .ToList();

            ViewBag.HasData = validData.Count > 0;
            if (validData.Count == 0) return View();

            var points = validData.Select(d =>
            {
                var v12 = (double)d.VoltL1L2!.Value;
                var v23 = (double)d.VoltL2L3!.Value;
                var v13 = (double)d.VoltL1L3!.Value;
                var vMax = Math.Max(v12, Math.Max(v23, v13));
                var vMin = Math.Min(v12, Math.Min(v23, v13));
                var vAvg = (v12 + v23 + v13) / 3.0;
                var imbalance = vAvg > 0 ? (vMax - vMin) / vAvg * 100.0 : 0;
                return new { d.DateTime, Imbalance = Math.Round(imbalance, 2), VAvg = Math.Round(vAvg, 1) };
            }).ToList();

            var avgImbalance = points.Average(p => p.Imbalance);
            var maxImbalance = points.Max(p => p.Imbalance);
            var peakAt = points.First(p => p.Imbalance == maxImbalance).DateTime;
            var avgLineVoltage = points.Average(p => p.VAvg);

            // NEMA MG1: <=1% good, >2% requires attention, >5% severe (same standard applied to line-neutral page)
            var withinLimit = points.Count(p => p.Imbalance <= 2.0);
            var pctWithinLimit = Math.Round((double)withinLimit / points.Count * 100, 1);
            var violationCount = points.Count(p => p.Imbalance > 2.0);

            ViewBag.AvgImbalance = Math.Round(avgImbalance, 2);
            ViewBag.MaxImbalance = Math.Round(maxImbalance, 2);
            ViewBag.PeakAt = peakAt?.ToString("MMM dd, HH:mm");
            ViewBag.AvgLineVoltage = Math.Round(avgLineVoltage, 1);
            ViewBag.PctWithinLimit = pctWithinLimit;
            ViewBag.ViolationCount = violationCount;
            ViewBag.TotalReadings = points.Count;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            var sampleEvery = Math.Max(1, points.Count / 200);
            var sampled = points.Where((p, i) => i % sampleEvery == 0).ToList();

            ViewBag.TrendLabels = JsonSerializer.Serialize(sampled.Select(p => p.DateTime!.Value.ToString("MM/dd HH:mm")));
            ViewBag.TrendValues = JsonSerializer.Serialize(sampled.Select(p => p.Imbalance));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading line-to-line voltage balance");
            return View("Error");
        }
    }
}
