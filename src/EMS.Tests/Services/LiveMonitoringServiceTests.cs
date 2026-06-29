using Microsoft.Extensions.Logging;
using Moq;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Web.Services;

namespace EMS.Tests.Services;

public class LiveMonitoringServiceTests
{
    private readonly Mock<IEnergyMeterLiveRepository> _liveRepo = new();
    private readonly Mock<IMonitoringDeviceRepository> _deviceRepo = new();
    private readonly Mock<IAlarmRepository> _alarmRepo = new();
    private readonly Mock<ILogger<LiveMonitoringService>> _logger = new();

    private LiveMonitoringService CreateService() =>
        new(_liveRepo.Object, _deviceRepo.Object, _alarmRepo.Object, _logger.Object);

    [Fact]
    public async Task GetLiveMetersAsync_ReturnsRealData()
    {
        var liveData = new List<EnergyMeterLive>
        {
            new() { Id = 1, MeterNo = 1, DateTime = DateTime.Now, IsValid = true, VoltL1N = 230, CurrentL1 = 10, kWtotal = 5.5, PFL1 = 0.95, MFreq = 50 },
            new() { Id = 2, MeterNo = 2, DateTime = DateTime.Now, IsValid = true, VoltL1N = 228, CurrentL1 = 8, kWtotal = 3.2, PFL1 = 0.92, MFreq = 50 }
        };
        var devices = new List<MonitoringDevice>
        {
            new() { DeviceID = 1, DeviceName = "EM-001", Type = "EM", Location = "L1", Building = "B1", Plant = "P1", IsActive = true },
            new() { DeviceID = 2, DeviceName = "EM-002", Type = "EM", Location = "L2", Building = "B1", Plant = "P1", IsActive = true }
        };

        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(liveData);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(devices);
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(new List<Alarm>());

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.Equal(2, result.Meters.Count);
        Assert.Equal("EM-001", result.Meters[0].Name);
        Assert.Equal(5.5, result.Meters[0].Power.kW);
        Assert.Equal(2, result.StatusSummary.Online);
    }

    [Fact]
    public async Task GetLiveMetersAsync_StaleMeter_ShowsOffline()
    {
        var liveData = new List<EnergyMeterLive>
        {
            new() { Id = 1, MeterNo = 1, DateTime = DateTime.Now.AddMinutes(-10), IsValid = true, VoltL1N = 230 }
        };

        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(liveData);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(new List<Alarm>());

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.Single(result.Meters);
        Assert.Equal("offline", result.Meters[0].Status);
        Assert.Equal(1, result.StatusSummary.Offline);
    }

    [Fact]
    public async Task GetLiveMetersAsync_InvalidMeter_ShowsOffline()
    {
        var liveData = new List<EnergyMeterLive>
        {
            new() { Id = 1, MeterNo = 1, DateTime = DateTime.Now, IsValid = false, VoltL1N = 230 }
        };

        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(liveData);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(new List<Alarm>());

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.Equal("offline", result.Meters[0].Status);
    }

    [Fact]
    public async Task GetLiveMetersAsync_AbnormalVoltage_ShowsWarning()
    {
        var liveData = new List<EnergyMeterLive>
        {
            new() { Id = 1, MeterNo = 1, DateTime = DateTime.Now, IsValid = true, VoltL1N = 190 }
        };

        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(liveData);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(new List<Alarm>());

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.Equal("warning", result.Meters[0].Status);
    }

    [Fact]
    public async Task GetLiveMetersAsync_FiltersbyStatus()
    {
        var liveData = new List<EnergyMeterLive>
        {
            new() { Id = 1, MeterNo = 1, DateTime = DateTime.Now, IsValid = true, VoltL1N = 230 },
            new() { Id = 2, MeterNo = 2, DateTime = DateTime.Now.AddMinutes(-10), IsValid = true, VoltL1N = 230 }
        };

        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(liveData);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(new List<Alarm>());

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto { Status = "online" });

        Assert.Single(result.Meters);
        Assert.Equal("online", result.Meters[0].Status);
    }

    [Fact]
    public async Task GetLiveMetersAsync_IncludesActiveAlarms()
    {
        var alarms = new List<Alarm>
        {
            new() { Id = 1, MeterNo = 1, DeviceName = "EM-001", Severity = "warning", Message = "Voltage low", Parameter = "VoltL1N", IsActive = true, CreatedAt = DateTime.Now }
        };

        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(new List<EnergyMeterLive>());
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(alarms);

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.Single(result.ActiveAlarms);
        Assert.Equal("Voltage low", result.ActiveAlarms[0].Message);
    }

    [Fact]
    public async Task GetLiveMetersAsync_OnError_ReturnsEmptyResponse()
    {
        _liveRepo.Setup(r => r.GetAllLive()).ThrowsAsync(new Exception("DB connection failed"));

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.NotNull(result);
        Assert.Empty(result.Meters);
        Assert.Empty(result.ActiveAlarms);
    }

    [Fact]
    public async Task GetLiveMetersAsync_EmptyDb_ReturnsEmptyMeters()
    {
        _liveRepo.Setup(r => r.GetAllLive()).ReturnsAsync(new List<EnergyMeterLive>());
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());
        _alarmRepo.Setup(r => r.GetActiveAlarms()).ReturnsAsync(new List<Alarm>());

        var service = CreateService();
        var result = await service.GetLiveMetersAsync(new LiveMonitoringFilterDto());

        Assert.Empty(result.Meters);
        Assert.Equal(0, result.StatusSummary.Online);
    }
}
