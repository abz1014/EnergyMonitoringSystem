namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class WeekdayWeekendController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<WeekdayWeekendController> _logger;

    public WeekdayWeekendController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<WeekdayWeekendController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
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

            bool IsWeekend(DateTime dt) => dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;

            var weekdayData = validData.Where(d => !IsWeekend(d.DateTime!.Value)).ToList();
            var weekendData = validData.Where(d => IsWeekend(d.DateTime!.Value)).ToList();

            var weekdayDates = weekdayData.Select(d => d.DateTime!.Value.Date).Distinct().Count();
            var weekendDates = weekendData.Select(d => d.DateTime!.Value.Date).Distinct().Count();

            var weekdayTotal = weekdayData.Sum(d => (double)(d.kWh ?? 0));
            var weekendTotal = weekendData.Sum(d => (double)(d.kWh ?? 0));

            var weekdayAvgDaily = weekdayDates > 0 ? weekdayTotal / weekdayDates : 0;
            var weekendAvgDaily = weekendDates > 0 ? weekendTotal / weekendDates : 0;

            // Weekend as % of weekday — used to estimate "should be" weekend consumption
            var weekendVsWeekdayPct = weekdayAvgDaily > 0 ? Math.Round(weekendAvgDaily / weekdayAvgDaily * 100, 1) : 0;

            // Estimated waste: if weekend should be near-zero (non-productive), excess above a minimal baseline is "waste"
            // Use 20% of weekday average as the expected minimal weekend baseline (security/standby loads)
            var expectedWeekendDaily = weekdayAvgDaily * 0.20;
            var excessWeekendDaily = Math.Max(weekendAvgDaily - expectedWeekendDaily, 0);
            var annualWeekendWasteCost = excessWeekendDaily * 52 * 2 * tariffRate; // 52 weeks * 2 weekend days

            ViewBag.WeekdayTotal = Math.Round(weekdayTotal, 0);
            ViewBag.WeekendTotal = Math.Round(weekendTotal, 0);
            ViewBag.WeekdayAvgDaily = Math.Round(weekdayAvgDaily, 0);
            ViewBag.WeekendAvgDaily = Math.Round(weekendAvgDaily, 0);
            ViewBag.WeekendVsWeekdayPct = weekendVsWeekdayPct;
            ViewBag.AnnualWeekendWasteCost = Math.Round(annualWeekendWasteCost, 0);
            ViewBag.Currency = currency;
            ViewBag.WeekdayDays = weekdayDates;
            ViewBag.WeekendDays = weekendDates;

            // Daily breakdown chart: all days in range, colored by weekday/weekend
            var dailyBreakdown = validData
                .GroupBy(d => d.DateTime!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key.ToString("MMM dd"),
                    IsWeekend = IsWeekend(g.Key),
                    Total = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)), 0)
                }).ToList();

            ViewBag.DailyDates = JsonSerializer.Serialize(dailyBreakdown.Select(d => d.Date));
            ViewBag.DailyValues = JsonSerializer.Serialize(dailyBreakdown.Select(d => d.Total));
            ViewBag.DailyColors = JsonSerializer.Serialize(dailyBreakdown.Select(d => d.IsWeekend ? "#EF4444" : "#2563EB"));

            // Average daily profile comparison (24h)
            var weekdayByHour = weekdayData
                .GroupBy(d => d.DateTime!.Value.Hour)
                .ToDictionary(g => g.Key, g => g.Average(x => (double)(x.kWh ?? 0)));
            var weekendByHour = weekendData
                .GroupBy(d => d.DateTime!.Value.Hour)
                .ToDictionary(g => g.Key, g => g.Average(x => (double)(x.kWh ?? 0)));

            ViewBag.WeekdayProfile = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => Math.Round(weekdayByHour.GetValueOrDefault(h, 0), 1)));
            ViewBag.WeekendProfile = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => Math.Round(weekendByHour.GetValueOrDefault(h, 0), 1)));
            ViewBag.HourLabels = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => $"{h:D2}:00"));

            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading weekday vs weekend analysis");
            return View("Error");
        }
    }
}
