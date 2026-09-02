using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Operum.Model.Models
{
    /// <summary>
    /// One user's connection to one provider account. What flows out of it is configured per
    /// tracker on <see cref="IntegrationTarget"/>, so a single connection can feed several.
    /// </summary>
    public class Integration
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>An <c>IIntegrationProvider.Key</c>, e.g. "intervals.icu".</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Who the credential turned out to belong to, resolved when the connection was made.
        /// Null for a push-only connection, which never authenticates outbound and so has no
        /// account to resolve.
        /// </summary>
        public string? ExternalAccountId { get; set; }

        /// <summary>
        /// The user's own instance, for self-hosted providers. Null for a cloud provider,
        /// whose host its own code knows.
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Encrypted with Data Protection, never returned to a client -- the API exposes a
        /// masked suffix only. Null for a push-only connection.
        /// </summary>
        public string? CredentialCiphertext { get; set; }

        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual List<IntegrationTarget> Targets { get; set; } = [];
    }
}
