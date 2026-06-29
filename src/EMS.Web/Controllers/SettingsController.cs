namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Web.Services;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly AppSettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(AppSettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _settingsService.GetAllSettingsAsync();
        var categories = settings.GroupBy(s => s.Category ?? "Other").OrderBy(g => g.Key).ToList();
        ViewBag.Categories = categories;
        ViewBag.Success = TempData["Success"];
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(IFormCollection form)
    {
        try
        {
            var userName = User.Identity?.Name ?? "unknown";
            var updates = new Dictionary<string, string>();

            foreach (var key in form.Keys)
            {
                if (key.StartsWith("setting_"))
                {
                    var settingKey = key.Substring(8);
                    var value = form[key].ToString().Trim();
                    updates[settingKey] = value;
                }
            }

            if (updates.Count > 0)
            {
                await _settingsService.UpdateMultipleAsync(updates, userName);
                _logger.LogInformation("Settings updated by {User}: {Count} values changed", userName, updates.Count);
                TempData["Success"] = $"{updates.Count} settings updated successfully.";
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            TempData["Success"] = "Error saving settings: " + ex.Message;
            return RedirectToAction("Index");
        }
    }
}
