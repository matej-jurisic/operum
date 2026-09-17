namespace Operum.Model.Constants.Analytics
{
    public static class GoalDirections
    {
        // Progress = value/target; achieved once value >= target.
        public const string HigherIsBetter = "HigherIsBetter";

        // Progress = target/value; achieved while value <= target.
        public const string LowerIsBetter = "LowerIsBetter";

        public static readonly HashSet<string> All = [HigherIsBetter, LowerIsBetter];

        public static bool IsValid(string direction) => All.Contains(direction);
    }
}
