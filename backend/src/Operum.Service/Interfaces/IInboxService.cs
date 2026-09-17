using Operum.Model.Common;
using Operum.Model.DTOs.Notifications;

namespace Operum.Service.Interfaces
{
    public interface IInboxService
    {
        Task<Result<InboxPageDto>> GetInbox(int skip, int take);
        Task<Result<int>> GetUnreadCount();
        Task<Result> MarkRead(string id);
        Task<Result> MarkAllRead();
        Task<Result> Delete(string id);

        // Does not call SaveChanges; the caller owns the unit of work.
        Task CreateForTrackerMembersAsync(
            string trackerId, string? notificationId, string title, string body, string url, CancellationToken ct = default);
    }
}
