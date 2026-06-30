namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class BudgetController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<BudgetController> _logger;

    public BudgetController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<BudgetController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var monthlyTarget = await _settings.GetDoubleAsync("Budget.MonthlyTargetKwh", 50000);
            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

            var now = DateTime.Now;
            var yearStart = new DateTime(now.Year, 1, 1);
            var data = await _meterRepo.GetByDateRange(yearStart, now.AddDays(1));

            // Determine reference "now" — use latest data date if current data is sparse
            var refDate = now;
            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(now.AddDays(-400), now.AddDays(1));
                if (recent.Count > 0)
                {
                    refDate = recent.Max(d => d.DateTime ?? now);
                    yearStart = new DateTime(refDate.Year, 1, 1);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= yearStart).ToList();
                }
            }

            var validData = data.Where(d => d.DateTime.HasValue).ToList();
            ViewBag.HasData = validData.Count > 0;
            if (validData.Count == 0) return View();

            var monthlyActuals = validData
                .GroupBy(d => d.DateTime!.Value.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => (double)(x.kWh ?? 0)));

            var months = new List<string>();
            var actualValues = new List<double>();
            var targetValues = new List<double>();
            var variancePcts = new List<double>();

            double ytdActual = 0;
            double ytdTarget = 0;

            for (int m = 1; m <= refDate.Month; m++)
            {
                var actual = monthlyActuals.GetValueOrDefault(m, 0);
                months.Add(new DateTime(refDate.Year, m, 1).ToString("MMM"));
                actualValues.Add(Math.Round(actual, 0));
                targetValues.Add(monthlyTarget);
                variancePcts.Add(monthlyTarget > 0 ? Math.Round((actual - monthlyTarget) / monthlyTarget * 100, 1) : 0);

                ytdActual += actual;
                ytdTarget += monthlyTarget;
            }

            var ytdVariance = ytdTarget > 0 ? Math.Round((ytdActual - ytdTarget) / ytdTarget * 100, 1) : 0;
            var ytdVarianceCost = (ytdActual - ytdTarget) * tariffRate;

            var currentMonthActual = monthlyActuals.GetValueOrDefault(refDate.Month, 0);
            var currentMonthVariance = monthlyTarget > 0 ? Math.Round((currentMonthActual - monthlyTarget) / monthlyTarget * 100, 1) : 0;

            ViewBag.Months = JsonSerializer.Serialize(months);
            ViewBag.ActualValues = JsonSerializer.Serialize(actualValues);
            ViewBag.TargetValues = JsonSerializer.Serialize(targetValues);
            ViewBag.MonthlyTarget = monthlyTarget;
            ViewBag.YtdActual = Math.Round(ytdActual, 0);
            ViewBag.YtdTarget = Math.Round(ytdTarget, 0);
            ViewBag.YtdVariance = ytdVariance;
            ViewBag.YtdVarianceCost = Math.Round(ytdVarianceCost, 0);
            ViewBag.CurrentMonthActual = Math.Round(currentMonthActual, 0);
            ViewBag.CurrentMonthVariance = currentMonthVariance;
            ViewBag.Currency = currency;
            ViewBag.Year = refDate.Year;
            ViewBag.MonthsElapsed = refDate.Month;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading budget analysis");
            return View("Error");
        }
    }
}
