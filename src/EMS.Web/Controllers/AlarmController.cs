namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using System.Text.Json;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class AlarmController : Controller
{
    private readonly IAlarmRepository _alarmRepo;
    private readonly ILogger<AlarmController> _logger;

    public AlarmController(IAlarmRepository alarmRepo, ILogger<AlarmController> logger)
    {
        _alarmRepo = alarmRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index(byte? severity = null, string? device = null)
    {
        try
        {
            var allAlarms = await _alarmRepo.GetActiveAlarms();
            var acknowledgedAlarms = await GetAllAlarms();
            var combined = acknowledgedAlarms;

            if (severity.HasValue)
                combined = combined.Where(a => a.Severity == severity.Value).ToList();
            if (!string.IsNullOrEmpty(device))
                combined = combined.Where(a => a.DeviceName == device).ToList();

            var now = DateTime.Now;
            var activeCount = combined.Count(a => a.IsActive);
            var acknowledgedCount = combined.Count(a => !a.IsActive && a.AckTime.HasValue);

            var responseTimes = combined
                .Where(a => !a.IsActive && a.AckTime.HasValue)
                .Select(a => (a.AckTime!.Value - a.CreatedAt).TotalMinutes)
                .ToList();
            var avgResponseMinutes = responseTimes.Count > 0 ? responseTimes.Average() : 0;

            var worstDevice = combined
                .GroupBy(a => a.DeviceName)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            var distinctDevices = combined.Select(a => a.DeviceName).Distinct().OrderBy(d => d).ToList();

            // Build timeline data for ApexCharts rangeBar
            var timelineData = combined.Select(a =>
            {
                var start = a.CreatedAt;
                var end = a.AckTime ?? now;
                var color = a.Severity switch { 3 => "#EF4444", 2 => "#F59E0B", _ => "#3B82F6" };
                var label = a.Severity switch { 3 => "Critical", 2 => "Warning", _ => "Info" };
                return new
                {
                    device = a.DeviceName,
                    start = start.ToString("yyyy-MM-dd HH:mm"),
                    end = end.ToString("yyyy-MM-dd HH:mm"),
                    color,
                    label,
                    tag = a.TagName,
                    active = a.IsActive
                };
            }).ToList();

            // Group by device for rangeBar series
            var seriesGroups = timelineData.GroupBy(t => t.device).Select(g => new
            {
                name = g.Key,
                data = g.Select(t => new { x = t.tag + " (" + t.label + ")", y = new[] { t.start, t.end }, fillColor = t.color }).ToList()
            }).ToList();

            ViewBag.Alarms = combined;
            ViewBag.ActiveCount = activeCount;
            ViewBag.AcknowledgedCount = acknowledgedCount;
            ViewBag.TotalCount = combined.Count;
            ViewBag.AvgResponseMinutes = avgResponseMinutes;
            ViewBag.WorstDevice = worstDevice?.Key ?? "—";
            ViewBag.WorstDeviceCount = worstDevice?.Count() ?? 0;
            ViewBag.DistinctDevices = distinctDevices;
            ViewBag.SelectedSeverity = severity;
            ViewBag.SelectedDevice = device;
            ViewBag.TimelineJson = JsonSerializer.Serialize(timelineData);
            ViewBag.HasData = combined.Count > 0;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading alarm timeline");
            return View("Error");
        }
    }

    private async Task<List<Alarm>> GetAllAlarms()
    {
        var active = await _alarmRepo.GetActiveAlarms();
        // Also get recently acknowledged (workaround since repo only has GetActiveAlarms)
        // In production, add a GetAllAlarms method to the repo
        var all = new List<Alarm>(active);

        // Query acknowledged alarms via severity levels
        for (byte sev = 1; sev <= 3; sev++)
        {
            var bySev = await _alarmRepo.GetAlarmsBySeverity(sev);
            foreach (var a in bySev)
            {
                if (!all.Any(x => x.AlarmID == a.AlarmID))
                    all.Add(a);
            }
        }

        return all.OrderByDescending(a => a.CreatedAt).ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> Acknowledge(int alarmId)
    {
        var userName = User.Identity?.Name ?? "unknown";
        await _alarmRepo.AcknowledgeAlarm(alarmId, userName);
        _logger.LogInformation("Alarm {AlarmId} acknowledged by {User}", alarmId, userName);
        return RedirectToAction("Index");
    }
}
