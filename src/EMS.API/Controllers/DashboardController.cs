namespace EMS.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get Executive Dashboard with KPIs and charts
    /// </summary>
    [HttpGet("executive")]
    [ProducesResponseType(typeof(ExecutiveDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetExecutiveDashboard(
        [FromQuery] string plant = "All",
        [FromQuery] string building = "All",
        [FromQuery] string area = "All",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            var filter = new DashboardFilterDto
            {
                Plant = plant,
                Building = building,
                Area = area,
                DateFrom = dateFrom ?? DateTime.Now.Date,
                DateTo = dateTo ?? DateTime.Now.Date.AddDays(1).AddTicks(-1)
            };

            var result = await _dashboardService.GetExecutiveDashboardAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting executive dashboard");
            return BadRequest(new { error = ex.Message });
        }
    }
}
