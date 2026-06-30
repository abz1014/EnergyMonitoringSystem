namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class HeatmapController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ILogger<HeatmapController> _logger;

    public HeatmapController(IEnergyMeterRepository meterRepo, ILogger<HeatmapController> logger)
    {
        _meterRepo = meterRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? year)
    {
        try
        {
            var targetYear = year ?? DateTime.Now.Year;
            var yearStart = new DateTime(targetYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            var data = await _meterRepo.GetByDateRange(yearStart, yearEnd);

            // Fallback: if requested year has no data, use the year of the most recent data
            if (data.Count == 0 && !year.HasValue)
            {
                var recent = await _meterRepo.GetByDateRange(DateTime.Now.AddYears(-2), yearEnd);
                if (recent.Count > 0)
                {
                    targetYear = recent.Max(d => d.DateTime ?? DateTime.MinValue).Year;
                    yearStart = new DateTime(targetYear, 1, 1);
                    yearEnd = yearStart.AddYears(1);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= yearStart && d.DateTime.Value < yearEnd).ToList();
                }
            }

            var validData = data.Where(d => d.DateTime.HasValue && d.kWh.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            ViewBag.Year = targetYear;
            if (validData.Count == 0) return View();

            var dailyTotals = validData
                .GroupBy(d => d.DateTime!.Value.Date)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => (double)x.kWh!.Value), 1));

            var maxVal = dailyTotals.Count > 0 ? dailyTotals.Values.Max() : 0;
            var minVal = dailyTotals.Count > 0 ? dailyTotals.Values.Min() : 0;
            var avgVal = dailyTotals.Count > 0 ? dailyTotals.Values.Average() : 0;

            // Build month-row x day-column grid for ApexCharts heatmap (12 series, one per month)
            var monthNames = new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
            var series = new List<object>();
            for (int m = 1; m <= 12; m++)
            {
                var daysInMonth = DateTime.DaysInMonth(targetYear, m);
                var rowData = new List<object>();
                for (int day = 1; day <= 31; day++)
                {
                    if (day > daysInMonth)
                    {
                        rowData.Add(new { x = day.ToString(), y = (double?)null });
                        continue;
                    }
                    var date = new DateTime(targetYear, m, day);
                    if (date > DateTime.Now)
                    {
                        rowData.Add(new { x = day.ToString(), y = (double?)null });
                    }
                    else
                    {
                        var val = dailyTotals.GetValueOrDefault(date, 0);
                        rowData.Add(new { x = day.ToString(), y = (double?)val });
                    }
                }
                series.Add(new { name = monthNames[m - 1], data = rowData });
            }

            ViewBag.MaxVal = Math.Round(maxVal, 0);
            ViewBag.MinVal = Math.Round(minVal, 0);
            ViewBag.AvgVal = Math.Round(avgVal, 0);
            ViewBag.DaysWithData = dailyTotals.Count;
            ViewBag.HeatmapSeries = JsonSerializer.Serialize(series);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading heatmap calendar");
            return View("Error");
        }
    }
}
