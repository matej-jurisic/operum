using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    /// <summary>How records reach Operum for this target.</summary>
    public enum IntegrationMode { Pull, Push }

    public enum SyncStatus { Never, Ok, Error }

    /// <summary>
    /// One connection's data flowing into one tracker. Kept apart from
    /// <see cref="Integration"/> so a second resource -- activities beside wellness, say --
    /// is another target on the same connection rather than a second connection.
    /// </summary>
    public class IntegrationTarget
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>One of the provider's <c>ResourceTypes</c>, e.g. "wellness".</summary>
        public string ResourceType { get; set; } = string.Empty;

        public IntegrationMode Mode { get; set; } = IntegrationMode.Pull;
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// How far back the first pull reaches. Deliberately modest by default rather than
        /// "all history" -- a tracker has a finite entry cap and a backfill can exhaust it.
        /// </summary>
        public DateOnly BackfillFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));

        public DateTime? LastSyncedAt { get; set; }
        public SyncStatus LastSyncStatus { get; set; } = SyncStatus.Never;
        public string? LastSyncError { get; set; }

        /// <summary>Newest revision seen, so a pull can skip what it already has.</summary>
        public DateTime? LastCursor { get; set; }

        /// <summary>
        /// The unguessable path segment a push provider posts to. Null for a pull target.
        /// Unique on its own, since the webhook route carries nothing else to look up by.
        /// </summary>
        public string? WebhookToken { get; set; }

        /// <summary>
        /// Shared secret the delivery is signed with, encrypted at rest. Shown to the user
        /// once, when the target is created, and never again.
        /// </summary>
        public string? WebhookSecretCiphertext { get; set; }

        public string IntegrationId { get; set; } = string.Empty;
        [ForeignKey(nameof(IntegrationId))]
        public virtual Integration Integration { get; set; } = null!;

        public string TrackerId { get; set; } = string.Empty;
        [ForeignKey(nameof(TrackerId))]
        public virtual Tracker Tracker { get; set; } = null!;

        public virtual List<IntegrationFieldMapping> Mappings { get; set; } = [];
    }
}
