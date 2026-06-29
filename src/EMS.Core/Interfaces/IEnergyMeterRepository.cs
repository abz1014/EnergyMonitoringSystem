namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IEnergyMeterRepository
{
    Task<List<EnergyMeterData>> GetDailyConsumption(int meterId, DateTime date);
    Task<List<EnergyMeterData>> GetConsumptionRange(int meterId, DateTime from, DateTime to);
    Task<EnergyMeterData?> GetLatestReading(int meterId);
    Task<double> GetTodaysTotalConsumption();
    Task<double> GetPeakDemandToday();
    Task<List<EnergyMeterData>> GetByDateRange(DateTime from, DateTime to);
}
