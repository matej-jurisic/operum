using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Operum.Model;
using Operum.Model.Configuration;
using Operum.Model.DTOs.Push;
using Operum.Model.Models;
using Operum.Service.Interfaces;
using System.Net;
using System.Text.Json;
using WebPush;

namespace Operum.Service.Services.Push
{
    public class WebPushService(OperumContext db, IOptions<VapidSettings> vapidOptions, ILogger<WebPushService> logger) : IWebPushService
    {
        private readonly VapidSettings _vapid = vapidOptions.Value;

        public string GetVapidPublicKey() => _vapid.PublicKey;

        public async Task RegisterSubscriptionAsync(string userId, RegisterPushSubscriptionDto dto)
        {
            var exists = await db.UserPushSubscriptions
                .AnyAsync(s => s.UserId == userId && s.Endpoint == dto.Endpoint);

            if (!exists)
            {
                db.UserPushSubscriptions.Add(new UserPushSubscription
                {
                    UserId = userId,
                    Endpoint = dto.Endpoint,
                    P256dh = dto.P256dh,
                    Auth = dto.Auth,
                });
                await db.SaveChangesAsync();
            }
        }

        public async Task UnregisterSubscriptionAsync(string userId, string endpoint)
        {
            await db.UserPushSubscriptions
                .Where(s => s.UserId == userId && s.Endpoint == endpoint)
                .ExecuteDeleteAsync();
        }

        public async Task SendToTrackerUsersAsync(string trackerId, string title, string body, string url, CancellationToken ct = default)
        {
            var tracker = await db.Trackers
                .Include(t => t.ApplicationUserTrackers)
                .FirstOrDefaultAsync(t => t.Id == trackerId, ct);

            if (tracker == null) return;

            var memberIds = tracker.ApplicationUserTrackers
                .Select(ut => ut.ApplicationUserId)
                .Append(tracker.OwnerId)
                .Distinct()
                .ToList();

            var subscriptions = await db.UserPushSubscriptions
                .Where(s => memberIds.Contains(s.UserId))
                .ToListAsync(ct);

            if (subscriptions.Count == 0) return;

            var client = new WebPushClient();
            var vapidDetails = new VapidDetails(_vapid.Subject, _vapid.PublicKey, _vapid.PrivateKey);
            var payload = JsonSerializer.Serialize(new { title, body, data = new { url = url } });

            var expiredIds = new List<string>();

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                    await client.SendNotificationAsync(pushSub, payload, vapidDetails, ct);
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    expiredIds.Add(sub.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send push notification to subscription {Id}", sub.Id);
                }
            }

            if (expiredIds.Count > 0)
            {
                await db.UserPushSubscriptions
                    .Where(s => expiredIds.Contains(s.Id))
                    .ExecuteDeleteAsync(ct);
            }
        }
    }
}
