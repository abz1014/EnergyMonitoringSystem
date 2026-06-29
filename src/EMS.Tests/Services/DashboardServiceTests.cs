using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Web.Services;

namespace EMS.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IEnergyMeterRepository> _meterRepo = new();
    private readonly Mock<IMonitoringDeviceRepository> _deviceRepo = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ILogger<WebDashboardService>> _logger = new();

    private WebDashboardService CreateService() =>
        new(_meterRepo.Object, _deviceRepo.Object, _cache, _logger.Object);

    [Fact]
    public async Task GetExecutiveDashboardAsync_ReturnsKpiCards()
    {
        _meterRepo.Setup(r => r.GetTodaysTotalConsumption()).ReturnsAsync(5000);
        _meterRepo.Setup(r => r.GetPeakDemandToday()).ReturnsAsync(120);
        _meterRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EnergyMeterData>());
        _deviceRepo.Setup(r => r.GetOnlineDeviceCount()).ReturnsAsync(10);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());


        var service = CreateService();
        var result = await service.GetExecutiveDashboardAsync(new DashboardFilterDto());

        Assert.NotNull(result);
        Assert.NotNull(result.KpiCards);
        Assert.Equal(5000, result.KpiCards.TodayConsumption.Value);
        Assert.Equal(120, result.KpiCards.PeakDemand.Value);
        Assert.Equal(10, result.KpiCards.OnlineMeters.Value);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_CachesResult()
    {
        _meterRepo.Setup(r => r.GetTodaysTotalConsumption()).ReturnsAsync(1000);
        _meterRepo.Setup(r => r.GetPeakDemandToday()).ReturnsAsync(50);
        _meterRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EnergyMeterData>());
        _deviceRepo.Setup(r => r.GetOnlineDeviceCount()).ReturnsAsync(5);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());


        var service = CreateService();
        var filter = new DashboardFilterDto { Plant = "TestPlant" };

        var first = await service.GetExecutiveDashboardAsync(filter);
        var second = await service.GetExecutiveDashboardAsync(filter);

        Assert.Same(first, second);
        _meterRepo.Verify(r => r.GetTodaysTotalConsumption(), Times.Once);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_BuildsChartData_FromRealData()
    {
        var today = DateTime.Now.Date;
        var meterData = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = today.AddHours(9), kWh = 100 },
            new() { MeterNo = 1, DateTime = today.AddHours(10), kWh = 200 },
            new() { MeterNo = 2, DateTime = today.AddHours(9), kWh = 150 }
        };
        var devices = new List<MonitoringDevice>
        {
            new() { DeviceID = 1, DeviceName = "Panel-A", GroupName = "P1", DeviceType = "EM", Location = "L1", IsActive = true },
            new() { DeviceID = 2, DeviceName = "Panel-B", GroupName = "P1", DeviceType = "EM", Location = "L2", IsActive = true }
        };

        _meterRepo.Setup(r => r.GetTodaysTotalConsumption()).ReturnsAsync(450);
        _meterRepo.Setup(r => r.GetPeakDemandToday()).ReturnsAsync(200);
        _meterRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(meterData);
        _deviceRepo.Setup(r => r.GetOnlineDeviceCount()).ReturnsAsync(2);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(devices);


        var service = CreateService();
        var result = await service.GetExecutiveDashboardAsync(new DashboardFilterDto());

        Assert.NotEmpty(result.Charts.ConsumptionTrend);
        Assert.NotEmpty(result.Charts.LocationBreakdown);
        Assert.NotEmpty(result.Charts.TopConsumers);
        Assert.Equal(2, result.Charts.LocationBreakdown.Count);
        Assert.Equal(2, result.Charts.TopConsumers.Count);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_OnDbError_ReturnsFallback()
    {
        _meterRepo.Setup(r => r.GetTodaysTotalConsumption()).ThrowsAsync(new Exception("DB down"));

        var service = CreateService();
        var result = await service.GetExecutiveDashboardAsync(new DashboardFilterDto());

        Assert.NotNull(result);
        Assert.Equal(0, result.KpiCards.TodayConsumption.Value);
        Assert.Equal("warning", result.KpiCards.TodayConsumption.Status);
    }

    [Fact]
    public async Task GetExecutiveDashboardAsync_MonthlyTotal_IsRealSum()
    {
        var monthData = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = DateTime.Now.AddDays(-5), kWh = 1000 },
            new() { MeterNo = 1, DateTime = DateTime.Now.AddDays(-3), kWh = 2000 },
            new() { MeterNo = 1, DateTime = DateTime.Now.AddDays(-1), kWh = 1500 }
        };

        _meterRepo.Setup(r => r.GetTodaysTotalConsumption()).ReturnsAsync(500);
        _meterRepo.Setup(r => r.GetPeakDemandToday()).ReturnsAsync(100);
        _meterRepo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(monthData);
        _deviceRepo.Setup(r => r.GetOnlineDeviceCount()).ReturnsAsync(1);
        _deviceRepo.Setup(r => r.GetAllDevices()).ReturnsAsync(new List<MonitoringDevice>());


        var service = CreateService();
        var result = await service.GetExecutiveDashboardAsync(new DashboardFilterDto());

        Assert.Equal(4500, result.KpiCards.MonthlyTotal.Value);
    }
}
