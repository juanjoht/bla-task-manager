using TaskManager.Application.Abstractions.Time;

namespace TaskManager.Infrastructure.Time;

public sealed class SystemClock(TimeProvider timeProvider, TimeZoneInfo businessTimeZone) : IClock
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    // The same instant seen from the business time zone (e.g. 02:00 UTC = 21:00 of the previous day in Bogotá).
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), businessTimeZone).DateTime);
}
