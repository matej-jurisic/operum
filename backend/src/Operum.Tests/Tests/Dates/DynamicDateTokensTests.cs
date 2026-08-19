using Operum.Model.Constants;

namespace Operum.Tests.Tests.Dates
{
    public class DynamicDateTokensTests
    {
        // UTC+2 with no DST in the tested ranges keeps the expected values readable.
        private static readonly TimeZoneInfo Plus2 =
            TimeZoneInfo.CreateCustomTimeZone("Test/Plus2", TimeSpan.FromHours(2), "Test +2", "Test +2");

        private static DateTime LocalNow(TimeZoneInfo tz) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        private static DateTime Resolve(string token, TimeZoneInfo tz)
        {
            var resolved = DynamicDateTokens.Resolve(token, tz);
            Assert.NotNull(resolved);
            return resolved!.Value;
        }

        [Theory]
        [InlineData("today")]
        [InlineData("start_of_month")]
        [InlineData("end_of_year")]
        public void BareAnchorMeansOffsetZero(string token)
        {
            Assert.Equal(Resolve(token, TimeZoneInfo.Utc), Resolve($"{token}:0", TimeZoneInfo.Utc));
        }

        [Fact]
        public void StartOfMonthOffsetGoesBackWholeMonths()
        {
            var now = LocalNow(TimeZoneInfo.Utc);
            var expected = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);

            Assert.Equal(expected, Resolve("start_of_month:-1", TimeZoneInfo.Utc));
        }

        [Fact]
        public void EndOfMonthIsTheLastInstantOfThatMonth()
        {
            var startOfLastMonth = Resolve("start_of_month:-1", TimeZoneInfo.Utc);
            var endOfLastMonth = Resolve("end_of_month:-1", TimeZoneInfo.Utc);

            // The two together must cover exactly the month, leaving no gap at either edge.
            Assert.Equal(Resolve("start_of_month", TimeZoneInfo.Utc), endOfLastMonth.AddTicks(1));
            Assert.True(endOfLastMonth > startOfLastMonth);
        }

        [Fact]
        public void EndOfMonthKeepsTheFinalSecondOfTheDay()
        {
            var endOfMonth = Resolve("end_of_month", TimeZoneInfo.Utc);

            // The old 23:59:59 bound silently excluded anything later in that second.
            Assert.Equal(23, endOfMonth.Hour);
            Assert.Equal(59, endOfMonth.Minute);
            Assert.Equal(59, endOfMonth.Second);
            Assert.True(endOfMonth.Millisecond > 0);
        }

        [Fact]
        public void LastMonthRangeDoesNotOverlapThisMonth()
        {
            var endOfLastMonth = Resolve("end_of_month:-1", TimeZoneInfo.Utc);
            var startOfThisMonth = Resolve("start_of_month", TimeZoneInfo.Utc);

            Assert.True(endOfLastMonth < startOfThisMonth);
        }

        [Fact]
        public void AnchorsAreBuiltFromLocalMidnightNotUtcMidnight()
        {
            var startOfMonth = Resolve("start_of_month", Plus2);
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(startOfMonth, Plus2);

            Assert.Equal(1, localStart.Day);
            Assert.Equal(TimeSpan.Zero, localStart.TimeOfDay);
            // A UTC-built boundary would have landed on 00:00Z instead.
            Assert.Equal(22, startOfMonth.Hour);
        }

        [Fact]
        public void TodayOffsetWalksWholeDays()
        {
            var today = Resolve("today", Plus2);

            Assert.Equal(today.AddDays(-1), Resolve("today:-1", Plus2));
            Assert.Equal(today.AddDays(1), Resolve("today:1", Plus2));
        }

        [Fact]
        public void EndOfDayClosesTheDayItStartsOn()
        {
            Assert.Equal(Resolve("today", Plus2), Resolve("end_of_day:-1", Plus2).AddTicks(1));
        }

        [Fact]
        public void StartOfWeekIsMonday()
        {
            var startOfWeek = TimeZoneInfo.ConvertTimeFromUtc(Resolve("start_of_week", Plus2), Plus2);

            Assert.Equal(DayOfWeek.Monday, startOfWeek.DayOfWeek);
            Assert.Equal(TimeSpan.Zero, startOfWeek.TimeOfDay);
        }

        [Fact]
        public void WeekOffsetGoesBackSevenDays()
        {
            Assert.Equal(Resolve("start_of_week", Plus2).AddDays(-7), Resolve("start_of_week:-1", Plus2));
        }

        [Fact]
        public void LookbackHoursIgnoreCalendarBoundaries()
        {
            var resolved = Resolve("last_n_hours:3", Plus2);

            Assert.InRange(DateTime.UtcNow - resolved, TimeSpan.FromHours(3) - TimeSpan.FromMinutes(1), TimeSpan.FromHours(3) + TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void LookbackDaysSnapToLocalMidnight()
        {
            var resolved = TimeZoneInfo.ConvertTimeFromUtc(Resolve("last_n_days:7", Plus2), Plus2);

            Assert.Equal(TimeSpan.Zero, resolved.TimeOfDay);
            Assert.Equal(LocalNow(Plus2).Date.AddDays(-7), resolved);
        }

        [Theory]
        [InlineData("today")]
        [InlineData("today:-1")]
        [InlineData("end_of_day:3")]
        [InlineData("start_of_month:-12")]
        [InlineData("last_n_days:7")]
        [InlineData("last_n_hours:-3")]
        public void ValidTokensAreAccepted(string token) => Assert.True(DynamicDateTokens.IsValid(token));

        [Theory]
        [InlineData("")]
        [InlineData("tomorrow")]
        [InlineData("start_of_fortnight")]
        [InlineData("today:abc")]
        [InlineData("last_n_days")]        // lookbacks require their argument
        [InlineData("last_n_days:0")]      // a zero-length lookback is meaningless
        [InlineData("2026-08-19T00:00:00Z")]
        public void InvalidTokensAreRejected(string token) => Assert.False(DynamicDateTokens.IsValid(token));

        [Fact]
        public void UnparseableTokensResolveToNull()
        {
            Assert.Null(DynamicDateTokens.Resolve("start_of_fortnight", TimeZoneInfo.Utc));
        }
    }
}
