using Operum.Model.Common;
using Operum.Model.Integrations;

namespace Operum.Service.Integrations
{
    public interface IIntegrationProvider
    {
        // Stamped onto every entry as Entry.Source and stored in saved connections; must never change.
        string Key { get; }

        string DisplayName { get; }

        IntegrationCapabilities Capabilities { get; }

        // True for anything self-hosted, where we cannot know the instance's address.
        bool RequiresBaseUrl { get; }

        IReadOnlyList<string> ResourceTypes { get; }

        // Empty for a resource type this provider does not serve.
        IReadOnlyList<SourceField> Catalog(string resourceType);
    }

    // A provider Operum fetches from on a schedule.
    public interface IPullIntegrationProvider : IIntegrationProvider
    {
        Task<Result<ProviderAccount>> ValidateCredentialAsync(
            ProviderConnection connection,
            CancellationToken ct = default);

        // Async stream so a paginated source yields page by page instead of materialising a backfill in memory.
        IAsyncEnumerable<SourceRecord> FetchAsync(
            ProviderConnection connection,
            string resourceType,
            SyncWindow window,
            CancellationToken ct = default);
    }

    // A provider that posts to Operum when something changes, so a self-hosted instance
    // never needs to be reachable from here.
    public interface IPushIntegrationProvider : IIntegrationProvider
    {
        // True when the provider mints its own signing secret (e.g. Firefly III) rather than Operum generating one.
        bool ProviderSuppliesSecret { get; }

        // rawBody must be the exact delivered bytes: signatures are computed over those, and
        // re-serializing a parsed object won't hash the same. Returns Forbidden and applies
        // nothing on a bad signature.
        Result<IReadOnlyList<SourceRecord>> VerifyAndParse(
            string resourceType,
            string secret,
            string rawBody,
            IReadOnlyDictionary<string, string> headers);
    }
}
