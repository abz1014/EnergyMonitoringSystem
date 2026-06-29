namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class EnergyMeterRepository : IEnergyMeterRepository
{
    private readonly ScadaDbContext _context;

    public EnergyMeterRepository(ScadaDbContext context)
    {
        _context = context;
    }

    public async Task<List<EnergyMeterData>> GetDailyConsumption(int meterId, DateTime date)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1).AddTicks(-1);

        return await _context.EnergyMetersData
            .Where(e => e.MeterNo == meterId && e.DateTime >= startDate && e.DateTime <= endDate)
            .OrderBy(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<List<EnergyMeterData>> GetConsumptionRange(int meterId, DateTime from, DateTime to)
    {
        return await _context.EnergyMetersData
            .Where(e => e.MeterNo == meterId && e.DateTime >= from && e.DateTime <= to)
            .OrderBy(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<EnergyMeterData?> GetLatestReading(int meterId)
    {
        return await _context.EnergyMetersData
            .Where(e => e.MeterNo == meterId)
            .OrderByDescending(e => e.DateTime)
            .FirstOrDefaultAsync();
    }

    public async Task<double> GetTodaysTotalConsumption()
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        var result = await _context.EnergyMetersData
            .Where(e => e.DateTime >= today && e.DateTime < tomorrow && e.kWh.HasValue)
            .SumAsync(e => e.kWh.Value);

        return result;
    }

    public async Task<double> GetPeakDemandToday()
    {
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        var result = await _context.EnergyMetersData
            .Where(e => e.DateTime >= today && e.DateTime < tomorrow && e.kWtotal.HasValue)
            .MaxAsync(e => (double?)e.kWtotal) ?? 0;

        return result;
    }

    public async Task<List<EnergyMeterData>> GetByDateRange(DateTime from, DateTime to)
    {
        return await _context.EnergyMetersData
            .Where(e => e.DateTime >= from && e.DateTime <= to)
            .OrderBy(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<List<EnergyMeterData>> GetAggregatedByDay(DateTime from, DateTime to)
    {
        return await _context.EnergyMetersData
            .Where(e => e.DateTime >= from && e.DateTime <= to)
            .OrderBy(e => e.DateTime)
            .ToListAsync();
    }

    public async Task<List<EnergyMeterData>> GetAggregatedByHour(DateTime from, DateTime to)
    {
        return await _context.EnergyMetersData
            .Where(e => e.DateTime >= from && e.DateTime <= to)
            .OrderBy(e => e.DateTime)
            .ToListAsync();
    }
}
