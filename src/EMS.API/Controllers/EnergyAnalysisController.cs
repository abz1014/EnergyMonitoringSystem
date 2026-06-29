namespace EMS.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class EnergyAnalysisController : ControllerBase
{
    private readonly IEnergyMeterRepository _energyMeterRepository;
    private readonly ILogger<EnergyAnalysisController> _logger;

    public EnergyAnalysisController(
        IEnergyMeterRepository energyMeterRepository,
        ILogger<EnergyAnalysisController> logger)
    {
        _energyMeterRepository = energyMeterRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get consumption data for analysis over a date range
    /// </summary>
    [HttpGet("consumption")]
    [ProducesResponseType(typeof(ConsumptionAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConsumptionAnalysis(
        [FromQuery] string timeframe = "daily",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string compareWith = "")
    {
        try
        {
            if (dateFrom == null || dateTo == null)
            {
                var today = DateTime.Now.Date;
                dateFrom = dateFrom ?? today;
                dateTo = dateTo ?? today.AddDays(1).AddTicks(-1);
            }

            var data = await _energyMeterRepository.GetByDateRange(dateFrom.Value, dateTo.Value);

            var analysis = new ConsumptionAnalysisDto
            {
                Timeframe = timeframe,
                DateFrom = dateFrom.Value,
                DateTo = dateTo.Value,
                TotalConsumption = data.Sum(d => d.kWh ?? 0),
                PeakValue = data.Max(d => (double?)(d.kWtotal ?? 0)) ?? 0,
                AverageValue = data.Any() ? data.Average(d => d.kWh ?? 0) : 0,
                MinimumValue = data.Min(d => (double?)(d.kWh ?? 0)) ?? 0,
                DataPoints = data.Count
            };

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consumption analysis");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get consumption trend for a specific timeframe
    /// </summary>
    [HttpGet("trend")]
    [ProducesResponseType(typeof(List<TrendPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConsumptionTrend(
        [FromQuery] string timeframe = "daily",
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        try
        {
            if (dateFrom == null || dateTo == null)
            {
                var today = DateTime.Now.Date;
                dateFrom = dateFrom ?? today;
                dateTo = dateTo ?? today.AddDays(1).AddTicks(-1);
            }

            var data = await _energyMeterRepository.GetByDateRange(dateFrom.Value, dateTo.Value);

            var trend = timeframe switch
            {
                "hourly" => data
                    .GroupBy(d => d.DateTime.Hour)
                    .Select(g => new TrendPointDto
                    {
                        Time = g.Key.ToString("D2") + ":00",
                        Value = g.Sum(x => x.kWh ?? 0)
                    })
                    .ToList(),
                "daily" or _ => data
                    .GroupBy(d => d.DateTime.Date)
                    .Select(g => new TrendPointDto
                    {
                        Time = g.Key.ToString("yyyy-MM-dd"),
                        Value = g.Sum(x => x.kWh ?? 0)
                    })
                    .ToList()
            };

            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consumption trend");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get top consumers for a date range
    /// </summary>
    [HttpGet("top-consumers")]
    [ProducesResponseType(typeof(List<TopConsumerAnalysisDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopConsumers(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            if (dateFrom == null || dateTo == null)
            {
                var today = DateTime.Now.Date;
                dateFrom = dateFrom ?? today;
                dateTo = dateTo ?? today.AddDays(1).AddTicks(-1);
            }

            var data = await _energyMeterRepository.GetByDateRange(dateFrom.Value, dateTo.Value);

            var topConsumers = data
                .GroupBy(d => d.MeterNo)
                .Select((g, i) => new TopConsumerAnalysisDto
                {
                    Rank = i + 1,
                    MeterId = g.Key,
                    Name = $"Meter-{g.Key}",
                    TotalConsumption = g.Sum(x => x.kWh ?? 0),
                    AverageConsumption = g.Average(x => x.kWh ?? 0)
                })
                .OrderByDescending(x => x.TotalConsumption)
                .Take(limit)
                .ToList();

            return Ok(topConsumers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top consumers");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class ConsumptionAnalysisDto
{
    public string Timeframe { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public double TotalConsumption { get; set; }
    public double PeakValue { get; set; }
    public double AverageValue { get; set; }
    public double MinimumValue { get; set; }
    public int DataPoints { get; set; }
}

public class TrendPointDto
{
    public string Time { get; set; }
    public double Value { get; set; }
}

public class TopConsumerAnalysisDto
{
    public int Rank { get; set; }
    public int MeterId { get; set; }
    public string Name { get; set; }
    public double TotalConsumption { get; set; }
    public double AverageConsumption { get; set; }
}
