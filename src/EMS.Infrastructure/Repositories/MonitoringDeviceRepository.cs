namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class MonitoringDeviceRepository : IMonitoringDeviceRepository
{
    private readonly ScadaDbContext _context;
    private readonly IMemoryCache _cache;
    private const string AllDevicesCacheKey = "monitoring_devices_all";
    private static readonly TimeSpan DeviceCacheDuration = TimeSpan.FromMinutes(10);

    public MonitoringDeviceRepository(ScadaDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<MonitoringDevice>> GetAllDevices()
    {
        if (_cache.TryGetValue(AllDevicesCacheKey, out List<MonitoringDevice>? cached) && cached != null)
            return cached;

        var devices = await _context.MonitoringDevices.AsNoTracking().ToListAsync();
        _cache.Set(AllDevicesCacheKey, devices, DeviceCacheDuration);
        return devices;
    }

    public async Task<List<MonitoringDevice>> GetDevicesByType(string type)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.DeviceType == type)
            .ToListAsync();
    }

    public async Task<List<MonitoringDevice>> GetDevicesByPlant(string plant)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.GroupName == plant && d.IsActive == 1)
            .ToListAsync();
    }

    public async Task<List<MonitoringDevice>> GetDevicesByBuilding(string building)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.Location == building && d.IsActive == 1)
            .ToListAsync();
    }

    public async Task<MonitoringDevice?> GetDeviceById(int deviceId)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.SrNo == deviceId);
    }

    public async Task<MonitoringDevice?> GetDeviceByDeviceId(int deviceId)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeviceID == deviceId);
    }

    public async Task<int> GetOnlineDeviceCount()
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .CountAsync(d => d.IsActive == 1);
    }

    public async Task<List<string>> GetAllPlants()
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Select(d => d.GroupName ?? "")
            .Distinct()
            .Where(p => !string.IsNullOrEmpty(p))
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<List<string>> GetBuildingsByPlant(string plant)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.GroupName == plant)
            .Select(d => d.Location)
            .Distinct()
            .Where(b => !string.IsNullOrEmpty(b))
            .OrderBy(b => b)
            .ToListAsync();
    }

    public async Task<List<string>> GetLocationsByBuilding(string plant, string building)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.GroupName == plant && d.Location == building)
            .Select(d => d.Description ?? "")
            .Distinct()
            .Where(l => !string.IsNullOrEmpty(l))
            .OrderBy(l => l)
            .ToListAsync();
    }
}
