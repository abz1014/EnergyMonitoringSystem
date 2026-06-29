namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
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
