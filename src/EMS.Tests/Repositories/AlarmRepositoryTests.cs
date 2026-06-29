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
            new Alarm { Id = 1, MeterNo = 1, DeviceName = "M1", Parameter = "V", Severity = "warning", Message = "Low", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { Id = 2, MeterNo = 2, DeviceName = "M2", Parameter = "V", Severity = "critical", Message = "High", IsActive = false, CreatedAt = DateTime.UtcNow }
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
        context.Alarms.Add(new Alarm { Id = 1, MeterNo = 1, DeviceName = "M1", Parameter = "V", Severity = "warning", Message = "Test", IsActive = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var repo = new AlarmRepository(context);
        await repo.AcknowledgeAlarm(1, "admin@test.com");

        var alarm = await context.Alarms.FindAsync(1);
        Assert.False(alarm!.IsActive);
        Assert.Equal("admin@test.com", alarm.AckBy);
        Assert.NotNull(alarm.AckTime);
    }

    [Fact]
    public async Task GetActiveAlarmCount_ReturnsCorrectCount()
    {
        using var context = CreateContext();
        context.Alarms.AddRange(
            new Alarm { Id = 1, MeterNo = 1, DeviceName = "M1", Parameter = "V", Severity = "w", Message = "1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { Id = 2, MeterNo = 2, DeviceName = "M2", Parameter = "V", Severity = "w", Message = "2", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { Id = 3, MeterNo = 3, DeviceName = "M3", Parameter = "V", Severity = "w", Message = "3", IsActive = false, CreatedAt = DateTime.UtcNow }
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
            MeterNo = 5,
            DeviceName = "EM-005",
            Parameter = "CurrentL1",
            Severity = "critical",
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
            new Alarm { Id = 1, MeterNo = 1, DeviceName = "M1", Parameter = "V", Severity = "critical", Message = "A", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { Id = 2, MeterNo = 2, DeviceName = "M2", Parameter = "V", Severity = "warning", Message = "B", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Alarm { Id = 3, MeterNo = 3, DeviceName = "M3", Parameter = "V", Severity = "critical", Message = "C", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var repo = new AlarmRepository(context);
        var result = await repo.GetAlarmsBySeverity("critical");

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal("critical", a.Severity));
    }
}
