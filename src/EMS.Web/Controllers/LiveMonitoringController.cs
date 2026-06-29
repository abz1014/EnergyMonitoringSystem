namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using EMS.Core.Interfaces;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class LiveMonitoringController : Controller
{
    private readonly ILiveMonitoringService _liveMonitoringService;
    private readonly IValidator<LiveMonitoringFilterDto> _filterValidator;
    private readonly ILogger<LiveMonitoringController> _logger;

    public LiveMonitoringController(
        ILiveMonitoringService liveMonitoringService,
        IValidator<LiveMonitoringFilterDto> filterValidator,
        ILogger<LiveMonitoringController> logger)
    {
        _liveMonitoringService = liveMonitoringService;
        _filterValidator = filterValidator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string plant = "All", string building = "All", string status = "all")
    {
        try
        {
            var filter = new LiveMonitoringFilterDto
            {
                Plant = plant ?? "All",
                Building = building ?? "All",
                Status = status ?? "all",
                IncludeSparklines = true
            };

            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Live monitoring filter validation failed: {@Errors}", validationResult.Errors);
                ModelState.AddModelError("filter", "Invalid filter parameters provided");
                return BadRequest(ModelState);
            }

            var liveData = await _liveMonitoringService.GetLiveMetersAsync(filter);
            return View(liveData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading live monitoring data");
            return View("Error");
        }
    }
}
