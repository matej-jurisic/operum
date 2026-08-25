using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Model.DTOs.Views.Requests;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Operum.Tests.Tests.Views
{
    /// <summary>
    /// A view's columns are the fields it shows. They belong to the view rather than being
    /// queries of their own: a column names a field and nothing else, so there is no clause
    /// to author and nothing to reuse across views.
    ///
    /// They are also the one part of a view the server never applies to the entries query:
    /// whatever renders the entries decides what to draw, so a filter or a sort over a
    /// hidden field has to keep working exactly as it did.
    /// </summary>
    public class ViewColumnsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("columns");

        private sealed record Fixture(string TrackerId, string AmountFieldId, string NoteFieldId);

        private static async Task<Fixture> CreateTracker(HttpClient client, string name)
        {
            var trackerId = await TestApi.CreateTracker(client, name);
            return new Fixture(
                trackerId,
                await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number),
                await TestApi.CreateField(client, trackerId, "Note", DataTypes.String));
        }

        private static async Task<List<string>> ViewColumnFieldIds(HttpClient client, string trackerId, string viewId)
        {
            var view = await TestApi.Data(await client.GetAsync($"trackers/{trackerId}/views/{viewId}"));
            return [.. view.GetProperty("columnFieldIds").EnumerateArray().Select(id => id.GetString()!)];
        }

        [Fact]
        public async Task CreateView_WithColumns_KeepsThemInViewOrder()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Column order");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Note first",
                ColumnFieldIds = [fixture.NoteFieldId, fixture.AmountFieldId]
            });

            // The order the view names them in is the column order, which is why it is not
            // re-sorted into field order.
            Assert.Equal(
                [fixture.NoteFieldId, fixture.AmountFieldId],
                await ViewColumnFieldIds(client, fixture.TrackerId, viewId));
        }

        [Fact]
        public async Task CreateView_WithColumns_CreatesNoQueries()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Columns are not queries");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Note only",
                ColumnFieldIds = [fixture.NoteFieldId]
            });

            // Columns cost the tracker no query budget and never show up in its query list,
            // which is the whole reason they are not queries.
            var trackerQueries = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/queries"));
            Assert.Empty(trackerQueries.EnumerateArray());

            var view = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/views/{viewId}"));
            Assert.Empty(view.GetProperty("queries").EnumerateArray());
        }

        [Fact]
        public async Task CreateView_RepeatingAColumn_KeepsItOnce()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Repeated column");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Amount twice",
                ColumnFieldIds = [fixture.AmountFieldId, fixture.AmountFieldId]
            });

            Assert.Equal([fixture.AmountFieldId], await ViewColumnFieldIds(client, fixture.TrackerId, viewId));
        }

        [Fact]
        public async Task CreateView_WithAColumnOverAnotherTrackersField_IsRejected()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Column own tracker");
            var other = await CreateTracker(client, "Column other tracker");

            var response = await TestApi.PostView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Foreign column",
                ColumnFieldIds = [other.AmountFieldId]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetEntries_ViewWithColumns_StillFiltersAndSortsOnHiddenFields()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Columns are last");

            foreach (var amount in new[] { "1", "5", "9" })
                await TestApi.CreateEntry(client, fixture.TrackerId, new Dictionary<string, string?>
                {
                    ["Amount"] = amount,
                    ["Note"] = $"note {amount}",
                });

            // Amount is filtered on, sorted by, and hidden, all at once.
            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Big amounts, note only",
                Queries =
                [
                    new ViewQueryRefDto { NewQuery = TestApi.FilterClause(fixture.AmountFieldId, OperatorTypes.GreaterThan, "1") },
                    new ViewQueryRefDto { NewQuery = TestApi.SortClause(fixture.AmountFieldId, descending: true) },
                ],
                ColumnFieldIds = [fixture.NoteFieldId]
            });

            Assert.Equal(["note 9", "note 5"], await TestApi.ListValues(client, fixture.TrackerId, "Note", viewId));
        }

        [Fact]
        public async Task GetEntries_ViewWithColumns_StillReturnsEveryValue()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Hidden values are sent");
            await TestApi.CreateEntry(client, fixture.TrackerId, new Dictionary<string, string?>
            {
                ["Amount"] = "7",
                ["Note"] = "kept",
            });

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Note only",
                ColumnFieldIds = [fixture.NoteFieldId]
            });

            // Columns narrow what is drawn, never what is sent: the entry still has to carry
            // the values an edit dialog, a recalculation or an export needs.
            var entry = (await TestApi.ListEntries(client, fixture.TrackerId, viewId)).Single();
            Assert.Equal(7, TestApi.ValueOf(entry, "Amount")?.GetDouble());
            Assert.Equal("kept", TestApi.ValueOf(entry, "Note")?.GetString());
        }

        [Fact]
        public async Task DeleteField_DropsItsColumnFromTheViewsUsingIt()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Deleted column field");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Both columns",
                ColumnFieldIds = [fixture.AmountFieldId, fixture.NoteFieldId]
            });

            await client.DeleteAsync($"trackers/{fixture.TrackerId}/fields/{fixture.AmountFieldId}");

            Assert.Equal([fixture.NoteFieldId], await ViewColumnFieldIds(client, fixture.TrackerId, viewId));
        }

        [Fact]
        public async Task UpdateView_WithoutItsColumns_ClearsThemAndShowsEveryFieldAgain()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Cleared columns");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Note only",
                ColumnFieldIds = [fixture.NoteFieldId]
            });

            var response = await client.PutAsJsonAsync($"trackers/{fixture.TrackerId}/views/{viewId}", new UpdateViewDto
            {
                Name = "Everything",
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // A view naming no columns says nothing about them, which reads as every field.
            Assert.Empty(await ViewColumnFieldIds(client, fixture.TrackerId, viewId));
        }

        [Fact]
        public async Task UpdateView_ReplacesItsColumnsWholesale()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Replaced columns");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Note only",
                ColumnFieldIds = [fixture.NoteFieldId]
            });

            var response = await client.PutAsJsonAsync($"trackers/{fixture.TrackerId}/views/{viewId}", new UpdateViewDto
            {
                Name = "Amount only",
                ColumnFieldIds = [fixture.AmountFieldId]
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal([fixture.AmountFieldId], await ViewColumnFieldIds(client, fixture.TrackerId, viewId));
        }

        [Fact]
        public async Task DeleteView_LeavesTheTrackersFieldsAlone()
        {
            var client = await OwnerClient();
            var fixture = await CreateTracker(client, "Deleted view columns");

            var viewId = await TestApi.CreateView(client, fixture.TrackerId, new CreateViewDto
            {
                Name = "Note only",
                ColumnFieldIds = [fixture.NoteFieldId]
            });

            var response = await client.DeleteAsync($"trackers/{fixture.TrackerId}/views/{viewId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fields = await TestApi.Data(await client.GetAsync($"trackers/{fixture.TrackerId}/fields"));
            Assert.Equal(2, fields.EnumerateArray().Count());
        }
    }
}
