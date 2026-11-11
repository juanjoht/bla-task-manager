namespace TaskManager.Application.Abstractions.Time;

public interface IClock
{
    /// <summary>Current instant in UTC, used for audit fields.</summary>
    DateTime UtcNow { get; }

    /// <summary>Current date in the configured business time zone, used for date rules.</summary>
    DateOnly Today { get; }
}
