using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    public class Entry
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Provider key (e.g. "intervals.icu"), or null for hand-created/CSV-imported entries.
        public string? Source { get; set; }

        // Paired with Source, forms the idempotency key a re-sync updates on; see the
        // filtered unique index in OperumContext and Domain/Entries/EntryWriter.
        public string? ExternalId { get; set; }

        // Parent record's id for providers whose records nest (e.g. a Firefly transaction
        // group's splits). Lets EntryWriter remove children missing from a later sync. Null
        // for flat records and hand-created entries.
        public string? ExternalGroupId { get; set; }

        public virtual List<FieldValue> FieldValues { get; set; } = [];
    }
}
