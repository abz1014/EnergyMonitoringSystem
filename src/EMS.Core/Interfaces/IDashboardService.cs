namespace EMS.Core.Interfaces;

public interface IDashboardService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DashboardFilterDto filter);
}
