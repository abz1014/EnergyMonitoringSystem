namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Models;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class EnergyAnalysisController : Controller
{
    private readonly IEnergyAnalysisService _analysisService;
    private readonly ILogger<EnergyAnalysisController> _logger;

    public EnergyAnalysisController(IEnergyAnalysisService analysisService, ILogger<EnergyAnalysisController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string timeframe = "daily", string compareWith = "", string metric = "kwh", string view = "peak")
    {
        try
        {
            var result = await _analysisService.GetAnalysisAsync(timeframe, metric, compareWith);

            var model = new EnergyAnalysisViewModel
            {
                Timeframe = result.Timeframe,
                Metric = result.Metric,
                CompareWith = result.CompareWith,
                DateFrom = result.DateFrom,
                DateTo = result.DateTo,
                ConsumptionData = result.ConsumptionData,
                ComparisonData = result.ComparisonData,
                Rows = result.Rows,
                Statistics = result.Statistics,
                Peak = result.Peak,
                Average = result.Average,
                Minimum = result.Minimum
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading energy analysis");
            return View("Error");
        }
    }
}
