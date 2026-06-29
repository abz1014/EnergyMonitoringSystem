namespace EMS.Core.Interfaces;

using EMS.Core.Models;

public interface IFlowmeterRepository
{
    Task<List<FlowmeterData>> GetFlowmeterData(int deviceId, DateTime from, DateTime to);
    Task<FlowmeterData?> GetLatestFlowReading(int deviceId);
    Task<List<FlowmeterData>> GetAllLatestReadings();
}
