namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IDeviceTagRepository
{
    Task<List<DeviceTag>> GetTagsByDeviceModel(string deviceModel);
    Task<List<DeviceTag>> GetAllTags();
    Task<List<string>> GetDistinctDeviceModels();
}
