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

        /// <summary>
        /// Which integration produced this entry (a provider key, e.g. "intervals.icu"), or
        /// null for anything a user created by hand or imported from CSV.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// The provider's own stable id for the record behind this entry -- a wellness date,
        /// a transaction journal id. Together with <see cref="Source"/> this is what makes a
        /// re-sync update rather than duplicate; see the filtered unique index in
        /// OperumContext and Domain/Entries/EntryWriter.
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// The provider's id for the parent record this entry came from, when the provider has
        /// that shape -- a Firefly transaction group fans out into one entry per split, and
        /// all of them carry the group's id here.
        /// <para>
        /// It exists so a parent that arrives with fewer children than last time can have the
        /// missing ones removed: see EntryWriter's group reconciliation. Null for providers
        /// whose records are flat, and for everything a user made by hand.
        /// </para>
        /// </summary>
        public string? ExternalGroupId { get; set; }

        public virtual List<FieldValue> FieldValues { get; set; } = [];
    }
}
