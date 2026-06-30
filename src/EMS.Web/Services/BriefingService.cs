namespace EMS.Web.Services;

using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Web.Models;

public class BriefingService
{
    private readonly IEnergyMeterRepository _meterRepo;
    private readonly IMonitoringDeviceRepository _deviceRepo;
    private readonly IAlarmRepository _alarmRepo;
    private readonly ILogger<BriefingService> _logger;

    public BriefingService(
        IEnergyMeterRepository meterRepo,
        IMonitoringDeviceRepository deviceRepo,
        IAlarmRepository alarmRepo,
        ILogger<BriefingService> logger)
    {
        _meterRepo = meterRepo;
        _deviceRepo = deviceRepo;
        _alarmRepo = alarmRepo;
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

            var devices = await _deviceRepo.GetAllDevices();
            var activeDevices = devices.Where(d => d.IsActive == 1).GroupBy(d => d.DeviceID).Select(g => g.First()).ToList();
            model.TotalMeters = activeDevices.Count;

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
                model.TopConsumerName = topConsumer.Name ?? $"Meter-{topConsumer.MeterNo}";
                model.TopConsumerKwh = Math.Round(topConsumer.Total, 0);
            }

            CalculateScore(model);
            GenerateInsights(model);

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
}
