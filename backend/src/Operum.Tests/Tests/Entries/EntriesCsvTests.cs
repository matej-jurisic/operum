using Operum.Model.Constants;
using Operum.Model.Constants.Fields;
using Operum.Tests.Util;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Operum.Tests.Tests.Entries
{
    public class EntriesCsvTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory = factory;

        private Task<HttpClient> OwnerClient() => _factory.NewUserClient("csv");

        private static async Task<HttpResponseMessage> ImportCsv(HttpClient client, string trackerId, string csv)
        {
            using var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(file, "File", "entries.csv");
            return await client.PostAsync($"trackers/{trackerId}/entries/import-csv", content);
        }

        private static Task<HttpResponseMessage> ExportCsv(HttpClient client, string trackerId, params string[] viewIds) =>
            client.GetAsync($"trackers/{trackerId}/entries/export-csv"
                + (viewIds.Length == 0 ? "" : "?" + string.Join("&", viewIds.Select(v => $"viewId={v}"))));

        private static async Task<List<string>> ExportedLines(HttpClient client, string trackerId, params string[] viewIds)
        {
            var response = await ExportCsv(client, trackerId, viewIds);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            return [.. body.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r'))];
        }

        private static async Task<string> TrackerWithNoteAndAmount(HttpClient client, string name)
        {
            var trackerId = await TestApi.CreateTracker(client, name);
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            return trackerId;
        }

        [Fact]
        public async Task ExportCsv_WritesAHeaderOfFieldNamesAndOneRowPerEntry()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Export");
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "first", ["Amount"] = "1" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "second", ["Amount"] = "2" });

            var lines = await ExportedLines(client, trackerId);

            Assert.Equal("Note,Amount", lines[0]);
            Assert.Equal(3, lines.Count);
            // Numbers are written with two decimals, not as they were typed in.
            Assert.Contains("first,1.00", lines);
            Assert.Contains("second,2.00", lines);
        }

        [Fact]
        public async Task ExportCsv_ColumnsFollowTheFieldOrder()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Export order");
            await TestApi.CreateField(client, trackerId, "Second", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "First", DataTypes.String);
            await TestApi.CreateEntry(client, trackerId, new() { ["Second"] = "b", ["First"] = "a" });

            var lines = await ExportedLines(client, trackerId);

            Assert.Equal("Second,First", lines[0]);
            Assert.Equal("b,a", lines[1]);
        }

        [Fact]
        public async Task ExportCsv_NoEntries_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Export empty");

            var response = await ExportCsv(client, trackerId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.NoEntriesFound, await TestApi.Messages(response));
        }

        [Fact]
        public async Task ExportCsv_WithAView_ExportsOnlyTheMatchingEntries()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Export filtered");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            var amountId = await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "small", ["Amount"] = "1" });
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "big", ["Amount"] = "10" });
            var viewId = await TestApi.CreateFilterView(client, trackerId, "Big", amountId, OperatorTypes.GreaterThan, "5");

            var lines = await ExportedLines(client, trackerId, viewId);

            Assert.Equal(2, lines.Count);
            Assert.Equal("big,10.00", lines[1]);
        }

        [Fact]
        public async Task ExportCsv_UnknownView_ReturnsNotFound()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Export unknown view");
            await TestApi.CreateEntry(client, trackerId, new() { ["Note"] = "hi", ["Amount"] = "1" });

            var response = await ExportCsv(client, trackerId, Guid.NewGuid().ToString());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ExportCsv_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(owner, "Export guard");
            await TestApi.CreateEntry(owner, trackerId, new() { ["Note"] = "hi", ["Amount"] = "1" });

            var stranger = await _factory.NewUserClient("csvreader");
            var response = await ExportCsv(stranger, trackerId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ImportCsv_CreatesOneEntryPerRow()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Import");

            var response = await ImportCsv(client, trackerId, "Note,Amount\nfirst,1\nsecond,2\n");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var notes = (await TestApi.ListValues(client, trackerId, "Note")).Order().ToList();
            Assert.Equal(["first", "second"], notes);
        }

        [Fact]
        public async Task ImportCsv_SemicolonDelimited_IsAccepted()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Import semicolons");

            // The delimiter is sniffed from the header line, for the sake of European exports.
            var response = await ImportCsv(client, trackerId, "Note;Amount\nfirst;1\n");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entries = await TestApi.ListEntries(client, trackerId);
            Assert.Single(entries);
            Assert.Equal("first", TestApi.ValueOf(entries[0], "Note")!.Value.GetString());
            Assert.Equal(1, TestApi.ValueOf(entries[0], "Amount")!.Value.GetDouble());
        }

        [Fact]
        public async Task ImportCsv_ColumnsThatAreNotFields_AreIgnored()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Import extra columns");

            var response = await ImportCsv(client, trackerId, "Note,Amount,Nonsense\nfirst,1,ignored\n");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entries = await TestApi.ListEntries(client, trackerId);
            Assert.Single(entries);
            Assert.Equal(2, entries[0].GetProperty("fieldValues").GetArrayLength());
        }

        [Fact]
        public async Task ImportCsv_BlankValues_LeaveTheFieldUnset()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Import blanks");

            var response = await ImportCsv(client, trackerId, "Note,Amount\nfirst,\n");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entries = await TestApi.ListEntries(client, trackerId);
            Assert.Null(TestApi.ValueOf(entries[0], "Amount"));
        }

        [Fact]
        public async Task ImportCsv_RowMissingARequiredField_ReturnsBadRequestAndImportsNothing()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Import required");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number, required: true);

            var response = await ImportCsv(client, trackerId, "Note,Amount\nfine,1\nbroken,\n");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Missing required fields: Amount", await TestApi.Messages(response));
            // One bad row rejects the file, rather than importing the rows around it.
            Assert.Empty(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task ImportCsv_EmptyFile_ReturnsBadRequest()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Import empty");

            var response = await ImportCsv(client, trackerId, "");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(Messages.FielIsEmpty, await TestApi.Messages(response));
        }

        [Fact]
        public async Task ImportCsv_HeaderOnly_ImportsNothing()
        {
            var client = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(client, "Import header only");

            var response = await ImportCsv(client, trackerId, "Note,Amount\n");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(await TestApi.ListEntries(client, trackerId));
        }

        [Fact]
        public async Task ImportCsv_FillsInCalculatedFields()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Import calculated");
            await TestApi.CreateField(client, trackerId, "Base", DataTypes.Number);
            await TestApi.CreateCalculatedField(client, trackerId, "Doubled", "{Base} * 2", DataTypes.Number);

            var response = await ImportCsv(client, trackerId, "Base\n7\n");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var entries = await TestApi.ListEntries(client, trackerId);
            Assert.Equal(14, TestApi.ValueOf(entries[0], "Doubled")!.Value.GetDouble());
        }

        [Fact]
        public async Task ImportCsv_OnATrackerOwnedBySomeoneElse_ReturnsForbidden()
        {
            var owner = await OwnerClient();
            var trackerId = await TrackerWithNoteAndAmount(owner, "Import guard");

            var stranger = await _factory.NewUserClient("csvwriter");
            var response = await ImportCsv(stranger, trackerId, "Note,Amount\nfirst,1\n");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await TestApi.ListEntries(owner, trackerId));
        }

        [Fact]
        public async Task ExportThenImport_RoundTripsEveryFieldType()
        {
            var client = await OwnerClient();
            var trackerId = await TestApi.CreateTracker(client, "Round trip");
            await TestApi.CreateField(client, trackerId, "Note", DataTypes.String);
            await TestApi.CreateField(client, trackerId, "Amount", DataTypes.Number);
            await TestApi.CreateField(client, trackerId, "Day", DataTypes.Date);
            await TestApi.CreateField(client, trackerId, "Duration", DataTypes.TimeSpan);
            await TestApi.CreateField(client, trackerId, "Done", DataTypes.Bool);
            await TestApi.CreateEntry(client, trackerId, new()
            {
                ["Note"] = "hi",
                ["Amount"] = "5",
                ["Day"] = "2026-01-01",
                ["Duration"] = "04:00:00",
                ["Done"] = "true"
            });

            var exported = await ExportCsv(client, trackerId);
            var csv = await exported.Content.ReadAsStringAsync();
            var response = await ImportCsv(client, trackerId, csv);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // The re-imported row has to match the original, or an export is not a backup.
            var entries = await TestApi.ListEntries(client, trackerId);
            Assert.Equal(2, entries.Count);
            foreach (var entry in entries)
            {
                Assert.Equal("hi", TestApi.ValueOf(entry, "Note")!.Value.GetString());
                Assert.Equal(5, TestApi.ValueOf(entry, "Amount")!.Value.GetDouble());
                Assert.StartsWith("2026-01-01", TestApi.ValueOf(entry, "Day")!.Value.GetString());
                Assert.Equal(TimeSpan.FromHours(4), TimeSpan.Parse(TestApi.ValueOf(entry, "Duration")!.Value.GetString()!));
                Assert.True(TestApi.ValueOf(entry, "Done")!.Value.GetBoolean());
            }
        }
    }
}
