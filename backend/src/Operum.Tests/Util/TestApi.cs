using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
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

        public static string EntriesUrl(string trackerId, params string[] viewIds) =>
            $"trackers/{trackerId}/entries" + (viewIds.Length == 0 ? "" : "?" + string.Join("&", viewIds.Select(v => $"viewId={v}")));

        /// <summary>The entries a tracker returns, in the order the given views sort them.</summary>
        public static async Task<List<JsonElement>> ListEntries(HttpClient client, string trackerId, params string[] viewIds)
        {
            var data = await Data(await client.GetAsync(EntriesUrl(trackerId, viewIds)));
            return [.. data.GetProperty("items").EnumerateArray()];
        }

        /// <summary>One named field's value for every entry, which is what an ordering assertion reads.</summary>
        public static async Task<List<string?>> ListValues(HttpClient client, string trackerId, string fieldName, params string[] viewIds)
        {
            var entries = await ListEntries(client, trackerId, viewIds);
            return [.. entries.Select(e => ValueOf(e, fieldName)?.ToString())];
        }

        public static Task<HttpResponseMessage> PostView(HttpClient client, string trackerId, CreateViewDto view) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/views", view);

        public static async Task<string> CreateView(HttpClient client, string trackerId, CreateViewDto view) =>
            await IdOf(await PostView(client, trackerId, view));

        /// <summary>A single-filter view, the shape most filtering tests need.</summary>
        public static Task<string> CreateFilterView(HttpClient client, string trackerId, string name, string fieldId, string op, string? value) =>
            CreateView(client, trackerId, new CreateViewDto
            {
                Name = name,
                Filters = [new CreateViewFilterDto { FieldId = fieldId, Operator = op, Value = value }]
            });

        /// <summary>A single-sort view.</summary>
        public static Task<string> CreateSortView(HttpClient client, string trackerId, string name, string fieldId, bool descending) =>
            CreateView(client, trackerId, new CreateViewDto
            {
                Name = name,
                Sorts = [new CreateViewSortDto { FieldId = fieldId, Descending = descending }]
            });
    }
}
