namespace Operum.Model.DTOs.Entries.Requests
{
    /// <summary>
    /// Identifies a set of entries to act on, either explicitly by id or by
    /// "everything matching the currently selected views", minus any exclusions.
    /// </summary>
    public class EntrySelectionDto
    {
        public List<string> EntryIds { get; set; } = [];
        public bool SelectAllMatching { get; set; }
        public List<string> ViewIds { get; set; } = [];
        public List<string> ExcludedEntryIds { get; set; } = [];
    }
}
