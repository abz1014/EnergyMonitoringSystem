namespace EMS.Core.Interfaces;

public interface ILiveMonitoringService
{
    Task<LiveMonitoringResponseDto> GetLiveMetersAsync(LiveMonitoringFilterDto filter);
}
