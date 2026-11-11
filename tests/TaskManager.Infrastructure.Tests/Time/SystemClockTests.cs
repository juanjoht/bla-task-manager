using FluentAssertions;
using TaskManager.Infrastructure.Tests.Common;
using TaskManager.Infrastructure.Time;

namespace TaskManager.Infrastructure.Tests.Time;

public class SystemClockTests
{
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

    private static SystemClock ClockAt(DateTime utcNow, TimeZoneInfo zone) =>
        new(new FixedTimeProvider(new DateTimeOffset(utcNow, TimeSpan.Zero)), zone);

    [Fact]
    public void UtcNow_ReturnsTheCurrentInstantInUtc()
    {
        var utcNow = new DateTime(2026, 11, 11, 15, 30, 0, DateTimeKind.Utc);

        var clock = ClockAt(utcNow, Bogota);

        clock.UtcNow.Should().Be(utcNow);
        clock.UtcNow.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Today_DuringTheDayInBogota_MatchesTheUtcDate()
    {
        // 10:00 in Bogotá = 15:00 UTC, same calendar day.
        var clock = ClockAt(new DateTime(2026, 11, 11, 15, 0, 0, DateTimeKind.Utc), Bogota);

        clock.Today.Should().Be(new DateOnly(2026, 11, 11));
    }

    [Fact]
    public void Today_At9PmInBogota_IsStillTheLocalDate()
    {
        // 21:00 in Bogotá = 02:00 of the next day in UTC: the bug found in Phase 2.
        var clock = ClockAt(new DateTime(2026, 11, 12, 2, 0, 0, DateTimeKind.Utc), Bogota);

        clock.Today.Should().Be(new DateOnly(2026, 11, 11));
    }
}
