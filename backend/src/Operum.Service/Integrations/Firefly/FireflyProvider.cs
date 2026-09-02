using Microsoft.Extensions.Logging;
using Operum.Model.Common;
using Operum.Model.Constants.Fields;
using Operum.Model.Constants.Integrations;
using Operum.Model.Enums;
using Operum.Model.Integrations;
using System.Globalization;
using System.Text.Json;

namespace Operum.Service.Integrations.Firefly
{
    /// <summary>
    /// Firefly III transactions, delivered by webhook.
    /// <para>
    /// Push rather than pull, and that is the point: a Firefly instance is self-hosted and
    /// usually behind NAT. A webhook is an outbound call from the user's own box, so the
    /// integration works without them exposing their finance server to the internet -- and
    /// there is no user-supplied address for this server to fetch, so no SSRF surface either.
    /// The cost is no history before the day it was connected.
    /// </para>
    /// </summary>
    public class FireflyProvider(ILogger<FireflyProvider> logger) : IPushIntegrationProvider
    {
        public const string ProviderKey = "firefly-iii";

        public string Key => ProviderKey;
        public string DisplayName => "Firefly III";
        public IntegrationCapabilities Capabilities => IntegrationCapabilities.Push;

        // Push-only, so nothing here ever calls the user's instance.
        public bool RequiresBaseUrl => false;

        public IReadOnlyList<string> ResourceTypes => [FireflyTransactionCatalog.ResourceType];

        public IReadOnlyList<SourceField> Catalog(string resourceType) =>
            resourceType == FireflyTransactionCatalog.ResourceType
                ? FireflyTransactionCatalog.Fields
                : [];

        public Result<IReadOnlyList<SourceRecord>> VerifyAndParse(
            string resourceType,
            string secret,
            string rawBody,
            IReadOnlyDictionary<string, string> headers)
        {
            if (resourceType != FireflyTransactionCatalog.ResourceType)
                return Result.Failure(ResultStatusCodes.BadRequest, "Unsupported resource type.");

            headers.TryGetValue(FireflySignature.HeaderName, out var signature);

            var outcome = FireflySignature.Verify(signature, rawBody, secret, DateTime.UtcNow);
            if (outcome != FireflySignature.Outcome.Valid)
            {
                // Deliberately one message for every failure: which part was wrong is not
                // something an unauthenticated caller should learn.
                logger.LogWarning("Rejected a Firefly III webhook delivery: {Outcome}", outcome);
                return Result.Failure(ResultStatusCodes.Forbidden, "Signature verification failed.");
            }

            JsonElement payload;
            try
            {
                payload = JsonDocument.Parse(rawBody).RootElement;
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Firefly III delivered a body that is not JSON");
                return Result.Failure(ResultStatusCodes.BadRequest, "The delivery was not valid JSON.");
            }

            var trigger = Text(payload, "trigger") ?? string.Empty;
            var isDelete = trigger.Contains("DESTROY", StringComparison.OrdinalIgnoreCase);

            if (!payload.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Object)
                return Result.Success<IReadOnlyList<SourceRecord>>([]);

            var groupId = Text(content, "id");
            var updatedAt = ParseDate(Text(content, "updated_at"));

            if (!content.TryGetProperty("transactions", out var splits) || splits.ValueKind != JsonValueKind.Array)
                return Result.Success<IReadOnlyList<SourceRecord>>([]);

            var records = new List<SourceRecord>();

            foreach (var split in splits.EnumerateArray())
            {
                var journalId = Text(split, "transaction_journal_id");
                if (string.IsNullOrWhiteSpace(journalId))
                {
                    // Without the split's own id there is no idempotency key, and keying on the
                    // group would merge the splits into one entry.
                    logger.LogWarning("Skipped a Firefly III split with no transaction_journal_id");
                    continue;
                }

                records.Add(isDelete
                    ? new SourceRecord(journalId, SourceOperation.Delete, updatedAt, new Dictionary<string, string?>(), groupId)
                    : new SourceRecord(journalId, SourceOperation.Upsert, updatedAt, ReadValues(split, journalId, groupId), groupId));
            }

            return Result.Success<IReadOnlyList<SourceRecord>>(records);
        }

        private Dictionary<string, string?> ReadValues(JsonElement split, string journalId, string? groupId)
        {
            var values = new Dictionary<string, string?>();
            var type = Text(split, FireflyTransactionCatalog.TypeKey);

            foreach (var field in FireflyTransactionCatalog.Fields)
            {
                values[field.Key] = field.Key switch
                {
                    // The two ids are read from where they actually live rather than from the
                    // split's own properties.
                    FireflyTransactionCatalog.JournalIdKey => journalId,
                    FireflyTransactionCatalog.GroupIdKey => groupId,

                    FireflyTransactionCatalog.AmountKey => SignedAmount(split, FireflyTransactionCatalog.AmountKey, type),
                    "foreign_amount" => SignedAmount(split, "foreign_amount", type),

                    "tags" => JoinTags(split),

                    _ => Read(split, field),
                };
            }

            return values;
        }

        /// <summary>
        /// Firefly reports every amount positive and says what kind it is separately. A column
        /// mixing withdrawals and deposits only sums to a meaningful number if the sign is
        /// real, so it is applied here rather than left for the user to model.
        /// </summary>
        private string? SignedAmount(JsonElement split, string key, string? type)
        {
            var raw = Text(split, key);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            // Amounts arrive as strings, so parse invariantly rather than by the server locale.
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
            {
                logger.LogWarning("Could not read a Firefly III {Key} value", key);
                return null;
            }

            return FireflyTransactionCatalog.ApplySign(amount, type)
                .ToString(CultureInfo.InvariantCulture);
        }

        private static string? JoinTags(JsonElement split)
        {
            if (!split.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
                return null;

            var joined = string.Join(", ", tags.EnumerateArray()
                .Select(t => t.ValueKind == JsonValueKind.String ? t.GetString() : null)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

            return string.IsNullOrEmpty(joined) ? null : joined;
        }

        private string? Read(JsonElement split, SourceField field)
        {
            if (!split.TryGetProperty(field.Key, out var element) ||
                element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return null;
            }

            try
            {
                return field.Type switch
                {
                    DataTypes.Bool when element.ValueKind is JsonValueKind.True or JsonValueKind.False =>
                        element.GetBoolean().ToString(),

                    DataTypes.Number when element.ValueKind == JsonValueKind.Number =>
                        element.GetDouble().ToString(CultureInfo.InvariantCulture),

                    // Firefly sends numbers as strings often enough that a number field has to
                    // accept one.
                    DataTypes.Number when element.ValueKind == JsonValueKind.String =>
                        double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
                            ? n.ToString(CultureInfo.InvariantCulture)
                            : null,

                    DataTypes.String or DataTypes.Date or DataTypes.DateTime => AsText(element),

                    _ => null,
                };
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read Firefly III value for {Key}", field.Key);
                return null;
            }
        }

        private static string? Text(JsonElement element, string property) =>
            element.TryGetProperty(property, out var value) ? AsText(value) : null;

        private static string? AsText(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonValueKind.True or JsonValueKind.False => element.GetBoolean().ToString(),
            _ => null,
        };

        private static DateTime? ParseDate(string? value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : null;
    }
}
