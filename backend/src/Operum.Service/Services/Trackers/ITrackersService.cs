using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;

namespace Operum.Service.Services.Trackers
{
    public interface ITrackersService
    {
        public Task<ServiceResponse<TrackerDto>> CreateTracker(CreateTrackerDto tracker);
        public Task<ServiceResponse<TrackerDto>> GetTracker(string id);
        public Task<ServiceResponse<List<TrackerDto>>> GetTrackerList();
        public Task<ServiceResponse<List<TrackerDto>>> GetAllTemplateTrackerList();
        public Task<ServiceResponse<List<TrackerDto>>> GetPublicTemplateTrackerList();
        public Task<ServiceResponse<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker);
        public Task<ServiceResponse> DeleteTracker(string id);
        public Task<ServiceResponse<List<FieldAnalyticsDto>>> GetTrackerAnalytics(string trackerId, string? viewId);
    }
}
