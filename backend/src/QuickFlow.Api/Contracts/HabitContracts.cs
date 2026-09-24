using QuickFlow.Domain.Habits;

namespace QuickFlow.Api.Contracts;

/// <summary>A habit with its completion progress as of today.</summary>
/// <param name="CompletedThisPeriod">Daily: completed today. Weekly: completed at least once this ISO week (Mon–Sun).</param>
/// <param name="CurrentStreak">Consecutive days (Daily) or weeks (Weekly) with a completion.</param>
public record HabitResponse(
    int Id,
    string Name,
    string? Description,
    HabitFrequency Frequency,
    DateTime CreatedAt,
    bool IsActive,
    bool CompletedToday,
    bool CompletedThisPeriod,
    int CurrentStreak,
    int TotalCompletions,
    DateOnly? LastCompletedDate)
{
    public static HabitResponse From(Habit h, DateOnly today)
    {
        var p = h.Progress(today);
        return new(h.Id, h.Name, h.Description, h.Frequency, h.CreatedAt, h.IsActive,
            p.CompletedToday, p.CompletedThisPeriod, p.CurrentStreak, p.TotalCompletions, p.LastCompletedDate);
    }
}

/// <summary>A completion event of a habit.</summary>
public record HabitCompletionResponse(int Id, int HabitId, DateOnly CompletionDate, DateTime CreatedAt)
{
    public static HabitCompletionResponse From(HabitCompletion c) => new(c.Id, c.HabitId, c.CompletionDate, c.CreatedAt);
}

/// <summary>Body for creating or updating a habit.</summary>
public class HabitRequest
{
    /// <summary>Required, at most 150 characters.</summary>
    public string? Name { get; set; }
    /// <summary>Optional, at most 2000 characters.</summary>
    public string? Description { get; set; }
    /// <summary>Daily or Weekly. Defaults to Daily on create; unchanged on update when omitted.</summary>
    public HabitFrequency? Frequency { get; set; }
}

/// <summary>Body for completing a habit.</summary>
public class HabitCompletionRequest
{
    /// <summary>Date of the completion (yyyy-MM-dd). Defaults to today; cannot be in the future.</summary>
    public DateOnly? Date { get; set; }
}
