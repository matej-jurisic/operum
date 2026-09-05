namespace Operum.Tests.Util
{
    /// <summary>
    /// The app with the notification feature flag on -- see RequiresNotificationsAttribute --
    /// so the controller endpoints answer instead of 404. The hosted evaluator is still not
    /// registered here (that wiring lives in ServiceConfiguration, not the flag alone, and
    /// tests drive NotificationEvaluatorService's pieces directly rather than waiting on a
    /// timer), so this only unlocks the CRUD surface.
    /// </summary>
    public class NotificationsEnabledFactory : CustomWebApplicationFactory
    {
        protected override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>
        {
            ["Features:Notifications"] = "true",
        };
    }
}
