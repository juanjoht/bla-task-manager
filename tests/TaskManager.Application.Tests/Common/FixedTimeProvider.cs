namespace TaskManager.Application.Tests.Common;

/// <summary>A clock frozen at a known instant so tests are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
