namespace Operum.Model.Configuration
{
    /// <summary>
    /// Switches for features that are not ready to be exposed in every deployment yet.
    /// Bound from the "Features" section (env: Features__Notifications).
    /// </summary>
    public class FeatureSettings
    {
        /// <summary>
        /// Tracker notifications: the notification and web push endpoints plus the
        /// background evaluator. Off unless a deployment explicitly opts in.
        /// </summary>
        public bool Notifications { get; set; }

        /// <summary>
        /// Integrations: the integration endpoints, the webhook receiver and the background
        /// sync loop. Off unless a deployment explicitly opts in (env: Features__Integrations).
        /// </summary>
        public bool Integrations { get; set; }
    }
}
