namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class DemandCurveController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ILogger<DemandCurveController> _logger;

    public DemandCurveController(IEnergyMeterRepository meterRepo, ILogger<DemandCurveController> logger)
    {
        _meterRepo = meterRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d", double? contractedDemand = null)
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

            var kwValues = data.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).OrderByDescending(v => v).ToList();
            ViewBag.HasData = kwValues.Count > 0;
            if (kwValues.Count == 0) return View();

            var n = kwValues.Count;

            // Build duration curve: sample at every 1% to keep chart light
            var sampleCount = Math.Min(n, 100);
            var curvePoints = new List<double>();
            var curveLabels = new List<string>();

            for (int i = 0; i <= sampleCount; i++)
            {
                var pct = (double)i / sampleCount * 100;
                var index = Math.Min((int)(pct / 100 * n), n - 1);
                curvePoints.Add(Math.Round(kwValues[index], 1));
                curveLabels.Add($"{pct:F0}%");
            }

            var peakKw = kwValues.Max();
            var avgKw = kwValues.Average();
            var minKw = kwValues.Min();
            var loadFactor = peakKw > 0 ? Math.Round(avgKw / peakKw * 100, 1) : 0;

            // % of time above contracted demand (if specified)
            double? pctAboveContract = null;
            if (contractedDemand.HasValue && contractedDemand.Value > 0)
            {
                var countAbove = kwValues.Count(v => v > contractedDemand.Value);
                pctAboveContract = Math.Round((double)countAbove / n * 100, 1);
            }

            // Percentile markers — kwValues is sorted DESCENDING (peak first), so to get the
            // value below which P% of readings fall, we index from the END (ascending position).
            double Percentile(double p)
            {
                var idx = Math.Min((int)((100 - p) / 100 * n), n - 1);
                return Math.Round(kwValues[idx], 1);
            }

            ViewBag.PeakKw = Math.Round(peakKw, 1);
            ViewBag.AvgKw = Math.Round(avgKw, 1);
            ViewBag.MinKw = Math.Round(minKw, 1);
            ViewBag.LoadFactor = loadFactor;
            ViewBag.P10 = Percentile(10);
            ViewBag.P50 = Percentile(50);
            ViewBag.P90 = Percentile(90);
            ViewBag.PctAboveContract = pctAboveContract;
            ViewBag.ContractedDemand = contractedDemand;
            ViewBag.CurvePoints = JsonSerializer.Serialize(curvePoints);
            ViewBag.CurveLabels = JsonSerializer.Serialize(curveLabels);
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            ViewBag.DataPoints = n;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading demand duration curve");
            return View("Error");
        }
    }
}
