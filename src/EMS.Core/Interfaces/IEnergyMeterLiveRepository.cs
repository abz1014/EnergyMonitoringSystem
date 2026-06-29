namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IEnergyMeterLiveRepository
{
    Task<List<EnergyMeterLive>> GetAllLive();
    Task<List<EnergyMeterLive>> GetLiveByPlant(string plant);
    Task<List<EnergyMeterLive>> GetLiveByBuilding(string building);
    Task<EnergyMeterLive?> GetLiveByMeterId(int meterId);
    Task UpdateLive(EnergyMeterLive meterLive);
}
