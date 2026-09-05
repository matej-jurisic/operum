using Operum.Model.Constants;
using Operum.Model.Models;
using Operum.Service.Domain.Notifications;

namespace Operum.Tests.Tests.Notifications
{
    // NotificationScheduleResolver is pure and unit-testable but had zero coverage before
    // this file -- it is the one piece every Frequency notification depends on, so a bug
    // here silently breaks (or over-fires) every Day/Week/Month notification. Dates are
    // located relative to a fixed reference and walked to the desired weekday with .NET's
    // own DayOfWeek rather than hardcoded, so the tests don't depend on memorized calendar
    // trivia.
    public class NotificationScheduleResolverTests
    {
        private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

        private static DateTime NextWeekday(DateTime from, DayOfWeek dow)
        {
            var d = from;
            while (d.DayOfWeek != dow) d = d.AddDays(1);
            return d;
        }

        private static NotificationEvent DayEvent(int intervalDays, TimeOnly timeOfDay, bool skipWeekends) => new()
        {
            EventType = NotificationEventType.Day,
            TimeOfDay = timeOfDay,
            IntervalDays = intervalDays,
            SkipWeekendsDay = skipWeekends,
        };

        private static NotificationEvent WeekEvent(int intervalWeeks, TimeOnly timeOfDay, params DayOfWeek[] days) => new()
        {
            EventType = NotificationEventType.Week,
            TimeOfDay = timeOfDay,
            IntervalWeeks = intervalWeeks,
            DaysOfWeekMask = DaysOfWeekMaskHelper.FromStringList(days.Select(ToMaskName)),
        };

        private static string ToMaskName(DayOfWeek d) => d switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => throw new ArgumentOutOfRangeException(nameof(d)),
        };

        private static NotificationEvent MonthEvent(int? dayOfMonth, bool lastDayOfMonth, TimeOnly timeOfDay, bool skipWeekends) => new()
        {
            EventType = NotificationEventType.Month,
            TimeOfDay = timeOfDay,
            DayOfMonth = dayOfMonth,
            LastDayOfMonth = lastDayOfMonth,
            SkipWeekendsMonth = skipWeekends,
        };

        [Fact]
        public void IsDue_Triggered_AlwaysDue()
        {
            var ev = new NotificationEvent { EventType = NotificationEventType.Triggered };
            Assert.True(NotificationScheduleResolver.IsDue(ev, DateTime.UtcNow, DateTime.UtcNow, Utc));
        }

        [Fact]
        public void IsDue_Day_SkipWeekends_SkipsSaturdayAndSunday_FiresOnMonday()
        {
            var ev = DayEvent(1, new TimeOnly(9, 0), skipWeekends: true);

            var friday = NextWeekday(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DayOfWeek.Friday);
            var lastEvaluated = friday.AddHours(10); // Friday 10:00, after that day's tick
            var saturday930 = friday.AddDays(1).AddHours(9).AddMinutes(30);
            var sunday930 = friday.AddDays(2).AddHours(9).AddMinutes(30);
            var monday930 = friday.AddDays(3).AddHours(9).AddMinutes(30);

            Assert.False(NotificationScheduleResolver.IsDue(ev, saturday930, lastEvaluated, Utc));
            Assert.False(NotificationScheduleResolver.IsDue(ev, sunday930, lastEvaluated, Utc));
            Assert.True(NotificationScheduleResolver.IsDue(ev, monday930, lastEvaluated, Utc));
        }

        [Fact]
        public void IsDue_Day_Interval_OnlyFiresEveryNthDay()
        {
            var ev = DayEvent(3, new TimeOnly(9, 0), skipWeekends: false);
            var start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

            var dueDays = Enumerable.Range(0, 10)
                .Select(offset => (offset, due: NotificationScheduleResolver.IsDue(
                    ev,
                    start.AddDays(offset).AddMinutes(1),
                    start.AddDays(offset).AddMinutes(-1),
                    Utc)))
                .Where(x => x.due)
                .Select(x => x.offset)
                .ToList();

            // Anchored on 2000-01-01, so `start`'s own phase in the 3-day cycle is whatever
            // it is -- what matters is every due offset shares that same phase (spaced
            // exactly 3 days apart), never firing on an off day.
            Assert.NotEmpty(dueDays);
            Assert.All(dueDays, d => Assert.Equal(0, (d - dueDays[0]) % 3));
        }

        [Fact]
        public void IsDue_Day_CatchesUpAcrossMissedTicks()
        {
            // Service was down for a week; the next run must still recognize the instant as due
            // rather than requiring the tick to land inside a tiny polling window.
            var ev = DayEvent(1, new TimeOnly(9, 0), skipWeekends: false);
            var lastEvaluated = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
            var resumedAt = new DateTime(2026, 1, 8, 15, 0, 0, DateTimeKind.Utc);

            Assert.True(NotificationScheduleResolver.IsDue(ev, resumedAt, lastEvaluated, Utc));
        }

