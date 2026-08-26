using Operum.Model.Common;
using Operum.Model.DTOs.Trackers;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Users;


namespace Operum.Service.Interfaces
{
    public interface ITrackersService
    {
        public Task<Result<TrackerDto>> CreateTracker(CreateTrackerDto tracker);
        public Task<Result<TrackerDto>> GetTracker(string id);
        public Task<Result<List<TrackerDto>>> GetTrackerList(string filter);
        public Task<Result<List<TrackerDto>>> GetAllTemplateTrackerList();
        public Task<Result<List<TrackerDto>>> GetPublicTemplateTrackerList();
        public Task<Result<TrackerDto>> UpdateTracker(string id, UpdateTrackerDto tracker);
        public Task<Result> UpdateDefaultView(string id, string? viewId);
        public Task<Result> DeleteTracker(string id);
        public Task<Result<List<TrackerCollaboratorDto>>> GetApplicationUserTrackerList(string trackerId);
        public Task<Result> AddUserToTracker(string trackerId, AddUserToTrackerDto addUserToTracker);
        public Task<Result> RemoveUserFromTracker(string trackerId, RemoveUserFromTrackerDto addUserToTracker);
        public Task<Result> UpdateCollaboratorPermissions(string trackerId, UpdateCollaboratorPermissionsDto dto);
        public Task<Result> ReorderTrackers(ReorderTrackersDto dto);
    }
}
