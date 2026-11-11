using TaskManager.Application.Abstractions.Time;

namespace TaskManager.Infrastructure.Time;

public sealed class SystemClock(TimeProvider timeProvider, TimeZoneInfo businessTimeZone) : IClock
{
    public DateTime UtcNow => throw new NotImplementedException();

    public DateOnly Today => throw new NotImplementedException();
}
