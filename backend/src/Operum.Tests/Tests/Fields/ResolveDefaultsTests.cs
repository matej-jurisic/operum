using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Fields.Requests;
using Operum.Model.DTOs.TrackerConstants.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Fields
{
    /// <summary>The resolve-defaults endpoint previews what a new entry's default-linked fields would resolve to, against unsaved form values.</summary>
    public class ResolveDefaultsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("resolvedefaults");

        private static async Task<string> CreateConstant(HttpClient client, string trackerId, CreateTrackerConstantDto dto) =>
            await TestApi.IdOf(await client.PostAsJsonAsync($"trackers/{trackerId}/constants", dto));

        private static Task<HttpResponseMessage> ResolveDefaults(HttpClient client, string trackerId, Dictionary<string, string?> fieldValues) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/fields/resolve-defaults", new ResolveDefaultValuesDto { FieldValues = fieldValues });

        [Fact]
        public async Task NoFieldHasADefault_ReturnsAnEmptyList()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "No defaults");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var response = await ResolveDefaults(client, trackerId, []);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await TestApi.Data(response);
            Assert.Empty(data.GetProperty("defaults").EnumerateArray());
        }

        [Fact]
        public async Task UnconditionalConstantDefault_ResolvesWithNoSubmittedValues()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Unconditional default");
            var constantId = await CreateConstant(client, trackerId,
                new CreateTrackerConstantDto { Name = "Rate", Type = DataTypes.Number, Value = "5" });
            var fieldId = await TestApi.IdOf(await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Rate", Type = DataTypes.Number, DefaultValueConstantId = constantId }));

            var response = await ResolveDefaults(client, trackerId, []);

            var defaults = (await TestApi.Data(response)).GetProperty("defaults").EnumerateArray().ToList();
            var resolved = Assert.Single(defaults);
            Assert.Equal(fieldId, resolved.GetProperty("fieldId").GetString());
            Assert.Equal("5", resolved.GetProperty("value").GetString());
        }

        [Fact]
        public async Task ConditionalConstantDefault_ResolvesDifferentlyBasedOnUnsavedFieldValues()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Conditional default");
            var tierId = await TestApi.CreateField(client, trackerId, "Tier", DataTypes.String);
            var constantId = await CreateConstant(client, trackerId, new CreateTrackerConstantDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                Value = "1",
                Values =
                [
                    new()
                    {
                        Priority = 0,
                        Value = "10",
                        Filters = [new() { FieldId = tierId, Operator = OperatorTypes.EqualsOperator, Value = "gold" }]
                    }
                ]
            });
            await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Rate", Type = DataTypes.Number, DefaultValueConstantId = constantId });

            var unconditional = await ResolveDefaults(client, trackerId, new() { ["Tier"] = "bronze" });
            var gold = await ResolveDefaults(client, trackerId, new() { ["Tier"] = "gold" });

            var unconditionalValue = (await TestApi.Data(unconditional)).GetProperty("defaults")[0].GetProperty("value").GetString();
            var goldValue = (await TestApi.Data(gold)).GetProperty("defaults")[0].GetProperty("value").GetString();
            Assert.Equal("1", unconditionalValue);
            Assert.Equal("10", goldValue);
        }

        [Fact]
        public async Task DateDefault_ResolvesARelativeTokenToAConcreteInstant()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Date token default");
            await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Due", Type = DataTypes.Date, DefaultValue = "today" });

            var response = await ResolveDefaults(client, trackerId, []);

            var resolved = (await TestApi.Data(response)).GetProperty("defaults")[0].GetProperty("value").GetString();
            Assert.NotNull(resolved);
            Assert.NotEqual("today", resolved);
            Assert.True(DateTime.TryParse(resolved, null, System.Globalization.DateTimeStyles.RoundtripKind, out _));
        }

        [Fact]
        public async Task FieldWithoutADefault_IsAbsentFromTheResponse()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Mixed fields");
            var constantId = await CreateConstant(client, trackerId,
                new CreateTrackerConstantDto { Name = "Rate", Type = DataTypes.Number, Value = "5" });
            await TestApi.PostField(client, trackerId,
                new CreateFieldDto { Name = "Rate", Type = DataTypes.Number, DefaultValueConstantId = constantId });
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var response = await ResolveDefaults(client, trackerId, []);

            var defaults = (await TestApi.Data(response)).GetProperty("defaults").EnumerateArray().ToList();
            Assert.Single(defaults);
            Assert.Equal("Rate", defaults[0].GetProperty("fieldName").GetString());
        }

        [Fact]
        public async Task VisibilityCondition_ResolvesTrueOrFalseBasedOnUnsavedFieldValues()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Conditional visibility");
            var statusId = await TestApi.CreateField(client, trackerId, "Status", DataTypes.String);
            var reasonId = await TestApi.IdOf(await TestApi.PostField(client, trackerId, new CreateFieldDto
            {
                Name = "Reason",
                Type = DataTypes.String,
                VisibilityFieldId = statusId,
                VisibilityOperator = OperatorTypes.EqualsOperator,
                VisibilityValue = "Refunded"
            }));

            var hidden = await ResolveDefaults(client, trackerId, new() { ["Status"] = "Open" });
            var shown = await ResolveDefaults(client, trackerId, new() { ["Status"] = "Refunded" });

            var hiddenVisibility = (await TestApi.Data(hidden)).GetProperty("visibility").EnumerateArray().ToList();
            var shownVisibility = (await TestApi.Data(shown)).GetProperty("visibility").EnumerateArray().ToList();

            var hiddenEntry = Assert.Single(hiddenVisibility);
            Assert.Equal(reasonId, hiddenEntry.GetProperty("fieldId").GetString());
            Assert.False(hiddenEntry.GetProperty("visible").GetBoolean());

            var shownEntry = Assert.Single(shownVisibility);
            Assert.True(shownEntry.GetProperty("visible").GetBoolean());
        }

        [Fact]
        public async Task FieldWithoutAVisibilityCondition_IsAbsentFromVisibilityList()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "No visibility condition");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var response = await ResolveDefaults(client, trackerId, []);

            var visibility = (await TestApi.Data(response)).GetProperty("visibility").EnumerateArray().ToList();
            Assert.Empty(visibility);
        }
    }
}
