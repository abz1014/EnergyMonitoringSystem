namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using System.Text.Json;

// Data Provenance:
// Table: Alarms | Columns: CreatedAt, AckTime, AckBy, Severity, DeviceName | Direct measurement
// Formula: Response Time = AckTime - CreatedAt
// Assumption: AckTime reflects when a human actually acknowledged the alarm in the UI, not
// when it was first noticed -- there can be a gap between "saw it" and "clicked acknowledge."
// Validatable against SCADA: yes, both timestamps are stored directly, no derivation beyond subtraction.
// Confidence: High for the calculation itself; the small sample size in this install (12 alarms
// total) limits how representative the statistics are -- explicitly disclosed on the page.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class AlarmResponseController : Controller
{
    private readonly IAlarmRepository _alarmRepo;
    private readonly ILogger<AlarmResponseController> _logger;

    public AlarmResponseController(IAlarmRepository alarmRepo, ILogger<AlarmResponseController> logger)
    {
        _alarmRepo = alarmRepo;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var allAlarms = await _alarmRepo.GetAllAlarms();
            ViewBag.HasData = allAlarms.Count > 0;
            if (allAlarms.Count == 0) return View();

            var acked = allAlarms.Where(a => a.AckTime.HasValue).ToList();
            var unacked = allAlarms.Where(a => !a.AckTime.HasValue).ToList();

            ViewBag.TotalAlarms = allAlarms.Count;
            ViewBag.AckedCount = acked.Count;
            ViewBag.UnackedCount = unacked.Count;

            if (acked.Count == 0)
            {
                ViewBag.HasResponseData = false;
                return View();
            }
            ViewBag.HasResponseData = true;

            var responseTimes = acked
                .Select(a => new
                {
                    Alarm = a,
                    ResponseMinutes = (a.AckTime!.Value - a.CreatedAt).TotalMinutes
                })
                .OrderByDescending(x => x.ResponseMinutes)
                .ToList();

            var avgResponseMin = responseTimes.Average(x => x.ResponseMinutes);
            var sortedTimes = responseTimes.Select(x => x.ResponseMinutes).OrderBy(x => x).ToList();
            var medianResponseMin = sortedTimes.Count % 2 == 0
                ? (sortedTimes[sortedTimes.Count / 2 - 1] + sortedTimes[sortedTimes.Count / 2]) / 2.0
                : sortedTimes[sortedTimes.Count / 2];

            ViewBag.AvgResponseMin = Math.Round(avgResponseMin, 0);
            ViewBag.MedianResponseMin = Math.Round(medianResponseMin, 0);
            ViewBag.SlowestResponseMin = Math.Round(responseTimes.Max(x => x.ResponseMinutes), 0);
            ViewBag.FastestResponseMin = Math.Round(responseTimes.Min(x => x.ResponseMinutes), 0);

            // By severity (1=info, 2=warning, 3=critical)
            var bySeverity = responseTimes
                .GroupBy(x => x.Alarm.Severity)
                .Select(g => new
                {
                    Severity = g.Key,
                    SeverityName = g.Key switch { 3 => "Critical", 2 => "Warning", _ => "Info" },
                    AvgMinutes = Math.Round(g.Average(x => x.ResponseMinutes), 0),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Severity)
                .ToList();
            ViewBag.BySeverity = bySeverity;

            // By acknowledger
            var byUser = responseTimes
                .Where(x => !string.IsNullOrEmpty(x.Alarm.AckBy))
                .GroupBy(x => x.Alarm.AckBy)
                .Select(g => new
                {
                    User = g.Key,
                    AvgMinutes = Math.Round(g.Average(x => x.ResponseMinutes), 0),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .ToList();
            ViewBag.ByUser = byUser;

            // Slowest 10 individual responses
            ViewBag.SlowestAlarms = responseTimes.Take(10).Select(x => new
            {
                x.Alarm.DeviceName,
                x.Alarm.Message,
                SeverityName = x.Alarm.Severity switch { 3 => "Critical", 2 => "Warning", _ => "Info" },
                x.Alarm.CreatedAt,
                ResponseMinutes = Math.Round(x.ResponseMinutes, 0)
            }).ToList();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading alarm response time analytics");
            return View("Error");
        }
    }
}
