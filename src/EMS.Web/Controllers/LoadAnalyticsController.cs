namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Web.Services;
using System.Text.Json;

// Sprint 4: four analytics that are Class A (zero new data required) under
// SPRINT4_ANALYTICS_FEASIBILITY.md, sharing one data fetch the same way TimeOfUseController does.
// All kWh-summing here uses DataIntegrityHelper.ExcludeContaminated -- see that file for the
// full provenance note. The underlying kWh cumulative-vs-interval semantics question is still
// open with the client (kwh-semantics-pending-confirmation memory); this page does not hide that,
// it states it directly in the view.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class LoadAnalyticsController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ILogger<LoadAnalyticsController> _logger;

    public LoadAnalyticsController(IEnergyMeterRepository meterRepo, ILogger<LoadAnalyticsController> logger)
    {
        _meterRepo = meterRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string tab = "profile", string range = "30d")
    {
        try
        {
            var days = range switch { "7d" => 7, "90d" => 90, _ => 30 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            var data = await _meterRepo.GetByDateRange(from, to);
            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-180), to);
                if (recent.Count > 0)
                {
                    var latestDate = recent.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    to = latestDate.AddDays(1);
                    from = to.AddDays(-days);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= from && d.DateTime.Value < to).ToList();
                }
            }

            var validData = data.Where(d => d.DateTime.HasValue).ToList();
            var clean = validData.ExcludeContaminated().ToList();

            ViewBag.HasData = validData.Count > 0;
            ViewBag.Tab = tab;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            var distinctDays = validData.Select(d => d.DateTime!.Value.Date).Distinct().Count();
            ViewBag.DaysOfDataUsed = distinctDays;
            if (validData.Count == 0) return View();

            BuildLoadProfileTab(clean);
            BuildMeterRankingTab(clean);
            BuildDailyVarianceTab(clean);
            await BuildMonthlyVarianceTab();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading load analytics");
            return View("Error");
        }
    }

    // --- Tab 1: Load Profile (typical daily load shape, hour-of-day average kWtotal) ---
    private void BuildLoadProfileTab(List<EnergyMeterData> clean)
    {
        var hourlyAvg = clean
            .GroupBy(d => d.DateTime!.Value.Hour)
            .Select(g => new { Hour = g.Key, AvgKw = g.Average(x => x.kWtotal ?? 0) })
            .ToDictionary(g => g.Hour, g => g.AvgKw);

        var hours = Enumerable.Range(0, 24).ToList();
        var profileValues = hours.Select(h => hourlyAvg.ContainsKey(h) ? Math.Round(hourlyAvg[h], 2) : 0).ToList();
        var labels = hours.Select(h => $"{h:00}:00").ToList();

        var peakHour = hourlyAvg.Count > 0 ? hourlyAvg.OrderByDescending(kv => kv.Value).First() : (KeyValuePair<int, double>?)null;
        var troughHour = hourlyAvg.Count > 0 ? hourlyAvg.OrderBy(kv => kv.Value).First() : (KeyValuePair<int, double>?)null;

        ViewBag.ProfileLabels = JsonSerializer.Serialize(labels);
        ViewBag.ProfileValues = JsonSerializer.Serialize(profileValues);
        ViewBag.ProfilePeakHour = peakHour.HasValue ? $"{peakHour.Value.Key:00}:00" : "—";
        ViewBag.ProfilePeakKw = peakHour.HasValue ? Math.Round(peakHour.Value.Value, 1) : 0;
        ViewBag.ProfileTroughHour = troughHour.HasValue ? $"{troughHour.Value.Key:00}:00" : "—";
        ViewBag.ProfileTroughKw = troughHour.HasValue ? Math.Round(troughHour.Value.Value, 1) : 0;
    }

    // --- Tab 2: Meter Ranking (total kWh per meter, % share, sorted descending) ---
    private void BuildMeterRankingTab(List<EnergyMeterData> clean)
    {
        var totalAll = clean.Sum(d => (double)(d.kWh ?? 0));

        var ranking = clean
            .Where(d => d.MeterNo.HasValue)
            .GroupBy(d => d.MeterNo!.Value)
            .Select(g => new
            {
                MeterNo = g.Key,
                MeterName = g.First().MeterName ?? $"Meter-{g.Key}",
                TotalKwh = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)), 0),
                SharePct = totalAll > 0 ? Math.Round(g.Sum(x => (double)(x.kWh ?? 0)) / totalAll * 100, 1) : 0
            })
            .OrderByDescending(r => r.TotalKwh)
            .ToList();

        ViewBag.Ranking = ranking;
    }

    // --- Tab 3: Daily Variance (day-over-day % change in system-wide kWh) ---
    private void BuildDailyVarianceTab(List<EnergyMeterData> clean)
    {
        var dailyTotals = clean
            .GroupBy(d => d.DateTime!.Value.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => (double)(x.kWh ?? 0)) })
            .OrderBy(g => g.Date)
            .ToList();

        var rows = new List<object>();
        for (int i = 1; i < dailyTotals.Count; i++)
        {
            var prev = dailyTotals[i - 1].Total;
            var curr = dailyTotals[i].Total;
            double? pctChange = prev > 0 ? Math.Round((curr - prev) / prev * 100, 1) : null;
            rows.Add(new
            {
                Date = dailyTotals[i].Date.ToString("MMM dd"),
                Total = Math.Round(curr, 0),
                Previous = Math.Round(prev, 0),
                PctChange = pctChange
            });
        }
        rows.Reverse();

        ViewBag.DailyVariance = rows;
    }

    // --- Tab 4: Monthly Variance (month-over-month % change in system-wide kWh) ---
    private async Task BuildMonthlyVarianceTab()
    {
        // Independent fetch covering all history, not just the selected range -- monthly
        // comparison needs full calendar months, which may fall outside the page's range selector.
        var all = await _meterRepo.GetByDateRange(DateTime.Now.AddYears(-2), DateTime.Now.AddDays(1));
        var clean = all.Where(d => d.DateTime.HasValue).ToList().ExcludeContaminated().ToList();

        var monthlyTotals = clean
            .GroupBy(d => new DateTime(d.DateTime!.Value.Year, d.DateTime.Value.Month, 1))
            .Select(g => new { Month = g.Key, Total = g.Sum(x => (double)(x.kWh ?? 0)) })
            .OrderBy(g => g.Month)
            .ToList();

        ViewBag.MonthsOfDataAvailable = monthlyTotals.Count;

        var rows = new List<object>();
        for (int i = 1; i < monthlyTotals.Count; i++)
        {
            var prev = monthlyTotals[i - 1].Total;
            var curr = monthlyTotals[i].Total;
            double? pctChange = prev > 0 ? Math.Round((curr - prev) / prev * 100, 1) : null;
            rows.Add(new
            {
                Month = monthlyTotals[i].Month.ToString("MMMM yyyy"),
                Total = Math.Round(curr, 0),
                Previous = Math.Round(prev, 0),
                PctChange = pctChange
            });
        }
        rows.Reverse();

        ViewBag.MonthlyVariance = rows;
    }
}
