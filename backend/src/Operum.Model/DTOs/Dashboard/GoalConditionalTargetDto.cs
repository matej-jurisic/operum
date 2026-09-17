namespace Operum.Model.DTOs.Dashboard
{
    // A clause not named in Conditions is a wildcard; a row with no conditions never applies.
    // Rows are evaluated in order, first full match wins and its Target replaces the widget
    // default. A condition naming a clause this placement no longer follows makes the row inert.
    public class GoalConditionalTargetDto
    {
        public Dictionary<string, string> Conditions { get; set; } = [];
        public string Target { get; set; } = string.Empty;
    }
}
