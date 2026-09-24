namespace QuickFlow.Domain.Common;

/// <summary>Source of the current time, injectable so rules stay testable.</summary>
public interface IClock
{
    /// <summary>Current instant in UTC.</summary>
    DateTime UtcNow { get; }

    /// <summary>Current calendar date in the user's (server's) local time zone.</summary>
    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
