namespace EMS.Web.Services;

using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Web.Models;

public class BriefingService
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly IAlarmRepository _alarmRepo;
    private readonly AppSettingsService _settings;
    private readonly ILogger<BriefingService> _logger;

    public BriefingService(
        IEnergyMeterRepository meterRepo,
        IMonitoringDeviceRepository deviceRepo,
        IAlarmRepository alarmRepo,
        AppSettingsService settings,
        ILogger<BriefingService> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _alarmRepo = alarmRepo;
        _settings = settings;
        _logger = logger;
    }

    public async Task<BriefingViewModel> BuildBriefingAsync(string userName)
    {
        var model = new BriefingViewModel { UserName = userName };

        var hour = DateTime.Now.Hour;
        model.Greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";

        try
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            var yesterdayEnd = today.AddTicks(-1);
            var weekAgo = today.AddDays(-7);

            var yesterdayData = await _meterRepo.GetByDateRange(yesterday, yesterdayEnd);

            if (yesterdayData.Count == 0)
            {
                var recentData = await _meterRepo.GetByDateRange(today.AddDays(-30), today);
                if (recentData.Count > 0)
                {
                    var latestDate = recentData.Max(d => d.DateTime ?? DateTime.MinValue).Date;
                    yesterdayData = recentData.Where(d => d.DateTime.HasValue && d.DateTime.Value.Date == latestDate).ToList();
                    yesterday = latestDate;
                    yesterdayEnd = latestDate.AddDays(1).AddTicks(-1);
                    model.DataDateLabel = latestDate.ToString("MMM dd, yyyy");
                }
            }
            else
            {
                model.DataDateLabel = "Yesterday";
            }

            model.ReportDate = yesterday;
            model.HasData = yesterdayData.Count > 0;

            if (!model.HasData)
                return model;

            model.TotalConsumption = yesterdayData.Sum(d => (double)(d.kWh ?? 0));

            weekAgo = yesterday.AddDays(-7);
            var weekData = await _meterRepo.GetByDateRange(weekAgo, yesterdayEnd);
            var dailyTotals = weekData
                .Where(d => d.DateTime.HasValue)
                .GroupBy(d => d.DateTime!.Value.Date)
                .Select(g => g.Sum(x => (double)(x.kWh ?? 0)))
                .ToList();
            model.SevenDayAverage = dailyTotals.Count > 0 ? dailyTotals.Average() : 0;

            model.ConsumptionChange = model.SevenDayAverage > 0
                ? Math.Round((model.TotalConsumption - model.SevenDayAverage) / model.SevenDayAverage * 100, 1)
                : 0;

            var peakReading = yesterdayData.OrderByDescending(d => d.kWtotal ?? 0).FirstOrDefault();
            model.PeakDemand = peakReading?.kWtotal ?? 0;
            model.PeakDemandTime = peakReading?.DateTime?.ToString("HH:mm") ?? "";

            var pfValues = yesterdayData.Select(PowerFactorHelper.ThreePhaseAverage).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            model.AveragePowerFactor = pfValues.Count > 0 ? Math.Round(pfValues.Average(), 3) : 0;

            model.ActiveAlarmCount = await _alarmRepo.GetActiveAlarmCount();

            // Scoped to EnergyMeter-type devices only, matching ReportingMeters below -- fuel
            // tanks and PLCs are also "active devices" but structurally never produce a kWh
            // reading, so counting them in the denominator here permanently capped this ratio
            // at ~50% even on a perfect day (confirmed: 3 of 6 active devices are EnergyMeter
            // type). That mismatch was the reason this page's Plant Score and Dashboard's Plant
            // Score disagreed -- Dashboard's equivalent ratio only ever counted IsActive flags,
            // not actual reporting, so it sat near 100% regardless. Both now measure the same
            // thing: energy meters that actually reported data vs. energy meters that exist.
            var devices = await _deviceRepo.GetAllDevices();
            var activeMeters = devices
                .Where(d => d.IsActive == 1 && d.DeviceType == "EnergyMeter")
                .GroupBy(d => d.DeviceID).Select(g => g.First()).ToList();
            model.TotalMeters = activeMeters.Count;

            var reportingMeterNos = yesterdayData.Where(d => d.MeterNo.HasValue).Select(d => d.MeterNo!.Value).Distinct().ToList();
            model.ReportingMeters = reportingMeterNos.Count;

            var topConsumer = yesterdayData
                .Where(d => d.MeterNo.HasValue)
                .GroupBy(d => d.MeterNo!.Value)
                .Select(g => new { MeterNo = g.Key, Total = g.Sum(x => (double)(x.kWh ?? 0)), Name = g.First().MeterName })
                .OrderByDescending(g => g.Total)
                .FirstOrDefault();

            if (topConsumer != null)
            {
                model.TopConsumerMeterNo = topConsumer.MeterNo;
                model.TopConsumerName = topConsumer.Name ?? $"Meter-{topConsumer.MeterNo}";
                model.TopConsumerKwh = Math.Round(topConsumer.Total, 0);
            }

            CalculateScore(model);
            GenerateInsights(model);
            await BuildWeeklyHealthAsync(model);
            await BuildMonthlyReportAsync(model);

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building briefing");
            model.HasData = false;
            return model;
        }
    }

    private void CalculateScore(BriefingViewModel model)
    {
        var dataRatio = model.TotalMeters > 0 ? (double)model.ReportingMeters / model.TotalMeters : 0;
        var result = PlantScoreCalculator.Calculate(model.AveragePowerFactor, model.ConsumptionChange, model.ActiveAlarmCount, dataRatio);

        model.PfScore = result.PfScore;
        model.ConsumptionScore = result.ConsumptionScore;
        model.AlarmScore = result.AlarmScore;
        model.PowerQualityScore = result.PowerQualityScore;
        model.DataQualityScore = result.DataQualityScore;
        model.PlantScore = result.Score;
    }

    private void GenerateInsights(BriefingViewModel model)
    {
        // Insight 1: Consumption trend
        if (model.ConsumptionChange > 5)
        {
            model.Insights.Add(new BriefingInsight
            {
                Icon = "&#9650;",
                Title = $"Consumption up {model.ConsumptionChange:F1}% vs average",
                Description = $"Total {model.TotalConsumption:F0} kWh compared to 7-day average of {model.SevenDayAverage:F0} kWh. {model.TopConsumerName} was the highest consumer at {model.TopConsumerKwh:F0} kWh.",
                Color = "#EF4444",
                ActionUrl = "/EnergyAnalysis",
                ActionLabel = "Analyze"
            });
        }
        else if (model.ConsumptionChange < -5)
        {
            model.Insights.Add(new BriefingInsight
            {
                Icon = "&#9660;",
                Title = $"Consumption down {Math.Abs(model.ConsumptionChange):F1}% vs average",
                Description = $"Total {model.TotalConsumption:F0} kWh — below the 7-day average of {model.SevenDayAverage:F0} kWh. Good efficiency.",
                Color = "#10B981",
                ActionUrl = "/EnergyAnalysis",
                ActionLabel = "View Details"
            });
        }
        else
        {
            model.Insights.Add(new BriefingInsight
            {
                Icon = "&#8594;",
                Title = "Consumption is stable",
                Description = $"Total {model.TotalConsumption:F0} kWh — within normal range of 7-day average ({model.SevenDayAverage:F0} kWh).",
                Color = "#3B82F6",
                ActionUrl = "/EnergyAnalysis",
                ActionLabel = "View Trend"
            });
        }

        // Insight 2: Peak demand
        model.Insights.Add(new BriefingInsight
        {
            Icon = "&#9889;",
            Title = $"Peak demand: {model.PeakDemand:F1} kW at {model.PeakDemandTime}",
            Description = $"Highest power draw recorded. {model.TopConsumerName} was the largest contributor.",
            Color = "#F59E0B",
            ActionUrl = "/EnergyAnalysis?metric=peak",
            ActionLabel = "View Peak Analysis"
        });

        // Insight 3: Power factor or alarms
        if (model.ActiveAlarmCount > 0)
        {
            model.Insights.Add(new BriefingInsight
            {
                Icon = "&#9888;",
                Title = $"{model.ActiveAlarmCount} active alarm{(model.ActiveAlarmCount > 1 ? "s" : "")} require attention",
                Description = "Unacknowledged alarms may indicate equipment issues. Review and acknowledge.",
                Color = "#EF4444",
                ActionUrl = "/LiveMonitoring",
                ActionLabel = "View Alarms"
            });
        }
        else if (model.AveragePowerFactor > 0 && model.AveragePowerFactor < 0.90)
        {
            model.Insights.Add(new BriefingInsight
            {
                Icon = "&#9888;",
                Title = $"Power factor below target: {model.AveragePowerFactor:F3}",
                Description = "Average PF is below 0.90 threshold. This may result in utility penalty charges. Check capacitor banks.",
                Color = "#F59E0B",
                ActionUrl = "/MeterFaceplate",
                ActionLabel = "Check Meters"
            });
        }
        else
        {
            model.Insights.Add(new BriefingInsight
            {
                Icon = "&#10003;",
                Title = $"Power factor healthy: {model.AveragePowerFactor:F3}",
                Description = "All power quality indicators within normal range. No alarms active.",
                Color = "#10B981",
                ActionUrl = "/MeterFaceplate",
                ActionLabel = "View Details"
            });
        }
    }

    // Sprint 5: Weekly Energy Health. Anchored to model.ReportDate (the latest date with actual
    // data, same fallback already resolved above) rather than DateTime.Now, so this stays
    // consistent with the rest of the briefing instead of comparing against a "today" the
    // database has no readings for.
    private async Task BuildWeeklyHealthAsync(BriefingViewModel model)
    {
        var weekEnd = model.ReportDate.AddDays(1).AddTicks(-1);
        var weekStart = model.ReportDate.AddDays(-6);
        var priorWeekEnd = weekStart.AddTicks(-1);
        var priorWeekStart = weekStart.AddDays(-7);

        var weekData = (await _meterRepo.GetByDateRange(weekStart, weekEnd))
            .ExcludeContaminated().ToList();
        var priorWeekData = (await _meterRepo.GetByDateRange(priorWeekStart, priorWeekEnd))
            .ExcludeContaminated().ToList();

        model.Weekly.WeekLabel = $"{weekStart:MMM dd} — {model.ReportDate:MMM dd, yyyy}";
        model.Weekly.TotalKwh = Math.Round(weekData.Sum(d => (double)(d.kWh ?? 0)), 0);

        var pfValues = weekData.Select(PowerFactorHelper.ThreePhaseAverage).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        model.Weekly.AvgPf = pfValues.Count > 0 ? Math.Round(pfValues.Average(), 3) : 0;

        var allAlarms = await _alarmRepo.GetAllAlarms();
        model.Weekly.AlarmCount = allAlarms.Count(a => a.CreatedAt >= weekStart && a.CreatedAt <= weekEnd);

        model.Weekly.HasPriorWeek = priorWeekData.Count > 0;
        if (model.Weekly.HasPriorWeek)
        {
            model.Weekly.PriorWeekKwh = Math.Round(priorWeekData.Sum(d => (double)(d.kWh ?? 0)), 0);
            model.Weekly.WowChangePct = model.Weekly.PriorWeekKwh > 0
                ? Math.Round((model.Weekly.TotalKwh - model.Weekly.PriorWeekKwh) / model.Weekly.PriorWeekKwh * 100, 1)
                : null;
        }
    }

    // Sprint 5: Monthly Energy Report. Same anchoring rationale as Weekly Energy Health above.
    private async Task BuildMonthlyReportAsync(BriefingViewModel model)
    {
        var monthStart = new DateTime(model.ReportDate.Year, model.ReportDate.Month, 1);
        var monthEnd = model.ReportDate.AddDays(1).AddTicks(-1);
        var priorMonthStart = monthStart.AddMonths(-1);
        var priorMonthEnd = monthStart.AddTicks(-1);

        var monthData = (await _meterRepo.GetByDateRange(monthStart, monthEnd))
            .ExcludeContaminated().ToList();
        var priorMonthData = (await _meterRepo.GetByDateRange(priorMonthStart, priorMonthEnd))
            .ExcludeContaminated().ToList();

        model.Monthly.MonthLabel = model.ReportDate.ToString("MMMM yyyy");
        model.Monthly.MonthToDateKwh = Math.Round(monthData.Sum(d => (double)(d.kWh ?? 0)), 0);
        model.Monthly.PeakKw = monthData.Count > 0 ? Math.Round(monthData.Max(d => d.kWtotal ?? 0), 1) : 0;

        var tariffRate = await _settings.GetDoubleAsync("Tariff.DefaultRate", 52.0);
        model.Monthly.EstimatedCost = Math.Round(model.Monthly.MonthToDateKwh * tariffRate, 0);

        model.Monthly.HasPriorMonth = priorMonthData.Count > 0;
        if (model.Monthly.HasPriorMonth)
        {
            model.Monthly.PriorMonthKwh = Math.Round(priorMonthData.Sum(d => (double)(d.kWh ?? 0)), 0);
            model.Monthly.MomChangePct = model.Monthly.PriorMonthKwh > 0
                ? Math.Round((model.Monthly.MonthToDateKwh - model.Monthly.PriorMonthKwh) / model.Monthly.PriorMonthKwh * 100, 1)
                : null;
        }
    }
}
