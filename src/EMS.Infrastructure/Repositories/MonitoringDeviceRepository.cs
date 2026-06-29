namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class MonitoringDeviceRepository : IMonitoringDeviceRepository
{
    private readonly ScadaDbContext _context;

    public MonitoringDeviceRepository(ScadaDbContext context)
    {
        _context = context;
    }

    public async Task<List<MonitoringDevice>> GetAllDevices()
    {
        return await _context.MonitoringDevices.AsNoTracking().ToListAsync();
    }

    public async Task<List<MonitoringDevice>> GetDevicesByType(string type)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.Type == type)
            .ToListAsync();
    }

    public async Task<List<MonitoringDevice>> GetDevicesByPlant(string plant)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.Plant == plant && d.IsActive)
            .ToListAsync();
    }

    public async Task<List<MonitoringDevice>> GetDevicesByBuilding(string building)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.Building == building && d.IsActive)
            .ToListAsync();
    }

    public async Task<MonitoringDevice?> GetDeviceById(int deviceId)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId);
    }

    public async Task<MonitoringDevice?> GetDeviceByDeviceId(int deviceId)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DeviceID == deviceId);
    }

    public async Task<int> GetOnlineDeviceCount()
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .CountAsync(d => d.IsActive);
    }

    public async Task<List<string>> GetAllPlants()
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Select(d => d.Plant)
            .Distinct()
            .Where(p => !string.IsNullOrEmpty(p))
            .OrderBy(p => p)
            .ToListAsync();
    }

    public async Task<List<string>> GetBuildingsByPlant(string plant)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.Plant == plant)
            .Select(d => d.Building)
            .Distinct()
            .Where(b => !string.IsNullOrEmpty(b))
            .OrderBy(b => b)
            .ToListAsync();
    }

    public async Task<List<string>> GetLocationsByBuilding(string plant, string building)
    {
        return await _context.MonitoringDevices.AsNoTracking()
            .Where(d => d.Plant == plant && d.Building == building)
            .Select(d => d.Location)
            .Distinct()
            .Where(l => !string.IsNullOrEmpty(l))
            .OrderBy(l => l)
            .ToListAsync();
    }
}
