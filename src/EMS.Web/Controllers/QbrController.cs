namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Services;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class QbrController : Controller
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IAlarmRepository _alarmRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<QbrController> _logger;

    public QbrController(IEnergyMeterRepository meterRepo, IAlarmRepository alarmRepo, AppSettingsService settings, ILogger<QbrController> logger)
    {
        _meterRepo = meterRepo;
        _alarmRepo = alarmRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? quarter, int? year)
    {
        try
        {
            var now = DateTime.Now;
            var targetYear = year ?? now.Year;
            var targetQuarter = quarter ?? ((now.Month - 1) / 3 + 1);

            var qStartMonth = (targetQuarter - 1) * 3 + 1;
            var qStart = new DateTime(targetYear, qStartMonth, 1);
            var qEnd = qStart.AddMonths(3);

            var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
            var targetPf = await _settings.GetDoubleAsync("General.PFTarget", 0.90);
            var currency = await _settings.GetAsync("Tariff.Currency", "Rs.");
            var companyName = await _settings.GetAsync("General.CompanyName", "Energy Monitoring System");

            var data = await _meterRepo.GetByDateRange(qStart, qEnd);

            // If the requested quarter has no data, fall back to the quarter containing the most recent reading
            if (data.Count == 0)
            {
                var recent = await _meterRepo.GetByDateRange(now.AddYears(-2), now.AddDays(1));
                if (recent.Count > 0)
                {
                    var latest = recent.Max(d => d.DateTime ?? now);
                    targetYear = latest.Year;
                    targetQuarter = (latest.Month - 1) / 3 + 1;
                    qStartMonth = (targetQuarter - 1) * 3 + 1;
                    qStart = new DateTime(targetYear, qStartMonth, 1);
                    qEnd = qStart.AddMonths(3);
                    data = recent.Where(d => d.DateTime.HasValue && d.DateTime.Value >= qStart && d.DateTime.Value < qEnd).ToList();
                }
            }

            ViewBag.HasData = data.Count > 0;
            ViewBag.Quarter = targetQuarter;
            ViewBag.Year = targetYear;
            ViewBag.QuarterLabel = $"Q{targetQuarter} {targetYear}";
            ViewBag.PeriodLabel = $"{qStart:MMM dd, yyyy} — {qEnd.AddDays(-1):MMM dd, yyyy}";
            ViewBag.CompanyName = companyName;
            ViewBag.Currency = currency;
            ViewBag.GeneratedAt = now;

            if (data.Count == 0) return View();

            var validData = data.Where(d => d.DateTime.HasValue).ToList();

            var totalKwh = validData.Sum(d => (double)(d.kWh ?? 0));
            var totalCost = totalKwh * tariffRate;
            var peakKw = validData.Where(d => d.kWtotal.HasValue).Select(d => d.kWtotal!.Value).DefaultIfEmpty(0).Max();
            var pfValues = validData.Where(d => d.PFL1.HasValue && d.PFL1.Value > 0).Select(d => d.PFL1!.Value).ToList();
            var avgPf = pfValues.Count > 0 ? pfValues.Average() : 0;

            var pfPenalty = avgPf > 0 && avgPf < targetPf ? totalCost * ((double)targetPf / avgPf - 1) : 0;

            // Monthly breakdown within the quarter
            var monthly = validData
                .GroupBy(d => d.DateTime!.Value.Month)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Month = new DateTime(targetYear, g.Key, 1).ToString("MMM"),
                    Kwh = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)), 0),
                    Cost = Math.Round(g.Sum(x => (double)(x.kWh ?? 0)) * tariffRate, 0)
                }).ToList();

            // Alarms in the quarter
            var allAlarms = await _alarmRepo.GetAllAlarms();
            var quarterAlarms = allAlarms.Where(a => a.CreatedAt >= qStart && a.CreatedAt < qEnd).ToList();
            var criticalAlarms = quarterAlarms.Count(a => a.Severity == 3);

            // Weekend waste estimate (same logic as B4)
            bool IsWeekend(DateTime dt) => dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
            var weekdayKwh = validData.Where(d => !IsWeekend(d.DateTime!.Value)).Sum(d => (double)(d.kWh ?? 0));
            var weekendKwh = validData.Where(d => IsWeekend(d.DateTime!.Value)).Sum(d => (double)(d.kWh ?? 0));
            var weekdayDays = validData.Where(d => !IsWeekend(d.DateTime!.Value)).Select(d => d.DateTime!.Value.Date).Distinct().Count();
            var weekendDays = validData.Where(d => IsWeekend(d.DateTime!.Value)).Select(d => d.DateTime!.Value.Date).Distinct().Count();
            var weekdayAvgDaily = weekdayDays > 0 ? weekdayKwh / weekdayDays : 0;
            var weekendAvgDaily = weekendDays > 0 ? weekendKwh / weekendDays : 0;
            var expectedWeekendDaily = weekdayAvgDaily * 0.20;
            var excessWeekendDaily = Math.Max(weekendAvgDaily - expectedWeekendDaily, 0);
            var weekendWasteCost = excessWeekendDaily * weekendDays * tariffRate;

            ViewBag.TotalKwh = Math.Round(totalKwh, 0);
            ViewBag.TotalCost = Math.Round(totalCost, 0);
            ViewBag.PeakKw = Math.Round(peakKw, 1);
            ViewBag.AvgPf = Math.Round(avgPf, 3);
            ViewBag.PfPenalty = Math.Round(pfPenalty, 0);
            ViewBag.WeekendWasteCost = Math.Round(weekendWasteCost, 0);
            ViewBag.TotalSavingsOpportunity = Math.Round(pfPenalty + weekendWasteCost, 0);
            ViewBag.AlarmCount = quarterAlarms.Count;
            ViewBag.CriticalAlarmCount = criticalAlarms;
            ViewBag.Monthly = monthly;
            ViewBag.RecordCount = validData.Count;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QBR");
            return View("Error");
        }
    }
}
