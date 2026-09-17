using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;

namespace Operum.Model.Constants.Integrations
{
    /// <summary>
    /// The activity payload carries ~180 fields; this is a deliberate endurance-training subset
    /// (adding more later is non-breaking). Keys are matched case-insensitive, underscores ignored.
    /// </summary>
    public static class IntervalsActivitiesCatalog
    {
        public const string ResourceType = "activities";

        // Opaque id (e.g. i77123456), used as ExternalId. Not in Mappable.
        public const string RecordKey = "id";

        // No "last modified" timestamp exists for this resource; each incremental sync's
        // reconciliation window picks up edited activities instead, absorbed as an idempotent upsert.
        public static readonly IReadOnlyList<SourceField> Fields =
        [
            new("start_date_local", DataTypes.DateTime, "Start time", "When the activity started, in the athlete's local time."),
            new("type", DataTypes.String, "Sport", "Ride, Run, Swim, and so on."),
            new("name", DataTypes.String, "Name"),
            new("description", DataTypes.String, "Description"),

            new("distance", DataTypes.Number, "Distance", "Metres."),
            new("moving_time", DataTypes.TimeSpan, "Moving time"),
            new("elapsed_time", DataTypes.TimeSpan, "Elapsed time"),
            new("total_elevation_gain", DataTypes.Number, "Elevation gain", "Metres."),

            new("average_speed", DataTypes.Number, "Average speed", "Metres per second."),
            new("max_speed", DataTypes.Number, "Max speed", "Metres per second."),
            new("average_cadence", DataTypes.Number, "Average cadence"),

            new("average_heartrate", DataTypes.Number, "Average HR"),
            new("max_heartrate", DataTypes.Number, "Max HR"),

            new("icu_average_watts", DataTypes.Number, "Average power", "Watts."),
            new("icu_weighted_avg_watts", DataTypes.Number, "Weighted average power", "Normalised power, watts."),

            new("calories", DataTypes.Number, "Calories"),
            new("carbs_ingested", DataTypes.Number, "Carbs ingested", "Grams."),

            // Computed, not athlete-logged.
            new("icu_training_load", DataTypes.Number, "Training load", "intervals.icu load, TSS-equivalent."),
            new("icu_intensity", DataTypes.Number, "Intensity", "Intensity factor, percent."),
            new("trimp", DataTypes.Number, "TRIMP"),
            new("icu_efficiency_factor", DataTypes.Number, "Efficiency factor"),
            new("decoupling", DataTypes.Number, "Decoupling", "Aerobic decoupling, percent."),
            new("polarization_index", DataTypes.Number, "Polarization index"),
            new("icu_ctl", DataTypes.Number, "Fitness (CTL)", "As of this activity."),
            new("icu_atl", DataTypes.Number, "Fatigue (ATL)", "As of this activity."),

            // Subjective, athlete-entered.
            new("feel", DataTypes.Number, "Feel", "1 (worst) to 5 (best)."),
            new("perceived_exertion", DataTypes.Number, "Perceived exertion", "RPE."),
        ];

        public static readonly IReadOnlyList<SourceField> Mappable = [.. Fields];
    }
}
