using Operum.Service.Domain.Notifications;

namespace Operum.Tests.Tests.Notifications
{
    // NotificationMessageBuilder is the one place a user-authored push body ("Amount is
    // {value}") gets combined with the generic default ("Condition met") and the evaluator's
    // tokens. Pure and unit-testable in isolation from the evaluator itself.
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
    }
}
