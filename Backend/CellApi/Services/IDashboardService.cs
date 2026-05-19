using CellApi.DTOs;

namespace CellApi.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}
