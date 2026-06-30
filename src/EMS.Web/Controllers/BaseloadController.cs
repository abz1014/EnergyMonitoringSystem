namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class BaseloadController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<BaseloadController> _logger;

    public BaseloadController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<BaseloadController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var nightStart = await _settings.GetIntAsync("Shift.NightStart", 22);
            var nightEnd = await _settings.GetIntAsync("Shift.NightEnd", 6);
            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
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

            var validData = data.Where(d => d.DateTime.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            if (validData.Count == 0) return View();

            bool IsNonProductive(DateTime dt)
            {
                var isWeekend = dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
                var hour = dt.Hour;
                var isNightHour = nightStart > nightEnd
                    ? (hour >= nightStart || hour < nightEnd)
                    : (hour >= nightStart && hour < nightEnd);
                return isWeekend || isNightHour;
            }

            var nonProductive = validData.Where(d => IsNonProductive(d.DateTime!.Value)).ToList();
            var productive = validData.Where(d => !IsNonProductive(d.DateTime!.Value)).ToList();

            // Baseload = average kW during non-productive hours
            var baseloadKwValues = nonProductive.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).ToList();
            var baseloadKw = baseloadKwValues.Count > 0 ? baseloadKwValues.Average() : 0;

            var productiveKwValues = productive.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).ToList();
            var peakKw = validData.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).DefaultIfEmpty(0).Max();
            var avgProductiveKw = productiveKwValues.Count > 0 ? productiveKwValues.Average() : 0;

            var baseloadPctOfPeak = peakKw > 0 ? Math.Round(baseloadKw / peakKw * 100, 1) : 0;

            var nonProductiveKwh = nonProductive.Sum(d => (double)(d.kWh ?? 0));
            var totalKwh = validData.Sum(d => (double)(d.kWh ?? 0));
            var nonProductivePct = totalKwh > 0 ? Math.Round(nonProductiveKwh / totalKwh * 100, 1) : 0;

            // Annualize: baseload kW running 24/7/365
            var annualBaseloadKwh = baseloadKw * 24 * 365;
            var annualBaseloadCost = annualBaseloadKwh * tariffRate;

            // Period actual non-productive cost
            var periodNonProductiveCost = nonProductiveKwh * tariffRate;

            ViewBag.BaseloadKw = Math.Round(baseloadKw, 2);
            ViewBag.PeakKw = Math.Round(peakKw, 1);
            ViewBag.AvgProductiveKw = Math.Round(avgProductiveKw, 2);
            ViewBag.BaseloadPctOfPeak = baseloadPctOfPeak;
            ViewBag.NonProductiveKwh = Math.Round(nonProductiveKwh, 0);
            ViewBag.NonProductivePct = nonProductivePct;
            ViewBag.AnnualBaseloadKwh = Math.Round(annualBaseloadKwh, 0);
            ViewBag.AnnualBaseloadCost = Math.Round(annualBaseloadCost, 0);
            ViewBag.PeriodNonProductiveCost = Math.Round(periodNonProductiveCost, 0);
            ViewBag.Currency = currency;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            ViewBag.NightWindow = $"{nightStart:D2}:00 — {nightEnd:D2}:00";

            // 24h profile showing baseload band
            var hourlyAvg = validData
                .Where(d => d.kWtotal.HasValue)
                .GroupBy(d => d.DateTime!.Value.Hour)
                .OrderBy(g => g.Key)
                .Select(g => Math.Round(g.Average(x => x.kWtotal!.Value), 1))
                .ToList();

            // Pad to 24 hours if missing
            var hourlyByHour = validData
                .Where(d => d.kWtotal.HasValue)
                .GroupBy(d => d.DateTime!.Value.Hour)
                .ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.kWtotal!.Value), 1));
            var hourlyProfile = Enumerable.Range(0, 24).Select(h => hourlyByHour.GetValueOrDefault(h, 0.0)).ToList();

            ViewBag.HourlyProfile = JsonSerializer.Serialize(hourlyProfile);
            ViewBag.HourlyLabels = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => $"{h:D2}:00"));
            ViewBag.BaseloadLine = JsonSerializer.Serialize(Enumerable.Repeat(Math.Round(baseloadKw, 1), 24));

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading baseload analysis");
            return View("Error");
        }
    }
}
