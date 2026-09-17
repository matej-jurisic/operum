namespace Operum.Tests.Util
{
    /// <summary>
    /// Turns on the notification feature flag (see RequiresNotificationsAttribute) so the CRUD
    /// endpoints answer instead of 404. The hosted evaluator is separate wiring (ServiceConfiguration)
    /// and isn't registered here; tests drive NotificationEvaluatorService's pieces directly.
    /// </summary>
    public class NotificationsEnabledFactory : CustomWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
        {
            ["Features:Notifications"] = "true",
        };
    }
}
