using Operum.Model.Constants.Fields;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;

namespace Operum.Tests.Tests.Notifications
{
    // Combines a user-authored push body ("Amount is {value}") with the generic default
    // ("Condition met") and the evaluator's tokens.
    public class NotificationMessageBuilderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Build_NoTemplate_ReturnsFallback(string? template)
        {
            var body = NotificationMessageBuilder.Build(template, "Condition met", new Dictionary<string, string>());

            Assert.Equal("Condition met", body);
        }

        [Fact]
        public void Build_TemplateWithToken_ReplacesToken()
        {
            var tokens = new Dictionary<string, string> { ["value"] = "42", ["tracker"] = "Weight" };

            var body = NotificationMessageBuilder.Build("{tracker}: value is now {value}", "Condition met", tokens);

            Assert.Equal("Weight: value is now 42", body);
        }

        [Fact]
        public void Build_UnknownToken_LeftAsIs()
        {
            var tokens = new Dictionary<string, string> { ["value"] = "42" };

            var body = NotificationMessageBuilder.Build("{value} - {somethingElse}", "Condition met", tokens);

            Assert.Equal("42 - {somethingElse}", body);
        }

        [Fact]
        public void Build_TemplateWithNoTokens_ReturnsTemplateVerbatim()
        {
            var body = NotificationMessageBuilder.Build("Check the tracker", "Condition met", new Dictionary<string, string> { ["value"] = "1" });

            Assert.Equal("Check the tracker", body);
        }

        private static Entry MakeEntry(string fieldName, string value) =>
            new()
            {
                FieldValues =
                [
                    new FieldValue
                    {
                        Field = new Field { Name = fieldName, Type = DataTypes.String },
                        StringValue = value,
                    },
                ],
            };

        [Fact]
        public void BuildForEntries_FieldToken_RendersOneLinePerEntry()
        {
            var entries = new List<Entry> { MakeEntry("Status", "Open"), MakeEntry("Status", "Closed") };

            var body = NotificationMessageBuilder.BuildForEntries(
                "{notification}: {status}", "fallback",
                new Dictionary<string, string> { ["notification"] = "Review" },
                entries, 2, ["Status"]);

            Assert.Equal("Review: Open\nReview: Closed", body);
        }

        [Fact]
        public void BuildForEntries_MoreThanCap_AppendsRemaining()
        {
            var entries = Enumerable.Range(0, 5).Select(i => MakeEntry("Status", $"s{i}")).ToList();

            var body = NotificationMessageBuilder.BuildForEntries(
                "{Status}", "fallback", new Dictionary<string, string>(), entries, 7, ["Status"]);

            Assert.EndsWith("and 2 more", body);
        }

        [Fact]
        public void BuildForEntries_NoFieldToken_RendersOnce()
        {
            var entries = new List<Entry> { MakeEntry("Status", "Open"), MakeEntry("Status", "Closed") };

            var body = NotificationMessageBuilder.BuildForEntries(
                "{count} need review", "fallback",
                new Dictionary<string, string> { ["count"] = "2" },
                entries, 2, ["Status"]);

            Assert.Equal("2 need review", body);
        }

        [Fact]
        public void BuildForEntries_MissingValue_RendersDash()
        {
            var entries = new List<Entry> { new() };

            var body = NotificationMessageBuilder.BuildForEntries(
                "{Status}", "fallback", new Dictionary<string, string>(), entries, 1, ["Status"]);

            Assert.Equal("-", body);
        }
    }
}
