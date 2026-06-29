namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class EnergyMeterLiveRepository : IEnergyMeterLiveRepository
{
    private readonly ScadaDbContext _context;

    public EnergyMeterLiveRepository(ScadaDbContext context)
    {
        _context = context;
    }

    public async Task<List<EnergyMeterLive>> GetAllLive()
    {
        return await _context.EnergyMeterLive.AsNoTracking().ToListAsync();
    }

    public async Task<List<EnergyMeterLive>> GetLiveByPlant(string plant)
    {
        return await _context.EnergyMeterLive.AsNoTracking()
            .Join(_context.MonitoringDevices.AsNoTracking(),
                live => live.MeterNo,
                device => device.DeviceID,
                (live, device) => new { live, device })
            .Where(x => x.device.Plant == plant)
            .Select(x => x.live)
            .ToListAsync();
    }

    public async Task<List<EnergyMeterLive>> GetLiveByBuilding(string building)
    {
        return await _context.EnergyMeterLive.AsNoTracking()
            .Join(_context.MonitoringDevices.AsNoTracking(),
                live => live.MeterNo,
                device => device.DeviceID,
                (live, device) => new { live, device })
            .Where(x => x.device.Building == building)
            .Select(x => x.live)
            .ToListAsync();
    }

    public async Task<EnergyMeterLive?> GetLiveByMeterId(int meterId)
    {
        return await _context.EnergyMeterLive.AsNoTracking()
            .FirstOrDefaultAsync(e => e.MeterNo == meterId);
    }

    public async Task UpdateLive(EnergyMeterLive meterLive)
    {
        var existing = await _context.EnergyMeterLive
            .FirstOrDefaultAsync(e => e.MeterNo == meterLive.MeterNo);

        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(meterLive);
            await _context.SaveChangesAsync();
        }
    }
}
