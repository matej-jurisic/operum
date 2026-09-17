namespace Operum.Model.Integrations
{
    /// <param name="BaseUrl">Self-hosted providers only; null for a cloud provider.</param>
    /// <param name="Credential">Null for a push-only connection.</param>
    public sealed record ProviderConnection(
        string? BaseUrl,
        string? Credential,
        string? ExternalAccountId);

    public sealed record ProviderAccount(string ExternalAccountId, string DisplayName);

    /// <param name="Cursor">Newest revision already seen; providers that can't filter by it return the window instead.</param>
    public readonly record struct SyncWindow(DateOnly From, DateOnly To, DateTime? Cursor);
}
