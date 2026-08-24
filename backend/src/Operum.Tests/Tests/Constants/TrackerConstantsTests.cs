using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.TrackerConstants.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;

namespace Operum.Tests.Tests.Constants
{
    /// <summary>
    /// Tracker constants are named values a formula can reference. A constant may carry
    /// conditional values, each guarded by filters over the entry being calculated.
    /// </summary>
    public class TrackerConstantsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("constants");

        private static Task<HttpResponseMessage> PostConstant(HttpClient client, string trackerId, CreateTrackerConstantDto dto) =>
            client.PostAsJsonAsync($"trackers/{trackerId}/constants", dto);

        private static async Task<string> CreateConstant(HttpClient client, string trackerId, CreateTrackerConstantDto dto) =>
            await TestApi.IdOf(await PostConstant(client, trackerId, dto));

        private static CreateTrackerConstantDto Number(string name, string value) =>
            new() { Name = name, Type = DataTypes.Number, Value = value };

        private static async Task<List<string>> ConstantNames(HttpClient client, string trackerId)
        {
            var data = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/constants"));
            return [.. data.EnumerateArray().Select(c => c.GetProperty("name").GetString()!)];
        }

        [Fact]
        public async Task CreateConstant_StoresItAndListsItAlphabetically()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Create constant");

            await CreateConstant(client, trackerId, Number("Rate", "2.5"));
            await CreateConstant(client, trackerId, Number("Bonus", "1"));

