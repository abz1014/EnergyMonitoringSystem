namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IAlarmRepository
{
    Task<List<Alarm>> GetActiveAlarms();
    Task<List<Alarm>> GetAlarmsByMeterId(int meterId);
    Task<List<Alarm>> GetAlarmsBySeverity(string severity);
    Task<Alarm?> GetAlarmById(int id);
    Task AcknowledgeAlarm(int id, string acknowledgedBy, string? note = null);
    Task<int> GetActiveAlarmCount();
    Task AddAlarm(Alarm alarm);
}
