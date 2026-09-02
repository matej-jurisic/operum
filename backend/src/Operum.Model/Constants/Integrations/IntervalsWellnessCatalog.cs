using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;

namespace Operum.Model.Constants.Integrations
{
    /// <summary>
    /// The values an intervals.icu daily wellness record can supply. A fixed schema, so this
    /// is a hardcoded catalog rather than anything inferred at runtime -- same shape as
    /// <see cref="DataTypes"/> and <c>OperatorTypes</c>.
    /// <para>
    /// Each Key is also the JSON property the provider reads. Matching is done on a
    /// normalised form (case-insensitive, underscores ignored), so a payload using
    /// <c>sleep_secs</c> resolves the same key as one using <c>sleepSecs</c> -- both spellings
    /// appear in circulation and the provider should not care which arrives.
    /// </para>
    /// </summary>
    public static class IntervalsWellnessCatalog
    {
        public const string ResourceType = "wellness";

        /// <summary>
        /// The record's own id -- the date it describes. The provider always uses it as the
        /// ExternalId, which is what makes a re-sync update rather than duplicate. It stays
        /// mappable as well, since a tracker of daily records nearly always wants that date
        /// in a field of its own.
        /// </summary>
        public const string RecordKey = "id";

        /// <summary>Last revision timestamp; used as the sync cursor, not offered for mapping.</summary>
        public const string UpdatedKey = "updated";

        /// <summary>Seconds in the payload, offered as a timespan; the provider converts.</summary>
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

            // Computed training load, not something the athlete logs.
            new("ctl", DataTypes.Number, "Fitness (CTL)"),
            new("atl", DataTypes.Number, "Fatigue (ATL)"),
            new("rampRate", DataTypes.Number, "Ramp rate"),
            new("ctlLoad", DataTypes.Number, "CTL load"),
            new("atlLoad", DataTypes.Number, "ATL load"),

            // Subjective 1-5 scales.
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

            // sportInfo is left out on purpose: it is a nested array of per-sport values
            // (type/eftp/wPrime/pMax) and a field holds a flat scalar. Adding it means
            // flattening per sport -- eFTP_Ride, eFTP_Run -- which is its own decision.
        ];

        /// <summary>What a user may map: everything but the record key and the cursor.</summary>
        public static readonly IReadOnlyList<SourceField> Mappable =
            [.. Fields.Where(f => f.Key != UpdatedKey)];
    }
}
