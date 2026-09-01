namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Parameter item. Serialized camelCase
    // like ViewSelectorWidgetConfigDto, and written by hand rather than through the
    // controller's JSON formatting.
    //
    // QueryIds is the widget's own ordered clause set -- pooled Query ids (see QueryPool),
    // resolved from the clauses the editor sends. Unlike a view selector, a parameter widget
    // does not point at a named DashboardView; it owns its clauses outright.
    // ValueByQuery is the persisted current value per clause, keyed by pooled query id (the
    // same key ViewSelectorLinkDto.FieldByQuery uses) -- changing it (see SetParameterValues)
    // re-filters every followed widget for every future load. A clause with no entry here
    // (or an empty one) is left unapplied. Links carry, per followed Analytic/Entries widget
    // and per tracker it reads from, which field each clause runs against -- reused verbatim
    // from the view selector.
    public class ParameterWidgetConfigDto
    {
        public List<string> QueryIds { get; set; } = [];
        public Dictionary<string, string?> ValueByQuery { get; set; } = [];
        public List<ViewSelectorLinkDto> Links { get; set; } = [];
    }
}
