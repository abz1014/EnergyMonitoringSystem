namespace EMS.Web.Services;

using EMS.Core.Interfaces;
using EMS.Core.Models;

public class LiveMonitoringService : ILiveMonitoringService
{
    private readonly IEnergyMeterRepository _meterRepository;
    private readonly IMonitoringDeviceRepository _deviceRepository;
    private readonly IAlarmRepository _alarmRepository;
    private readonly ILogger<LiveMonitoringService> _logger;

    public LiveMonitoringService(
        IEnergyMeterRepository meterRepository,
        IMonitoringDeviceRepository deviceRepository,
        IAlarmRepository alarmRepository,
        ILogger<LiveMonitoringService> logger)
    {
        _meterRepository = meterRepository;
        _deviceRepository = deviceRepository;
        _alarmRepository = alarmRepository;
        _logger = logger;
    }

    public async Task<LiveMonitoringResponseDto> GetLiveMetersAsync(LiveMonitoringFilterDto filter)
    {
        try
        {
            var devices = await _deviceRepository.GetAllDevices();
            var activeAlarms = await _alarmRepository.GetActiveAlarms();

            var now = DateTime.Now;
            var recentData = await _meterRepository.GetByDateRange(now.AddHours(-1), now);

            var latestPerMeter = recentData
                .GroupBy(d => d.MeterNo)
                .Select(g => g.OrderByDescending(d => d.DateTime).First())
                .ToList();

            var deviceLookup = devices.ToDictionary(d => d.DeviceID, d => d);

            var meters = latestPerMeter.Select(live =>
            {
                var device = deviceLookup.GetValueOrDefault(live.MeterNo);
                var status = DetermineStatus(live);

                return new MeterLiveDto
                {
                    MeterId = live.MeterNo,
                    Name = device?.DeviceName ?? live.MeterName ?? $"Meter-{live.MeterNo}",
                    Status = status,
                    Voltage = new VoltageReadingDto
                    {
                        L1 = live.VoltL1N ?? 0,
                        L2 = live.VoltL2N ?? 0,
                        L3 = live.VoltL3N ?? 0
                    },
                    Current = new CurrentReadingDto
                    {
                        L1 = live.CurrentL1 ?? 0,
                        L2 = live.CurrentL2 ?? 0,
                        L3 = live.CurrentL3 ?? 0
                    },
                    Power = new PowerReadingDto
                    {
                        kW = live.kWtotal ?? 0,
                        kVA = live.kVAtotal ?? 0,
                        kVAR = live.kVARtotal ?? 0
                    },
                    PowerFactor = live.PFL1 ?? 0,
                    Frequency = live.MFreq ?? 0,
                    LastUpdated = live.DateTime,
                    Sparkline = new List<double>()
                };
            })
            .Where(m => filter.Status == "all" || m.Status == filter.Status)
            .OrderBy(m => m.Name)
            .ToList();

            var alarmDtos = activeAlarms.Select(a => new AlarmDto
            {
                Id = a.Id,
                MeterId = a.MeterNo,
                DeviceName = deviceLookup.GetValueOrDefault(a.MeterNo)?.DeviceName ?? a.DeviceName,
                Parameter = a.Parameter,
                CurrentValue = a.CurrentValue,
                Threshold = a.Threshold,
                Severity = a.Severity,
                Message = a.Message,
                CreatedAt = a.CreatedAt
            }).ToList();

            return new LiveMonitoringResponseDto
            {
                Meters = meters,
                StatusSummary = new StatusSummaryDto
                {
                    Online = meters.Count(m => m.Status == "online"),
                    Warning = meters.Count(m => m.Status == "warning"),
                    Offline = meters.Count(m => m.Status == "offline"),
                    Unknown = meters.Count(m => m.Status == "unknown")
                },
                ActiveAlarms = alarmDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching live monitoring data");
            return new LiveMonitoringResponseDto
            {
                Meters = new(),
                StatusSummary = new(),
                ActiveAlarms = new()
            };
        }
    }

    private static string DetermineStatus(EnergyMeterData reading)
    {
        if (reading.DateTime < DateTime.Now.AddMinutes(-5))
            return "offline";

        if (reading.DateTime < DateTime.Now.AddMinutes(-2))
            return "warning";

        var voltage = reading.VoltL1N ?? 0;
        if (voltage > 0 && (voltage < 200 || voltage > 260))
            return "warning";

        return "online";
    }
}
