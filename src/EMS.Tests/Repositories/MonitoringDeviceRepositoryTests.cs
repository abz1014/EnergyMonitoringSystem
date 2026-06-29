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
            new MonitoringDevice { Id = 1, DeviceID = 1, DeviceName = "EM-001", Type = "Energy Meter", Location = "Floor 1", Building = "Main", Plant = "Plant-1", IsActive = true, CreatedDate = DateTime.Now },
            new MonitoringDevice { Id = 2, DeviceID = 2, DeviceName = "EM-002", Type = "Energy Meter", Location = "Floor 2", Building = "Main", Plant = "Plant-1", IsActive = true, CreatedDate = DateTime.Now },
            new MonitoringDevice { Id = 3, DeviceID = 3, DeviceName = "FM-001", Type = "Flowmeter", Location = "Utility", Building = "Warehouse", Plant = "Plant-2", IsActive = false, CreatedDate = DateTime.Now }
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
        Assert.All(result, d => Assert.Equal("Plant-1", d.Plant));
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

        Assert.Single(buildings);
        Assert.Equal("Main", buildings[0]);
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
