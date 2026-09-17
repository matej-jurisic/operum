namespace Operum.Model.DTOs.Dashboard.Requests
{
    // Keyed by SlotId. Persisted on the item's Config, so it's not just this session. A
    // missing or empty value leaves that clause unapplied.
    public class SetFilterValuesDto
    {
        public Dictionary<string, string?> Values { get; set; } = [];
    }
}
