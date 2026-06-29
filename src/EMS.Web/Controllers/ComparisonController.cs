namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class ComparisonController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<ComparisonController> _logger;

    public ComparisonController(IEnergyMeterRepository meterRepo, IMonitoringDeviceRepository deviceRepo, ILogger<ComparisonController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? meters = null, string timeframe = "daily", string? dateFrom = null, string? dateTo = null)
    {
        try
        {
            var devices = await _deviceRepo.GetAllDevices();
            var energyMeters = devices
                .Where(d => d.DeviceID.HasValue && d.IsActive == 1 && (d.DeviceType ?? "").Contains("Energy", StringComparison.OrdinalIgnoreCase))
                .GroupBy(d => d.DeviceID!.Value)
                .Select(g => g.First())
                .OrderBy(d => d.DeviceName)
                .ToList();

            var selectedIds = new List<int>();
            if (!string.IsNullOrEmpty(meters))
                selectedIds = meters.Split(',').Where(s => int.TryParse(s, out _)).Select(int.Parse).ToList();

            DateTime from, to;
            if (timeframe == "custom" && DateTime.TryParse(dateFrom, out var parsedFrom) && DateTime.TryParse(dateTo, out var parsedTo))
            {
                from = parsedFrom;
                to = parsedTo.AddDays(1).AddTicks(-1);
            }
            else
            {
                var today = DateTime.Now.Date;
                (from, to) = timeframe switch
                {
                    "weekly" => (today.AddDays(-7), today.AddDays(1).AddTicks(-1)),
                    "monthly" => (today.AddDays(-30), today.AddDays(1).AddTicks(-1)),
                    _ => (today, today.AddDays(1).AddTicks(-1))
                };
            }

            // Smart fallback
            if (selectedIds.Count > 0)
            {
                var testData = await _meterRepo.GetByDateRange(from, to);
                if (testData.Count == 0)
                {
                    var recentData = await _meterRepo.GetByDateRange(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(1));
                    if (recentData.Count > 0)
                    {
                        var latestDate = recentData.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                        if (timeframe == "daily") { from = latestDate; to = latestDate.AddDays(1).AddTicks(-1); }
                        else if (timeframe == "weekly") { from = latestDate.AddDays(-7); to = latestDate.AddDays(1).AddTicks(-1); }
                        else { from = latestDate.AddDays(-30); to = latestDate.AddDays(1).AddTicks(-1); }
                    }
                }
            }

            var chartSeries = new List<object>();
            var meterTotals = new List<object>();
            var categories = new List<string>();
            var isDaily = timeframe == "daily";

            if (selectedIds.Count > 0)
            {
                var allData = await _meterRepo.GetByDateRange(from, to);

                // Build categories (shared X-axis)
                if (isDaily)
                {
                    categories = Enumerable.Range(0, 24).Select(h => $"{h:D2}:00").ToList();
                }
                else
                {
                    var days = (to.Date - from.Date).Days + 1;
                    categories = Enumerable.Range(0, days).Select(i => from.AddDays(i).ToString("MMM dd")).ToList();
                }

                foreach (var meterId in selectedIds)
                {
                    var meterData = allData.Where(d => d.MeterNo == meterId && d.DateTime.HasValue).ToList();
                    var device = energyMeters.FirstOrDefault(d => d.DeviceID == meterId);
                    var name = device?.DeviceName ?? meterData.FirstOrDefault()?.MeterName ?? $"Meter-{meterId}";

                    List<double> values;
                    if (isDaily)
                    {
                        var byHour = meterData.GroupBy(d => d.DateTime!.Value.Hour).ToDictionary(g => g.Key, g => g.Sum(x => (double)(x.kWh ?? 0)));
                        values = Enumerable.Range(0, 24).Select(h => Math.Round(byHour.GetValueOrDefault(h, 0), 1)).ToList();
                    }
                    else
                    {
                        var byDate = meterData.GroupBy(d => d.DateTime!.Value.Date).ToDictionary(g => g.Key, g => g.Sum(x => (double)(x.kWh ?? 0)));
                        var days = (to.Date - from.Date).Days + 1;
                        values = Enumerable.Range(0, days).Select(i => Math.Round(byDate.GetValueOrDefault(from.AddDays(i).Date, 0), 1)).ToList();
                    }

                    chartSeries.Add(new { name, data = values });
                    meterTotals.Add(new { name, total = Math.Round(values.Sum(), 0) });
                }
            }

            ViewBag.EnergyMeters = energyMeters;
            ViewBag.SelectedIds = selectedIds;
            ViewBag.Timeframe = timeframe;
            ViewBag.DateFrom = from.ToString("yyyy-MM-dd");
            ViewBag.DateTo = to.Date.ToString("yyyy-MM-dd");
            ViewBag.ChartSeries = JsonSerializer.Serialize(chartSeries);
            ViewBag.Categories = JsonSerializer.Serialize(categories);
            ViewBag.MeterTotals = meterTotals;
            ViewBag.HasData = chartSeries.Count > 0;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading comparison");
            return View("Error");
        }
    }
}
