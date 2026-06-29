namespace EMS.Web.Services;

using EMS.Core.Interfaces;
using EMS.Core.Models;

public class EnergyAnalysisService : IEnergyAnalysisService
{
    private readonly IEnergyMeterRepository _repo;
    private readonly ILogger<EnergyAnalysisService> _logger;

    private static readonly HashSet<string> ValidTimeframes = new() { "daily", "weekly", "monthly", "yearly" };
    private static readonly HashSet<string> ValidMetrics = new() { "kwh", "peak", "kva", "kvar" };
    private static readonly HashSet<string> ValidCompare = new() { "", "yesterday", "lastweek", "lastmonth" };

    public EnergyAnalysisService(IEnergyMeterRepository repo, ILogger<EnergyAnalysisService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<EnergyAnalysisResultDto> GetAnalysisAsync(string timeframe, string metric, string compareWith)
    {
        if (!ValidTimeframes.Contains(timeframe)) timeframe = "daily";
        if (!ValidMetrics.Contains(metric)) metric = "kwh";
        if (!ValidCompare.Contains(compareWith)) compareWith = "";

        var (dateFrom, dateTo) = GetDateRange(timeframe);

        List<EnergyMeterData> consumptionData;
        try
        {
            consumptionData = await _repo.GetByDateRange(dateFrom, dateTo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database query failed for {From} to {To}", dateFrom, dateTo);
            consumptionData = new();
        }

        var aggregated = AggregateByTimeframe(consumptionData, timeframe, metric);
        var comparison = await GetComparisonData(timeframe, compareWith, dateFrom, metric);
        var stats = CalculateStats(aggregated);
        var rows = BuildRows(consumptionData, timeframe, metric);

        return new EnergyAnalysisResultDto
        {
            Timeframe = timeframe,
            Metric = metric,
            CompareWith = compareWith,
            DateFrom = dateFrom,
            DateTo = dateTo,
            ConsumptionData = aggregated,
            ComparisonData = comparison,
            Rows = rows,
            Statistics = stats,
            Peak = stats.GetValueOrDefault("peak"),
            Average = stats.GetValueOrDefault("average"),
            Minimum = stats.GetValueOrDefault("minimum")
        };
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

    private async Task<List<(DateTime, double)>> GetComparisonData(string timeframe, string compareWith, DateTime dateFrom, string metric)
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

        if (compFrom == DateTime.MinValue) return new();

        try
        {
            var data = await _repo.GetByDateRange(compFrom, compTo);
            return data.Count == 0 ? new() : AggregateByTimeframe(data, timeframe, metric);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch comparison data");
            return new();
        }
    }

    private static double ExtractMetric(EnergyMeterData d, string metric)
    {
        return metric switch
        {
            "peak" => d.kWtotal ?? 0,
            "kva" => d.kVAtotal ?? ((d.kWtotal ?? 0) / Math.Max(d.PFL1 ?? 0.9, 0.1)),
            "kvar" => d.kVARtotal ?? ((d.kWtotal ?? 0) * Math.Tan(Math.Acos(Math.Clamp(d.PFL1 ?? 0.9, 0, 1)))),
            _ => d.kWh ?? 0
        };
    }

    private static List<(DateTime, double)> AggregateByTimeframe(List<EnergyMeterData> data, string timeframe, string metric)
    {
        return timeframe switch
        {
            "daily" => data.GroupBy(d => d.DateTime.Hour)
                .Select(g => (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, g.Key, 0, 0), g.Sum(x => ExtractMetric(x, metric))))
                .OrderBy(x => x.Item1)
                .ToList(),
            _ => data.GroupBy(d => d.DateTime.Date)
                .Select(g => (g.Key, g.Sum(x => ExtractMetric(x, metric))))
                .OrderBy(x => x.Key)
                .ToList()
        };
    }

    private static List<AnalysisRowDto> BuildRows(List<EnergyMeterData> data, string timeframe, string metric)
    {
        var groups = timeframe == "daily"
            ? data.GroupBy(d => d.DateTime.Hour).Select(g => (Date: new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, g.Key, 0, 0), Items: g.ToList()))
            : data.GroupBy(d => d.DateTime.Date).Select(g => (Date: g.Key, Items: g.ToList()));

        return groups.OrderBy(g => g.Date).Select(g =>
        {
            var values = g.Items.Select(x => ExtractMetric(x, metric)).ToList();
            return new AnalysisRowDto
            {
                Date = g.Date,
                Total = values.Sum(),
                Peak = values.Count > 0 ? values.Max() : 0,
                Average = values.Count > 0 ? values.Average() : 0,
                Min = values.Count > 0 ? values.Min() : 0
            };
        }).ToList();
    }

    private static Dictionary<string, double> CalculateStats(List<(DateTime, double)> data)
    {
        var values = data.Select(x => x.Item2).ToList();
        return new()
        {
            { "peak", values.Count > 0 ? values.Max() : 0 },
            { "average", values.Count > 0 ? values.Average() : 0 },
            { "minimum", values.Count > 0 ? values.Min() : 0 },
            { "total", values.Sum() }
        };
    }
}
