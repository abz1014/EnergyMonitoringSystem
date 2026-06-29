namespace EMS.Web.Services;

using EMS.Core.Interfaces;

public class LiveMonitoringService : ILiveMonitoringService
{
    private readonly ILogger<LiveMonitoringService> _logger;

    public LiveMonitoringService(ILogger<LiveMonitoringService> logger)
    {
        _logger = logger;
    }

    public async Task<LiveMonitoringResponseDto> GetLiveMetersAsync(LiveMonitoringFilterDto filter)
    {
        try
        {
            _logger.LogInformation("Generating mock live monitoring data");
            return await Task.FromResult(GenerateMockLiveData());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in live monitoring service, returning empty data");
            return new LiveMonitoringResponseDto
            {
                Meters = new(),
                StatusSummary = new(),
                ActiveAlarms = new()
            };
        }
    }

    private LiveMonitoringResponseDto GenerateMockLiveData()
    {
        var random = new Random(42);
        var meters = new List<MeterLiveDto>();

        for (int i = 1; i <= 5; i++)
        {
            meters.Add(new MeterLiveDto
            {
                MeterId = i,
                Name = $"Meter-{i}",
                Status = random.Next(100) > 20 ? "online" : (random.Next(2) == 0 ? "warning" : "offline"),
                Voltage = new VoltageReadingDto
                {
                    L1 = 230 + random.Next(-10, 10),
                    L2 = 230 + random.Next(-10, 10),
                    L3 = 230 + random.Next(-10, 10)
                },
                Current = new CurrentReadingDto
                {
                    L1 = 10 + random.Next(-2, 5),
                    L2 = 10 + random.Next(-2, 5),
                    L3 = 10 + random.Next(-2, 5)
                },
                Power = new PowerReadingDto
                {
                    kW = 2.5 + random.Next(-1, 3),
                    kVA = 3.0 + random.Next(-1, 3),
                    kVAR = 1.0 + random.Next(0, 2)
                },
                PowerFactor = 0.95 + (random.NextDouble() * 0.05),
                Frequency = 50 + random.Next(-1, 2),
                LastUpdated = DateTime.Now.AddSeconds(-random.Next(0, 30)),
                Sparkline = GenerateMockSparkline()
            });
        }

        return new LiveMonitoringResponseDto
        {
            Meters = meters,
            StatusSummary = new StatusSummaryDto
            {
                Online = meters.Count(m => m.Status == "online"),
                Offline = meters.Count(m => m.Status == "offline"),
                Warning = meters.Count(m => m.Status == "warning"),
                Unknown = meters.Count(m => m.Status == "unknown")
            },
            ActiveAlarms = new List<AlarmDto>
            {
                new AlarmDto
                {
                    Id = 1,
                    MeterId = 2,
                    DeviceName = "Meter-2",
                    Severity = "warning",
                    Message = "Voltage deviation detected on Phase L1",
                    CreatedAt = DateTime.Now.AddMinutes(-5)
                }
            }
        };
    }

    private List<double> GenerateMockSparkline()
    {
        var random = new Random();
        return Enumerable.Range(0, 24).Select(_ => random.NextDouble() * 100).ToList();
    }
}
