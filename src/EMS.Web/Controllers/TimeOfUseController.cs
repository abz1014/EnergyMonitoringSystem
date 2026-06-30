namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

// Consolidation note: this replaces three previously-independent pages (Shift Comparison,
// Baseload Analysis, Weekday/Weekend) that each ran their own copy of the same "fetch data,
// fall back to the latest available date" logic and answered overlapping variants of "when is
// this plant wasting energy." They are combined here as tabs sharing ONE data fetch, so the
// three sub-analyses can never again silently diverge on what date range or fallback logic
// they used -- which they were already at risk of doing (this is the exact failure mode the
// Equipment Health Score bug and the Plant Score drift both came from). The math inside each
// tab is unchanged from the original three controllers; only the data-fetching and page
// structure were consolidated.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class TimeOfUseController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<TimeOfUseController> _logger;

    public TimeOfUseController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<TimeOfUseController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string tab = "shift", string range = "30d")
    {
        try
        {
            var morningStart = await _settings.GetIntAsync("Shift.MorningStart", 6);
            var morningEnd = await _settings.GetIntAsync("Shift.MorningEnd", 14);
            var afternoonStart = await _settings.GetIntAsync("Shift.AfternoonStart", 14);
            var afternoonEnd = await _settings.GetIntAsync("Shift.AfternoonEnd", 22);
            var nightStart = await _settings.GetIntAsync("Shift.NightStart", 22);
            var nightEnd = await _settings.GetIntAsync("Shift.NightEnd", 6);
            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

            var days = range switch { "7d" => 7, "90d" => 90, _ => 30 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            // Single shared fetch (with the single shared fallback) used by all three tabs.
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
            ViewBag.Tab = tab;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            ViewBag.Currency = currency;
            if (validData.Count == 0) return View();

            BuildShiftTab(validData, morningStart, morningEnd, afternoonStart, afternoonEnd, nightStart, nightEnd);
            BuildBaseloadTab(validData, nightStart, nightEnd, tariffRate);
            BuildWeekdayWeekendTab(validData, tariffRate);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading time-of-use analysis");
            return View("Error");
        }
    }

    private void BuildShiftTab(List<EMS.Core.Models.EnergyMeterData> validData, int morningStart, int morningEnd, int afternoonStart, int afternoonEnd, int nightStart, int nightEnd)
    {
        string GetShift(int hour)
        {
            if (hour >= morningStart && hour < morningEnd) return "Morning";
            if (hour >= afternoonStart && hour < afternoonEnd) return "Afternoon";
            return "Night";
        }

        var shiftTotals = validData
            .GroupBy(d => GetShift(d.DateTime!.Value.Hour))
            .Select(g => new { Shift = g.Key, Total = g.Sum(x => (double)(x.kWh ?? 0)), Avg = g.Average(x => (double)(x.kWh ?? 0)), Peak = g.Max(x => x.kWtotal ?? 0) })
            .ToDictionary(g => g.Shift);

        double Total(string shift) => shiftTotals.ContainsKey(shift) ? shiftTotals[shift].Total : 0;
        double Avg(string shift) => shiftTotals.ContainsKey(shift) ? shiftTotals[shift].Avg : 0;
        double Peak(string shift) => shiftTotals.ContainsKey(shift) ? shiftTotals[shift].Peak : 0;

        ViewBag.MorningTotal = Math.Round(Total("Morning"), 0);
        ViewBag.AfternoonTotal = Math.Round(Total("Afternoon"), 0);
        ViewBag.NightTotal = Math.Round(Total("Night"), 0);
        ViewBag.MorningAvg = Math.Round(Avg("Morning"), 1);
        ViewBag.AfternoonAvg = Math.Round(Avg("Afternoon"), 1);
        ViewBag.NightAvg = Math.Round(Avg("Night"), 1);
        ViewBag.MorningPeak = Math.Round(Peak("Morning"), 1);
        ViewBag.AfternoonPeak = Math.Round(Peak("Afternoon"), 1);
        ViewBag.NightPeak = Math.Round(Peak("Night"), 1);

        var grandTotal = Total("Morning") + Total("Afternoon") + Total("Night");
        ViewBag.MorningPct = grandTotal > 0 ? Math.Round(Total("Morning") / grandTotal * 100, 1) : 0;
        ViewBag.AfternoonPct = grandTotal > 0 ? Math.Round(Total("Afternoon") / grandTotal * 100, 1) : 0;
        ViewBag.NightPct = grandTotal > 0 ? Math.Round(Total("Night") / grandTotal * 100, 1) : 0;

        var mostEfficient = new[] { ("Morning", Avg("Morning")), ("Afternoon", Avg("Afternoon")), ("Night", Avg("Night")) }
            .Where(x => x.Item2 > 0).OrderBy(x => x.Item2).FirstOrDefault();
        var mostConsuming = new[] { ("Morning", Total("Morning")), ("Afternoon", Total("Afternoon")), ("Night", Total("Night")) }
            .OrderByDescending(x => x.Item2).FirstOrDefault();

        ViewBag.MostEfficientShift = mostEfficient.Item1 ?? "—";
        ViewBag.MostConsumingShift = mostConsuming.Item1 ?? "—";

        var dailyShifts = validData
            .GroupBy(d => d.DateTime!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key.ToString("MMM dd"),
                Morning = Math.Round(g.Where(x => GetShift(x.DateTime!.Value.Hour) == "Morning").Sum(x => (double)(x.kWh ?? 0)), 0),
                Afternoon = Math.Round(g.Where(x => GetShift(x.DateTime!.Value.Hour) == "Afternoon").Sum(x => (double)(x.kWh ?? 0)), 0),
                Night = Math.Round(g.Where(x => GetShift(x.DateTime!.Value.Hour) == "Night").Sum(x => (double)(x.kWh ?? 0)), 0)
            }).ToList();

        ViewBag.DailyDates = JsonSerializer.Serialize(dailyShifts.Select(d => d.Date));
        ViewBag.DailyMorning = JsonSerializer.Serialize(dailyShifts.Select(d => d.Morning));
        ViewBag.DailyAfternoon = JsonSerializer.Serialize(dailyShifts.Select(d => d.Afternoon));
        ViewBag.DailyNight = JsonSerializer.Serialize(dailyShifts.Select(d => d.Night));

        ViewBag.MorningLabel = $"Morning ({morningStart:D2}:00-{morningEnd:D2}:00)";
        ViewBag.AfternoonLabel = $"Afternoon ({afternoonStart:D2}:00-{afternoonEnd:D2}:00)";
        ViewBag.NightLabel = $"Night ({nightStart:D2}:00-{nightEnd:D2}:00)";
    }

    private void BuildBaseloadTab(List<EMS.Core.Models.EnergyMeterData> validData, int nightStart, int nightEnd, double tariffRate)
    {
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

        var baseloadKwValues = nonProductive.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).ToList();
        var baseloadKw = baseloadKwValues.Count > 0 ? baseloadKwValues.Average() : 0;

        var productiveKwValues = productive.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).ToList();
        var peakKw = validData.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).DefaultIfEmpty(0).Max();
        var avgProductiveKw = productiveKwValues.Count > 0 ? productiveKwValues.Average() : 0;

        var baseloadPctOfPeak = peakKw > 0 ? Math.Round(baseloadKw / peakKw * 100, 1) : 0;

        var nonProductiveKwh = nonProductive.Sum(d => (double)(d.kWh ?? 0));
        var totalKwh = validData.Sum(d => (double)(d.kWh ?? 0));
        var nonProductivePct = totalKwh > 0 ? Math.Round(nonProductiveKwh / totalKwh * 100, 1) : 0;

        var annualBaseloadKwh = baseloadKw * 24 * 365;
        var annualBaseloadCost = annualBaseloadKwh * tariffRate;
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
        ViewBag.NightWindow = $"{nightStart:D2}:00 — {nightEnd:D2}:00";

        var hourlyByHour = validData
            .Where(d => d.kWtotal.HasValue)
            .GroupBy(d => d.DateTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.kWtotal!.Value), 1));
        var hourlyProfile = Enumerable.Range(0, 24).Select(h => hourlyByHour.GetValueOrDefault(h, 0.0)).ToList();

        ViewBag.HourlyProfile = JsonSerializer.Serialize(hourlyProfile);
        ViewBag.HourlyLabels = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => $"{h:D2}:00"));
        ViewBag.BaseloadLine = JsonSerializer.Serialize(Enumerable.Repeat(Math.Round(baseloadKw, 1), 24));
    }

    private void BuildWeekdayWeekendTab(List<EMS.Core.Models.EnergyMeterData> validData, double tariffRate)
    {
        bool IsWeekend(DateTime dt) => dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;

        var weekdayData = validData.Where(d => !IsWeekend(d.DateTime!.Value)).ToList();
        var weekendData = validData.Where(d => IsWeekend(d.DateTime!.Value)).ToList();

        var weekdayDates = weekdayData.Select(d => d.DateTime!.Value.Date).Distinct().Count();
        var weekendDates = weekendData.Select(d => d.DateTime!.Value.Date).Distinct().Count();

        var weekdayTotal = weekdayData.Sum(d => (double)(d.kWh ?? 0));
        var weekendTotal = weekendData.Sum(d => (double)(d.kWh ?? 0));

        var weekdayAvgDaily = weekdayDates > 0 ? weekdayTotal / weekdayDates : 0;
        var weekendAvgDaily = weekendDates > 0 ? weekendTotal / weekendDates : 0;

        var weekendVsWeekdayPct = weekdayAvgDaily > 0 ? Math.Round(weekendAvgDaily / weekdayAvgDaily * 100, 1) : 0;

        var expectedWeekendDaily = weekdayAvgDaily * 0.20;
        var excessWeekendDaily = Math.Max(weekendAvgDaily - expectedWeekendDaily, 0);
        var annualWeekendWasteCost = excessWeekendDaily * 52 * 2 * tariffRate;

        ViewBag.WeekdayTotal = Math.Round(weekdayTotal, 0);
        ViewBag.WeekendTotal = Math.Round(weekendTotal, 0);
        ViewBag.WeekdayAvgDaily = Math.Round(weekdayAvgDaily, 0);
        ViewBag.WeekendAvgDaily = Math.Round(weekendAvgDaily, 0);
        ViewBag.WeekendVsWeekdayPct = weekendVsWeekdayPct;
        ViewBag.AnnualWeekendWasteCost = Math.Round(annualWeekendWasteCost, 0);
        ViewBag.WeekdayDays = weekdayDates;
        ViewBag.WeekendDays = weekendDates;

        var dailyBreakdown = validData
            .GroupBy(d => d.DateTime!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key.ToString("MMM dd"),
                IsWeekend = IsWeekend(g.Key),
                Total = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)), 0)
            }).ToList();

        ViewBag.WwDailyDates = JsonSerializer.Serialize(dailyBreakdown.Select(d => d.Date));
        ViewBag.WwDailyValues = JsonSerializer.Serialize(dailyBreakdown.Select(d => d.Total));
        ViewBag.WwDailyColors = JsonSerializer.Serialize(dailyBreakdown.Select(d => d.IsWeekend ? "#EF4444" : "#2563EB"));

        var weekdayByHour = weekdayData
            .GroupBy(d => d.DateTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => g.Average(x => (double)(x.kWh ?? 0)));
        var weekendByHour = weekendData
            .GroupBy(d => d.DateTime!.Value.Hour)
            .ToDictionary(g => g.Key, g => g.Average(x => (double)(x.kWh ?? 0)));

        ViewBag.WeekdayProfile = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => Math.Round(weekdayByHour.GetValueOrDefault(h, 0), 1)));
        ViewBag.WeekendProfile = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => Math.Round(weekendByHour.GetValueOrDefault(h, 0), 1)));
        ViewBag.HourLabels = JsonSerializer.Serialize(Enumerable.Range(0, 24).Select(h => $"{h:D2}:00"));
    }
}
