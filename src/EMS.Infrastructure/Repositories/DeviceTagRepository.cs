namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class DeviceTagRepository : IDeviceTagRepository
{
    private readonly ScadaDbContext _context;

    public DeviceTagRepository(ScadaDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeviceTag>> GetTagsByDeviceModel(string deviceModel)
    {
        return await _context.DeviceTags.AsNoTracking()
            .Where(t => t.DeviceModel == deviceModel)
            .OrderBy(t => t.TagName)
            .ToListAsync();
    }

    public async Task<List<DeviceTag>> GetAllTags()
    {
        return await _context.DeviceTags.AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctDeviceModels()
    {
        return await _context.DeviceTags.AsNoTracking()
            .Where(t => t.DeviceModel != null)
            .Select(t => t.DeviceModel!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }
}
