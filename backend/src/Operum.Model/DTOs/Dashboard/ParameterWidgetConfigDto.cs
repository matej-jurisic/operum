namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Parameter item. Serialized camelCase
    // like ViewSelectorWidgetConfigDto, and written by hand rather than through the
    // controller's JSON formatting.
    //
    // ViewId names the single DashboardView on this board whose clauses the widget drives.
    // ValueByQuery is the persisted current value per clause, keyed by the pooled query id
    // (the same key ViewSelectorLinkDto.FieldByQuery uses) -- changing it (see
    // SetParameterValues) re-filters every followed widget for every future load. A clause
    // with no entry here (or an empty one) is left unapplied. Links carry, per followed
    // Analytic/Entries widget and per tracker it reads from, which field each clause runs
    // against -- reused verbatim from the view selector.
    public class ParameterWidgetConfigDto
    {
        public string ViewId { get; set; } = string.Empty;
        public Dictionary<string, string?> ValueByQuery { get; set; } = [];
        public List<ViewSelectorLinkDto> Links { get; set; } = [];
    }
}
