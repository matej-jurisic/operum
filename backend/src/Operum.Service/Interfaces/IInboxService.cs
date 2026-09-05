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

        /// <summary>
        /// Adds one inbox row for every member of the tracker (owner + collaborators). Does not
        /// call SaveChanges: the caller owns the unit of work (the notification evaluator batches
        /// this with its own state writes).
        /// </summary>
        Task CreateForTrackerMembersAsync(
            string trackerId, string? notificationId, string title, string body, string url, CancellationToken ct = default);
    }
}
