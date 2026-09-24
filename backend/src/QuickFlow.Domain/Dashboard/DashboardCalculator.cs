using QuickFlow.Domain.Common;
using QuickFlow.Domain.Habits;
using QuickFlow.Domain.Learning;
using QuickFlow.Domain.Plans;
using QuickFlow.Domain.Tasks;

namespace QuickFlow.Domain.Dashboard;

/// <summary>Task metrics over non-archived tasks.</summary>
/// <param name="CompletionPercentage">Done / Total × 100 (2 decimals).</param>
public record TaskSummary(int Total, int Todo, int InProgress, int Done, int DueToday, int Overdue, int CompletedToday, double CompletionPercentage);

/// <summary>Habit metrics over active habits.</summary>
/// <param name="CompletionPercentage">Active habits completed today / active habits × 100.</param>
public record HabitSummary(int Total, int Active, int CompletedToday, double CompletionPercentage);

/// <summary>Plan metrics by computed status.</summary>
/// <param name="AverageActiveCompletionPercentage">Mean completion % of in-progress plans (0 when none).</param>
public record PlanSummary(int Total, int InProgress, int Upcoming, int Completed, double AverageActiveCompletionPercentage);

/// <summary>Learning snapshot.</summary>
/// <param name="MilestonesCompletedLast7Days">Milestones completed today or in the 6 days before.</param>
public record LearningSummary(int TotalCards, int NotStarted, int InProgress, int Completed,
    int MilestonesTotal, int MilestonesDone, double MilestoneCompletionPercentage, int MilestonesCompletedLast7Days);

public record DashboardSummary(TaskSummary Tasks, HabitSummary Habits, PlanSummary Plans, LearningSummary Learning);

/// <summary>Dashboard metrics (FR-09), computed from the underlying entities so they always match the data.</summary>
public static class DashboardCalculator
{
    public static DashboardSummary Compute(IEnumerable<TaskItem> tasks, IEnumerable<Habit> habits,
        IEnumerable<PlanProgress> plans, IEnumerable<LearningCard> cards, DateOnly today) =>
        new(Tasks(tasks, today), Habits(habits, today), Plans(plans), Learning(cards, today));

    public static TaskSummary Tasks(IEnumerable<TaskItem> tasks, DateOnly today)
    {
        var active = tasks.Where(t => !t.IsArchived).ToList();
        var done = active.Count(t => t.Status == TaskItemStatus.Done);
        return new TaskSummary(
            active.Count,
            active.Count(t => t.Status == TaskItemStatus.Todo),
            active.Count(t => t.Status == TaskItemStatus.InProgress),
            done,
            active.Count(t => IsDueToday(t, today)),
            active.Count(t => t.IsOverdue(today)),
            active.Count(t => IsCompletedOn(t, today)),
            Percent.Of(done, active.Count));
    }

    public static bool IsDueToday(TaskItem t, DateOnly today) => !t.IsArchived && t.DueDate == today;

    public static bool IsCompletedOn(TaskItem t, DateOnly day) =>
        !t.IsArchived && t.Status == TaskItemStatus.Done && t.CompletedAt is { } c && Time.LocalDate(c) == day;

    public static HabitSummary Habits(IEnumerable<Habit> habits, DateOnly today)
    {
        var all = habits.ToList();
        var active = all.Where(h => h.IsActive).ToList();
        var completed = active.Count(h => h.IsCompletedOn(today));
        return new HabitSummary(all.Count, active.Count, completed, Percent.Of(completed, active.Count));
    }

    public static PlanSummary Plans(IEnumerable<PlanProgress> plans)
    {
        var all = plans.ToList();
        var active = all.Where(p => p.Status == PlanStatus.InProgress).ToList();
        var avg = active.Count == 0 ? 0 : Math.Round(active.Average(p => p.CompletionPercentage), 2, MidpointRounding.AwayFromZero);
        return new PlanSummary(all.Count, active.Count,
            all.Count(p => p.Status == PlanStatus.NotStarted),
            all.Count(p => p.Status == PlanStatus.Completed), avg);
    }

    public static LearningSummary Learning(IEnumerable<LearningCard> cards, DateOnly today)
    {
        var all = cards.ToList();
        var milestones = all.SelectMany(c => c.Milestones).ToList();
        var done = milestones.Count(m => m.IsDone);
        var since = today.AddDays(-6);
        var recent = milestones.Count(m => m.IsDone && m.CompletedAt is { } c && Time.LocalDate(c) >= since && Time.LocalDate(c) <= today);
        return new LearningSummary(all.Count,
            all.Count(c => c.Status == LearningStatus.NotStarted),
            all.Count(c => c.Status == LearningStatus.InProgress),
            all.Count(c => c.Status == LearningStatus.Completed),
            milestones.Count, done, Percent.Of(done, milestones.Count), recent);
    }
}
