namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Filter item. Serialized camelCase
    // like every other hand-serialized dashboard Config, since this one is written by hand
    // rather than through the controller's JSON formatting.
    //
    // A filter widget owns an ordered set of filter clauses whose values are typed on the
    // board, narrowing every followed widget in Links:
    //
    // QueryIds is the widget's own ordered clause set (pooled Query ids, see QueryPool),
    // resolved from the clauses the editor sends. ValueByQuery is the persisted current
    // value per clause, keyed by pooled query id -- changing it (see SetFilterValues)
    // re-filters every follower for every future load. A clause with no entry here (or an
    // empty one) is left unapplied. Links carries, per followed Analytic/Entries widget and
    // per tracker it reads from, which field each of these clauses runs against.
    //
    // PresetIds names the board's DashboardViews this widget offers as presets. A preset is
    // a named set of values whose clause shape (data type + operator, in order) matches this
    // widget's clauses exactly; picking one on the board just writes ValueByQuery, so a
    // preset needs no separate resolution or follower list of its own.
    public class FilterWidgetConfigDto
    {
        public List<string> QueryIds { get; set; } = [];
        public Dictionary<string, string?> ValueByQuery { get; set; } = [];
        public List<WidgetLinkDto> Links { get; set; } = [];

        public List<string> PresetIds { get; set; } = [];
    }
}