            Assert.Equal(["Bonus", "Rate"], await ConstantNames(client, trackerId));
        }

        [Fact]
        public async Task GetConstant_ReturnsItsTypeAndValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Read constant");
            var constantId = await CreateConstant(client, trackerId, Number("Rate", "2.5"));

            var constant = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/constants/{constantId}"));

            Assert.Equal("Rate", constant.GetProperty("name").GetString());
            Assert.Equal(DataTypes.Number, constant.GetProperty("type").GetString());
            Assert.Equal("2.5", constant.GetProperty("value").GetString());
        }

        [Fact]
        public async Task GetConstant_UnknownConstant_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Unknown constant");

            var response = await client.GetAsync($"trackers/{trackerId}/constants/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateConstant_NameAlreadyUsedByAnotherConstant_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Duplicate constant");
            await CreateConstant(client, trackerId, Number("Rate", "1"));

            // The comparison ignores case, because formulas resolve names that way too.
            var response = await PostConstant(client, trackerId, Number("rate", "2"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("already exists", await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateConstant_NameAlreadyUsedByAField_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Constant shadows field");
            await TestApi.CreateField(client, trackerId, "Rate", DataTypes.Number);

            // Fields win name resolution in formulas, so the constant would be unreachable.
            var response = await PostConstant(client, trackerId, Number("Rate", "1"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataTypes.String, "hello")]
        [InlineData(DataTypes.Date, "2026-01-01")]
        public async Task CreateConstant_TypeThatCannotHoldAFormulaValue_ReturnsBadRequest(string type, string value)
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, $"Constant type {type}");

            var response = await PostConstant(client, trackerId,
                new CreateTrackerConstantDto { Name = "Thing", Type = type, Value = value });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(DataTypes.Number, "quite a lot")]
        [InlineData(DataTypes.Bool, "maybe")]
        [InlineData(DataTypes.TimeSpan, "half an hour")]
        public async Task CreateConstant_ValueThatDoesNotMatchTheType_ReturnsBadRequest(string type, string value)
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, $"Constant value {type}");

            var response = await PostConstant(client, trackerId,
                new CreateTrackerConstantDto { Name = "Thing", Type = type, Value = value });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateConstant_MoreConstantsThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Constant limit");
            for (var i = 0; i < DataLimits.MaxConstantCount; i++)
                await CreateConstant(client, trackerId, Number($"Rate{i}", "1"));

            var response = await PostConstant(client, trackerId, Number("OneTooMany", "1"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.MaxNumberReached("constants", DataLimits.MaxConstantCount), await TestApi.Messages(response));
        }

        [Fact]
        public async Task CreateConstant_ConditionalValueWithoutFilters_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Unguarded conditional");

            // A conditional value with no filter would always win over the base value.
            var response = await PostConstant(client, trackerId, new CreateTrackerConstantDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                Value = "1",
                Values = [new() { Priority = 0, Value = "2" }]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateConstant_ConditionalValueWithAnUnknownOperator_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Bad conditional operator");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var response = await PostConstant(client, trackerId, new CreateTrackerConstantDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                Value = "1",
                Values =
                [
                    new()
                    {
                        Priority = 0,
                        Value = "2",
                        Filters = [new() { FieldId = noteId, Operator = "Sort of equals", Value = "gold" }]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateConstant_ConditionalValueOfTheWrongType_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Bad conditional value");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var response = await PostConstant(client, trackerId, new CreateTrackerConstantDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                Value = "1",
                Values =
                [
                    new()
                    {
                        Priority = 0,
                        Value = "loads",
                        Filters = [new() { FieldId = noteId, Operator = OperatorTypes.EqualsOperator, Value = "gold" }]
                    }
                ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateConstant_MoreConditionalValuesThanTheLimit_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Conditional limit");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);

            var values = Enumerable.Range(0, DataLimits.MaxConstantValueCount + 1)
                .Select(i => new CreateTrackerConstantValueDto
                {
                    Priority = i,
                    Value = i.ToString(),
                    Filters = [new() { FieldId = noteId, Operator = OperatorTypes.EqualsOperator, Value = $"tier{i}" }]
                }).ToList();
            var response = await PostConstant(client, trackerId, new CreateTrackerConstantDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                Value = "1",
                Values = values
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateConstant_ReplacesTheValueAndTheConditions()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Update constant");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
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
                        Filters = [new() { FieldId = noteId, Operator = OperatorTypes.EqualsOperator, Value = "gold" }]
                    }
                ]
            });

            var response = await client.PutAsJsonAsync($"trackers/{trackerId}/constants/{constantId}",
                new UpdateTrackerConstantDto { Name = "Rate", Type = DataTypes.Number, Value = "3" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var constant = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/constants/{constantId}"));
            Assert.Equal("3", constant.GetProperty("value").GetString());
            Assert.Empty(constant.GetProperty("values").EnumerateArray());
        }

        [Fact]
        public async Task DeleteConstant_RemovesIt()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete constant");
            var constantId = await CreateConstant(client, trackerId, Number("Rate", "1"));

            var response = await client.DeleteAsync($"trackers/{trackerId}/constants/{constantId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await ConstantNames(client, trackerId));
        }

        [Fact]
        public async Task DeleteConstant_UnknownConstant_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Delete unknown constant");

            var response = await client.DeleteAsync($"trackers/{trackerId}/constants/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Constant_IsUsableFromAFormula()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Constant in formula");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            await CreateConstant(client, trackerId, Number("Rate", "2.5"));
            await TestApi.CreateCalculatedField(client, trackerId, "Total", "{Base} * {Rate}", DataTypes.Number);

            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Base"] = "4" });

            Assert.Equal(10, await TestApi.NumberValueOf(client, trackerId, entryId, "Total"));
        }

        [Fact]
        public async Task Constant_ConditionalValueIsPickedByTheEntryItIsCalculatedFor()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Conditional constant");
            var tierId = await TestApi.CreateField(client, trackerId, "Tier", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            await CreateConstant(client, trackerId, new CreateTrackerConstantDto
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
                    },
                    new()
                    {
                        Priority = 1,
                        Value = "5",
                        Filters = [new() { FieldId = tierId, Operator = OperatorTypes.EqualsOperator, Value = "silver" }]
                    }
                ]
            });
            await TestApi.CreateCalculatedField(client, trackerId, "Total", "{Base} * {Rate}", DataTypes.Number);

            var gold = await TestApi.CreateEntry(client, trackerId, new() { ["Tier"] = "gold", ["Base"] = "2" });
            var silver = await TestApi.CreateEntry(client, trackerId, new() { ["Tier"] = "silver", ["Base"] = "2" });
            var bronze = await TestApi.CreateEntry(client, trackerId, new() { ["Tier"] = "bronze", ["Base"] = "2" });

            Assert.Equal(20, await TestApi.NumberValueOf(client, trackerId, gold, "Total"));
            Assert.Equal(10, await TestApi.NumberValueOf(client, trackerId, silver, "Total"));
            // Nothing matched, so the constant falls back to its plain value.
            Assert.Equal(2, await TestApi.NumberValueOf(client, trackerId, bronze, "Total"));
        }

        [Fact]
        public async Task Constant_LowestPriorityWinsWhenSeveralConditionsMatch()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Competing conditions");
            var tierId = await TestApi.CreateField(client, trackerId, "Tier", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await CreateConstant(client, trackerId, new CreateTrackerConstantDto
            {
                Name = "Rate",
                Type = DataTypes.Number,
                Value = "1",
                Values =
                [
                    new()
                    {
                        Priority = 5,
                        Value = "3",
                        Filters = [new() { FieldId = tierId, Operator = OperatorTypes.EqualsOperator, Value = "gold" }]
                    },
                    new()
                    {
                        Priority = 2,
                        Value = "7",
                        Filters = [new() { FieldId = amountId, Operator = OperatorTypes.GreaterThan, Value = "1" }]
                    }
                ]
            });
            await TestApi.CreateCalculatedField(client, trackerId, "Total", "{Amount} * {Rate}", DataTypes.Number);

            var entryId = await TestApi.CreateEntry(client, trackerId, new() { ["Tier"] = "gold", ["Amount"] = "2" });

            // Both conditions hold; the smaller priority number decides.
            Assert.Equal(14, await TestApi.NumberValueOf(client, trackerId, entryId, "Total"));
        }

        [Fact]
        public async Task Constant_ConditionWithSeveralFiltersNeedsThemAllToHold()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Combined conditions");
            var tierId = await TestApi.CreateField(client, trackerId, "Tier", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await CreateConstant(client, trackerId, new CreateTrackerConstantDto
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
                        Filters =
                        [
                            new() { FieldId = tierId, Operator = OperatorTypes.EqualsOperator, Value = "gold" },
                            new() { FieldId = amountId, Operator = OperatorTypes.GreaterThan, Value = "5" }
                        ]
                    }
                ]
            });
            await TestApi.CreateCalculatedField(client, trackerId, "Total", "{Amount} * {Rate}", DataTypes.Number);

            var both = await TestApi.CreateEntry(client, trackerId, new() { ["Tier"] = "gold", ["Amount"] = "10" });
            var onlyOne = await TestApi.CreateEntry(client, trackerId, new() { ["Tier"] = "gold", ["Amount"] = "2" });

            Assert.Equal(100, await TestApi.NumberValueOf(client, trackerId, both, "Total"));
            Assert.Equal(2, await TestApi.NumberValueOf(client, trackerId, onlyOne, "Total"));
        }

        [Fact]
        public async Task ReadingConstants_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Constant read guard");
            var constantId = await CreateConstant(owner, trackerId, Number("Rate", "1"));

            var stranger = await _factory.NewUserClient("constantreader");
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{trackerId}/constants")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"trackers/{trackerId}/constants/{constantId}")).StatusCode);
        }

        [Fact]
        public async Task WritingConstants_OnATrackerOwnedBySomeoneElse_ReturnsNotFound()
        {
            var owner = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(owner, "Constant write guard");
            var constantId = await CreateConstant(owner, trackerId, Number("Rate", "1"));

            var stranger = await _factory.NewUserClient("constantwriter");
            Assert.Equal(HttpStatusCode.NotFound, (await PostConstant(stranger, trackerId, Number("Sneak", "1"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.PutAsJsonAsync($"trackers/{trackerId}/constants/{constantId}",
                    new UpdateTrackerConstantDto { Name = "Sneak", Type = DataTypes.Number, Value = "2" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await stranger.DeleteAsync($"trackers/{trackerId}/constants/{constantId}")).StatusCode);

            Assert.Equal(["Rate"], await ConstantNames(owner, trackerId));
        }
    }
}
