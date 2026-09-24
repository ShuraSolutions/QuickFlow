namespace QuickFlow.Domain.Habits;

/// <summary>Completion progress of a habit as of a given day.</summary>
/// <param name="CompletedToday">A completion exists for today.</param>
/// <param name="CompletedThisPeriod">Daily: completed today. Weekly: completed at least once this ISO week (Mon–Sun).</param>
/// <param name="CurrentStreak">Consecutive periods (days or weeks) with a completion, ending in the current period, or in the previous one if the current is not done yet.</param>
/// <param name="TotalCompletions">Number of completion records.</param>
/// <param name="LastCompletedDate">Most recent completion date, if any.</param>
public record HabitProgress(bool CompletedToday, bool CompletedThisPeriod, int CurrentStreak, int TotalCompletions, DateOnly? LastCompletedDate);

public static class HabitProgressCalculator
{
    public static HabitProgress Compute(HabitFrequency frequency, IEnumerable<DateOnly> completionDates, DateOnly today)
    {
        var dates = completionDates.Where(d => d <= today).ToHashSet();
        var completedToday = dates.Contains(today);
        var last = dates.Count == 0 ? (DateOnly?)null : dates.Max();

        if (frequency == HabitFrequency.Weekly)
        {
            var weeks = dates.Select(WeekStart).ToHashSet();
            var current = WeekStart(today);
            return new HabitProgress(completedToday, weeks.Contains(current),
                Streak(weeks, current, 7), dates.Count, last);
        }

        return new HabitProgress(completedToday, completedToday, Streak(dates, today, 1), dates.Count, last);
    }

    /// <summary>Monday of the ISO week containing <paramref name="d"/>.</summary>
    public static DateOnly WeekStart(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    private static int Streak(HashSet<DateOnly> periods, DateOnly current, int step)
    {
        var cursor = periods.Contains(current) ? current : current.AddDays(-step);
        var streak = 0;
        while (periods.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-step);
        }
        return streak;
    }
}
