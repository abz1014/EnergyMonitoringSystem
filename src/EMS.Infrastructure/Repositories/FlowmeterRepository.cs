namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class FlowmeterRepository : IFlowmeterRepository
{
    private readonly ScadaDbContext _context;

    public FlowmeterRepository(ScadaDbContext context)
    {
        _context = context;
    }

    public async Task<List<FlowmeterData>> GetFlowmeterData(int deviceId, DateTime from, DateTime to)
    {
        return await _context.FlowmetersData.AsNoTracking()
            .Where(f => f.MeterNo == deviceId && f.DateTime >= from && f.DateTime <= to)
            .OrderBy(f => f.DateTime)
            .ToListAsync();
    }

    public async Task<FlowmeterData?> GetLatestFlowReading(int deviceId)
    {
        return await _context.FlowmetersData.AsNoTracking()
            .Where(f => f.MeterNo == deviceId)
            .OrderByDescending(f => f.DateTime)
            .FirstOrDefaultAsync();
    }

    public async Task<List<FlowmeterData>> GetAllLatestReadings()
    {
        var latestReadings = await _context.FlowmetersData.AsNoTracking()
            .GroupBy(f => f.MeterNo)
            .Select(g => g.OrderByDescending(f => f.DateTime).FirstOrDefault())
            .Where(f => f != null)
            .ToListAsync();

        return latestReadings.Where(f => f != null).ToList()!;
    }
}
