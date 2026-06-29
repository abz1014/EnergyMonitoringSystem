namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using EMS.Core.Interfaces;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IValidator<DashboardFilterDto> _filterValidator;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IValidator<DashboardFilterDto> filterValidator,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _filterValidator = filterValidator;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string plant = "All", string building = "All", string area = "All")
    {
        try
        {
            var filter = new DashboardFilterDto
            {
                Plant = plant ?? "All",
                Building = building ?? "All",
                Area = area ?? "All",
                DateFrom = DateTime.Now.Date,
                DateTo = DateTime.Now.Date.AddDays(1).AddTicks(-1)
            };

            var validationResult = await _filterValidator.ValidateAsync(filter);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Dashboard filter validation failed: {@Errors}", validationResult.Errors);
                ModelState.AddModelError("filter", "Invalid filter parameters provided");
                return BadRequest(ModelState);
            }

            var dashboard = await _dashboardService.GetExecutiveDashboardAsync(filter);
            return View(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading executive dashboard");
            return View("Error");
        }
    }
}
