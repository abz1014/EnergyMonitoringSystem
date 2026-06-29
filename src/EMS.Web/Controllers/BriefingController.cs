namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Web.Services;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class BriefingController : Controller
{
    private readonly BriefingService _briefingService;

    public BriefingController(BriefingService briefingService)
    {
        _briefingService = briefingService;
    }

    public async Task<IActionResult> Index()
    {
        var userName = User.Identity?.Name ?? "User";
        var model = await _briefingService.BuildBriefingAsync(userName);
        return View(model);
    }
}
