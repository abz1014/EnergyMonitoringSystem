namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class WhatIfController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<WhatIfController> _logger;

    public WhatIfController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<WhatIfController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
            var targetPf = await _settings.GetDoubleAsync("General.PFTarget", 0.90);
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

            var to = DateTime.Now.Date.AddDays(1);
            var data = await _meterRepo.GetByDateRange(to.AddDays(-30), to);
            if (data.Count == 0)
            {
                data = await _meterRepo.GetByDateRange(to.AddDays(-90), to);
            }

            var monthlyKwh = data.Where(d => d.kWh.HasValue).Sum(d => (double)d.kWh!.Value);
            var pfValues = data.Where(d => d.PFL1.HasValue && d.PFL1.Value > 0).Select(d => d.PFL1!.Value).ToList();
            var avgPf = pfValues.Count > 0 ? pfValues.Average() : (double)targetPf;

            var monthlyBill = monthlyKwh * tariffRate;

            ViewBag.MonthlyKwh = Math.Round(monthlyKwh, 0);
            ViewBag.MonthlyBill = Math.Round(monthlyBill, 0);
            ViewBag.TariffRate = tariffRate;
            ViewBag.AvgPf = Math.Round(avgPf, 3);
            ViewBag.TargetPf = targetPf;
            ViewBag.Currency = currency;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading what-if modeler");
            return View("Error");
        }
    }
}