        [Fact]
        public void IsDue_Day_DoesNotRefireWithinSameScheduledInstant()
        {
            var ev = DayEvent(1, new TimeOnly(9, 0), skipWeekends: false);
            var scheduled = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

            // Evaluator already ran right at (or after) the scheduled tick and recorded it.
            Assert.False(NotificationScheduleResolver.IsDue(ev, scheduled.AddMinutes(5), scheduled.AddMinutes(1), Utc));
        }

        [Fact]
        public void IsDue_Week_RespectsIntervalAndDaySelection()
        {
            var ev = WeekEvent(2, new TimeOnly(9, 0), DayOfWeek.Tuesday, DayOfWeek.Thursday);

            var week1Tuesday = NextWeekday(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DayOfWeek.Tuesday);

            // Walk 6 consecutive weekly Tuesdays and Thursdays; exactly every other one should fire,
            // and the two selected weekdays within a firing week must agree with each other.
            var tuesdayResults = new List<bool>();
            var thursdayResults = new List<bool>();
            for (var i = 0; i < 6; i++)
            {
                var tue = week1Tuesday.AddDays(7 * i);
                var thu = tue.AddDays(2);

                tuesdayResults.Add(NotificationScheduleResolver.IsDue(ev, tue.AddHours(9).AddMinutes(1), tue.AddHours(9).AddMinutes(-1), Utc));
                thursdayResults.Add(NotificationScheduleResolver.IsDue(ev, thu.AddHours(9).AddMinutes(1), thu.AddHours(9).AddMinutes(-1), Utc));
            }

            Assert.Equal(tuesdayResults, thursdayResults);
            // Not every week fires (biweekly), and not zero of them do.
            Assert.Contains(true, tuesdayResults);
            Assert.Contains(false, tuesdayResults);
        }

        [Fact]
        public void IsDue_Week_UnselectedDayNeverFires()
        {
            var ev = WeekEvent(1, new TimeOnly(9, 0), DayOfWeek.Monday);
            var wednesday = NextWeekday(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DayOfWeek.Wednesday);

            Assert.False(NotificationScheduleResolver.IsDue(
                ev, wednesday.AddHours(9).AddMinutes(1), wednesday.AddHours(9).AddMinutes(-1), Utc));
        }

        [Fact]
        public void IsDue_Month_LastDayOfMonth_HandlesFebruary()
        {
            var ev = MonthEvent(null, lastDayOfMonth: true, new TimeOnly(23, 0), skipWeekends: false);

            // Non-leap Feb has 28 days; leap Feb has 29. Both must resolve to that month's actual
            // last day, not a fixed day-of-month.
            var feb2026 = new DateTime(2026, 2, 28, 23, 0, 0, DateTimeKind.Utc); // 2026 is not a leap year
            var feb2028 = new DateTime(2028, 2, 29, 23, 0, 0, DateTimeKind.Utc); // 2028 is a leap year

            Assert.True(NotificationScheduleResolver.IsDue(ev, feb2026.AddMinutes(1), feb2026.AddMinutes(-1), Utc));
            Assert.False(NotificationScheduleResolver.IsDue(ev, new DateTime(2026, 2, 27, 23, 1, 0, DateTimeKind.Utc), new DateTime(2026, 2, 27, 22, 59, 0, DateTimeKind.Utc), Utc));

            Assert.True(NotificationScheduleResolver.IsDue(ev, feb2028.AddMinutes(1), feb2028.AddMinutes(-1), Utc));
        }

        [Fact]
        public void IsDue_Month_SkipWeekends_DropsOccurrenceEntirely()
        {
            // Find a month whose target day-of-month lands on a weekend, and confirm that month
            // is skipped outright rather than shifted to the nearest weekday.
            var ev = MonthEvent(15, lastDayOfMonth: false, new TimeOnly(9, 0), skipWeekends: true);

            DateTime? weekendTarget = null;
            for (var m = 1; m <= 24; m++)
            {
                var candidate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc).AddMonths(m - 1);
                if (candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    weekendTarget = candidate;
                    break;
                }
            }

            Assert.NotNull(weekendTarget);
            var target = weekendTarget!.Value.AddHours(9);

            // No occurrence that month at all -- neither on the 15th nor shifted elsewhere.
            var monthStart = new DateTime(target.Year, target.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            Assert.False(NotificationScheduleResolver.IsDue(ev, monthEnd, monthStart, Utc));
        }

        [Fact]
        public void IsDue_Month_DayBeyondMonthLength_ClampsRatherThanOverflows()
        {
            // DayOfMonth=31 in a 30-day (or 28/29-day) month must resolve within that month,
            // not roll over into the next one.
            var ev = MonthEvent(31, lastDayOfMonth: false, new TimeOnly(9, 0), skipWeekends: false);

            var aprilStart = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var aprilEnd = aprilStart.AddMonths(1);

            Assert.True(NotificationScheduleResolver.IsDue(ev, aprilEnd, aprilStart, Utc));

            // And it must not also fire again on May 1st as a rolled-over "April 31st".
            var may1 = new DateTime(2026, 5, 1, 9, 1, 0, DateTimeKind.Utc);
            var april30Evening = new DateTime(2026, 4, 30, 23, 59, 0, DateTimeKind.Utc);
            Assert.False(NotificationScheduleResolver.IsDue(ev, may1, april30Evening, Utc));
        }
    }
}
