namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using EMS.Web.Services;

[Authorize]
public class MyDashboardController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IAlarmRepository _alarmRepo;
    private readonly AppSettingsService _settings;
    private readonly ScadaDbContext _context;
    private readonly ILogger<MyDashboardController> _logger;

    public MyDashboardController(IEnergyMeterRepository meterRepo, IAlarmRepository alarmRepo, AppSettingsService settings, ScadaDbContext context, ILogger<MyDashboardController> logger)
    {
        _meterRepo = meterRepo;
        _alarmRepo = alarmRepo;
        _settings = settings;
        _context = context;
        _logger = logger;
    }

    public static readonly Dictionary<string, string> WidgetCatalog = new()
    {
        ["total_kwh"] = "Total Consumption (30d)",
        ["total_cost"] = "Total Cost (30d)",
        ["peak_kw"] = "Peak Demand (30d)",
        ["avg_pf"] = "Average Power Factor (30d)",
        ["active_alarms"] = "Active Alarms",
        ["critical_alarms"] = "Critical Alarms",
        ["baseload_kw"] = "Estimated Baseload (kW)",
    };

    private string CurrentUserId => User.Identity?.Name ?? "anonymous";

    public async Task<IActionResult> Index()
    {
        try
        {
            var layout = await _context.UserDashboardWidgets.AsNoTracking()
                .Where(w => w.UserId == CurrentUserId)
                .OrderBy(w => w.Position)
                .ToListAsync();

            if (layout.Count == 0)
            {
                // Default layout for first-time users
                var defaults = new[] { "total_kwh", "total_cost", "peak_kw", "active_alarms" };
                layout = defaults.Select((k, i) => new UserDashboardWidget { WidgetKey = k, Position = i, UserId = CurrentUserId }).ToList();
            }

            var widgetValues = await ComputeWidgetValues();

            ViewBag.Layout = layout;
            ViewBag.WidgetValues = widgetValues;
            ViewBag.Catalog = WidgetCatalog;
            ViewBag.AvailableToAdd = WidgetCatalog.Keys.Except(layout.Select(l => l.WidgetKey)).ToList();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading custom dashboard");
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLayout([FromBody] List<string> widgetKeys)
    {
        try
        {
            var existing = await _context.UserDashboardWidgets.Where(w => w.UserId == CurrentUserId).ToListAsync();
            _context.UserDashboardWidgets.RemoveRange(existing);

            for (int i = 0; i < widgetKeys.Count; i++)
            {
                if (!WidgetCatalog.ContainsKey(widgetKeys[i])) continue;
                _context.UserDashboardWidgets.Add(new UserDashboardWidget { UserId = CurrentUserId, WidgetKey = widgetKeys[i], Position = i });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving dashboard layout");
            return Json(new { success = false });
        }
    }

    private async Task<Dictionary<string, string>> ComputeWidgetValues()
    {
        var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
        var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");
        var nightStart = await _settings.GetIntAsync("Shift.NightStart", 22);
        var nightEnd = await _settings.GetIntAsync("Shift.NightEnd", 6);

        var to = DateTime.Now.Date.AddDays(1);
        var data = await _meterRepo.GetByDateRange(to.AddDays(-30), to);
        if (data.Count == 0)
        {
            data = await _meterRepo.GetByDateRange(to.AddDays(-90), to);
        }
        var validData = data.Where(d => d.DateTime.HasValue).ToList();

        var totalKwh = validData.Sum(d => (double)(d.kWh ?? 0));
        var totalCost = totalKwh * tariffRate;
        var peakKw = validData.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).DefaultIfEmpty(0).Max();
        var pfValues = validData.Select(PowerFactorHelper.ThreePhaseAverage).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        var avgPf = pfValues.Count > 0 ? pfValues.Average() : 0;

        var allAlarms = await _alarmRepo.GetAllAlarms();
        var activeAlarms = allAlarms.Count(a => a.IsActive);
        var criticalAlarms = allAlarms.Count(a => a.IsActive && a.Severity == 3);

        bool IsNight(DateTime dt)
        {
            var hour = dt.Hour;
            return nightStart > nightEnd ? (hour >= nightStart || hour < nightEnd) : (hour >= nightStart && hour < nightEnd);
        }
        var nightValues = validData.Where(d => IsNight(d.DateTime!.Value) && d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).ToList();
        var baseloadKw = nightValues.Count > 0 ? nightValues.Average() : 0;

        return new Dictionary<string, string>
        {
            ["total_kwh"] = $"{Math.Round(totalKwh, 0):N0} kWh",
            ["total_cost"] = $"{currency} {Math.Round(totalCost, 0):N0}",
            ["peak_kw"] = $"{Math.Round(peakKw, 1)} kW",
            ["avg_pf"] = $"{Math.Round(avgPf, 3)}",
            ["active_alarms"] = $"{activeAlarms}",
            ["critical_alarms"] = $"{criticalAlarms}",
            ["baseload_kw"] = $"{Math.Round(baseloadKw, 1)} kW",
        };
    }
}
