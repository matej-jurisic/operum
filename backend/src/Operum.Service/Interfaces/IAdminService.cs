using Operum.Model.Common;
using Operum.Model.DTOs.Admin;

namespace Operum.Service.Interfaces
{
    public interface IAdminService
    {
        Task<Result<AdminStatsDto>> GetStats();
        Task<Result<List<AdminTrackerDto>>> GetAllTrackers();
    }
}
