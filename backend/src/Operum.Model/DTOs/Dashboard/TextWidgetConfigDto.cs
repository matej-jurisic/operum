namespace Operum.Model.DTOs.Dashboard
{
    // The Config payload for a DashboardWidgetTypes.Header or DashboardWidgetTypes.Note
    // widget: the two widgets that are nothing but user-entered text, a heading read at a
    // glance or a free-form note. A Divider widget draws nothing but a line and needs no
    // config at all, so it has none.
    //
    // Serialized with camelCase to match every other DTO the client reads, since this one
    // is written by hand rather than through the controller's own JSON formatting.
    public class TextWidgetConfigDto
    {
        public string Text { get; set; } = string.Empty;
    }
}
