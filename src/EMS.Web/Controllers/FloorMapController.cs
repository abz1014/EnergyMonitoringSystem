namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class FloorMapController : Controller
{
    private readonly ILiveMonitoringService _liveMonitoringService;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly ILogger<FloorMapController> _logger;

    public FloorMapController(ILiveMonitoringService liveMonitoringService, IMonitoringDeviceRepository deviceRepo, ILogger<FloorMapController> logger)
    {
        _liveMonitoringService = liveMonitoringService;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var filter = new LiveMonitoringFilterDto { Plant = "All", Building = "All", Status = "all", IncludeSparklines = false };
            var liveData = await _liveMonitoringService.GetLiveMetersAsync(filter);

            var devices = await _deviceRepo.GetAllDevices();
            var locations = devices
                .Where(d => d.DeviceID.HasValue)
                .GroupBy(d => d.DeviceID!.Value)
                .ToDictionary(g => g.Key, g => g.First().Location ?? "");

            ViewBag.HasData = liveData.Meters.Count > 0;
            ViewBag.Locations = locations;

            return View(liveData.Meters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading floor map");
            return View("Error");
        }
    }
}
