namespace EMS.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

public class EnergyAnalysisController : Controller
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly ILogger<EnergyAnalysisController> _logger;

    public EnergyAnalysisController(IEnergyMeterRepository energyMeterRepository, ILogger<EnergyAnalysisController> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string timeframe = "daily", string compareWith = "")
    {
        try
        {
            // Get date range based on timeframe
            var (dateFrom, dateTo) = GetDateRange(timeframe);

            // Get consumption data
            var consumptionData = await _energyMeterRepository.GetByDateRange(dateFrom, dateTo);

            // Aggregate by timeframe
            var aggregatedData = AggregateByTimeframe(consumptionData, timeframe);

            // Get comparison data if requested
            var comparisonData = await GetComparisonData(timeframe, compareWith, dateFrom);

            // Calculate statistics
            var stats = CalculateStats(aggregatedData);

            var model = new EnergyAnalysisViewModel
            {
                Timeframe = timeframe,
                DateFrom = dateFrom,
                DateTo = dateTo,
                ConsumptionData = aggregatedData,
                ComparisonData = comparisonData,
                Statistics = stats,
                Peak = stats["peak"],
                Average = stats["average"],
                Minimum = stats["minimum"]
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

    private async Task<List<(DateTime, double)>> GetComparisonData(string timeframe, string compareWith, DateTime dateFrom)
    {
        if (string.IsNullOrEmpty(compareWith))
            return new();

        var (compFrom, compTo) = timeframe switch
        {
            "daily" when compareWith == "yesterday" => (dateFrom.AddDays(-1), dateFrom.AddTicks(-1)),
            "weekly" when compareWith == "lastweek" => (dateFrom.AddDays(-7), dateFrom.AddTicks(-1)),
            "monthly" when compareWith == "lastmonth" => (dateFrom.AddMonths(-1), dateFrom.AddTicks(-1)),
            _ => (DateTime.MinValue, DateTime.MinValue)
        };

        if (compFrom == DateTime.MinValue)
            return new();

        var data = await _energyMeterRepository.GetByDateRange(compFrom, compTo);
        return AggregateByTimeframe(data, timeframe);
    }

    private List<(DateTime, double)> AggregateByTimeframe(List<EMS.Core.Models.EnergyMeterData> data, string timeframe)
    {
        return timeframe switch
        {
            "daily" => data.GroupBy(d => d.DateTime.Hour)
                .Select(g => (new DateTime(2026, 1, 1, g.Key, 0, 0), g.Sum(x => x.kWh ?? 0)))
                .OrderBy(x => x.Item1)
                .ToList(),
            "weekly" => data.GroupBy(d => d.DateTime.Date)
                .Select(g => (g.Key, g.Sum(x => x.kWh ?? 0)))
                .OrderBy(x => x.Key)
                .ToList(),
            "monthly" => data.GroupBy(d => d.DateTime.Date)
                .Select(g => (g.Key, g.Sum(x => x.kWh ?? 0)))
                .OrderBy(x => x.Key)
                .ToList(),
            _ => data.GroupBy(d => d.DateTime.Date)
                .Select(g => (g.Key, g.Sum(x => x.kWh ?? 0)))
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

public class EnergyAnalysisViewModel
{
    public string Timeframe { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<(DateTime, double)> ConsumptionData { get; set; }
    public List<(DateTime, double)> ComparisonData { get; set; }
    public Dictionary<string, double> Statistics { get; set; }
    public double Peak { get; set; }
    public double Average { get; set; }
    public double Minimum { get; set; }
}
