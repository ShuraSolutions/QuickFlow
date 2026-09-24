namespace QuickFlow.Domain.Common;

public static class Time
{
    /// <summary>
    /// Normalizes an incoming date-time to UTC. Values without an offset (Kind Unspecified)
    /// are taken as the user's (server's) local time.
    /// </summary>
    public static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    public static DateTime? ToUtc(DateTime? value) => value is { } v ? ToUtc(v) : null;

    /// <summary>Local calendar date of a UTC instant.</summary>
    public static DateOnly LocalDate(DateTime utc) => DateOnly.FromDateTime(ToUtc(utc).ToLocalTime());
}
