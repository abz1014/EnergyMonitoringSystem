using Microsoft.EntityFrameworkCore;
using EMS.Core.Models;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;

namespace EMS.Tests.Repositories;

public class MonitoringDeviceRepositoryTests
{
    private ScadaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ScadaDbContext(options);
    }

    private void SeedDevices(ScadaDbContext context)
    {
        context.MonitoringDevices.AddRange(
            new MonitoringDevice { SrNo = 1, DeviceID = 1, DeviceName = "EM-001", DeviceType = "Energy Meter", Location = "Floor 1", GroupName = "Plant-1", IsActive = true },
            new MonitoringDevice { SrNo = 2, DeviceID = 2, DeviceName = "EM-002", DeviceType = "Energy Meter", Location = "Floor 2", GroupName = "Plant-1", IsActive = true },
            new MonitoringDevice { SrNo = 3, DeviceID = 3, DeviceName = "FM-001", DeviceType = "Flowmeter", Location = "Utility", GroupName = "Plant-2", IsActive = false }
        );
        context.SaveChanges();
    }

    [Fact]
    public async Task GetAllDevices_ReturnsAll()
    {
        using var context = CreateContext();
        SeedDevices(context);
        var repo = new MonitoringDeviceRepository(context);
        var result = await repo.GetAllDevices();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetOnlineDeviceCount_CountsActiveOnly()
    {
        using var context = CreateContext();
        SeedDevices(context);
        var repo = new MonitoringDeviceRepository(context);
        var count = await repo.GetOnlineDeviceCount();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetDevicesByPlant_FiltersCorrectly()
    {
        using var context = CreateContext();
        SeedDevices(context);
        var repo = new MonitoringDeviceRepository(context);
        var result = await repo.GetDevicesByPlant("Plant-1");
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllPlants_ReturnsDistinct()
    {
        using var context = CreateContext();
        SeedDevices(context);
        var repo = new MonitoringDeviceRepository(context);
        var plants = await repo.GetAllPlants();
        Assert.Equal(2, plants.Count);
        Assert.Contains("Plant-1", plants);
        Assert.Contains("Plant-2", plants);
    }

    [Fact]
    public async Task GetBuildingsByPlant_ReturnsDistinct()
    {
        using var context = CreateContext();
        SeedDevices(context);
        var repo = new MonitoringDeviceRepository(context);
        var buildings = await repo.GetBuildingsByPlant("Plant-1");
        Assert.Equal(2, buildings.Count);
    }

    [Fact]
    public async Task GetDeviceByDeviceId_ReturnsCorrect()
    {
        using var context = CreateContext();
        SeedDevices(context);
        var repo = new MonitoringDeviceRepository(context);
        var device = await repo.GetDeviceByDeviceId(2);
        Assert.NotNull(device);
        Assert.Equal("EM-002", device.DeviceName);
    }

    [Fact]
    public async Task GetDeviceByDeviceId_NotFound_ReturnsNull()
    {
        using var context = CreateContext();
        var repo = new MonitoringDeviceRepository(context);
        var device = await repo.GetDeviceByDeviceId(999);
        Assert.Null(device);
    }
}
