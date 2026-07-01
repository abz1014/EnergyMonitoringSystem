namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IAlarmRepository
{
    Task<List<Alarm>> GetAllAlarms();
    Task<List<Alarm>> GetActiveAlarms();
    Task<List<Alarm>> GetAlarmsByMeterId(int meterId);
    Task<List<Alarm>> GetAlarmsBySeverity(byte severity);
    Task<Alarm?> GetAlarmById(int id);
    Task AcknowledgeAlarm(int id, string acknowledgedBy, string? note = null);
    Task<int> GetActiveAlarmCount();
    Task<int> GetAlarmCountInRange(DateTime from, DateTime to);
    Task AddAlarm(Alarm alarm);
}
