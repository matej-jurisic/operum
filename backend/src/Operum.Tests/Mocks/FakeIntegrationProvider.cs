using Operum.Model.Common;
using Operum.Model.Constants.Fields;
using Operum.Model.Enums;
using Operum.Model.Integrations;
using Operum.Service.Integrations;
using System.Runtime.CompilerServices;

namespace Operum.Tests.Mocks
{
    /// <summary>
    /// A provider that does both ingest paths without touching a network, so the shared
    /// pipeline can be tested before any real provider exists -- and so a later regression in
    /// it is caught here rather than against someone's live account.
    /// </summary>
    public class FakeIntegrationProvider : IPullIntegrationProvider, IPushIntegrationProvider
    {
        public const string ResourceType = "things";
        public const string ValidSecret = "correct-secret";

        public FakeIntegrationProvider(string key = "fake") => Key = key;

        public string Key { get; }
        public string DisplayName => "Fake Provider";
        public IntegrationCapabilities Capabilities => IntegrationCapabilities.Pull | IntegrationCapabilities.Push;
        public bool RequiresBaseUrl => false;
        public IReadOnlyList<string> ResourceTypes => [ResourceType];

        /// <summary>What FetchAsync will yield; set by the test.</summary>
        public List<SourceRecord> Records { get; } = [];

        /// <summary>Windows FetchAsync was called with, so a test can assert on cursoring.</summary>
        public List<SyncWindow> RequestedWindows { get; } = [];

        /// <summary>Set to make FetchAsync fail, standing in for a revoked key or a dead host.</summary>
        public Exception? FetchThrows { get; set; }

        public IReadOnlyList<SourceField> Catalog(string resourceType) =>
            resourceType == ResourceType
                ? [
                    new SourceField("amount", DataTypes.Number, "Amount"),
                    new SourceField("note", DataTypes.String, "Note"),
                    new SourceField("duration", DataTypes.TimeSpan, "Duration"),
                    new SourceField("occurred", DataTypes.DateTime, "Occurred at"),
                    new SourceField("flagged", DataTypes.Bool, "Flagged"),
                  ]
                : [];

        public Task<Result<ProviderAccount>> ValidateCredentialAsync(
            ProviderConnection connection, CancellationToken ct = default) =>
            Task.FromResult(connection.Credential == "good-key"
                ? Result.Success(new ProviderAccount("athlete-1", "Test Account"))
                : (Result<ProviderAccount>)Result.Failure(ResultStatusCodes.BadRequest, "Invalid credential."));

        public async IAsyncEnumerable<SourceRecord> FetchAsync(
            ProviderConnection connection,
            string resourceType,
            SyncWindow window,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            RequestedWindows.Add(window);

            if (FetchThrows != null)
                throw FetchThrows;

            foreach (var record in Records)
            {
                ct.ThrowIfCancellationRequested();
                yield return record;
                await Task.Yield();
            }
        }

        public Result<IReadOnlyList<SourceRecord>> VerifyAndParse(
            string resourceType,
            string secret,
            string rawBody,
            IReadOnlyDictionary<string, string> headers)
        {
            // Stands in for a real signature check: the point under test is that a failure
            // yields Forbidden and no records, not the particular hash a provider uses.
            if (!headers.TryGetValue("X-Fake-Signature", out var signature) || signature != secret)
                return Result.Failure(ResultStatusCodes.Forbidden, "Signature verification failed.");

            return Result.Success<IReadOnlyList<SourceRecord>>(Records);
        }
    }
}
