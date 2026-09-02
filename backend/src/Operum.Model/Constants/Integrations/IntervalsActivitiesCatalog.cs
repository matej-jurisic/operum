using Operum.Model.Constants.Fields;
using Operum.Model.Integrations;

namespace Operum.Model.Constants.Integrations
{
    /// <summary>
    /// The values an intervals.icu activity can supply. A fixed schema, so this is a hardcoded
    /// catalog rather than anything inferred at runtime -- same shape as
    /// <see cref="IntervalsWellnessCatalog"/>.
    /// <para>
    /// The activity payload carries ~180 fields; this is a deliberate endurance-training
    /// subset. Adding one later is a single line here and a mapping the user makes -- nothing
    /// stored breaks -- so the bar for inclusion is "an athlete would plausibly track this",
    /// not "it exists".
    /// </para>
    /// <para>
    /// Each Key is the JSON property the provider reads, matched on a normalised form
    /// (case-insensitive, underscores ignored).
    /// </para>
    /// </summary>
    public static class IntervalsActivitiesCatalog
    {
        public const string ResourceType = "activities";

        /// <summary>
        /// The activity's own id (e.g. <c>i77123456</c>) -- an opaque string, not a date. The
        /// provider uses it as the ExternalId, which is what makes a re-sync update rather than
        /// duplicate. Unlike a wellness record's date it is not worth mapping, so it is not in
        /// <see cref="Mappable"/>.
        /// </summary>
        public const string RecordKey = "id";

        /// <summary>
        /// Activities carry no "last modified" timestamp (<c>created</c>, <c>icu_sync_date</c>
        /// and <c>analyzed</c> all mean something narrower), so there is no sync cursor for
        /// this resource. The reconciliation window on each incremental sync is what picks up
        /// an activity edited after the fact; the write path absorbs the re-read as an
        /// idempotent upsert.
        /// </summary>
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

            // Computed load and analysis, not something the athlete logs.
            new("icu_training_load", DataTypes.Number, "Training load", "intervals.icu load, TSS-equivalent."),
            new("icu_intensity", DataTypes.Number, "Intensity", "Intensity factor, percent."),
            new("trimp", DataTypes.Number, "TRIMP"),
            new("icu_efficiency_factor", DataTypes.Number, "Efficiency factor"),
            new("decoupling", DataTypes.Number, "Decoupling", "Aerobic decoupling, percent."),
            new("polarization_index", DataTypes.Number, "Polarization index"),
            new("icu_ctl", DataTypes.Number, "Fitness (CTL)", "As of this activity."),
            new("icu_atl", DataTypes.Number, "Fatigue (ATL)", "As of this activity."),

            // Subjective, entered by the athlete.
            new("feel", DataTypes.Number, "Feel", "1 (worst) to 5 (best)."),
            new("perceived_exertion", DataTypes.Number, "Perceived exertion", "RPE."),
        ];

        /// <summary>What a user may map: everything but the opaque record id.</summary>
        public static readonly IReadOnlyList<SourceField> Mappable = [.. Fields];
    }
}
