namespace Operum.Model.Constants
{
    public static class DaysOfWeekMaskHelper
    {
        private static readonly string[] Names = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        public static int FromStringList(IEnumerable<string> days)
        {
            int mask = 0;
            foreach (var day in days)
            {
                var idx = Array.IndexOf(Names, day);
                if (idx >= 0)
                    mask |= (1 << idx);
            }
            return mask;
        }

        public static List<string> ToStringList(int mask)
        {
            var result = new List<string>();
            for (int i = 0; i < Names.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                    result.Add(Names[i]);
            }
            return result;
        }

        public static bool Contains(int mask, DayOfWeek day)
        {
            // DayOfWeek: Sun=0, Mon=1..Sat=6; mask: Mon=0..Sun=6
            int idx = day == DayOfWeek.Sunday ? 6 : (int)day - 1;
            return (mask & (1 << idx)) != 0;
        }
    }
}
