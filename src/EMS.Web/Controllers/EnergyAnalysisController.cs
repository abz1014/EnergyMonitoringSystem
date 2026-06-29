namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;
using EMS.Web.Models;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class EnergyAnalysisController : Controller
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly ILogger<EnergyAnalysisController> _logger;

    public EnergyAnalysisController(IEnergyMeterRepository energyMeterRepository, ILogger<EnergyAnalysisController> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _logger = logger;
    }

    private static readonly HashSet<string> ValidTimeframes = new() { "daily", "weekly", "monthly", "yearly" };
    private static readonly HashSet<string> ValidMetrics = new() { "kwh", "peak", "kva", "kvar" };
    private static readonly HashSet<string> ValidCompare = new() { "", "yesterday", "lastweek", "lastmonth" };

    public async Task<IActionResult> Index(string timeframe = "daily", string compareWith = "", string metric = "kwh", string view = "peak")
    {
        try
        {
            if (!ValidTimeframes.Contains(timeframe)) timeframe = "daily";
            if (!ValidMetrics.Contains(metric)) metric = "kwh";
            if (!ValidCompare.Contains(compareWith)) compareWith = "";

            _logger.LogInformation("Energy Analysis: timeframe={timeframe}, metric={metric}, compare={compare}", timeframe, metric, compareWith);

            var (dateFrom, dateTo) = GetDateRange(timeframe);

            List<EMS.Core.Models.EnergyMeterData> consumptionData = new();
            try
            {
                consumptionData = await _energyMeterRepository.GetByDateRange(dateFrom, dateTo);
            }
            catch (Exception dbEx)
            {
                _logger.LogWarning(dbEx, "Database query failed for date range {From} to {To}", dateFrom, dateTo);
            }

            consumptionData ??= new();

            var aggregatedData = AggregateByTimeframe(consumptionData, timeframe, metric);

            var comparisonData = await GetComparisonData(timeframe, compareWith, dateFrom, metric);
            comparisonData ??= new();

            var stats = CalculateStats(aggregatedData);

            var model = new EnergyAnalysisViewModel
            {
                Timeframe = timeframe,
                Metric = metric,
                CompareWith = compareWith,
                DateFrom = dateFrom,
                DateTo = dateTo,
                ConsumptionData = aggregatedData ?? new(),
                ComparisonData = comparisonData,
                Statistics = stats,
                Peak = stats.ContainsKey("peak") ? stats["peak"] : 0,
                Average = stats.ContainsKey("average") ? stats["average"] : 0,
                Minimum = stats.ContainsKey("minimum") ? stats["minimum"] : 0
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading energy analysis data");
            return View("Error");
        }
    }

    private (DateTime, DateTime) GetDateRange(string timeframe)
    {
        var today = DateTime.Now.Date;
        return timeframe switch
        {
            "daily" => (today, today.AddDays(1).AddTicks(-1)),
            "weekly" => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(7 - (int)today.DayOfWeek).AddTicks(-1)),
            "monthly" => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1).AddTicks(-1)),
            "yearly" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year, 12, 31).AddTicks(-1)),
            _ => (today.AddDays(-30), today.AddDays(1).AddTicks(-1))
        };
    }

    private async Task<List<(DateTime, double)>> GetComparisonData(string timeframe, string compareWith, DateTime dateFrom, string metric = "kwh")
    {
        if (string.IsNullOrEmpty(compareWith))
            return new();

        var (compFrom, compTo) = compareWith switch
        {
            "yesterday" => (dateFrom.AddDays(-1), dateFrom.AddTicks(-1)),
            "lastweek" => (dateFrom.AddDays(-7), dateFrom.AddTicks(-1)),
            "lastmonth" => (dateFrom.AddMonths(-1), dateFrom.AddTicks(-1)),
            _ => (DateTime.MinValue, DateTime.MinValue)
        };

        if (compFrom == DateTime.MinValue)
            return new();

        try
        {
            var data = await _energyMeterRepository.GetByDateRange(compFrom, compTo);
            if (data == null || data.Count == 0)
                return new();
            return AggregateByTimeframe(data, timeframe, metric);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch comparison data");
            return new();
        }
    }

    private static double ExtractMetric(EMS.Core.Models.EnergyMeterData d, string metric)
    {
        return metric switch
        {
            "peak" => d.kWtotal ?? 0,
            "kva" => (d.kWtotal ?? 0) / Math.Max(d.PFL1 ?? 0.9, 0.1),
            "kvar" => (d.kWtotal ?? 0) * Math.Tan(Math.Acos(Math.Min(d.PFL1 ?? 0.9, 1.0))),
            _ => d.kWh ?? 0
        };
    }

    private List<(DateTime, double)> AggregateByTimeframe(List<EMS.Core.Models.EnergyMeterData> data, string timeframe, string metric = "kwh")
    {
        return timeframe switch
        {
            "daily" => data.GroupBy(d => d.DateTime.Hour)
                .Select(g => (new DateTime(2026, 1, 1, g.Key, 0, 0), g.Sum(x => ExtractMetric(x, metric))))
                .OrderBy(x => x.Item1)
                .ToList(),
            _ => data.GroupBy(d => d.DateTime.Date)
                .Select(g => (g.Key, g.Sum(x => ExtractMetric(x, metric))))
                .OrderBy(x => x.Key)
                .ToList()
        };
    }

    private Dictionary<string, double> CalculateStats(List<(DateTime, double)> data)
    {
        var values = data.Select(x => x.Item2).ToList();
        return new()
        {
            { "peak", values.Any() ? values.Max() : 0 },
            { "average", values.Any() ? values.Average() : 0 },
            { "minimum", values.Any() ? values.Min() : 0 },
            { "total", values.Sum() }
        };
    }

}
