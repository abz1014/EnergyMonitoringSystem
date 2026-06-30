namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Column: MFreq | Direct measurement
// Nominal frequency is an admin-configured setting (General.NominalFrequency, default 50Hz),
// not a measured value -- it's the grid's rated frequency, a known constant for the site.
// Tolerance band (+/-0.5Hz default) is a generic engineering threshold, not a specific grid
// code citation -- sites with stricter requirements should adjust it in Settings.
// Validatable against SCADA: yes, MFreq matches the meter's own frequency reading directly.
// Confidence: High (direct measurement, simple deviation calculation).
[Authorize(Roles = "Admin,Operator,Viewer")]
public class FrequencyController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<FrequencyController> _logger;

    public FrequencyController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<FrequencyController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "7d", double tolerance = 0.5)
    {
        try
        {
            tolerance = Math.Clamp(tolerance, 0.05, 5.0);
            var nominalFreq = await _settings.GetDoubleAsync("General.NominalFrequency", 50.0);

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
                .Where(d => d.DateTime.HasValue && d.MFreq.HasValue && d.MFreq.Value > 0)
                .OrderBy(d => d.DateTime)
                .ToList();

            ViewBag.HasData = validData.Count > 0;
            ViewBag.Range = range;
            ViewBag.Tolerance = tolerance;
            ViewBag.NominalFreq = nominalFreq;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            if (validData.Count == 0) return View();

            var freqValues = validData.Select(d => (double)d.MFreq!.Value).ToList();
            var avgFreq = freqValues.Average();
            var minFreq = freqValues.Min();
            var maxFreq = freqValues.Max();
            var maxDeviation = Math.Max(Math.Abs(maxFreq - (double)nominalFreq), Math.Abs(minFreq - (double)nominalFreq));

            var lowerBound = (double)nominalFreq - tolerance;
            var upperBound = (double)nominalFreq + tolerance;
            var excursions = validData.Count(d => (double)d.MFreq!.Value < lowerBound || (double)d.MFreq!.Value > upperBound);
            var excursionPct = validData.Count > 0 ? Math.Round((double)excursions / validData.Count * 100, 2) : 0;

            ViewBag.AvgFreq = Math.Round(avgFreq, 3);
            ViewBag.MinFreq = Math.Round(minFreq, 3);
            ViewBag.MaxFreq = Math.Round(maxFreq, 3);
            ViewBag.MaxDeviation = Math.Round(maxDeviation, 3);
            ViewBag.ExcursionCount = excursions;
            ViewBag.ExcursionPct = excursionPct;
            ViewBag.TotalReadings = validData.Count;

            // Sample for chart (max 300 points)
            var sampleEvery = Math.Max(1, validData.Count / 300);
            var sampled = validData.Where((d, i) => i % sampleEvery == 0).ToList();

            ViewBag.ChartLabels = JsonSerializer.Serialize(sampled.Select(d => d.DateTime!.Value.ToString(days <= 1 ? "HH:mm" : "MM/dd HH:mm")));
            ViewBag.ChartValues = JsonSerializer.Serialize(sampled.Select(d => Math.Round((double)d.MFreq!.Value, 3)));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading frequency stability analysis");
            return View("Error");
        }
    }
}
