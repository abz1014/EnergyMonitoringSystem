namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

// Data Provenance:
// Table: tblEnergyMetersData | Column: kWh | Direct measurement
// Setting: General.CO2Factor (kg CO2e per kWh) -- a configured emission factor constant,
// not a measured value. This is a known limitation of grid-average emission factors: it
// assumes a fixed kg CO2e/kWh regardless of actual generation mix at the time of consumption.
// Formula: Carbon (kgCO2e) = kWh x CO2Factor
// Validatable against SCADA: kWh yes; CO2Factor is a configured assumption, not measured --
// should be sourced from the utility's published grid emission factor, not invented.
// Confidence: Medium (the kWh is high-confidence; the emission factor is an external assumption).
//
// Note: until the kWh cumulative-vs-interval question is resolved with the client (see
// kwh-semantics-pending-confirmation memory), rows with kVAh=0 and kVARh=0 are excluded from
// totals -- same defensive filter used on the Reactive Power page, since those rows match the
// signature of the contaminated real-gateway capture (2026-06-27 18:00-18:15).
[Authorize(Roles = "Admin,Operator,Viewer")]
public class CarbonController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<CarbonController> _logger;

    public CarbonController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<CarbonController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var co2Factor = await _settings.GetDoubleAsync("General.CO2Factor", 0.82);

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

            var rawValidData = data.Where(d => d.DateTime.HasValue && d.kWh.HasValue).ToList();

            // Defensive filter: exclude rows matching the known-contaminated real-gateway signature
            // (kVAh=0 and kVARh=0 alongside a kWh value) until kWh semantics are confirmed.
            var validData = rawValidData.Where(d => !((d.kVAh ?? -1) == 0 && (d.kVARh ?? -1) == 0)).ToList();
            var excludedCount = rawValidData.Count - validData.Count;

            ViewBag.HasData = validData.Count > 0;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            ViewBag.ExcludedCount = excludedCount;
            ViewBag.CO2Factor = co2Factor;
            if (validData.Count == 0) return View();

            var totalKwh = validData.Sum(d => (double)(d.kWh ?? 0));
            var totalCarbonKg = totalKwh * co2Factor;
            var totalCarbonTonnes = totalCarbonKg / 1000.0;

            // Annualized projection based on this period's daily average
            var daysInPeriod = validData.Select(d => d.DateTime!.Value.Date).Distinct().Count();
            var avgDailyKwh = daysInPeriod > 0 ? totalKwh / daysInPeriod : 0;
            var annualizedCarbonTonnes = (avgDailyKwh * 365 * co2Factor) / 1000.0;

            ViewBag.TotalKwh = Math.Round(totalKwh, 0);
            ViewBag.TotalCarbonKg = Math.Round(totalCarbonKg, 0);
            ViewBag.TotalCarbonTonnes = Math.Round(totalCarbonTonnes, 2);
            ViewBag.AnnualizedCarbonTonnes = Math.Round(annualizedCarbonTonnes, 1);

            // Daily trend
            var daily = validData
                .GroupBy(d => d.DateTime!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("MMM dd"),
                    CarbonKg = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)) * co2Factor, 1)
                }).ToList();

            ViewBag.DailyLabels = JsonSerializer.Serialize(daily.Select(d => d.Date));
            ViewBag.DailyCarbon = JsonSerializer.Serialize(daily.Select(d => d.CarbonKg));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading carbon report");
            return View("Error");
        }
    }
}
