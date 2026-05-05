using Operum.Model.Constants;
using Operum.Model.Models;

namespace Operum.Service.Domain.Notifications
{
    public static class NotificationScheduleResolver
    {
        private static readonly DateOnly Anchor = new(2000, 1, 1);

        public static bool IsDue(
            NotificationEvent ev,
            DateTime nowUtc,
            DateTime? lastEvaluatedAtUtc,
            TimeZoneInfo userTz)
        {
            if (ev.EventType == NotificationEventType.Triggered)
                return true;

            var from = lastEvaluatedAtUtc ?? nowUtc.AddHours(-24);
            return GetScheduledInstantsInRange(ev, from, nowUtc, userTz).Any();
        }

        private static IEnumerable<DateTime> GetScheduledInstantsInRange(
            NotificationEvent ev,
            DateTime fromUtc,
            DateTime toUtc,
            TimeZoneInfo userTz)
        {
            var fromLocal = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, userTz);
            var toLocal = TimeZoneInfo.ConvertTimeFromUtc(toUtc, userTz);
            var fromDate = DateOnly.FromDateTime(fromLocal);
            var toDate = DateOnly.FromDateTime(toLocal);

            return ev.EventType switch
            {
                NotificationEventType.Day => GetDayInstances(ev, fromDate, toDate, fromUtc, toUtc, userTz),
                NotificationEventType.Week => GetWeekInstances(ev, fromDate, toDate, fromUtc, toUtc, userTz),
                NotificationEventType.Month => GetMonthInstances(ev, fromDate, toDate, fromUtc, toUtc, userTz),
                _ => []
            };
        }

        private static IEnumerable<DateTime> GetDayInstances(
            NotificationEvent ev, DateOnly fromDate, DateOnly toDate,
            DateTime fromUtc, DateTime toUtc, TimeZoneInfo userTz)
        {
            var interval = ev.IntervalDays ?? 1;
            var timeOfDay = ev.TimeOfDay ?? TimeOnly.MinValue;
            var skipWeekends = ev.SkipWeekendsDay ?? false;

            for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            {
                var dayIndex = d.DayNumber - Anchor.DayNumber;
                if (dayIndex % interval != 0) continue;
                if (skipWeekends && IsWeekend(d.DayOfWeek)) continue;

                var instant = ToUtcInstant(d, timeOfDay, userTz);
                if (instant > fromUtc && instant <= toUtc)
                    yield return instant;
            }
        }

        private static IEnumerable<DateTime> GetWeekInstances(
            NotificationEvent ev, DateOnly fromDate, DateOnly toDate,
            DateTime fromUtc, DateTime toUtc, TimeZoneInfo userTz)
        {
            var interval = ev.IntervalWeeks ?? 1;
            var mask = ev.DaysOfWeekMask ?? 0b0011111; // Mon-Fri default
            var timeOfDay = ev.TimeOfDay ?? TimeOnly.MinValue;

            for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            {
                if (!DaysOfWeekMaskHelper.Contains(mask, d.DayOfWeek)) continue;

                var weekIndex = (d.DayNumber - Anchor.DayNumber) / 7;
                if (weekIndex % interval != 0) continue;

                var instant = ToUtcInstant(d, timeOfDay, userTz);
                if (instant > fromUtc && instant <= toUtc)
                    yield return instant;
            }
        }

        private static IEnumerable<DateTime> GetMonthInstances(
            NotificationEvent ev, DateOnly fromDate, DateOnly toDate,
            DateTime fromUtc, DateTime toUtc, TimeZoneInfo userTz)
        {
            var timeOfDay = ev.TimeOfDay ?? TimeOnly.MinValue;
            var skipWeekends = ev.SkipWeekendsMonth ?? false;
            var lastDay = ev.LastDayOfMonth ?? false;

            var y = fromDate.Year;
            var m = fromDate.Month;
            var endYear = toDate.Year;
            var endMonth = toDate.Month;
            while (y < endYear || (y == endYear && m <= endMonth))
            {
                var daysInMonth = DateTime.DaysInMonth(y, m);
                var targetDay = lastDay ? daysInMonth : Math.Min(ev.DayOfMonth ?? 1, daysInMonth);
                var targetDate = new DateOnly(y, m, targetDay);

                if (skipWeekends && IsWeekend(targetDate.DayOfWeek))
                {
                    AdvanceMonth(ref y, ref m);
                    continue;
                }

                if (targetDate >= fromDate && targetDate <= toDate)
                {
                    var instant = ToUtcInstant(targetDate, timeOfDay, userTz);
                    if (instant > fromUtc && instant <= toUtc)
                        yield return instant;
                }

                AdvanceMonth(ref y, ref m);
            }
        }

        private static DateTime ToUtcInstant(DateOnly date, TimeOnly time, TimeZoneInfo tz)
        {
            var local = date.ToDateTime(time);
            return TimeZoneInfo.ConvertTimeToUtc(local, tz);
        }

        private static bool IsWeekend(DayOfWeek d) =>
            d == DayOfWeek.Saturday || d == DayOfWeek.Sunday;

        private static void AdvanceMonth(ref int year, ref int month)
        {
            if (++month > 12) { month = 1; year++; }
        }
    }
}
