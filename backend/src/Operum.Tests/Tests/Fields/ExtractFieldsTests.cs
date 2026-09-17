using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Fields
{
    public class ExtractFieldsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("extract");

        private static Task<HttpResponseMessage> PostExtract(HttpClient client, string trackerId, ExtractFieldsDto dto) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields/extract", dto);

        private static async Task<List<string>> FieldNames(HttpClient client, string trackerId)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            return [.. data.EnumerateArray().Select(f => f.GetProperty("name").GetString()!)];
        }

        private static async Task<JsonElement> Field(HttpClient client, string trackerId, string name)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            return data.EnumerateArray().First(f => f.GetProperty("name").GetString() == name);
        }

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

        // A vendor column that repeats across rows: the shape the extract operation normalises.
        private static async Task<(string trackerId, string vendorFieldId, List<string> entryIds)> Expenses(HttpClient client)
        {
            var trackerId = await TestApi.CreateTracker(client, "Expenses");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var vendorFieldId = await TestApi.CreateField(client, trackerId, "Vendor", DataTypes.String);
            var entryIds = new List<string>
            {
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "10", ["Vendor"] = "Acme" }),
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "20", ["Vendor"] = "Acme" }),
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "30", ["Vendor"] = "Globex" }),
            };
            return (trackerId, vendorFieldId, entryIds);
        }

        [Fact]
        public async Task ExtractFields_ReplacesTheFieldWithAReferenceAndLinksEveryRow()
        {
            var client = await OwnerClient();
            var (trackerId, vendorFieldId, entryIds) = await Expenses(client);

            var response = await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [vendorFieldId],
                NewTrackerName = "Vendors",
                ReferenceFieldName = "Vendor",
                DisplayFieldId = vendorFieldId,
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await TestApi.Data(response);
            var newTrackerId = result.GetProperty("newTrackerId").GetString()!;
            Assert.Equal(2, result.GetProperty("extractedEntryCount").GetInt32());

            Assert.Equal(["Amount", "Vendor"], await FieldNames(client, trackerId));
            Assert.Equal(DataTypes.Reference, (await Field(client, trackerId, "Vendor")).GetProperty("type").GetString());

            Assert.Equal(["Vendor"], await FieldNames(client, newTrackerId));
            var vendorEntries = await TestApi.ListEntries(client, newTrackerId);
            Assert.Equal(
                ["Acme", "Globex"],
                vendorEntries.Select(e => TestApi.ValueOf(e, "Vendor")?.GetString()).OrderBy(x => x));

            var expenses = await TestApi.ListEntries(client, trackerId);
            var links = expenses.ToDictionary(
                e => e.GetProperty("id").GetString()!,
                e => ReferencedEntryId(e, "Vendor"));
            Assert.Equal(links[entryIds[0]], links[entryIds[1]]);
            Assert.NotEqual(links[entryIds[0]], links[entryIds[2]]);
            Assert.All(links.Values, id => Assert.NotNull(id));

            // The cached label resolves through the chosen display field.
            Assert.Equal("Acme", TestApi.ValueOf(expenses.First(e => e.GetProperty("id").GetString() == entryIds[0]), "Vendor")?.GetString());
        }

        [Fact]
        public async Task ExtractFields_DeduplicatesOnTheWholeCombination()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Expenses");
            var nameId = await TestApi.CreateField(client, trackerId, "Name", DataTypes.String);
            var cityId = await TestApi.CreateField(client, trackerId, "City", DataTypes.String);
            await TestApi.CreateEntry(client, trackerId, new() { ["Name"] = "Acme", ["City"] = "London" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Name"] = "Acme", ["City"] = "Berlin" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Name"] = "Acme", ["City"] = "london" });

            var response = await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [nameId, cityId],
                NewTrackerName = "Vendors",
                ReferenceFieldName = "Vendor",
                DisplayFieldId = nameId,
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // "London" and "london" collapse; "Berlin" is the only other row.
            Assert.Equal(2, (await TestApi.Data(response)).GetProperty("extractedEntryCount").GetInt32());
        }

        [Fact]
        public async Task ExtractFields_RowsWithNoValue_AreLeftUnlinked()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Expenses");
            var vendorId = await TestApi.CreateField(client, trackerId, "Vendor", DataTypes.String);
            var blankEntryId = await TestApi.CreateEntry(client, trackerId, new() { ["Vendor"] = "" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Vendor"] = "Acme" });

            var response = await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [vendorId],
                NewTrackerName = "Vendors",
                ReferenceFieldName = "Vendor",
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await TestApi.Data(response);
            Assert.Equal(1, result.GetProperty("extractedEntryCount").GetInt32());

            var newTrackerId = result.GetProperty("newTrackerId").GetString()!;
            Assert.Single(await TestApi.ListEntries(client, newTrackerId));

            var blank = await TestApi.GetEntry(client, trackerId, blankEntryId);
            Assert.Null(ReferencedEntryId(blank, "Vendor"));
        }

        [Fact]
        public async Task ExtractFields_CalculatedFieldDependsOnASelectedField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Expenses");
            var baseId = await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);

            var response = await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [baseId],
                NewTrackerName = "Bases",
                ReferenceFieldName = "Base",
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Doubled", await TestApi.Messages(response));
        }

        [Fact]
        public async Task ExtractFields_SelectingAReferenceField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var libraryId = await TestApi.CreateTracker(client, "Library");
            var nameId = await TestApi.CreateField(client, libraryId, "Name", DataTypes.String);

            var trackerId = await TestApi.CreateTracker(client, "Workouts");
            var exerciseId = await TestApi.IdOf(await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Exercise",
                Type = DataTypes.Reference,
                ReferencedTrackerId = libraryId,
                ReferencedDisplayFieldId = nameId,
            }));

            var response = await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [exerciseId],
                NewTrackerName = "Exercises",
                ReferenceFieldName = "Exercise",
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExtractFields_LabelFieldNotAmongTheSelection_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var (trackerId, vendorFieldId, _) = await Expenses(client);
            var amountFieldId = (await Field(client, trackerId, "Amount")).GetProperty("id").GetString()!;

            var response = await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [vendorFieldId],
                NewTrackerName = "Vendors",
                ReferenceFieldName = "Vendor",
                DisplayFieldId = amountFieldId,
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExtractFields_OnATrackerOwnedBySomeoneElse_ReturnsNotFound()
        {
            var owner = await OwnerClient();
            var (trackerId, vendorFieldId, _) = await Expenses(owner);

            var stranger = await _factory.NewUserClient("extract-stranger");
            var response = await PostExtract(stranger, trackerId, new ExtractFieldsDto
            {
                FieldIds = [vendorFieldId],
                NewTrackerName = "Vendors",
                ReferenceFieldName = "Vendor",
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(["Amount", "Vendor"], await FieldNames(owner, trackerId));
        }

        [Fact]
        public async Task ExtractFields_RenamingTheLinkedEntry_PropagatesToTheSourceTracker()
        {
            var client = await OwnerClient();
            var (trackerId, vendorFieldId, entryIds) = await Expenses(client);

            var newTrackerId = (await TestApi.Data(await PostExtract(client, trackerId, new ExtractFieldsDto
            {
                FieldIds = [vendorFieldId],
                NewTrackerName = "Vendors",
                ReferenceFieldName = "Vendor",
                DisplayFieldId = vendorFieldId,
            }))).GetProperty("newTrackerId").GetString()!;

            var acme = (await TestApi.ListEntries(client, newTrackerId))
                .First(e => TestApi.ValueOf(e, "Vendor")?.GetString() == "Acme")
                .GetProperty("id").GetString()!;
            await TestApi.PutEntry(client, newTrackerId, acme, new() { ["Vendor"] = "Acme Inc" });

            var expense = await TestApi.GetEntry(client, trackerId, entryIds[0]);
            Assert.Equal("Acme Inc", TestApi.ValueOf(expense, "Vendor")?.GetString());
        }
    }
}
