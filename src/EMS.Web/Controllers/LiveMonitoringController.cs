namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

public class LiveMonitoringController : Controller
{
    private readonly ILiveMonitoringService _liveMonitoringService;
    private readonly ILogger<LiveMonitoringController> _logger;

    public LiveMonitoringController(ILiveMonitoringService liveMonitoringService, ILogger<LiveMonitoringController> logger)
    {
        _liveMonitoringService = liveMonitoringService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var liveData = await _liveMonitoringService.GetLiveMetersAsync(new()
            {
                Plant = "All",
                Building = "All",
                Status = "all",
                IncludeSparklines = true
            });

            return View(liveData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading live monitoring data");
            return View("Error");
        }
    }
}
