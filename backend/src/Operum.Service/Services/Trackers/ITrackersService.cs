using Operum.Model.Common;
using Operum.Model.DTOs.Analytics;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;

namespace Operum.Service.Services.Trackers
{
    public interface ITrackersService
    {
        public Task<ServiceResponse<TrackerDto>> CreateTracker(CreateTrackerDto tracker);
        public Task<ServiceResponse<TrackerDto>> GetTracker(string id);
        public Task<ServiceResponse<List<TrackerDto>>> GetTrackerList(string filter);
        public Task<ServiceResponse<List<TrackerDto>>> GetAllTemplateTrackerList();
        public Task<ServiceResponse<List<TrackerDto>>> GetPublicTemplateTrackerList();
        public Task<ServiceResponse<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker);
        public Task<ServiceResponse> UpdateDefaultView(string id, string? defaultViewId);
        public Task<ServiceResponse> DeleteTracker(string id);
        public Task<ServiceResponse<List<FieldAnalyticsDto>>> GetTrackerAnalytics(string trackerId, string? viewId);
        public Task<ServiceResponse<List<PublicApplicationUserDto>>> GetApplicationUserTrackerList(string trackerId);
        public Task<ServiceResponse> AddUserToTracker(string trackerId, ModifyUserTrackerDto addUserToTracker);
        public Task<ServiceResponse> RemoveUserFromTracker(string trackerId, ModifyUserTrackerDto addUserToTracker);
    }
}
