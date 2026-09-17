using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Fields
{
    public class ReferenceFieldsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("reference");

        private static async Task<(string trackerId, string nameFieldId, string benchId, string squatId)> LibraryWithEntries(HttpClient client)
        {
            var trackerId = await TestApi.CreateTracker(client, "Library");
            var nameFieldId = await TestApi.CreateField(client, trackerId, "Name", DataTypes.String);
            var benchId = await TestApi.CreateEntry(client, trackerId, new() { ["Name"] = "Bench press" });
            var squatId = await TestApi.CreateEntry(client, trackerId, new() { ["Name"] = "Squat" });
            return (trackerId, nameFieldId, benchId, squatId);
        }

        private static Task<HttpResponseMessage> PostReferenceField(
            HttpClient client, string trackerId, string name, string referencedTrackerId, string? displayFieldId) =>
            TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = name,
                Type = DataTypes.Reference,
                ReferencedTrackerId = referencedTrackerId,
                ReferencedDisplayFieldId = displayFieldId,
            });

        private static string? ReferencedEntryId(JsonElement entry, string fieldName)
        {
            foreach (var fv in entry.GetProperty("fieldValues").EnumerateArray())
            {
                if (fv.GetProperty("fieldName").GetString() != fieldName) continue;
                if (!fv.TryGetProperty("referencedEntryId", out var id)) return null;
                return id.ValueKind == JsonValueKind.Null ? null : id.GetString();
            }
            return null;
        }

        [Fact]
        public async Task CreateReferenceField_WithoutTracker_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Workouts");

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Exercise",
                Type = DataTypes.Reference,
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateReferenceField_TrackerTheUserCannotSee_ReturnsBadRequest()
        {
            var stranger = await _factory.NewUserClient("reference-stranger");
            var strangerTrackerId = await TestApi.CreateTracker(stranger, "Private");

            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Workouts");

            var response = await PostReferenceField(client, trackerId, "Exercise", strangerTrackerId, null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ReferenceValue_CachesTheDisplayLabelAndKeepsTheLink()
        {
            var client = await OwnerClient();
            var (libraryId, nameFieldId, benchId, _) = await LibraryWithEntries(client);

            var workoutsId = await TestApi.CreateTracker(client, "Workouts");
            await PostReferenceField(client, workoutsId, "Exercise", libraryId, nameFieldId);
            var entryId = await TestApi.CreateEntry(client, workoutsId, new() { ["Exercise"] = benchId });

            var entry = await TestApi.GetEntry(client, workoutsId, entryId);
            Assert.Equal("Bench press", TestApi.ValueOf(entry, "Exercise")?.GetString());
            Assert.Equal(benchId, ReferencedEntryId(entry, "Exercise"));
        }

        [Fact]
        public async Task RenamingTheLinkedEntry_RefreshesTheCachedLabel()
        {
            var client = await OwnerClient();
            var (libraryId, nameFieldId, benchId, _) = await LibraryWithEntries(client);

            var workoutsId = await TestApi.CreateTracker(client, "Workouts");
            await PostReferenceField(client, workoutsId, "Exercise", libraryId, nameFieldId);
            var entryId = await TestApi.CreateEntry(client, workoutsId, new() { ["Exercise"] = benchId });

            await TestApi.PutEntry(client, libraryId, benchId, new() { ["Name"] = "Incline bench press" });

            var entry = await TestApi.GetEntry(client, workoutsId, entryId);
            Assert.Equal("Incline bench press", TestApi.ValueOf(entry, "Exercise")?.GetString());
        }

        [Fact]
        public async Task DeletingTheLinkedEntry_ClearsTheCell()
        {
            var client = await OwnerClient();
            var (libraryId, nameFieldId, benchId, _) = await LibraryWithEntries(client);

            var workoutsId = await TestApi.CreateTracker(client, "Workouts");
            await PostReferenceField(client, workoutsId, "Exercise", libraryId, nameFieldId);
            var entryId = await TestApi.CreateEntry(client, workoutsId, new() { ["Exercise"] = benchId });

            var delete = await client.DeleteAsync($"trackers/{libraryId}/entries/{benchId}");
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

            var entry = await TestApi.GetEntry(client, workoutsId, entryId);
            Assert.Null(TestApi.ValueOf(entry, "Exercise"));
            Assert.Null(ReferencedEntryId(entry, "Exercise"));
        }

        [Fact]
        public async Task FilteringAViewByReferenceLabel_MatchesTheLinkedEntries()
        {
            var client = await OwnerClient();
            var (libraryId, nameFieldId, benchId, squatId) = await LibraryWithEntries(client);

            var workoutsId = await TestApi.CreateTracker(client, "Workouts");
            var exerciseFieldId = await TestApi.IdOf(await PostReferenceField(client, workoutsId, "Exercise", libraryId, nameFieldId));
            await TestApi.CreateEntry(client, workoutsId, new() { ["Exercise"] = benchId });
            await TestApi.CreateEntry(client, workoutsId, new() { ["Exercise"] = squatId });

            var viewId = await TestApi.CreateFilterView(client, workoutsId, "Squats only", exerciseFieldId, OperatorTypes.EqualsOperator, "Squat");

            var values = await TestApi.ListValues(client, workoutsId, "Exercise", viewId);
            Assert.Equal(["Squat"], values);
        }

        [Fact]
        public async Task EntryOptions_ReturnsIdAndLabelForEachEntry()
        {
            var client = await OwnerClient();
            var (libraryId, nameFieldId, benchId, _) = await LibraryWithEntries(client);

            var data = await TestApi.Data(await client.GetAsync(
                $"trackers/{libraryId}/entries/options?displayFieldId={nameFieldId}&search=bench"));

            var options = data.EnumerateArray().ToList();
            Assert.Single(options);
            Assert.Equal(benchId, options[0].GetProperty("id").GetString());
            Assert.Equal("Bench press", options[0].GetProperty("label").GetString());
        }

        [Fact]
        public async Task ChangingAFieldAwayFromReference_ClearsItsValues()
        {
            var client = await OwnerClient();
            var (libraryId, nameFieldId, benchId, _) = await LibraryWithEntries(client);

            var workoutsId = await TestApi.CreateTracker(client, "Workouts");
            var exerciseFieldId = await TestApi.IdOf(await PostReferenceField(client, workoutsId, "Exercise", libraryId, nameFieldId));
            var entryId = await TestApi.CreateEntry(client, workoutsId, new() { ["Exercise"] = benchId });

            var update = await client.PutAsJsonAsync($"trackers/{workoutsId}/fields/{exerciseFieldId}", new UpdateFieldDto
            {
                Name = "Exercise",
                Type = DataTypes.String,
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var entry = await TestApi.GetEntry(client, workoutsId, entryId);
            Assert.Null(TestApi.ValueOf(entry, "Exercise"));
        }
    }
}
