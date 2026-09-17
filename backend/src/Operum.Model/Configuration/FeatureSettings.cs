namespace Operum.Model.Configuration
{
    /// <summary>Feature flags bound from the "Features" config section.</summary>
    public class FeatureSettings
    {
        /// <summary>Gates the notification/web-push endpoints and the background evaluator.</summary>
        public bool Notifications { get; set; }

        /// <summary>Gates the integration endpoints, webhook receiver, and background sync loop.</summary>
        public bool Integrations { get; set; }
    }
}
