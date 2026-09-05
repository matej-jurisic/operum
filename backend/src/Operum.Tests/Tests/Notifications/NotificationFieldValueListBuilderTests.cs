using Operum.Model.Constants.Fields;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;

namespace Operum.Tests.Tests.Notifications
{
    // NotificationFieldValueListBuilder renders the {fieldValueList} push-body token for
    // Entry-mode notifications: the "Display" fields the user picked, one line per newly-matched
    // entry. Pure and unit-testable against hand-built Entry/FieldValue graphs, no DB needed.
    public class NotificationFieldValueListBuilderTests
    {
        private static Field MakeField(string id, string name, string type) =>
            new() { Id = id, Name = name, Type = type };

        private static Entry MakeEntry(params FieldValue[] values) =>
            new() { FieldValues = values.ToList() };

        [Fact]
        public void Build_NoDisplayFields_ReturnsEmpty()
        {
            var field = MakeField("f1", "Status", DataTypes.String);
            var entry = MakeEntry(new FieldValue { FieldId = "f1", Field = field, StringValue = "Open" });

            var result = NotificationFieldValueListBuilder.Build([entry], []);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Build_NoEntries_ReturnsEmpty()
        {
            var result = NotificationFieldValueListBuilder.Build([], ["f1"]);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Build_SingleEntrySingleField_RendersNameColonValue()
        {
            var field = MakeField("f1", "Status", DataTypes.String);
            var entry = MakeEntry(new FieldValue { FieldId = "f1", Field = field, StringValue = "Open" });

            var result = NotificationFieldValueListBuilder.Build([entry], ["f1"]);

            Assert.Equal("Status: Open", result);
        }

        [Fact]
        public void Build_MultipleFields_JoinsWithCommaInOrderRequested()
        {
            var statusField = MakeField("f1", "Status", DataTypes.String);
            var priorityField = MakeField("f2", "Priority", DataTypes.String);
            var entry = MakeEntry(
                new FieldValue { FieldId = "f2", Field = priorityField, StringValue = "High" },
                new FieldValue { FieldId = "f1", Field = statusField, StringValue = "Open" });

            var result = NotificationFieldValueListBuilder.Build([entry], ["f1", "f2"]);

            Assert.Equal("Status: Open, Priority: High", result);
        }

        [Fact]
        public void Build_MultipleEntries_OneLinePerEntry()
        {
            var field = MakeField("f1", "Status", DataTypes.String);
            var e1 = MakeEntry(new FieldValue { FieldId = "f1", Field = field, StringValue = "Open" });
            var e2 = MakeEntry(new FieldValue { FieldId = "f1", Field = field, StringValue = "Closed" });

            var result = NotificationFieldValueListBuilder.Build([e1, e2], ["f1"]);

            Assert.Equal("Status: Open\nStatus: Closed", result);
        }

        [Fact]
        public void Build_MoreThanFiveEntries_CapsAndAppendsRemainingCount()
        {
            var field = MakeField("f1", "Status", DataTypes.String);
            var entries = Enumerable.Range(1, 7)
                .Select(i => MakeEntry(new FieldValue { FieldId = "f1", Field = field, StringValue = $"v{i}" }))
                .ToList();

            var result = NotificationFieldValueListBuilder.Build(entries, ["f1"]);

            Assert.Equal("Status: v1\nStatus: v2\nStatus: v3\nStatus: v4\nStatus: v5\nand 2 more", result);
        }

        [Fact]
        public void Build_MissingFieldValueOnEntry_SkipsThatField()
        {
            var statusField = MakeField("f1", "Status", DataTypes.String);
            var entry = MakeEntry(new FieldValue { FieldId = "f1", Field = statusField, StringValue = "Open" });

            var result = NotificationFieldValueListBuilder.Build([entry], ["f1", "f2"]);

            Assert.Equal("Status: Open", result);
        }
    }
}
