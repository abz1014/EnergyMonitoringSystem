namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class ForecastController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<ForecastController> _logger;

    public ForecastController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<ForecastController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int windowDays = 7, int forecastDays = 7)
    {
        try
        {
            windowDays = Math.Clamp(windowDays, 3, 30);
            forecastDays = Math.Clamp(forecastDays, 1, 30);

            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

            var to = DateTime.Now.Date.AddDays(1);
            // Pull enough history for the moving-average window plus trend context
            var from = to.AddDays(-Math.Max(windowDays * 4, 30));
            var data = await _meterRepo.GetByDateRange(from, to);

            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-90), to);
                if (recent.Count > 0)
                {
                    var latestDate = recent.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    to = latestDate.AddDays(1);
                    from = to.AddDays(-Math.Max(windowDays * 4, 30));
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= from && d.DateTime.Value < to).ToList();
                }
            }

            var dailyTotals = data
                .Where(d => d.DateTime.HasValue && d.kWh.HasValue)
                .GroupBy(d => d.DateTime!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, Total = Math.Round(g.Sum(x => (double)x.kWh!.Value), 1) })
                .ToList();

            ViewBag.HasData = dailyTotals.Count >= windowDays;
            if (dailyTotals.Count < windowDays) return View();

            // Simple moving average forecast: each forecast day = average of the trailing `windowDays` actual/forecast values
            var series = dailyTotals.Select(d => d.Total).ToList();
            var lastActualDate = dailyTotals.Last().Date;
            var forecastValues = new List<double>();

            var workingSeries = new List<double>(series);
            for (int i = 0; i < forecastDays; i++)
            {
                var window = workingSeries.Skip(Math.Max(0, workingSeries.Count - windowDays)).Take(windowDays).ToList();
                var avg = window.Average();
                forecastValues.Add(Math.Round(avg, 1));
                workingSeries.Add(avg);
            }

            var forecastTotal = forecastValues.Sum();
            var forecastCost = forecastTotal * tariffRate;
            var avgDailyCost = (series.Count > 0 ? series.TakeLast(windowDays).Average() : 0) * tariffRate;

            ViewBag.WindowDays = windowDays;
            ViewBag.ForecastDays = forecastDays;
            ViewBag.ForecastTotal = Math.Round(forecastTotal, 0);
            ViewBag.ForecastCost = Math.Round(forecastCost, 0);
            ViewBag.AvgDailyCost = Math.Round(avgDailyCost, 0);
            ViewBag.Currency = currency;

            var actualLabels = dailyTotals.Select(d => d.Date.ToString("MMM dd")).ToList();
            var forecastLabels = Enumerable.Range(1, forecastDays).Select(i => lastActualDate.AddDays(i).ToString("MMM dd")).ToList();

            // Build aligned series: actual series has nulls for forecast period, forecast series has nulls for actual period
            // (except the last actual point, which bridges the two lines visually)
            var actualSeries = series.Select(v => (double?)v).ToList();
            var forecastSeries = Enumerable.Repeat((double?)null, series.Count - 1).ToList();
            forecastSeries.Add(series.Last());
            forecastSeries.AddRange(forecastValues.Select(v => (double?)v));
            actualSeries.AddRange(Enumerable.Repeat((double?)null, forecastDays));

            ViewBag.ChartLabels = JsonSerializer.Serialize(actualLabels.Concat(forecastLabels));
            ViewBag.ActualSeries = JsonSerializer.Serialize(actualSeries);
            ViewBag.ForecastSeries = JsonSerializer.Serialize(forecastSeries);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating consumption forecast");
            return View("Error");
        }
    }
}
