using TaskManager.Application.Abstractions.Time;

namespace TaskManager.Application.Tests.Common;

/// <summary>A clock frozen at a known instant so tests are deterministic.</summary>
internal sealed class FixedClock(DateTime utcNow, DateOnly today) : IClock
{
    public DateTime UtcNow => utcNow;
    public DateOnly Today => today;
}
