namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class PfPenaltyController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<PfPenaltyController> _logger;

    public PfPenaltyController(IEnergyMeterRepository meterRepo, AppSettingsService settings, ILogger<PfPenaltyController> logger)
    {
        _meterRepo = meterRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(double? bill, double? actualPf, double? targetPf)
    {
        try
        {
            var defaultTargetPf = await _settings.GetDoubleAsync("General.PFTarget", 0.90);
            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");

            ViewBag.DefaultTargetPf = defaultTargetPf;
            ViewBag.Currency = currency;

            // Auto-fill actual PF from latest meter reading if not provided
            double? autoActualPf = null;
            var to = DateTime.Now.Date.AddDays(1);
            var data = await _meterRepo.GetByDateRange(to.AddDays(-30), to);
            if (data.Count == 0)
            {
                data = await _meterRepo.GetByDateRange(to.AddDays(-90), to);
            }
            var validPf = data.Where(d => d.PFL1.HasValue && d.PFL1.Value > 0).ToList();
            if (validPf.Count > 0)
            {
                autoActualPf = Math.Round(validPf.Average(d => d.PFL1!.Value), 3);
            }
            ViewBag.AutoActualPf = autoActualPf;

            // Auto-fill bill estimate from latest month kWh * tariff
            double? autoBill = null;
            var validKwh = data.Where(d => d.kWh.HasValue).ToList();
            if (validKwh.Count > 0)
            {
                autoBill = Math.Round(validKwh.Sum(d => (double)d.kWh!.Value) * tariffRate, 0);
            }
            ViewBag.AutoBill = autoBill;

            if (bill.HasValue && actualPf.HasValue && actualPf.Value > 0)
            {
                var target = targetPf ?? defaultTargetPf;

                if (actualPf.Value >= target)
                {
                    ViewBag.HasResult = true;
                    ViewBag.Penalty = 0.0;
                    ViewBag.IsPenalty = false;
                    ViewBag.Bill = bill.Value;
                    ViewBag.ActualPf = actualPf.Value;
                    ViewBag.TargetPf = target;
                }
                else
                {
                    // Standard PF penalty formula: Penalty = Bill * (Target PF / Actual PF - 1)
                    var penalty = bill.Value * (target / actualPf.Value - 1);
                    ViewBag.HasResult = true;
                    ViewBag.Penalty = Math.Round(penalty, 0);
                    ViewBag.IsPenalty = true;
                    ViewBag.Bill = bill.Value;
                    ViewBag.ActualPf = actualPf.Value;
                    ViewBag.TargetPf = target;
                    ViewBag.AnnualPenalty = Math.Round(penalty * 12, 0);
                }
            }
            else
            {
                ViewBag.HasResult = false;
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PF penalty calculator");
            return View("Error");
        }
    }
}
