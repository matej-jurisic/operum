using Microsoft.Extensions.Logging;
using Operum.Model.Common;
using Operum.Model.Constants.Fields;
using Operum.Model.Constants.Integrations;
using Operum.Model.Enums;
using Operum.Model.Integrations;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Operum.Service.Integrations.Intervals
{
    // intervals.icu daily wellness and activities, pulled on a schedule. Both resources are a
    // date-range GET returning the whole window in one response; ResourceSpec captures the
    // differences (route, catalog, revision cursor). Uses IHttpClientFactory rather than a
    // singleton HttpClient so handlers rotate normally.
    public class IntervalsProvider(IHttpClientFactory httpClientFactory, ILogger<IntervalsProvider> logger) : IPullIntegrationProvider
    {
        public const string ProviderKey = "intervals.icu";

        // Basic auth with username "API_KEY" and the athlete's key as password.
        private const string ApiKeyUserName = "API_KEY";

        // Resolves to whichever athlete the key belongs to.
        private const string SelfAthleteId = "0";

        public string Key => ProviderKey;
        public string DisplayName => "intervals.icu";
        public IntegrationCapabilities Capabilities => IntegrationCapabilities.Pull;
        public bool RequiresBaseUrl => false;

        public IReadOnlyList<string> ResourceTypes =>
            [IntervalsWellnessCatalog.ResourceType, IntervalsActivitiesCatalog.ResourceType];

        public IReadOnlyList<SourceField> Catalog(string resourceType) => resourceType switch
        {
            IntervalsWellnessCatalog.ResourceType => IntervalsWellnessCatalog.Mappable,
            IntervalsActivitiesCatalog.ResourceType => IntervalsActivitiesCatalog.Mappable,
            _ => [],
        };

        private sealed record ResourceSpec(
            string RoutePath,
            string RecordKey,
            string? UpdatedKey,
            IReadOnlyList<SourceField> Fields);

        private static ResourceSpec? SpecFor(string resourceType) => resourceType switch
        {
            IntervalsWellnessCatalog.ResourceType => new(
                "wellness",
                IntervalsWellnessCatalog.RecordKey,
                IntervalsWellnessCatalog.UpdatedKey,
                IntervalsWellnessCatalog.Fields),

            IntervalsActivitiesCatalog.ResourceType => new(
                "activities",
                IntervalsActivitiesCatalog.RecordKey,
                // Activities carry no "last modified" field; every record reads as fresh.
                null,
                IntervalsActivitiesCatalog.Fields),

            _ => null,
        };

        public async Task<Result<ProviderAccount>> ValidateCredentialAsync(
            ProviderConnection connection, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(connection.Credential))
                return Result.Failure(ResultStatusCodes.BadRequest, "An API key is required.");

            using var http = httpClientFactory.CreateClient(ProviderKey);
            using var request = BuildRequest(HttpMethod.Get, $"api/v1/athlete/{SelfAthleteId}", connection.Credential);

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Could not reach intervals.icu to validate a credential");
                return Result.Failure(ResultStatusCodes.BadRequest, "Could not reach intervals.icu. Try again shortly.");
            }

            using (response)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    return Result.Failure(ResultStatusCodes.BadRequest, "intervals.icu rejected that API key.");

                if (!response.IsSuccessStatusCode)
                    return Result.Failure(ResultStatusCodes.BadRequest, $"intervals.icu returned {(int)response.StatusCode}.");

                var body = await response.Content.ReadAsStringAsync(ct);
                var athlete = ReadObject(body);

                if (athlete == null || !athlete.TryGetValue("id", out var id))
                    return Result.Failure(ResultStatusCodes.BadRequest, "intervals.icu did not identify the athlete for that key.");

                var athleteId = AsString(id) ?? SelfAthleteId;
                var name = athlete.TryGetValue("name", out var n) ? AsString(n) : null;

                return Result.Success(new ProviderAccount(athleteId, name ?? athleteId));
            }
        }

        public async IAsyncEnumerable<SourceRecord> FetchAsync(
            ProviderConnection connection,
            string resourceType,
            SyncWindow window,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var spec = SpecFor(resourceType);
            if (spec == null)
                yield break;

            var athleteId = string.IsNullOrWhiteSpace(connection.ExternalAccountId)
                ? SelfAthleteId
                : connection.ExternalAccountId;

            var route = $"api/v1/athlete/{athleteId}/{spec.RoutePath}" +
                $"?oldest={window.From:yyyy-MM-dd}&newest={window.To:yyyy-MM-dd}";

            using var http = httpClientFactory.CreateClient(ProviderKey);
            using var request = BuildRequest(HttpMethod.Get, route, connection.Credential);
            using var response = await http.SendAsync(request, ct);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            foreach (var record in ReadArray(body))
            {
                ct.ThrowIfCancellationRequested();

                var built = Build(record, spec);
                if (built != null)
                    yield return built;
            }
        }

        private SourceRecord? Build(IReadOnlyDictionary<string, JsonElement> record, ResourceSpec spec)
        {
            var normalised = Normalise(record);

            if (!normalised.TryGetValue(Normalise(spec.RecordKey), out var idElement))
                return null;

            var externalId = AsString(idElement);
            if (string.IsNullOrWhiteSpace(externalId))
                return null;

            DateTime? updatedAt = null;
            if (spec.UpdatedKey != null &&
                normalised.TryGetValue(Normalise(spec.UpdatedKey), out var updated) &&
                DateTime.TryParse(AsString(updated), CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                updatedAt = parsed;
            }

            var values = new Dictionary<string, string?>();

            // Every catalog key is emitted, null where the athlete logged nothing; SkipWhenNull acts on that presence.
            foreach (var field in spec.Fields)
            {
                normalised.TryGetValue(Normalise(field.Key), out var element);
                values[field.Key] = Coerce(element, field);
            }

            return new SourceRecord(externalId!, SourceOperation.Upsert, updatedAt, values);
        }

        // Anything absent, null, or of unexpected shape reads as null rather than 0/"": an
        // unlogged metric is missing data, not a zero value.
        private string? Coerce(JsonElement element, SourceField field)
        {
            if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return null;

            try
            {
                return field.Type switch
                {
                    // The payload counts seconds; the field holds a duration.
                    DataTypes.TimeSpan when element.ValueKind == JsonValueKind.Number =>
                        TimeSpan.FromSeconds(element.GetDouble()).ToString(),

                    DataTypes.Number when element.ValueKind == JsonValueKind.Number =>
                        element.GetDouble().ToString(CultureInfo.InvariantCulture),

                    DataTypes.Bool when element.ValueKind is JsonValueKind.True or JsonValueKind.False =>
                        element.GetBoolean().ToString(),

                    DataTypes.Date or DataTypes.DateTime or DataTypes.String => AsString(element),

                    _ => null,
                };
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read intervals.icu value for {Key}", field.Key);
                return null;
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string route, string? credential)
        {
            var request = new HttpRequestMessage(method, route);
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ApiKeyUserName}:{credential}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
            return request;
        }

        // Case and underscores ignored, so "sleep_secs" and "sleepSecs" resolve to the same catalog entry.
        private static string Normalise(string key) =>
            key.Replace("_", string.Empty).ToLowerInvariant();

        private static Dictionary<string, JsonElement> Normalise(IReadOnlyDictionary<string, JsonElement> record)
        {
            var result = new Dictionary<string, JsonElement>();
            foreach (var (key, value) in record)
                result[Normalise(key)] = value;
            return result;
        }

        private static string? AsString(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonValueKind.True or JsonValueKind.False => element.GetBoolean().ToString(),
            _ => null,
        };

        // Deserialized loosely rather than into a DTO: the catalog is already the schema, and
        // "absent" vs "null" stay distinguishable.
        private Dictionary<string, JsonElement>? ReadObject(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "intervals.icu returned a body that is not a JSON object");
                return null;
            }
        }

        private List<Dictionary<string, JsonElement>> ReadArray(string body)
        {
            try
            {
                return JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(body) ?? [];
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "intervals.icu returned a body that is not a JSON array");
                return [];
            }
        }
    }
}
