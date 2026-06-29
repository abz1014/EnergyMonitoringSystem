using Microsoft.EntityFrameworkCore;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;

namespace EMS.Tests.Repositories;

public class AlarmRepositoryTests
{
    private ScadaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ScadaDbContext(options);
    }

    [Fact]
    public async Task GetActiveAlarms_ReturnsOnlyActive()
    {
        using var context = CreateContext();
        context.Alarms.AddRange(
            new Alarm { AlarmID = 1, DeviceID = 1, DeviceName = "M1", TagName = "V", Severity = 2, Message = "Low", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { AlarmID = 2, DeviceID = 2, DeviceName = "M2", TagName = "V", Severity = 3, Message = "High", IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repo = new AlarmRepository(context);
        var result = await repo.GetActiveAlarms();

        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task AcknowledgeAlarm_DeactivatesAndSetsAckInfo()
    {
        using var context = CreateContext();
        context.Alarms.Add(new Alarm { AlarmID = 1, DeviceID = 1, DeviceName = "M1", TagName = "V", Severity = 2, Message = "Test", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var repo = new AlarmRepository(context);
        await repo.AcknowledgeAlarm(1, "admin@test.com");

        var alarm = await context.Alarms.FirstAsync(a => a.AlarmID == 1);
        Assert.False(alarm!.IsActive);
        Assert.Equal("admin@test.com", alarm.AckBy);
        Assert.NotNull(alarm.AckTime);
    }

    [Fact]
    public async Task GetActiveAlarmCount_ReturnsCorrectCount()
    {
        using var context = CreateContext();
        context.Alarms.AddRange(
            new Alarm { AlarmID = 1, DeviceID = 1, DeviceName = "M1", TagName = "V", Severity = 2, Message = "1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { AlarmID = 2, DeviceID = 2, DeviceName = "M2", TagName = "V", Severity = 2, Message = "2", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { AlarmID = 3, DeviceID = 3, DeviceName = "M3", TagName = "V", Severity = 2, Message = "3", IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repo = new AlarmRepository(context);
        var count = await repo.GetActiveAlarmCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task AddAlarm_PersistsToDb()
    {
        using var context = CreateContext();
        var repo = new AlarmRepository(context);

        await repo.AddAlarm(new Alarm
        {
            DeviceID = 5,
            DeviceName = "EM-005",
            TagName = "CurrentL1",
            Severity = 3,
            Message = "Overcurrent detected",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        Assert.Equal(1, await context.Alarms.CountAsync());
    }

    [Fact]
    public async Task GetAlarmsBySeverity_FiltersCorrectly()
    {
        using var context = CreateContext();
        context.Alarms.AddRange(
            new Alarm { AlarmID = 1, DeviceID = 1, DeviceName = "M1", TagName = "V", Severity = 3, Message = "A", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { AlarmID = 2, DeviceID = 2, DeviceName = "M2", TagName = "V", Severity = 2, Message = "B", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { AlarmID = 3, DeviceID = 3, DeviceName = "M3", TagName = "V", Severity = 3, Message = "C", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repo = new AlarmRepository(context);
        var result = await repo.GetAlarmsBySeverity(3);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal((byte)3, a.Severity));
    }
}
