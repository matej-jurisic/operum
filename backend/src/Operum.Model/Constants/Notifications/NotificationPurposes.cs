namespace Operum.Model.Constants.Notifications
{
    // Entry mode's own purpose; Analytic mode uses AnalyticPurposes instead. Display fields are
    // listed via the {fieldValueList} message token.
    public static class NotificationPurposes
    {
        public const string Display = "Display";

        public static readonly HashSet<string> All = [Display];

        public static bool IsValid(string purpose) => All.Contains(purpose);
    }
}
