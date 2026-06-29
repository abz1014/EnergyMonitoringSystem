using Microsoft.Extensions.Logging;
using Moq;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Web.Services;

namespace EMS.Tests.Services;

public class EnergyAnalysisServiceTests
{
    private readonly Mock<IEnergyMeterRepository> _repo = new();
    private readonly Mock<ILogger<EnergyAnalysisService>> _logger = new();

    private EnergyAnalysisService CreateService() => new(_repo.Object, _logger.Object);

    [Fact]
    public async Task GetAnalysisAsync_ReturnsData_ForDaily()
    {
        var today = DateTime.Now.Date;
        var data = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = today.AddHours(9), kWh = 100, kWtotal = 50 },
            new() { MeterNo = 1, DateTime = today.AddHours(10), kWh = 200, kWtotal = 80 },
            new() { MeterNo = 1, DateTime = today.AddHours(10), kWh = 150, kWtotal = 60 }
        };

        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "kwh", "");

        Assert.Equal("daily", result.Timeframe);
        Assert.Equal("kwh", result.Metric);
        Assert.NotEmpty(result.ConsumptionData);
        Assert.NotEmpty(result.Rows);
        Assert.True(result.Peak > 0);
        Assert.True(result.Statistics["total"] > 0);
    }

    [Fact]
    public async Task GetAnalysisAsync_RowStats_AreReal()
    {
        var today = DateTime.Now.Date;
        var data = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = today.AddHours(9), kWh = 100 },
            new() { MeterNo = 1, DateTime = today.AddHours(9), kWh = 300 },
            new() { MeterNo = 1, DateTime = today.AddHours(9), kWh = 200 }
        };

        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "kwh", "");

        var row = result.Rows.First();
        Assert.Equal(600, row.Total);
        Assert.Equal(300, row.Peak);
        Assert.Equal(200, row.Average);
        Assert.Equal(100, row.Min);
    }

    [Fact]
    public async Task GetAnalysisAsync_InvalidTimeframe_DefaultsToDaily()
    {
        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EnergyMeterData>());

        var service = CreateService();
        var result = await service.GetAnalysisAsync("invalid_timeframe", "kwh", "");

        Assert.Equal("daily", result.Timeframe);
    }

    [Fact]
    public async Task GetAnalysisAsync_InvalidMetric_DefaultsToKwh()
    {
        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EnergyMeterData>());

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "garbage", "");

        Assert.Equal("kwh", result.Metric);
    }

    [Fact]
    public async Task GetAnalysisAsync_PeakMetric_UsesKwTotal()
    {
        var today = DateTime.Now.Date;
        var data = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = today.AddHours(9), kWh = 100, kWtotal = 55 },
            new() { MeterNo = 1, DateTime = today.AddHours(10), kWh = 200, kWtotal = 85 }
        };

        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "peak", "");

        Assert.Equal(85, result.Peak);
    }

    [Fact]
    public async Task GetAnalysisAsync_Comparison_FetchesPreviousData()
    {
        var data = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = DateTime.Now.Date.AddHours(9), kWh = 100 }
        };

        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "kwh", "yesterday");

        Assert.Equal("yesterday", result.CompareWith);
        _repo.Verify(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAnalysisAsync_EmptyDb_ReturnsEmptyResult()
    {
        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EnergyMeterData>());

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "kwh", "");

        Assert.Empty(result.ConsumptionData);
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.Peak);
    }

    [Fact]
    public async Task GetAnalysisAsync_DbError_ReturnsEmptyGracefully()
    {
        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new Exception("Connection failed"));

        var service = CreateService();
        var result = await service.GetAnalysisAsync("daily", "kwh", "");

        Assert.Empty(result.ConsumptionData);
        Assert.Equal(0, result.Peak);
    }

    [Fact]
    public async Task GetAnalysisAsync_Monthly_GroupsByDate()
    {
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var data = new List<EnergyMeterData>
        {
            new() { MeterNo = 1, DateTime = monthStart.AddDays(1).AddHours(9), kWh = 100 },
            new() { MeterNo = 1, DateTime = monthStart.AddDays(1).AddHours(14), kWh = 200 },
            new() { MeterNo = 1, DateTime = monthStart.AddDays(2).AddHours(10), kWh = 150 }
        };

        _repo.Setup(r => r.GetByDateRange(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(data);

        var service = CreateService();
        var result = await service.GetAnalysisAsync("monthly", "kwh", "");

        Assert.Equal(2, result.ConsumptionData.Count);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(300, result.Rows[0].Total);
        Assert.Equal(150, result.Rows[1].Total);
    }
}
