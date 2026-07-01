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
            // Use 2-hour window so hourly-polled meters don't flip to "offline" mid-hour.
            // Falls back to 30-day window if the gateway has been down for hours (dev/demo mode).
            var recentData = await _meterRepository.GetByDateRange(now.AddHours(-2), now);

            if (recentData.Count == 0)
            {
                recentData = await _meterRepository.GetByDateRange(now.AddDays(-30), now);
            }

            var latestPerMeter = recentData
                .Where(d => d.MeterNo.HasValue)
                .GroupBy(d => d.MeterNo!.Value)
                .Select(g => g.OrderByDescending(d => d.DateTime).First())
                .ToList();

            var deviceLookup = devices.Where(d => d.DeviceID.HasValue).GroupBy(d => d.DeviceID!.Value).ToDictionary(g => g.Key, g => g.First());

            var meters = latestPerMeter.Select(live =>
            {
                var meterNo = live.MeterNo ?? 0;
                var device = deviceLookup.GetValueOrDefault(meterNo);
                var status = DetermineStatus(live);

                return new MeterLiveDto
                {
                    MeterId = meterNo,
                    Name = device?.DeviceName ?? live.MeterName ?? $"Meter-{meterNo}",
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
                    Frequency = (double)(live.MFreq ?? 0),
                    LastUpdated = live.DateTime ?? DateTime.MinValue,
                    Sparkline = new List<double>()
                };
            })
            .Where(m => filter.Status == "all" || m.Status == filter.Status)
            .OrderBy(m => m.Name)
            .ToList();

            var alarmDtos = activeAlarms.Select(a => new AlarmDto
            {
                Id = a.AlarmID,
                MeterId = a.DeviceID,
                DeviceName = (deviceLookup.ContainsKey(a.DeviceID) ? deviceLookup[a.DeviceID].DeviceName : null) ?? a.DeviceName,
                Parameter = a.TagName,
                CurrentValue = a.TagValue,
                Threshold = a.Threshold,
                Severity = a.SeverityName,
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
        // Thresholds match the gateway's 1-hour polling interval:
        //   > 90 min since last reading → offline (missed at least one poll cycle)
        //   30–90 min → warning (one reading received but next is overdue)
        //   < 30 min → online
        if (!reading.DateTime.HasValue || reading.DateTime.Value < DateTime.Now.AddMinutes(-90))
            return "offline";

        if (reading.DateTime.Value < DateTime.Now.AddMinutes(-30))
            return "warning";

        var voltage = reading.VoltL1N ?? 0;
        if (voltage > 0 && (voltage < 200 || voltage > 260))
            return "warning";

        return "online";
    }
}
