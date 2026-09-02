namespace Operum.Model.Integrations
{
    /// <summary>
    /// What a provider needs to reach one user's account, decrypted and ready to use. Kept
    /// apart from the stored entity so provider code never touches persistence -- and so a
    /// credential lives in memory for exactly as long as the call that uses it.
    /// </summary>
    /// <param name="BaseUrl">
    /// The user's own instance, for self-hosted providers. Null for a cloud provider, whose
    /// host the provider knows itself.
    /// </param>
    /// <param name="Credential">
    /// API key, personal access token, whatever the provider authenticates with. Null for a
    /// push-only connection, which needs no outbound credential at all.
    /// </param>
    public sealed record ProviderConnection(
        string? BaseUrl,
        string? Credential,
        string? ExternalAccountId);

    /// <summary>Who a credential turned out to belong to, resolved at connect time.</summary>
    public sealed record ProviderAccount(string ExternalAccountId, string DisplayName);

    /// <summary>
    /// The span a pull covers. <paramref name="Cursor"/> is the newest revision already seen,
    /// so a provider that can filter server-side by it should; one that cannot returns the
    /// window and lets the sync service drop what it has.
    /// </summary>
    public readonly record struct SyncWindow(DateOnly From, DateOnly To, DateTime? Cursor);
}
