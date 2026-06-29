namespace EMS.API.Services;

using EMS.Core.Interfaces;

public class LiveMonitoringService : ILiveMonitoringService
{
    private readonly IEnergyMeterLiveRepository _liveRepository;
    private readonly IAlarmRepository _alarmRepository;
    private readonly IEnergyMeterRepository _meterRepository;

    public LiveMonitoringService(
        IEnergyMeterLiveRepository liveRepository,
        IAlarmRepository alarmRepository,
        IEnergyMeterRepository meterRepository)
    {
        _liveRepository = liveRepository;
        _alarmRepository = alarmRepository;
        _meterRepository = meterRepository;
    }

    public async Task<LiveMonitoringResponseDto> GetLiveMetersAsync(LiveMonitoringFilterDto filter)
    {
        var allLiveMeters = await _liveRepository.GetAllLive();
        var activeAlarms = await _alarmRepository.GetActiveAlarms();

        // Calculate status summary
        var statusSummary = new StatusSummaryDto
        {
            Online = allLiveMeters.Count(m => m.IsValid),
            Warning = activeAlarms.Count(a => a.Severity == "warning"),
            Offline = allLiveMeters.Count(m => !m.IsValid),
            Unknown = 0
        };

        // Convert to DTOs
        var meters = allLiveMeters.Select(m => new MeterLiveDto
        {
            MeterId = m.MeterNo,
            Name = $"Meter-{m.MeterNo}",
            Status = m.IsValid ? "online" : "offline",
            Voltage = new VoltageReadingDto
            {
                L1 = m.VoltL1N ?? 0,
                L2 = m.VoltL2N ?? 0,
                L3 = m.VoltL3N ?? 0
            },
            Current = new CurrentReadingDto
            {
                L1 = m.CurrentL1 ?? 0,
                L2 = m.CurrentL2 ?? 0,
                L3 = m.CurrentL3 ?? 0
            },
            Power = new PowerReadingDto
            {
                kW = m.kWtotal ?? 0,
                kVAR = 0,
                kVA = 0
            },
            PowerFactor = m.PFL1 ?? 0.96,
            Frequency = m.MFreq ?? 50.0,
            Sparkline = GenerateSparkline(),
            LastUpdated = m.DateTime
        }).ToList();

        var alarms = activeAlarms.Select(a => new AlarmDto
        {
            Id = a.Id,
            MeterId = a.MeterNo,
            DeviceName = a.DeviceName,
            Parameter = a.Parameter,
            CurrentValue = a.CurrentValue,
            Threshold = a.Threshold,
            Severity = a.Severity,
            Message = a.Message,
            CreatedAt = a.CreatedAt
        }).ToList();

        return new LiveMonitoringResponseDto
        {
            StatusSummary = statusSummary,
            Meters = meters,
            ActiveAlarms = alarms
        };
    }

    private List<double> GenerateSparkline()
    {
        var sparkline = new List<double>();
        var baseValue = 3.4;
        var random = new Random();

        for (int i = 0; i < 24; i++)
        {
            sparkline.Add(baseValue + (random.NextDouble() * 0.5 - 0.25));
        }

        return sparkline;
    }
}
