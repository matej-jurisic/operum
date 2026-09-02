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
    /// <summary>
    /// intervals.icu daily wellness and activities, pulled on a schedule.
    /// <para>
    /// Chosen as the first connector because its API is self-serve -- an athlete makes a key
    /// in settings, with no partner programme to be approved by -- and because it already
    /// aggregates from Garmin, Strava, Wahoo, Zwift, Polar and Coros, so one connector fans in
    /// from most vendors.
    /// </para>
    /// <para>
    /// Both resources are a date-range GET that returns the whole window in one response, so
    /// they differ only in route, catalog, and -- for wellness alone -- a revision cursor.
    /// <see cref="ResourceSpec"/> captures that difference; everything else is shared.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Takes a client factory rather than an HttpClient: the provider is a singleton, and a
    /// singleton holding one typed client would pin a single handler for the life of the
    /// process instead of letting the factory rotate them.
    /// </remarks>
    public class IntervalsProvider(IHttpClientFactory httpClientFactory, ILogger<IntervalsProvider> logger) : IPullIntegrationProvider
    {
        public const string ProviderKey = "intervals.icu";

        // Documented as Basic auth with the username the literal "API_KEY" and the athlete's
        // key as the password.
        private const string ApiKeyUserName = "API_KEY";

        // "0" resolves to whichever athlete the key belongs to, so a user never has to find
        // their own id to connect.
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

        /// <summary>
        /// Everything that differs between the resources this provider serves: how the window
        /// becomes a route, which payload key is the stable id, which -- if any -- carries the
        /// revision timestamp, and the fields to read out. Null for a resource this provider
        /// does not serve.
        /// </summary>
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
                // Activities carry no "last modified" field, so there is no cursor: every
                // record reads as fresh and the reconciliation window bounds the re-read.
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

            // Let the sync service record the failure against the target: it knows which one
            // this is, and one athlete's revoked key must not stop the tick for everyone else.
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);

            // The whole window arrives in one response -- this endpoint takes a date range
            // rather than paging -- so there is nothing to loop over. FetchAsync is a stream
            // regardless, because a paginated provider needs it to be.
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

            // Every catalog key is emitted, present with a null where the athlete logged
            // nothing. That presence is what a mapping's SkipWhenNull acts on -- omitting the
            // key instead would mean "nothing to say", which is a different instruction.
            foreach (var field in spec.Fields)
            {
                normalised.TryGetValue(Normalise(field.Key), out var element);
                values[field.Key] = Coerce(element, field);
            }

            return new SourceRecord(externalId!, SourceOperation.Upsert, updatedAt, values);
        }

        /// <summary>
        /// Reads one payload value as the string the write path consumes, per the type the
        /// catalog declares. Anything absent, null, or of an unexpected shape reads as null:
        /// an unlogged metric is missing data, and coercing it to 0 or "" would drag averages
        /// and charts around.
        /// </summary>
        private string? Coerce(JsonElement element, SourceField field)
        {
            if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return null;

            try
            {
                return field.Type switch
                {
                    // The one unit conversion: the payload counts seconds, the field holds a
                    // duration.
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

        /// <summary>
        /// Keys are compared with case and underscores ignored, so a payload spelling a field
        /// sleep_secs resolves the same catalog entry as one spelling it sleepSecs.
        /// </summary>
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

        // Deserialized loosely rather than into a DTO of ~45 nullable properties: the catalog
        // is already the schema, a wrong property name is then a one-line fix there, and
        // "absent" and "null" stay distinguishable without every value type being nullable
        // by hand.
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
