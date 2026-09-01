namespace Operum.Model.DTOs.Dashboard
{
    // Names one Analytic/Entries widget a filter widget (currently only Filter) narrows:
    // the followed widget, the tracker on it this link reads from, and which of that
    // tracker's fields each clause runs against, keyed by the clause's pooled query id.
    // Shared by both of a Filter widget's independent link lists -- Links (its own typed
    // clauses) and PresetLinks (the clauses of whichever preset DashboardView is selected).
    public class WidgetLinkDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public Dictionary<string, string> FieldByQuery { get; set; } = [];
    }
}
