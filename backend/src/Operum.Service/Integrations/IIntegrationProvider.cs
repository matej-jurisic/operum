using Operum.Model.Common;
using Operum.Model.Integrations;

namespace Operum.Service.Integrations
{
    /// <summary>
    /// What every provider has regardless of how its data arrives. Adding a provider is this
    /// interface plus whichever ingest interface fits it, plus one DI registration -- nothing
    /// outside a provider class names a provider, so the sync loop, the webhook endpoint and
    /// the CRUD service never grow a branch per integration.
    /// </summary>
    public interface IIntegrationProvider
    {
        /// <summary>
        /// Stable id, e.g. "intervals.icu". Stamped onto every entry the provider writes as
        /// <c>Entry.Source</c> and stored in saved connections, so it must never change.
        /// </summary>
        string Key { get; }

        string DisplayName { get; }

        IntegrationCapabilities Capabilities { get; }

        /// <summary>
        /// Whether a connection must carry a BaseUrl -- true for anything self-hosted, where
        /// the instance is the user's own and we cannot know its address.
        /// </summary>
        bool RequiresBaseUrl { get; }

        /// <summary>
        /// The kinds of data this provider offers, e.g. "wellness", "transactions". Each is
        /// mapped separately, against its own catalog, onto its own tracker.
        /// </summary>
        IReadOnlyList<string> ResourceTypes { get; }

        /// <summary>
        /// The values this resource can supply, for the mapping UI to offer and the validator
        /// to check against. Empty for a resource type this provider does not serve.
        /// </summary>
        IReadOnlyList<SourceField> Catalog(string resourceType);
    }

    /// <summary>A provider Operum fetches from on a schedule.</summary>
    public interface IPullIntegrationProvider : IIntegrationProvider
    {
        /// <summary>
        /// Proves a credential works and resolves whose account it is, so a connection is
        /// never stored unverified. A wrong or revoked credential is a failed Result with a
        /// message worth showing, not an exception.
        /// </summary>
        Task<Result<ProviderAccount>> ValidateCredentialAsync(
            ProviderConnection connection,
            CancellationToken ct = default);

        /// <summary>
        /// Streams the window's records. Deliberately an async stream: a paginated source
        /// yields page by page instead of materialising a backfill in memory, and the caller
        /// can batch as it goes.
        /// </summary>
        IAsyncEnumerable<SourceRecord> FetchAsync(
            ProviderConnection connection,
            string resourceType,
            SyncWindow window,
            CancellationToken ct = default);
    }

    /// <summary>
    /// A provider that posts to Operum when something changes. This is what makes a
    /// self-hosted instance workable without the user exposing it: the call is outbound from
    /// their side, so nothing of theirs has to be reachable from here.
    /// </summary>
    public interface IPushIntegrationProvider : IIntegrationProvider
    {
        /// <summary>
        /// True when the provider mints the signing secret itself and the user copies it into
        /// Operum. Firefly III works this way: its webhook screen shows a secret and offers no
        /// field to paste one in, so Operum cannot choose it. False means Operum generates the
        /// secret and the user pastes it into the provider.
        /// </summary>
        bool ProviderSuppliesSecret { get; }

        /// <summary>
        /// Checks the delivery is genuine and turns it into records. One payload may produce
        /// several -- a transaction group fans out into its splits.
        /// <para>
        /// Headers are a plain dictionary rather than an ASP.NET type so a provider stays
        /// testable without a request, and <paramref name="rawBody"/> is the exact bytes as
        /// text because signatures are computed over those: re-serializing a parsed object
        /// will not hash the same.
        /// </para>
        /// Returns Forbidden on a signature that does not check out, and applies nothing --
        /// an unverified payload must never be partially written.
        /// </summary>
        Result<IReadOnlyList<SourceRecord>> VerifyAndParse(
            string resourceType,
            string secret,
            string rawBody,
            IReadOnlyDictionary<string, string> headers);
    }
}
