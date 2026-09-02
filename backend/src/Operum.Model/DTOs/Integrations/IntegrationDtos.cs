namespace Operum.Model.DTOs.Integrations
{
    /// <summary>What a provider offers, for the connect and mapping screens.</summary>
    public class ProviderDto
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool SupportsPull { get; set; }
        public bool SupportsPush { get; set; }

        /// <summary>Whether the connect form must ask for the user's own instance URL.</summary>
        public bool RequiresBaseUrl { get; set; }

        /// <summary>
        /// True when this push provider mints the webhook secret itself and the user pastes it
        /// into Operum (Firefly III). False when Operum generates it for the user to paste into
        /// the provider. Only meaningful for a push provider.
        /// </summary>
        public bool ProviderSuppliesSecret { get; set; }

        public List<ProviderResourceDto> Resources { get; set; } = [];
    }

    public class ProviderResourceDto
    {
        public string ResourceType { get; set; } = string.Empty;
        public List<SourceFieldDto> Fields { get; set; } = [];
    }

    public class SourceFieldDto
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class IntegrationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? ExternalAccountId { get; set; }
        public string? BaseUrl { get; set; }

        /// <summary>
        /// A suffix such as "…a91f", never the credential. The raw value must not appear in
        /// any response.
        /// </summary>
        public string MaskedCredential { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<IntegrationTargetDto> Targets { get; set; } = [];
    }

    public class IntegrationTargetDto
    {
        public string Id { get; set; } = string.Empty;
        public string TrackerId { get; set; } = string.Empty;
        public string TrackerName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateOnly BackfillFrom { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string LastSyncStatus { get; set; } = string.Empty;
        public string? LastSyncError { get; set; }

        /// <summary>Where a push provider should deliver. Null for a pull target.</summary>
        public string? WebhookUrl { get; set; }

        /// <summary>
        /// Only ever populated on the response that creates a target or issues a new secret for
        /// a provider Operum generates the secret for. It is not stored in readable form, so it
        /// cannot be shown again, the UI must tell the user to copy it now. Always null for a
        /// provider that supplies its own secret.
        /// </summary>
        public string? WebhookSecret { get; set; }

        /// <summary>
        /// Whether a signing secret has been set on this push target. False on a Firefly target
        /// between creating it and pasting in the secret from Firefly. Null for a pull target.
        /// </summary>
        public bool? HasWebhookSecret { get; set; }

        public List<FieldMappingDto> Mappings { get; set; } = [];
    }

    public class FieldMappingDto
    {
        public string SourceKey { get; set; } = string.Empty;
        public string FieldId { get; set; } = string.Empty;
        public bool SkipWhenNull { get; set; } = true;
    }

    /// <summary>What a manual or scheduled sync did, echoed back to the caller.</summary>
    public class SyncResultDto
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }
        public int Skipped { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = [];
    }
}
