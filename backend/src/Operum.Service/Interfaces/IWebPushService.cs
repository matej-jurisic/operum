using Operum.Model.DTOs.Push;

namespace Operum.Service.Interfaces
{
    public interface IWebPushService
    {
        string GetVapidPublicKey();
        Task RegisterSubscriptionAsync(string userId, RegisterPushSubscriptionDto dto);
        Task UnregisterSubscriptionAsync(string userId, string endpoint);
        Task SendToTrackerUsersAsync(string trackerId, string title, string body, string url, CancellationToken ct = default);
    }
}
