using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Entries.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Trackers.Requests;
using Operum.Tests.Extensions;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Fields
{
    public class CalculatedFieldsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private static async Task<JsonElement> Data(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(body).RootElement;
            if (!json.TryGetProperty("data", out var data))
                throw new Exception($"Response had no 'data' property. Status: {response.StatusCode}. Body: {body}");
            return data;
        }

        private async Task<HttpClient> AuthenticatedClient()
        {
            var client = _factory.CreateClientWithCookies();
            await _factory.SeedDatabaseAsync();
            await client.Authenticate(DefaultUsers.TestUserData);
            return client;
        }

        private static async Task<string> CreateTracker(HttpClient client, string name) =>
            (await Data(await client.PostAsJsonAsync("trackers", new CreateTrackerDto { Name = name })))
                .GetProperty("id").GetString()!;

        private static Task<HttpResponseMessage> CreateField(HttpClient client, string trackerId, string name, string type) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields", new CreateFieldDto { Name = name, Type = type });

        private static Task<HttpResponseMessage> CreateCalculatedField(HttpClient client, string trackerId, string name, string formula, string type = DataTypes.Number) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields",
                new CreateFieldDto { Name = name, Type = type, IsCalculated = true, Formula = formula });

        private static Task<HttpResponseMessage> UpdateCalculatedField(HttpClient client, string trackerId, string fieldId, string name, string formula, string type = DataTypes.Number) =>
            client.PutAsJsonAsync($"trackers/{trackerId}/fields/{fieldId}",
                new UpdateFieldDto { Name = name, Type = type, IsCalculated = true, Formula = formula });

        private static async Task<double?> GetNumberValue(HttpClient client, string trackerId, string entryId, string fieldName)
        {
            var entry = await Data(await client.GetAsync($"trackers/{trackerId}/entries/{entryId}"));
            foreach (var fieldValue in entry.GetProperty("fieldValues").EnumerateArray())
            {
                if (fieldValue.GetProperty("fieldName").GetString() == fieldName)
                {
                    var value = fieldValue.GetProperty("value");
                    return value.ValueKind == JsonValueKind.Null ? null : value.GetDouble();
                }
            }
            return null;
        }

        [Fact]
        public async Task CreateEntry_CalculatedFieldReferencingCalculatedField_ResolvesTheChain()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Chained");

            await CreateField(client, trackerId, "Base", DataTypes.Number);
            Assert.Equal(HttpStatusCode.OK, (await CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await CreateCalculatedField(client, trackerId, "Final", "{Doubled} + 1")).StatusCode);

            var entry = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Base"] = "10" } }));
            var entryId = entry.GetProperty("id").GetString()!;

            Assert.Equal(20, await GetNumberValue(client, trackerId, entryId, "Doubled"));
            Assert.Equal(21, await GetNumberValue(client, trackerId, entryId, "Final"));
        }

        [Fact]
        public async Task CreateEntry_ChainedFieldDefinedBeforeItsDependency_StillResolves()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Out of order");

            await CreateField(client, trackerId, "Base", DataTypes.Number);
            // "First" is created before "Second", then rewritten to depend on it, so the
            // evaluation order can only come from the dependency graph.
            var first = await Data(await CreateCalculatedField(client, trackerId, "First", "{Base} * 2"));
            await CreateCalculatedField(client, trackerId, "Second", "{Base} + 5");
            Assert.Equal(HttpStatusCode.OK,
                (await UpdateCalculatedField(client, trackerId, first.GetProperty("id").GetString()!, "First", "{Second} * 10")).StatusCode);

            var entry = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Base"] = "3" } }));
            var entryId = entry.GetProperty("id").GetString()!;

            Assert.Equal(8, await GetNumberValue(client, trackerId, entryId, "Second"));
            Assert.Equal(80, await GetNumberValue(client, trackerId, entryId, "First"));
        }

        [Fact]
        public async Task UpdateEntry_ChainedFieldsRecalculate()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Chained update");

            await CreateField(client, trackerId, "Base", DataTypes.Number);
            await CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2");
            await CreateCalculatedField(client, trackerId, "Final", "{Doubled} + 1");

            var entry = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Base"] = "10" } }));
            var entryId = entry.GetProperty("id").GetString()!;

            var updateResponse = await client.PutAsJsonAsync($"trackers/{trackerId}/entries/{entryId}",
                new UpdateEntryDto { FieldValues = new() { ["Base"] = "4" } });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            Assert.Equal(8, await GetNumberValue(client, trackerId, entryId, "Doubled"));
            Assert.Equal(9, await GetNumberValue(client, trackerId, entryId, "Final"));
        }

        [Fact]
        public async Task UpdateEntry_ChainedFieldClearedWhenSourceValueIsRemoved()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Chained clear");

            await CreateField(client, trackerId, "Base", DataTypes.Number);
            await CreateField(client, trackerId, "Note", DataTypes.String);
            await CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2");
            await CreateCalculatedField(client, trackerId, "Final", "{Doubled} + 1");

            var entry = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Base"] = "10", ["Note"] = "hi" } }));
            var entryId = entry.GetProperty("id").GetString()!;
            Assert.Equal(21, await GetNumberValue(client, trackerId, entryId, "Final"));

            // Dropping Base leaves Doubled unresolvable, which must clear Final too.
            var updateResponse = await client.PutAsJsonAsync($"trackers/{trackerId}/entries/{entryId}",
                new UpdateEntryDto { FieldValues = new() { ["Note"] = "hi" } });
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            Assert.Null(await GetNumberValue(client, trackerId, entryId, "Doubled"));
            Assert.Null(await GetNumberValue(client, trackerId, entryId, "Final"));
        }

        [Fact]
        public async Task CreateField_SelfReferencingFormula_ReturnsBadRequest()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Self reference");

            await CreateField(client, trackerId, "Base", DataTypes.Number);

            var response = await CreateCalculatedField(client, trackerId, "Loop", "{Loop} + 1");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateField_FormulaThatClosesACycle_ReturnsBadRequest()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Cycle");

            await CreateField(client, trackerId, "Base", DataTypes.Number);
            var first = await Data(await CreateCalculatedField(client, trackerId, "First", "{Base} * 2"));
            await CreateCalculatedField(client, trackerId, "Second", "{First} + 1");

            // First → Second → First
            var response = await UpdateCalculatedField(client, trackerId, first.GetProperty("id").GetString()!, "First", "{Second} * 2");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_UnknownToken_ReturnsBadRequest()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Unknown token");

            await CreateField(client, trackerId, "Base", DataTypes.Number);

            var response = await CreateCalculatedField(client, trackerId, "Broken", "{Nope} + 1");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEntry_ChainedTimespanField_UsesPropertyAccess()
        {
            var client = await AuthenticatedClient();
            var trackerId = await CreateTracker(client, "Chained timespan");

            await CreateField(client, trackerId, "Duration", DataTypes.TimeSpan);
            // Half the duration, then read it back in hours.
            await CreateCalculatedField(client, trackerId, "Half", "{Duration.seconds} / 2", DataTypes.TimeSpan);
            await CreateCalculatedField(client, trackerId, "HalfHours", "{Half.hours}");

            var entry = await Data(await client.PostAsJsonAsync($"trackers/{trackerId}/entries",
                new CreateEntryDto { FieldValues = new() { ["Duration"] = "04:00:00" } }));
            var entryId = entry.GetProperty("id").GetString()!;

            Assert.Equal(2, await GetNumberValue(client, trackerId, entryId, "HalfHours"));
        }
    }
}
