using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    // Written by EntriesService on every hand-driven create/update; cascades with the Entry,
    // so a deleted entry takes its history with it (there is no deleted-entries browser to
    // show it in). Bulk paths (CSV import, batch, recalculate) don't write these yet.
    public class EntryRevision
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string EntryId { get; set; } = string.Empty;
        [ForeignKey(nameof(EntryId))]
        public virtual Entry Entry { get; set; } = null!;

        public string ChangeType { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Nulled when the user is deleted; ChangedByUserName is kept so the entry it made no
        // longer needs the account to stay attributable.
        public string? ChangedByUserId { get; set; }
        [ForeignKey(nameof(ChangedByUserId))]
        public virtual User? ChangedByUser { get; set; }
        public string ChangedByUserName { get; set; } = string.Empty;

        // JSON-serialized List<EntryRevisionFieldSnapshot>: every field's display value at this
        // moment, captured with the field's name/type as they were then, since a field can be
        // renamed, retyped, or deleted later. Diffed against the previous revision at read time.
        public string Snapshot { get; set; } = "[]";
    }
}
