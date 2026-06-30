namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class ShiftAnalysisController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<ShiftAnalysisController> _logger;

    public ShiftAnalysisController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<ShiftAnalysisController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "7d")
    {
        try
        {
            var morningStart = await _settings.GetIntAsync("Shift.MorningStart", 6);
            var morningEnd = await _settings.GetIntAsync("Shift.MorningEnd", 14);
            var afternoonStart = await _settings.GetIntAsync("Shift.AfternoonStart", 14);
            var afternoonEnd = await _settings.GetIntAsync("Shift.AfternoonEnd", 22);
            var nightStart = await _settings.GetIntAsync("Shift.NightStart", 22);
            var nightEnd = await _settings.GetIntAsync("Shift.NightEnd", 6);

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

            string GetShift(int hour)
            {
                if (hour >= morningStart && hour < morningEnd) return "Morning";
                if (hour >= afternoonStart && hour < afternoonEnd) return "Afternoon";
                return "Night";
            }

            var validData = data.Where(d => d.DateTime.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;

            if (validData.Count == 0) { return View(); }

            // Overall shift totals
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

            // Daily breakdown by shift (for stacked bar chart)
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
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading shift analysis");
            return View("Error");
        }
    }
}
