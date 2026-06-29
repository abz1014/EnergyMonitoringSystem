namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Models;
using EMS.Web.Services;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class CostDashboardController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<CostDashboardController> _logger;

    public CostDashboardController(
        IEnergyMeterRepository meterRepo,
        IMonitoringDeviceRepository deviceRepo,
        AppSettingsService settings,
        ILogger<CostDashboardController> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var defaultRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
        var peakRate = await _settings.GetDoubleAsync("Tariff.PeakRate", 57.0);
        var offPeakRate = await _settings.GetDoubleAsync("Tariff.OffPeakRate", 45.0);
        var peakStart = await _settings.GetIntAsync("Tariff.PeakStartHour", 18);
        var peakEnd = await _settings.GetIntAsync("Tariff.PeakEndHour", 22);
        var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

        var model = new CostDashboardViewModel
        {
            Currency = currency,
            DefaultRate = defaultRate,
            PeakRate = peakRate,
            OffPeakRate = offPeakRate
        };

        try
        {
            var now = DateTime.Now;
            var today = now.Date;
            var weekStart = today.AddDays(-7);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var thirtyDaysAgo = today.AddDays(-30);

            var monthData = await _meterRepo.GetByDateRange(monthStart, now);

            if (monthData.Count == 0)
            {
                var recentData = await _meterRepo.GetByDateRange(today.AddDays(-60), now);
                if (recentData.Count > 0)
                {
                    var latestDate = recentData.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    monthStart = new DateTime(latestDate.Year, latestDate.Month, 1);
                    today = latestDate;
                    weekStart = today.AddDays(-7);
                    thirtyDaysAgo = today.AddDays(-30);
                    monthData = recentData.Where(d => d.DateTime.HasValue && d.DateTime.Value >= monthStart).ToList();
                }
            }

            model.HasData = monthData.Count > 0;
            if (!model.HasData) return View(model);

            var allData = await _meterRepo.GetByDateRange(thirtyDaysAgo, now.AddDays(1));
            var devices = await _deviceRepo.GetAllDevices();
            var deviceLookup = devices.Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value).ToDictionary(g => g.Key, g => g.First());

            double CalculateCost(double kwh, int hour)
            {
                var rate = (hour >= peakStart && hour < peakEnd) ? peakRate : offPeakRate;
                return kwh * rate;
            }

            // Today's cost
            var todayData = allData.Where(d => d.DateTime.HasValue && d.DateTime.Value.Date == today).ToList();
            model.TodayCost = todayData.Sum(d => CalculateCost((double)(d.kWh ?? 0), d.DateTime!.Value.Hour));

            // Week cost
            var weekData = allData.Where(d => d.DateTime.HasValue && d.DateTime.Value.Date >= weekStart).ToList();
            model.WeekCost = weekData.Sum(d => CalculateCost((double)(d.kWh ?? 0), d.DateTime!.Value.Hour));

            // Month cost
            model.MonthCost = monthData.Where(d => d.DateTime.HasValue)
                .Sum(d => CalculateCost((double)(d.kWh ?? 0), d.DateTime!.Value.Hour));

            // Projected
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var daysPassed = Math.Max((today - monthStart).Days + 1, 1);
            model.ProjectedMonthlyCost = (model.MonthCost / daysPassed) * daysInMonth;

            model.DataPeriod = $"{thirtyDaysAgo:MMM dd} — {today:MMM dd, yyyy}";

            // Peak vs Off-Peak
            var peakReadings = allData.Where(d => d.DateTime.HasValue && d.DateTime.Value.Hour >= peakStart && d.DateTime.Value.Hour < peakEnd).ToList();
            var offPeakReadings = allData.Where(d => d.DateTime.HasValue && (d.DateTime.Value.Hour < peakStart || d.DateTime.Value.Hour >= peakEnd)).ToList();

            model.PeakKwh = peakReadings.Sum(d => (double)(d.kWh ?? 0));
            model.OffPeakKwh = offPeakReadings.Sum(d => (double)(d.kWh ?? 0));
            model.PeakCost = model.PeakKwh * peakRate;
            model.OffPeakCost = model.OffPeakKwh * offPeakRate;

            // Cost by Meter
            model.CostByMeter = allData
                .Where(d => d.MeterNo.HasValue && d.DateTime.HasValue)
                .GroupBy(d => d.MeterNo!.Value)
                .Select(g =>
                {
                    var kwh = g.Sum(x => (double)(x.kWh ?? 0));
                    var cost = g.Sum(x => CalculateCost((double)(x.kWh ?? 0), x.DateTime!.Value.Hour));
                    var name = g.First().MeterName ?? (deviceLookup.ContainsKey(g.Key) ? deviceLookup[g.Key].DeviceName : $"Meter-{g.Key}");
                    var loc = g.First().MeterLocation ?? "";
                    return new MeterCostItem { MeterName = name ?? $"Meter-{g.Key}", Location = loc, Kwh = Math.Round(kwh, 0), Cost = Math.Round(cost, 0) };
                })
                .OrderByDescending(m => m.Cost)
                .ToList();

            // Cost by Location
            var totalCost = model.CostByMeter.Sum(m => m.Cost);
            model.CostByLocation = model.CostByMeter
                .GroupBy(m => string.IsNullOrEmpty(m.Location) ? "Unknown" : m.Location)
                .Select(g => new LocationCostItem
                {
                    Location = g.Key,
                    Cost = Math.Round(g.Sum(x => x.Cost), 0),
                    Percentage = totalCost > 0 ? Math.Round(g.Sum(x => x.Cost) / totalCost * 100, 1) : 0
                })
                .OrderByDescending(l => l.Cost)
                .ToList();

            // Daily Trend (30 days)
            model.DailyTrend = allData
                .Where(d => d.DateTime.HasValue)
                .GroupBy(d => d.DateTime!.Value.Date)
                .Select(g => new DailyCostItem
                {
                    Date = g.Key.ToString("MMM dd"),
                    Kwh = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)), 0),
                    Cost = Math.Round(g.Sum(x => CalculateCost((double)(x.kWh ?? 0), x.DateTime!.Value.Hour)), 0)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading cost dashboard");
            return View(model);
        }
    }
}
