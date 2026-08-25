using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.Constants;
using Operum.Model.DTOs.Queries.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Model.DTOs.Views.Requests;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Util
{
    /// <summary>
    /// Wrappers over the endpoints tests use to build their fixtures. Whatever a test is
    /// actually asserting on is still called inline, so the request under test stays visible.
    /// </summary>
    public static class TestApi
    {
        /// <summary>The "data" payload of an ApiResponse, with the body in the error when it is missing.</summary>
        public static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        /// <summary>The joined "messages" of an ApiResponse, for asserting on why a request failed.</summary>
        public static async Task<string> Messages(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("messages", out var messages))
                return string.Empty;
            return string.Join(" | ", messages.EnumerateArray().Select(m => m.GetString()));
        }

        public static async Task<string> IdOf(HttpResponseMessage response) =>
            (await Data(response)).GetProperty("id").GetString()!;

        public static async Task<string> CreateTracker(HttpClient client, string name) =>
            await IdOf(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name }));

        public static Task<HttpResponseMessage> PostField(HttpClient client, string trackerId, CreateFieldDto field) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields", field);

        public static async Task<string> CreateField(HttpClient client, string trackerId, string name, string type, bool required = false) =>
            await IdOf(await PostField(client, trackerId, new CreateFieldDto { Name = name, Type = type, Required = required }));

        public static async Task<string> CreateCalculatedField(HttpClient client, string trackerId, string name, string formula, string type) =>
            await IdOf(await PostField(client, trackerId,
                new CreateFieldDto { Name = name, Type = type, IsCalculated = true, Formula = formula }));

        public static Task<HttpResponseMessage> PostEntry(HttpClient client, string trackerId, Dictionary<string, string?> fieldValues) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/entries", new CreateEntryDto { FieldValues = fieldValues });

        public static async Task<string> CreateEntry(HttpClient client, string trackerId, Dictionary<string, string?> fieldValues) =>
            await IdOf(await PostEntry(client, trackerId, fieldValues));

        public static Task<HttpResponseMessage> PutEntry(HttpClient client, string trackerId, string entryId, Dictionary<string, string?> fieldValues) =>
            client.PutAsJsonAsync($"trackers/{trackerId}/entries/{entryId}", new UpdateEntryDto { FieldValues = fieldValues });

        public static async Task<JsonElement> GetEntry(HttpClient client, string trackerId, string entryId) =>
            await Data(await client.GetAsync($"trackers/{trackerId}/entries/{entryId}"));

        /// <summary>The raw value of one field of an entry, or null when the entry carries no value for it.</summary>
        public static JsonElement? ValueOf(JsonElement entry, string fieldName)
        {
            foreach (var fieldValue in entry.GetProperty("fieldValues").EnumerateArray())
            {
                if (fieldValue.GetProperty("fieldName").GetString() != fieldName)
                    continue;
                var value = fieldValue.GetProperty("value");
                return value.ValueKind == JsonValueKind.Null ? null : value;
            }
            return null;
        }

        public static async Task<string?> StringValueOf(HttpClient client, string trackerId, string entryId, string fieldName) =>
            ValueOf(await GetEntry(client, trackerId, entryId), fieldName)?.GetString();

        public static async Task<double?> NumberValueOf(HttpClient client, string trackerId, string entryId, string fieldName) =>
            ValueOf(await GetEntry(client, trackerId, entryId), fieldName)?.GetDouble();

        public static string EntriesUrl(string trackerId, string? viewId = null) =>
            $"trackers/{trackerId}/entries" + (viewId == null ? "" : $"?viewId={viewId}");

        /// <summary>The entries a tracker returns, in the order the given view sorts them.</summary>
        public static async Task<List<JsonElement>> ListEntries(HttpClient client, string trackerId, string? viewId = null)
        {
            var data = await Data(await client.GetAsync(EntriesUrl(trackerId, viewId)));
            return [.. data.GetProperty("items").EnumerateArray()];
        }

        /// <summary>One named field's value for every entry, which is what an ordering assertion reads.</summary>
        public static async Task<List<string?>> ListValues(HttpClient client, string trackerId, string fieldName, string? viewId = null)
        {
            var entries = await ListEntries(client, trackerId, viewId);
            return [.. entries.Select(e => ValueOf(e, fieldName)?.ToString())];
        }

        public static Task<HttpResponseMessage> PostView(HttpClient client, string trackerId, CreateViewDto view) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/views", view);

        public static async Task<string> CreateView(HttpClient client, string trackerId, CreateViewDto view) =>
            await IdOf(await PostView(client, trackerId, view));

        public static Task<HttpResponseMessage> PostQuery(HttpClient client, string trackerId, CreateQueryDto query) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/queries", query);

        public static async Task<string> CreateQuery(HttpClient client, string trackerId, CreateQueryDto query) =>
            await IdOf(await PostQuery(client, trackerId, query));

        /// <summary>A standalone filter query.</summary>
        public static Task<string> CreateFilterQuery(HttpClient client, string trackerId, string fieldId, string op, string? value) =>
            CreateQuery(client, trackerId, FilterClause(fieldId, op, value));

        /// <summary>A standalone sort query.</summary>
        public static Task<string> CreateSortQuery(HttpClient client, string trackerId, string fieldId, bool descending) =>
            CreateQuery(client, trackerId, SortClause(fieldId, descending));

        public static CreateQueryDto FilterClause(string fieldId, string op, string? value) => new()
        {
            Kind = QueryKinds.Filter,
            FieldId = fieldId,
            Operator = op,
            Value = value
        };

        public static CreateQueryDto SortClause(string fieldId, bool descending) => new()
        {
            Kind = QueryKinds.Sort,
            FieldId = fieldId,
            Descending = descending
        };

        /// <summary>A view built from one ad-hoc filter, the shape most filtering tests need.</summary>
        public static Task<string> CreateFilterView(HttpClient client, string trackerId, string name, string fieldId, string op, string? value) =>
            CreateView(client, trackerId, new CreateViewDto
            {
                Name = name,
                Queries = [new ViewQueryRefDto { NewQuery = FilterClause(fieldId, op, value) }]
            });

        /// <summary>A view built from one ad-hoc sort.</summary>
        public static Task<string> CreateSortView(HttpClient client, string trackerId, string name, string fieldId, bool descending) =>
            CreateView(client, trackerId, new CreateViewDto
            {
                Name = name,
                Queries = [new ViewQueryRefDto { NewQuery = SortClause(fieldId, descending) }]
            });

        /// <summary>A view composed of existing queries, in the given order (order decides sort precedence).</summary>
        public static Task<string> CreateViewFromQueries(HttpClient client, string trackerId, string name, params string[] queryIds) =>
            CreateView(client, trackerId, new CreateViewDto
            {
                Name = name,
                Queries = [.. queryIds.Select(id => new ViewQueryRefDto { QueryId = id })]
            });
    }
}
