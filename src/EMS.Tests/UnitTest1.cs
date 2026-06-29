using Microsoft.EntityFrameworkCore;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;

namespace EMS.Tests;

public class EnergyMeterRepositoryTests
{
    private ScadaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ScadaDbContext(options);
    }

    [Fact]
    public async Task GetByDateRange_ReturnsDataInRange()
    {
        using var context = CreateInMemoryContext();
        var now = DateTime.Now.Date;

        context.EnergyMetersData.AddRange(
            new EnergyMeterData { Id = 1, MeterNo = 1, DateTime = now.AddHours(1), kWh = 100 },
            new EnergyMeterData { Id = 2, MeterNo = 1, DateTime = now.AddHours(5), kWh = 200 },
            new EnergyMeterData { Id = 3, MeterNo = 1, DateTime = now.AddDays(-2), kWh = 300 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterRepository(context);
        var result = await repo.GetByDateRange(now, now.AddDays(1));

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.DateTime >= now && r.DateTime <= now.AddDays(1)));
    }

    [Fact]
    public async Task GetByDateRange_EmptyRange_ReturnsEmpty()
    {
        using var context = CreateInMemoryContext();
        var repo = new EnergyMeterRepository(context);

        var result = await repo.GetByDateRange(DateTime.Now, DateTime.Now.AddDays(1));

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDailyConsumption_FiltersByMeterAndDate()
    {
        using var context = CreateInMemoryContext();
        var today = DateTime.Now.Date;

        context.EnergyMetersData.AddRange(
            new EnergyMeterData { Id = 1, MeterNo = 1, DateTime = today.AddHours(2), kWh = 100 },
            new EnergyMeterData { Id = 2, MeterNo = 2, DateTime = today.AddHours(3), kWh = 150 },
            new EnergyMeterData { Id = 3, MeterNo = 1, DateTime = today.AddDays(-1).AddHours(5), kWh = 200 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterRepository(context);
        var result = await repo.GetDailyConsumption(1, today);

        Assert.Single(result);
        Assert.Equal(100, result[0].kWh);
    }

    [Fact]
    public async Task GetLatestReading_ReturnsNewest()
    {
        using var context = CreateInMemoryContext();
        var now = DateTime.Now;

        context.EnergyMetersData.AddRange(
            new EnergyMeterData { Id = 1, MeterNo = 5, DateTime = now.AddHours(-2), kWh = 100 },
            new EnergyMeterData { Id = 2, MeterNo = 5, DateTime = now.AddHours(-1), kWh = 200 },
            new EnergyMeterData { Id = 3, MeterNo = 5, DateTime = now.AddHours(-3), kWh = 50 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterRepository(context);
        var result = await repo.GetLatestReading(5);

        Assert.NotNull(result);
        Assert.Equal(200, result.kWh);
    }

    [Fact]
    public async Task GetLatestReading_NoData_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repo = new EnergyMeterRepository(context);

        var result = await repo.GetLatestReading(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTodaysTotalConsumption_SumsCorrectly()
    {
        using var context = CreateInMemoryContext();
        var today = DateTime.Now.Date;

        context.EnergyMetersData.AddRange(
            new EnergyMeterData { Id = 1, MeterNo = 1, DateTime = today.AddHours(1), kWh = 100 },
            new EnergyMeterData { Id = 2, MeterNo = 1, DateTime = today.AddHours(2), kWh = 200 },
            new EnergyMeterData { Id = 3, MeterNo = 1, DateTime = today.AddDays(-1), kWh = 999 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterRepository(context);
        var result = await repo.GetTodaysTotalConsumption();

        Assert.Equal(300, result);
    }

    [Fact]
    public async Task GetPeakDemandToday_ReturnsMax()
    {
        using var context = CreateInMemoryContext();
        var today = DateTime.Now.Date;

        context.EnergyMetersData.AddRange(
            new EnergyMeterData { Id = 1, MeterNo = 1, DateTime = today.AddHours(1), kWtotal = 50 },
            new EnergyMeterData { Id = 2, MeterNo = 1, DateTime = today.AddHours(2), kWtotal = 120 },
            new EnergyMeterData { Id = 3, MeterNo = 1, DateTime = today.AddHours(3), kWtotal = 80 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterRepository(context);
        var result = await repo.GetPeakDemandToday();

        Assert.Equal(120, result);
    }

    [Fact]
    public async Task GetConsumptionRange_FiltersByMeter()
    {
        using var context = CreateInMemoryContext();
        var from = DateTime.Now.Date;
        var to = from.AddDays(7);

        context.EnergyMetersData.AddRange(
            new EnergyMeterData { Id = 1, MeterNo = 1, DateTime = from.AddDays(1), kWh = 100 },
            new EnergyMeterData { Id = 2, MeterNo = 2, DateTime = from.AddDays(2), kWh = 200 },
            new EnergyMeterData { Id = 3, MeterNo = 1, DateTime = from.AddDays(3), kWh = 300 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterRepository(context);
        var result = await repo.GetConsumptionRange(1, from, to);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(1, r.MeterNo));
    }
}

public class EnergyMeterLiveRepositoryTests
{
    private ScadaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ScadaDbContext(options);
    }

    [Fact]
    public async Task GetAllLive_ReturnsAll()
    {
        using var context = CreateInMemoryContext();

        context.EnergyMeterLive.AddRange(
            new EnergyMeterLive { Id = 1, MeterNo = 1, DateTime = DateTime.Now, IsValid = true },
            new EnergyMeterLive { Id = 2, MeterNo = 2, DateTime = DateTime.Now, IsValid = true }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterLiveRepository(context);
        var result = await repo.GetAllLive();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetLiveByMeterId_ReturnsCorrectMeter()
    {
        using var context = CreateInMemoryContext();

        context.EnergyMeterLive.AddRange(
            new EnergyMeterLive { Id = 1, MeterNo = 10, DateTime = DateTime.Now, IsValid = true, kWtotal = 55.5 },
            new EnergyMeterLive { Id = 2, MeterNo = 20, DateTime = DateTime.Now, IsValid = true, kWtotal = 77.3 }
        );
        await context.SaveChangesAsync();

        var repo = new EnergyMeterLiveRepository(context);
        var result = await repo.GetLiveByMeterId(10);

        Assert.NotNull(result);
        Assert.Equal(55.5, result.kWtotal);
    }

    [Fact]
    public async Task GetLiveByMeterId_NotFound_ReturnsNull()
    {
        using var context = CreateInMemoryContext();
        var repo = new EnergyMeterLiveRepository(context);

        var result = await repo.GetLiveByMeterId(999);

        Assert.Null(result);
    }
}

public class ModelTests
{
    [Fact]
    public void EnergyMeterData_DefaultValues()
    {
        var data = new EnergyMeterData();

        Assert.Equal(0, data.Id);
        Assert.Equal(0, data.MeterNo);
        Assert.Null(data.kWh);
        Assert.Null(data.kWtotal);
        Assert.Null(data.VoltL1N);
    }

    [Fact]
    public void Alarm_Properties()
    {
        var alarm = new Alarm
        {
            MeterNo = 5,
            DeviceName = "Test Meter",
            Severity = "Critical",
            Message = "Voltage exceeds threshold",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal(5, alarm.MeterNo);
        Assert.Equal("Critical", alarm.Severity);
        Assert.True(alarm.IsActive);
        Assert.Null(alarm.AckBy);
    }

    [Fact]
    public void MonitoringDevice_Properties()
    {
        var device = new MonitoringDevice
        {
            DeviceID = 1,
            DeviceName = "EM-001",
            Type = "Energy Meter",
            Location = "Panel A",
            Building = "Main",
            Plant = "Plant-1",
            IsActive = true
        };

        Assert.Equal("Plant-1", device.Plant);
        Assert.True(device.IsActive);
    }
}
