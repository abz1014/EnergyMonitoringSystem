namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class WeatherController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ScadaDbContext _context;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IEnergyMeterRepository meterRepo, ScadaDbContext context, ILogger<WeatherController> logger)
    {
        _meterRepo = meterRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string range = "30d")
    {
        try
        {
            var days = range switch { "7d" => 7, "90d" => 90, _ => 30 };
            var to = DateTime.Now.Date.AddDays(1);
            var from = to.AddDays(-days);

            var temps = await _context.DailyTemperatures.AsNoTracking()
                .Where(t => t.TempDate >= from && t.TempDate < to)
                .ToListAsync();

            var data = await _meterRepo.GetByDateRange(from, to);
            if (data.Count == 0 || temps.Count == 0)
            {
                // Try anchoring to whatever temperature data actually exists
                var anyTemps = await _context.DailyTemperatures.AsNoTracking().OrderBy(t => t.TempDate).ToListAsync();
                if (anyTemps.Count > 0)
                {
                    var tFrom = anyTemps.Min(t => t.TempDate);
                    var tTo = anyTemps.Max(t => t.TempDate).AddDays(1);
                    temps = anyTemps.Where(t => t.TempDate >= tFrom && t.TempDate < tTo).ToList();
                    data = await _meterRepo.GetByDateRange(tFrom, tTo);
                    from = tFrom;
                    to = tTo;
                }
            }

            var dailyKwh = data
                .Where(d => d.DateTime.HasValue && d.kWh.HasValue)
                .GroupBy(d => d.DateTime!.Value.Date)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => (double)x.kWh!.Value), 1));

            var tempByDate = temps.ToDictionary(t => t.TempDate.Date, t => (double)t.AvgTempC);

            var matched = tempByDate.Keys.Where(d => dailyKwh.ContainsKey(d)).OrderBy(d => d).ToList();

            ViewBag.HasData = matched.Count >= 3;
            ViewBag.Range = range;
            ViewBag.DateRangeLabel = $"{from:MMM dd} — {to.AddDays(-1):MMM dd, yyyy}";
            ViewBag.AllTemps = temps.OrderByDescending(t => t.TempDate).ToList();

            if (matched.Count < 3) return View();

            var temperatures = matched.Select(d => tempByDate[d]).ToList();
            var consumptions = matched.Select(d => dailyKwh[d]).ToList();

            var correlation = PearsonCorrelation(temperatures, consumptions);

            ViewBag.Correlation = Math.Round(correlation, 3);
            ViewBag.CorrelationStrength = Math.Abs(correlation) switch
            {
                >= 0.7 => "Strong",
                >= 0.4 => "Moderate",
                >= 0.2 => "Weak",
                _ => "Negligible"
            };
            ViewBag.DataPoints = matched.Count;

            ViewBag.ScatterData = JsonSerializer.Serialize(matched.Select(d => new { x = tempByDate[d], y = dailyKwh[d] }));
            ViewBag.Dates = JsonSerializer.Serialize(matched.Select(d => d.ToString("MMM dd")));
            ViewBag.TempSeries = JsonSerializer.Serialize(temperatures);
            ViewBag.ConsumptionSeries = JsonSerializer.Serialize(consumptions);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading weather correlation");
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> AddTemperature(DateTime tempDate, decimal avgTempC)
    {
        try
        {
            var existing = await _context.DailyTemperatures.FindAsync(tempDate.Date);
            if (existing != null)
            {
                existing.AvgTempC = avgTempC;
                existing.UpdatedAt = DateTime.Now;
            }
            else
            {
                _context.DailyTemperatures.Add(new DailyTemperature { TempDate = tempDate.Date, AvgTempC = avgTempC, UpdatedAt = DateTime.Now });
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving daily temperature");
        }
        return RedirectToAction("Index");
    }

    private static double PearsonCorrelation(List<double> x, List<double> y)
    {
        var n = x.Count;
        var meanX = x.Average();
        var meanY = y.Average();

        var numerator = 0.0;
        var sumSqX = 0.0;
        var sumSqY = 0.0;

        for (int i = 0; i < n; i++)
        {
            var dx = x[i] - meanX;
            var dy = y[i] - meanY;
            numerator += dx * dy;
            sumSqX += dx * dx;
            sumSqY += dy * dy;
        }

        var denominator = Math.Sqrt(sumSqX * sumSqY);
        return denominator > 0 ? numerator / denominator : 0;
    }
}
