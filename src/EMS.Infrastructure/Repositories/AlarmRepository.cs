namespace EMS.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using EMS.Core.Interfaces;
using EMS.Core.Models;
using EMS.Infrastructure.Data;

public class AlarmRepository : IAlarmRepository
{
    private readonly ScadaDbContext _context;

    public AlarmRepository(ScadaDbContext context)
    {
        _context = context;
    }

    public async Task<List<Alarm>> GetActiveAlarms()
    {
        return await _context.Alarms
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Alarm>> GetAlarmsByMeterId(int meterId)
    {
        return await _context.Alarms
            .Where(a => a.MeterNo == meterId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Alarm>> GetAlarmsBySeverity(string severity)
    {
        return await _context.Alarms
            .Where(a => a.IsActive && a.Severity == severity)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Alarm?> GetAlarmById(int id)
    {
        return await _context.Alarms.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AcknowledgeAlarm(int id, string acknowledgedBy, string? note = null)
    {
        var alarm = await _context.Alarms.FirstOrDefaultAsync(a => a.Id == id);
        if (alarm != null)
        {
            alarm.IsActive = false;
            alarm.AckBy = acknowledgedBy;
            alarm.AckTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetActiveAlarmCount()
    {
        return await _context.Alarms.CountAsync(a => a.IsActive);
    }

    public async Task AddAlarm(Alarm alarm)
    {
        _context.Alarms.Add(alarm);
        await _context.SaveChangesAsync();
    }
}
