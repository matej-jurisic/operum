using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;

namespace Operum.Model.Constants.Integrations
{
    /// <summary>
    /// Keys are matched on a normalised form (case-insensitive, underscores ignored), so both
    /// <c>sleep_secs</c> and <c>sleepSecs</c> resolve the same key.
    /// </summary>
    public static class IntervalsWellnessCatalog
    {
        public const string ResourceType = "wellness";

        // The date this record describes; used as ExternalId, and kept mappable too.
        public const string RecordKey = "id";

        // Used as the sync cursor, not offered for mapping.
        public const string UpdatedKey = "updated";

        public const string SleepSecondsKey = "sleepSecs";

        public static readonly IReadOnlyList<SourceField> Fields =
        [
            new(RecordKey, DataTypes.Date, "Date", "The day this record describes."),

            new(SleepSecondsKey, DataTypes.TimeSpan, "Sleep", "Time asleep."),
            new("sleepScore", DataTypes.Number, "Sleep score"),
            new("sleepQuality", DataTypes.Number, "Sleep quality"),
            new("avgSleepingHR", DataTypes.Number, "Average sleeping HR"),

            new("weight", DataTypes.Number, "Weight"),
            new("restingHR", DataTypes.Number, "Resting HR"),
            new("hrv", DataTypes.Number, "HRV"),
            new("hrvSDNN", DataTypes.Number, "HRV SDNN"),
            new("vo2max", DataTypes.Number, "VO2 max"),
            new("bodyFat", DataTypes.Number, "Body fat"),
            new("abdomen", DataTypes.Number, "Abdomen"),

            // Computed, not athlete-logged.
            new("ctl", DataTypes.Number, "Fitness (CTL)"),
            new("atl", DataTypes.Number, "Fatigue (ATL)"),
            new("rampRate", DataTypes.Number, "Ramp rate"),
            new("ctlLoad", DataTypes.Number, "CTL load"),
            new("atlLoad", DataTypes.Number, "ATL load"),

            new("soreness", DataTypes.Number, "Soreness"),
            new("fatigue", DataTypes.Number, "Fatigue"),
            new("stress", DataTypes.Number, "Stress"),
            new("mood", DataTypes.Number, "Mood"),
            new("motivation", DataTypes.Number, "Motivation"),
            new("injury", DataTypes.Number, "Injury"),

            new("spO2", DataTypes.Number, "SpO2"),
            new("systolic", DataTypes.Number, "Systolic"),
            new("diastolic", DataTypes.Number, "Diastolic"),
            new("respiration", DataTypes.Number, "Respiration"),
            new("steps", DataTypes.Number, "Steps"),
            new("readiness", DataTypes.Number, "Readiness"),
            new("baevskySI", DataTypes.Number, "Baevsky stress index"),
            new("bloodGlucose", DataTypes.Number, "Blood glucose"),
            new("lactate", DataTypes.Number, "Lactate"),

            new("hydration", DataTypes.Number, "Hydration"),
            new("hydrationVolume", DataTypes.Number, "Hydration volume"),
            new("kcalConsumed", DataTypes.Number, "Calories consumed"),
            new("carbohydrates", DataTypes.Number, "Carbohydrates"),
            new("protein", DataTypes.Number, "Protein"),
            new("fatTotal", DataTypes.Number, "Fat"),

            new("menstrualPhase", DataTypes.String, "Menstrual phase"),
            new("menstrualPhasePredicted", DataTypes.String, "Menstrual phase (predicted)"),
            new("comments", DataTypes.String, "Comments"),

            new("locked", DataTypes.Bool, "Locked"),
            new("tempWeight", DataTypes.Bool, "Weight is provisional"),
            new("tempRestingHR", DataTypes.Bool, "Resting HR is provisional"),

            // sportInfo omitted: nested per-sport array, doesn't fit a flat scalar field.
        ];

        public static readonly IReadOnlyList<SourceField> Mappable =
            [.. Fields.Where(f => f.Key != UpdatedKey)];
    }
}
