namespace EMS.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using EMS.Core.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class MetersController : ControllerBase
{
    private readonly ILiveMonitoringService _liveMonitoringService;
    private readonly IEnergyMeterRepository _meterRepository;
    private readonly ILogger<MetersController> _logger;

    public MetersController(
        ILiveMonitoringService liveMonitoringService,
        IEnergyMeterRepository meterRepository,
        ILogger<MetersController> logger)
    {
        _liveMonitoringService = liveMonitoringService;
        _meterRepository = meterRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get live meter readings with status and sparklines
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(typeof(LiveMonitoringResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLiveMeters(
        [FromQuery] string plant = "All",
        [FromQuery] string building = "All",
        [FromQuery] string status = "all",
        [FromQuery] bool includeSparklines = true)
    {
        try
        {
            var filter = new LiveMonitoringFilterDto
            {
                Plant = plant,
                Building = building,
                Status = status,
                IncludeSparklines = includeSparklines
            };

            var result = await _liveMonitoringService.GetLiveMetersAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live meters");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get detailed information for a specific meter
    /// </summary>
    [HttpGet("{meterId}/details")]
    [ProducesResponseType(typeof(MeterDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeterDetails(
        [FromRoute] int meterId,
        [FromQuery] string timeframe = "24h")
    {
        try
        {
            var latestReading = await _meterRepository.GetLatestReading(meterId);
            if (latestReading == null)
            {
                return NotFound(new { error = "Meter not found" });
            }

            var details = new MeterDetailsDto
            {
                MeterId = meterId,
                Name = $"Meter-{meterId}",
                Status = "online",
                LastUpdated = latestReading.DateTime,
                LiveValues = new MeterLiveValuesDto
                {
                    Voltage_L1N = latestReading.VoltL1N ?? 0,
                    Voltage_L2N = latestReading.VoltL2N ?? 0,
                    Voltage_L3N = latestReading.VoltL3N ?? 0,
                    Current_L1 = latestReading.CurrentL1 ?? 0,
                    Current_L2 = latestReading.CurrentL2 ?? 0,
                    Current_L3 = latestReading.CurrentL3 ?? 0,
                    Power_kW = latestReading.kWtotal ?? 0,
                    PowerFactor = latestReading.PFL1 ?? 0.96,
                    Frequency = latestReading.MFreq ?? 50.0
                }
            };

            return Ok(details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meter details");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class MeterDetailsDto
{
    public int MeterId { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public DateTime LastUpdated { get; set; }
    public MeterLiveValuesDto LiveValues { get; set; }
}

public class MeterLiveValuesDto
{
    public double Voltage_L1N { get; set; }
    public double Voltage_L2N { get; set; }
    public double Voltage_L3N { get; set; }
    public double Current_L1 { get; set; }
    public double Current_L2 { get; set; }
    public double Current_L3 { get; set; }
    public double Power_kW { get; set; }
    public double PowerFactor { get; set; }
    public double Frequency { get; set; }
}
