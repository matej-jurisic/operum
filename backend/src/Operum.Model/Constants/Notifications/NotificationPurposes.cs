namespace Operum.Model.Constants.Notifications
{
    /// <summary>Purposes a NotificationConditionPurposeField can serve. Analytic mode uses AnalyticPurposes
    /// (Value, X-axis, ...) instead -- Display is Entry mode's own: the fields whose values get
    /// listed out for the entries a notification fires on, via the {fieldValueList} message token.</summary>
    public static class NotificationPurposes
    {
        public const string Display = "Display";

        public static readonly HashSet<string> All = [Display];

        public static bool IsValid(string purpose) => All.Contains(purpose);
    }
}
