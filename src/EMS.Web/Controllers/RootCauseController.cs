namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;
using EMS.Web.Services;

// Data Provenance:
// Tables: Alarms (CreatedAt, DeviceName, Severity, Message) + tblEnergyMetersData (meter
// readings near the alarm time) + tblDailyTemperature (same-day temp, if entered) + Settings
// (shift boundaries)
// THIS PAGE PERFORMS CORRELATION, NOT DIAGNOSIS. It surfaces facts that occurred near each
// alarm in time -- it does not claim any of them caused the alarm. Multiple unrelated things
// can coincide by chance, especially with a small alarm sample. Every section is phrased as
// "what else was happening" not "why this happened" -- this distinction is enforced in the
// view's wording, not just this comment, since that's where a user would actually misread it.
// Validatable against SCADA: the underlying facts (meter readings, alarm timestamps) are real
// and measured; the correlation/co-occurrence itself is not validatable as causation by this
// system and should not be treated as such.
// Confidence: Low-Medium for any inferred relationship; High for the underlying facts shown.
[Authorize(Roles = "Admin,Operator,Viewer")]
public class RootCauseController : Controller
{
    private readonly IAlarmRepository _alarmRepo;
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly ScadaDbContext _context;
    private readonly AppSettingsService _settings;
    private readonly ILogger<RootCauseController> _logger;

    public RootCauseController(IAlarmRepository alarmRepo, IEnergyMeterRepository meterRepo, ScadaDbContext context, AppSettingsService settings, ILogger<RootCauseController> logger)
    {
        _alarmRepo = alarmRepo;
        _meterRepo = meterRepo;
        _context = context;
        _settings = settings;
        _logger = logger;
    }

    public class AlarmContext
    {
        public int AlarmId { get; set; }
        public string DeviceName { get; set; } = "";
        public string Message { get; set; } = "";
        public string SeverityName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Shift { get; set; } = "";
        public double? SameDayTempC { get; set; }
        public int NearbyAlarmCount { get; set; }
        public double? PfNearTime { get; set; }
        public double? VoltImbalanceNearTime { get; set; }
        public double? FreqNearTime { get; set; }
        public bool IsWeekend { get; set; }
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var morningStart = await _settings.GetIntAsync("Shift.MorningStart", 6);
            var afternoonStart = await _settings.GetIntAsync("Shift.AfternoonStart", 14);
            var nightStart = await _settings.GetIntAsync("Shift.NightStart", 22);

            var allAlarms = await _alarmRepo.GetAllAlarms();
            ViewBag.HasData = allAlarms.Count > 0;
            if (allAlarms.Count == 0) return View(new List<AlarmContext>());

            var temps = await _context.DailyTemperatures.AsNoTracking().ToListAsync();
            var tempByDate = temps.ToDictionary(t => t.TempDate.Date, t => (double)t.AvgTempC);

            var results = new List<AlarmContext>();

            foreach (var alarm in allAlarms.OrderByDescending(a => a.CreatedAt))
            {
                var alarmTime = alarm.CreatedAt;
                var windowStart = alarmTime.AddMinutes(-30);
                var windowEnd = alarmTime.AddMinutes(30);

                // Nearby meter readings (same device/meter, +/- 30 min)
                var nearbyReadings = await _meterRepo.GetByDateRange(windowStart, windowEnd);
                var deviceReadings = nearbyReadings.Where(d => d.MeterNo == alarm.DeviceID).ToList();

                double? pf = null, voltImb = null, freq = null;
                if (deviceReadings.Count > 0)
                {
                    var pfVals = deviceReadings.Select(PowerFactorHelper.ThreePhaseAverage).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                    pf = pfVals.Count > 0 ? Math.Round(pfVals.Average(), 3) : null;

                    var imbVals = deviceReadings
                        .Where(d => d.VoltL1N.HasValue && d.VoltL2N.HasValue && d.VoltL3N.HasValue)
                        .Select(d =>
                        {
                            var v1 = d.VoltL1N!.Value; var v2 = d.VoltL2N!.Value; var v3 = d.VoltL3N!.Value;
                            var vAvg = (v1 + v2 + v3) / 3.0;
                            return vAvg > 0 ? (Math.Max(v1, Math.Max(v2, v3)) - Math.Min(v1, Math.Min(v2, v3))) / vAvg * 100.0 : 0;
                        }).ToList();
                    voltImb = imbVals.Count > 0 ? Math.Round(imbVals.Average(), 2) : null;

                    var freqVals = deviceReadings.Where(d => d.MFreq.HasValue && d.MFreq.Value > 0).Select(d => (double)d.MFreq!.Value).ToList();
                    freq = freqVals.Count > 0 ? Math.Round(freqVals.Average(), 2) : null;
                }

                // Other alarms within +/- 30 min (clustering -- multiple near-simultaneous alarms
                // often share one underlying cause, e.g. a supply dip affecting several meters)
                var nearbyAlarmCount = allAlarms.Count(a => a.AlarmID != alarm.AlarmID && a.CreatedAt >= windowStart && a.CreatedAt <= windowEnd);

                var hour = alarmTime.Hour;
                var shift = hour >= morningStart && hour < afternoonStart ? "Morning"
                    : hour >= afternoonStart && hour < nightStart ? "Afternoon"
                    : "Night";

                results.Add(new AlarmContext
                {
                    AlarmId = alarm.AlarmID,
                    DeviceName = alarm.DeviceName,
                    Message = alarm.Message,
                    SeverityName = alarm.Severity switch { 3 => "Critical", 2 => "Warning", _ => "Info" },
                    CreatedAt = alarmTime,
                    Shift = shift,
                    SameDayTempC = tempByDate.TryGetValue(alarmTime.Date, out var sameDayTemp) ? sameDayTemp : null,
                    NearbyAlarmCount = nearbyAlarmCount,
                    PfNearTime = pf,
                    VoltImbalanceNearTime = voltImb,
                    FreqNearTime = freq,
                    IsWeekend = alarmTime.DayOfWeek == DayOfWeek.Saturday || alarmTime.DayOfWeek == DayOfWeek.Sunday
                });
            }

            return View(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading root cause correlation");
            return View("Error");
        }
    }
}
