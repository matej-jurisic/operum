using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Views.Requests;
using Operum.Tests.Util;
using System.Net;

namespace Operum.Tests.Tests.Views
{
    /// <summary>
    /// What a view actually does to the entry list: the filters and sorts its queries carry
    /// are translated into the query, and several queries within one view combine.
    /// </summary>
    public class ViewFilteringTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("filtering");

        [Theory]
        [InlineData(OperatorTypes.EqualsOperator, "5", new[] { "5" })]
        [InlineData(OperatorTypes.NotEquals, "5", new[] { "1", "10" })]
        [InlineData(OperatorTypes.GreaterThan, "5", new[] { "10" })]
        [InlineData(OperatorTypes.GreaterThanOrEqual, "5", new[] { "5", "10" })]
        [InlineData(OperatorTypes.LessThan, "5", new[] { "1" })]
        [InlineData(OperatorTypes.LessThanOrEqual, "5", new[] { "1", "5" })]
        public async Task NumberFilter_KeepsTheMatchingEntries(string op, string value, string[] expected)
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, $"Number {op}");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "1", "5", "10" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Filtered", amountId, op, value);

            var amounts = (await TestApi.ListValues(client, trackerId, "Amount", viewId)).Order().ToList();

            Assert.Equal(expected.Order(), amounts);
        }

        [Theory]
        [InlineData(OperatorTypes.EqualsOperator, "walk", new[] { "walk" })]
        [InlineData(OperatorTypes.NotEquals, "walk", new[] { "long walk", "run" })]
        [InlineData(OperatorTypes.Contains, "wal", new[] { "long walk", "walk" })]
        [InlineData(OperatorTypes.StartsWith, "wal", new[] { "walk" })]
        [InlineData(OperatorTypes.EndsWith, "walk", new[] { "long walk", "walk" })]
        public async Task StringFilter_KeepsTheMatchingEntries(string op, string value, string[] expected)
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, $"String {op}");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            foreach (var note in new[] { "walk", "long walk", "run" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = note });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Filtered", noteId, op, value);

            var notes = (await TestApi.ListValues(client, trackerId, "Note", viewId)).Order().ToList();

            Assert.Equal(expected.Order(), notes);
        }

        [Theory]
        [InlineData("true", new[] { "done" })]
        [InlineData("false", new[] { "todo" })]
        public async Task BoolFilter_KeepsTheMatchingEntries(string value, string[] expected)
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, $"Bool {value}");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var doneId = await TestApi.CreateField(client, trackerId, "Done", DataTypes.Bool);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "done", ["Done"] = "true" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "todo", ["Done"] = "false" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Filtered", doneId, OperatorTypes.EqualsOperator, value);

            var notes = await TestApi.ListValues(client, trackerId, "Note", viewId);

            Assert.Equal(expected, notes);
        }

        // Comparing or ordering by a timespan in the database is deliberately not covered:
        // SQLite refuses TimeSpan in comparisons and ORDER BY, so such a test would fail here
        // for a reason Postgres does not share. Timespan values themselves are covered in
        // EntriesTests and EntriesCsvTests.

        [Fact]
        public async Task DateFilter_EqualsMatchesEveryTimeOfThatDay()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Date equality");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var momentId = await TestApi.CreateField(client, trackerId, "Moment", DataTypes.DateTime);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "midnight", ["Moment"] = "2026-03-01T00:00:00Z" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "late", ["Moment"] = "2026-03-01T23:59:00Z" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "next day", ["Moment"] = "2026-03-02T00:00:00Z" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "That day", momentId, OperatorTypes.EqualsOperator, "2026-03-01");

            var notes = (await TestApi.ListValues(client, trackerId, "Note", viewId)).Order().ToList();

            // Equality on a date is the whole calendar day, not the instant at its start.
            Assert.Equal(["late", "midnight"], notes);
        }

        [Fact]
        public async Task DateFilter_DynamicToken_ResolvesAgainstTheCurrentDay()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Dynamic date filter");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var dayId = await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date);
            await TestApi.CreateEntry(client, trackerId, new()
            {
                ["Note"] = "today",
                ["Day"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            });
            await TestApi.CreateEntry(client, trackerId, new()
            {
                ["Note"] = "last week",
                ["Day"] = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd")
            });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "From today", dayId, OperatorTypes.GreaterThanOrEqual, "today");

            var notes = await TestApi.ListValues(client, trackerId, "Note", viewId);

            Assert.Equal(["today"], notes);
        }

        [Fact]
        public async Task Filter_EntryWithNoRowForTheField_IsLeftOutEvenByNotEquals()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Missing value");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            var withValue = await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "has one", ["Amount"] = "1" });
            // Updating without the Amount key deletes its row entirely, which is not the same
            // as holding a null.
            await TestApi.PutEntry(client, trackerId, withValue, new() { ["Note"] = "has one" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "still has one", ["Amount"] = "9" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Not five", amountId, OperatorTypes.NotEquals, "5");

            var notes = await TestApi.ListValues(client, trackerId, "Note", viewId);

            Assert.Equal(["still has one"], notes);
        }

        [Fact]
        public async Task Filter_WithoutAValue_MatchesEntriesHoldingNoValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Null filter");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "empty", ["Amount"] = null });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "filled", ["Amount"] = "1" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Unset", amountId, OperatorTypes.EqualsOperator, null);

            var notes = await TestApi.ListValues(client, trackerId, "Note", viewId);

            Assert.Equal(["empty"], notes);
        }

        [Fact]
        public async Task Filter_IsEmpty_MatchesEntriesWithNoRowForTheField()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Empty, no row");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "before the field" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "also before" });
            // Adding the field now leaves the existing entries with no FieldValue row for it,
            // which "is empty" still has to treat as empty.
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "filled", ["Amount"] = "1" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Missing amount", amountId, OperatorTypes.EqualsOperator, null);

            var notes = (await TestApi.ListValues(client, trackerId, "Note", viewId)).Order().ToList();

            Assert.Equal(["also before", "before the field"], notes);
        }

        [Fact]
        public async Task Sorting_Ascending_OrdersByTheFieldValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Sort ascending");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "10", "1", "5" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });
            var viewId = await TestApi.CreateSortView(client, trackerId, "Smallest first", amountId, descending: false);

            Assert.Equal(["1", "5", "10"], await TestApi.ListValues(client, trackerId, "Amount", viewId));
        }

        [Fact]
        public async Task Sorting_Descending_OrdersByTheFieldValue()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Sort descending");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "10", "1", "5" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });
            var viewId = await TestApi.CreateSortView(client, trackerId, "Biggest first", amountId, descending: true);

            Assert.Equal(["10", "5", "1"], await TestApi.ListValues(client, trackerId, "Amount", viewId));
        }

        [Fact]
        public async Task Sorting_SecondSortBreaksTiesOfTheFirst()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Tie break");
            var groupId = await TestApi.CreateField(client, trackerId, "Group", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Group"] = "a", ["Amount"] = "2" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Group"] = "b", ["Amount"] = "1" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Group"] = "a", ["Amount"] = "1" });
            var viewId = await TestApi.CreateView(client, trackerId, new CreateViewDto
            {
                Name = "Group then amount",
                Queries =
                [
                    TestApi.SortClause(groupId, descending: false),
                    TestApi.SortClause(amountId, descending: true)
                ]
            });

            var entries = await TestApi.ListEntries(client, trackerId, viewId);
            var ordered = entries
                .Select(e => $"{TestApi.ValueOf(e, "Group")!.Value.GetString()}{TestApi.ValueOf(e, "Amount")!.Value.GetDouble()}")
                .ToList();

            Assert.Equal(["a2", "a1", "b1"], ordered);
        }

        [Fact]
        public async Task Sorting_StringField_OrdersAlphabetically()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Sort strings");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            foreach (var note in new[] { "run", "cycle", "walk" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = note });
            var viewId = await TestApi.CreateSortView(client, trackerId, "A to Z", noteId, descending: false);

            Assert.Equal(["cycle", "run", "walk"], await TestApi.ListValues(client, trackerId, "Note", viewId));
        }

        [Fact]
        public async Task Sorting_DateField_OrdersChronologically()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Sort dates");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var dayId = await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "middle", ["Day"] = "2026-02-01" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "last", ["Day"] = "2026-03-01" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "first", ["Day"] = "2026-01-01" });
            var viewId = await TestApi.CreateSortView(client, trackerId, "Newest first", dayId, descending: true);

            Assert.Equal(["last", "middle", "first"], await TestApi.ListValues(client, trackerId, "Note", viewId));
        }

        [Fact]
        public async Task TwoQueries_FiltersAreAndedTogether()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Two filters");
            var noteId = await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "walk", ["Amount"] = "10" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "walk", ["Amount"] = "1" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "run", ["Amount"] = "10" });
            var viewId = await TestApi.CreateViewFromClauses(client, trackerId, "Big walks",
                TestApi.FilterClause(noteId, OperatorTypes.EqualsOperator, "walk"),
                TestApi.FilterClause(amountId, OperatorTypes.GreaterThan, "5"));

            var entries = await TestApi.ListEntries(client, trackerId, viewId);

            // Combining both queries in one view narrows the list rather than widening it.
            Assert.Single(entries);
            Assert.Equal("walk", TestApi.ValueOf(entries[0], "Note")!.Value.GetString());
            Assert.Equal(10, TestApi.ValueOf(entries[0], "Amount")!.Value.GetDouble());
        }

        [Fact]
        public async Task TwoQueries_TheFirstOneWinsWhenBothSortTheSameField()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Competing sorts");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "1", "10", "5" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });
            var descending = TestApi.SortClause(amountId, descending: true);
            var ascending = TestApi.SortClause(amountId, descending: false);

            var descendingWins = await TestApi.CreateViewFromClauses(client, trackerId, "Descending wins", descending, ascending);
            var ascendingWins = await TestApi.CreateViewFromClauses(client, trackerId, "Ascending wins", ascending, descending);

            Assert.Equal(["10", "5", "1"], await TestApi.ListValues(client, trackerId, "Amount", descendingWins));
            Assert.Equal(["1", "5", "10"], await TestApi.ListValues(client, trackerId, "Amount", ascendingWins));
        }

        [Fact]
        public async Task TwoQueries_AFilterAndASortAreBothApplied()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Filter and sort");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "1", "10", "5", "7" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });
            var viewId = await TestApi.CreateViewFromClauses(client, trackerId, "Big, biggest first",
                TestApi.FilterClause(amountId, OperatorTypes.GreaterThan, "1"),
                TestApi.SortClause(amountId, descending: true));

            Assert.Equal(["10", "7", "5"], await TestApi.ListValues(client, trackerId, "Amount", viewId));
        }

        [Fact]
        public async Task NoView_ReturnsEveryEntry()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "No view");
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "1", "2", "3" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });

            Assert.Equal(3, (await TestApi.ListEntries(client, trackerId)).Count);
        }

        [Fact]
        public async Task Filtering_AlsoNarrowsTheTotalCount()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Filtered count");
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            foreach (var amount in new[] { "1", "5", "10" })
                await TestApi.CreateEntry(client, trackerId, new() { ["Amount"] = amount });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Big", amountId, OperatorTypes.GreaterThan, "1");

            var response = await client.GetAsync($"{TestApi.EntriesUrl(trackerId, viewId)}&page=1&pageSize=1");
            var data = await TestApi.Data(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // The total has to describe the filtered set, or the paging control lies.
            Assert.Equal(2, data.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, data.GetProperty("items").GetArrayLength());
        }
    }
}
