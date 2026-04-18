using Operum.Model.Common;
using Operum.Model.DTOs.TrackerConstants;
using Operum.Model.DTOs.TrackerConstants.Requests;

namespace Operum.Service.Interfaces
{
    public interface ITrackerConstantsService
    {
        Task<Result<TrackerConstantDto>> CreateConstant(string trackerId, CreateTrackerConstantDto dto);
        Task<Result<TrackerConstantDto>> GetConstant(string trackerId, string constantId);
        Task<Result<List<TrackerConstantDto>>> GetConstantList(string trackerId);
        Task<Result<TrackerConstantDto>> UpdateConstant(string trackerId, string constantId, UpdateTrackerConstantDto dto);
        Task<Result> DeleteConstant(string trackerId, string constantId);
    }
}
