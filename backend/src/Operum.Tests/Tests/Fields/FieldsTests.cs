using Operum.Model.Constants;
using Operum.Model.Constants.Analytics;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Analytics.Requests;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.Widgets.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Fields
{
    public class FieldsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("fields");

        private static async Task<List<string>> FieldNames(HttpClient client, string trackerId)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/fields"));
            return [.. data.EnumerateArray().Select(f => f.GetProperty("name").GetString()!)];
        }

        private static Task<HttpResponseMessage> PutField(HttpClient client, string trackerId, string fieldId, UpdateFieldDto field) =>
            client.PutAsJsonAsync($"trackers/{trackerId}/fields/{fieldId}", field);

        [Fact]
        public async Task CreateField_StoresTheDefinitionAndListsIt()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Create field");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Amount", Type = DataTypes.Number, Description = "How much", Required = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var field = await TestApi.Data(response);
            Assert.Equal("Amount", field.GetProperty("name").GetString());
            Assert.Equal(DataTypes.Number, field.GetProperty("type").GetString());
            Assert.Equal("How much", field.GetProperty("description").GetString());
            Assert.True(field.GetProperty("required").GetBoolean());
            Assert.Equal(["Amount"], await FieldNames(client, trackerId));
        }

        [Fact]
        public async Task GetFieldList_ReturnsFieldsInCreationOrder()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Field order");
            await TestApi.CreateField(client, trackerId, "First", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Second", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Third", DataTypes.String);

            Assert.Equal(["First", "Second", "Third"], await FieldNames(client, trackerId));
        }

        [Fact]
        public async Task CreateField_UnknownType_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Bad type");

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto { Name = "Thing", Type = "colour" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_NameLongerThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Long name");

            Assert.Equal(HttpStatusCode.OK,
                (await TestApi.PostField(client, trackerId, new CreateFieldDto { Name = new string('a', 30), Type = DataTypes.String })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await TestApi.PostField(client, trackerId, new CreateFieldDto { Name = new string('b', 31), Type = DataTypes.String })).StatusCode);
        }

        [Fact]
        public async Task CreateField_EmptyName_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Empty name");

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto { Name = "", Type = DataTypes.String });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_MoreFieldsThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Field limit");
            for (var i = 0; i < DataLimits.MaxFieldCount; i++)
                await TestApi.CreateField(client, trackerId, $"Field{i}", DataTypes.String);

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "OneTooMany", Type = DataTypes.String });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.MaxNumberReached("fields", DataLimits.MaxFieldCount), await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateField_SelectOptions_ComeBackAsAList()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Select options");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Mood", Type = DataTypes.String, SelectOptions = ["good", "bad"] });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var options = (await TestApi.Data(response)).GetProperty("selectOptions")
                .EnumerateArray().Select(o => o.GetString()).ToList();
            Assert.Equal(["good", "bad"], options);
        }

        [Fact]
        public async Task CreateField_NonNumericSelectOptionsOnANumberField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Bad select options");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Score", Type = DataTypes.Number, SelectOptions = ["1", "high"] });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_CalculatedAndRequired_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Calculated required");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);

            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Doubled",
                Type = DataTypes.Number,
                IsCalculated = true,
                Formula = "{Base} * 2",
                Required = true
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_CalculatedWithAnIncompatibleType_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Calculated string");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);

            // Only number, bool and timespan can hold a formula result.
            var response = await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Label",
                Type = DataTypes.String,
                IsCalculated = true,
                Formula = "{Base} * 2"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_CalculatedWithoutAFormula_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Calculated no formula");

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Doubled", Type = DataTypes.Number, IsCalculated = true });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateField_ManualFieldCarryingAFormula_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Manual with formula");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);

            var response = await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Doubled", Type = DataTypes.Number, Formula = "{Base} * 2" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateField_RenamesItAndKeepsTheEntryValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Rename");
            var fieldId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "5" });

            var response = await PutField(client, trackerId, fieldId,
                new UpdateFieldDto { Name = "Total", Type = DataTypes.Number });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["Total"], await FieldNames(client, trackerId));
            Assert.Equal(5, await TestApi.NumberValueOf(client, trackerId, entryId, "Total"));
        }

        [Fact]
        public async Task UpdateField_ChangingTheTypeStrandsExistingValues()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Retype");
            var fieldId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = "5" });

            // Stored values are not converted, so the number that was there reads as empty
            // once the field claims to be text.
            var response = await PutField(client, trackerId, fieldId,
                new UpdateFieldDto { Name = "Amount", Type = DataTypes.String });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entry = await TestApi.GetEntry(client, trackerId, entryId);
            Assert.Null(TestApi.ValueOf(entry, "Amount"));
        }

        [Fact]
        public async Task UpdateField_TurningACalculatedFieldManual_DropsTheFormula()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Calculated to manual");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            var doubledId = await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);

            var response = await PutField(client, trackerId, doubledId,
                new UpdateFieldDto { Name = "Doubled", Type = DataTypes.Number });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var field = await TestApi.Data(response);
            Assert.False(field.GetProperty("isCalculated").GetBoolean());
            Assert.Equal(JsonValueKind.Null, field.GetProperty("formula").ValueKind);
        }

        [Fact]
        public async Task UpdateField_AcceptsALongerNameThanCreateDoes()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Name limits differ");
            var fieldId = await TestApi.CreateField(client, trackerId, "Short", DataTypes.String);

            // Create caps names at 30 characters, update at 100.
            var response = await PutField(client, trackerId, fieldId,
                new UpdateFieldDto { Name = new string('a', 60), Type = DataTypes.String });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task UpdateField_UnknownField_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Update unknown");

            var response = await PutField(client, trackerId, Guid.NewGuid().ToString(),
                new UpdateFieldDto { Name = "Ghost", Type = DataTypes.String });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteField_RemovesItAndItsValues()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete field");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi", ["Amount"] = "5" });

            var response = await client.DeleteAsync($"trackers/{trackerId}/fields/{amountId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["Note"], await FieldNames(client, trackerId));
            var entry = await TestApi.GetEntry(client, trackerId, entryId);
            Assert.Single(entry.GetProperty("fieldValues").EnumerateArray());
        }

        [Fact]
        public async Task DeleteField_KeepsTheOrderOfTheRemainingFields()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete keeps order");
            await TestApi.CreateField(client, trackerId, "First", DataTypes.String);
            var secondId = await TestApi.CreateField(client, trackerId, "Second", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Third", DataTypes.String);

            await client.DeleteAsync($"trackers/{trackerId}/fields/{secondId}");

            Assert.Equal(["First", "Third"], await FieldNames(client, trackerId));
        }

        // A field's mapping into a Widget is the whole of what deleting the field can take
        // down -- the widget itself survives (across every dashboard placing it) and falls
        // back to a degraded render, unlike the old tracker Analytic this used to also
        // hard-delete.
        [Fact]
        public async Task DeleteField_UsedByAWidget_WidgetSurvivesWithADegradedFieldMapping()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete field of a widget");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);

            var widgetId = await TestApi.IdOf(await client.PostAsJsonAsync("widgets", new CreateWidgetDto
            {
                ResultType = AnalyticTypes.SingleValue,
                Code = AnalyticCodes.Average,
                Sources =
                [
                    new CreateWidgetSourceRequestDto
                    {
                        TrackerId = trackerId,
                        Fields = [new CreateAnalyticFieldDto { FieldId = amountId, Purpose = AnalyticPurposes.Value }]
                    }
                ]
            }));

            var response = await client.DeleteAsync($"trackers/{trackerId}/fields/{amountId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await FieldNames(client, trackerId));

            var widget = await TestApi.Data(await client.GetAsync($"widgets/{widgetId}"));
            Assert.Empty(widget.GetProperty("sources")[0].GetProperty("fields").EnumerateArray());
        }

        [Fact]
        public async Task DeleteField_UnknownField_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete unknown");

            var response = await client.DeleteAsync($"trackers/{trackerId}/fields/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReorderFields_AppliesTheGivenOrder()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Reorder");
            var firstId = await TestApi.CreateField(client, trackerId, "First", DataTypes.String);
            var secondId = await TestApi.CreateField(client, trackerId, "Second", DataTypes.String);
            var thirdId = await TestApi.CreateField(client, trackerId, "Third", DataTypes.String);

            var response = await client.PutAsJsonAsync($"trackers/{trackerId}/fields/reorder",
                new ReorderFieldsDto { FieldIds = [thirdId, firstId, secondId] });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["Third", "First", "Second"], await FieldNames(client, trackerId));
        }

        [Fact]
        public async Task ReorderFields_PartialList_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Reorder partial");
            var firstId = await TestApi.CreateField(client, trackerId, "First", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Second", DataTypes.String);

            // Reordering is all-or-nothing: the payload has to name every field of the tracker.
            var response = await client.PutAsJsonAsync($"trackers/{trackerId}/fields/reorder",
                new ReorderFieldsDto { FieldIds = [firstId] });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(["First", "Second"], await FieldNames(client, trackerId));
        }

        [Fact]
        public async Task ReorderFields_FieldFromAnotherTracker_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Reorder foreign");
            var fieldId = await TestApi.CreateField(client, trackerId, "Mine", DataTypes.String);
            var otherTrackerId = await TestApi.CreateTracker(client, "Reorder foreign source");
            var foreignFieldId = await TestApi.CreateField(client, otherTrackerId, "Theirs", DataTypes.String);

            var response = await client.PutAsJsonAsync($"trackers/{trackerId}/fields/reorder",
                new ReorderFieldsDto { FieldIds = [fieldId, foreignFieldId] });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetField_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Read guard");
            var fieldId = await TestApi.CreateField(owner, trackerId, "Amount", DataTypes.Number);

            var stranger = await _factory.NewUserClient("fieldreader");
            var response = await stranger.GetAsync($"trackers/{trackerId}/fields/{fieldId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task WritingFields_OnATrackerOwnedBySomeoneElse_ReturnsNotFound()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Write guard");
            var fieldId = await TestApi.CreateField(owner, trackerId, "Amount", DataTypes.Number);

            // Schema changes hide the tracker's existence instead of admitting it is off limits,
            // which is why these answer 404 where reads answer 403.
            var stranger = await _factory.NewUserClient("fieldwriter");
            Assert.Equal(HttpStatusCode.NotFound,
                (await TestApi.PostField(stranger, trackerId, new CreateFieldDto { Name = "Sneak", Type = DataTypes.String })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await PutField(stranger, trackerId, fieldId, new UpdateFieldDto { Name = "Sneak", Type = DataTypes.String })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.DeleteAsync($"trackers/{trackerId}/fields/{fieldId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.PutAsJsonAsync($"trackers/{trackerId}/fields/reorder", new ReorderFieldsDto { FieldIds = [fieldId] })).StatusCode);

            Assert.Equal(["Amount"], await FieldNames(owner, trackerId));
        }
    }
}
