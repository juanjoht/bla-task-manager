namespace TaskManager.Infrastructure.Tests.Common;

/// <summary>A system clock frozen at a known instant so tests are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
