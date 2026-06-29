namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IMonitoringDeviceRepository
{
    Task<List<MonitoringDevice>> GetAllDevices();
    Task<List<MonitoringDevice>> GetDevicesByType(string type);
    Task<List<MonitoringDevice>> GetDevicesByPlant(string plant);
    Task<List<MonitoringDevice>> GetDevicesByBuilding(string building);
    Task<MonitoringDevice?> GetDeviceById(int deviceId);
    Task<MonitoringDevice?> GetDeviceByDeviceId(int deviceId);
    Task<int> GetOnlineDeviceCount();
    Task<List<string>> GetAllPlants();
    Task<List<string>> GetBuildingsByPlant(string plant);
    Task<List<string>> GetLocationsByBuilding(string plant, string building);
}
