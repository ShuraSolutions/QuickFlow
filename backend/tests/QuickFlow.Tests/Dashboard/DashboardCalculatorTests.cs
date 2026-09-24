using QuickFlow.Domain.Dashboard;
using QuickFlow.Domain.Habits;
using QuickFlow.Domain.Learning;
using QuickFlow.Domain.Plans;
using QuickFlow.Domain.Tasks;

namespace QuickFlow.Tests.Dashboard;

public class DashboardCalculatorTests
{
    // Use the machine's local "today" so CompletedAt (UTC now) maps to the same local date.
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private static TaskItem Task(TaskItemStatus status, DateOnly? due = null) =>
        TaskItem.Create("t", null, status, TaskPriority.Medium, due, Now);

    [Fact]
    public void Task_metrics_match_the_tasks()
    {
        var archived = Task(TaskItemStatus.Done, Today);
        archived.Archive(Now);
        var tasks = new[]
        {
            Task(TaskItemStatus.Todo, Today),               // due today
            Task(TaskItemStatus.InProgress, Today.AddDays(-2)), // overdue
            Task(TaskItemStatus.Done, Today.AddDays(-1)),    // done today, not overdue
            Task(TaskItemStatus.Todo),
            archived,                                        // excluded
        };

        var s = DashboardCalculator.Tasks(tasks, Today);

        Assert.Equal(4, s.Total);
        Assert.Equal(2, s.Todo);
        Assert.Equal(1, s.InProgress);
        Assert.Equal(1, s.Done);
        Assert.Equal(1, s.DueToday);
        Assert.Equal(1, s.Overdue);
        Assert.Equal(1, s.CompletedToday);
        Assert.Equal(25, s.CompletionPercentage);
    }

    [Fact]
    public void Habit_metrics_count_active_habits_completed_today()
    {
        var done = Habit.Create("a", null, HabitFrequency.Daily, Now);
        done.Complete(Today, Today, Now);
        var open = Habit.Create("b", null, HabitFrequency.Weekly, Now);
        var inactive = Habit.Create("c", null, HabitFrequency.Daily, Now);
        inactive.Complete(Today, Today, Now);
        inactive.Deactivate();

        var s = DashboardCalculator.Habits(new[] { done, open, inactive }, Today);

        Assert.Equal(3, s.Total);
        Assert.Equal(2, s.Active);
        Assert.Equal(1, s.CompletedToday);
        Assert.Equal(50, s.CompletionPercentage);
    }

    [Fact]
    public void Plan_metrics_group_by_status_and_average_active_progress()
    {
        var plans = new[]
        {
            PlanProgressCalculator.Compute(Now.AddHours(-1), Now.AddHours(1), 4, 1, Now),  // in progress 25%
            PlanProgressCalculator.Compute(Now.AddHours(-1), Now.AddHours(1), 2, 1, Now),  // in progress 50%
            PlanProgressCalculator.Compute(Now.AddHours(1), Now.AddHours(2), 1, 0, Now),   // upcoming
            PlanProgressCalculator.Compute(Now.AddHours(-3), Now.AddHours(-2), 2, 1, Now), // completed (ended)
        };

        var s = DashboardCalculator.Plans(plans);

        Assert.Equal(4, s.Total);
        Assert.Equal(2, s.InProgress);
        Assert.Equal(1, s.Upcoming);
        Assert.Equal(1, s.Completed);
        Assert.Equal(37.5, s.AverageActiveCompletionPercentage);
    }

    [Fact]
    public void Learning_snapshot_matches_cards_and_milestones()
    {
        var a = LearningCard.Create("a", null, LearningStatus.NotStarted, Now);
        a.AddMilestone("m1", null);
        a.AddMilestone("m2", null);
        a.UpdateMilestone(0, null, true, null, false, Now); // completes m1, card -> InProgress
        var b = LearningCard.Create("b", null, LearningStatus.Completed, Now);
        var c = LearningCard.Create("c", null, LearningStatus.NotStarted, Now);

        var s = DashboardCalculator.Learning(new[] { a, b, c }, Today);

        Assert.Equal(3, s.TotalCards);
        Assert.Equal(1, s.NotStarted);
        Assert.Equal(1, s.InProgress);
        Assert.Equal(1, s.Completed);
        Assert.Equal(2, s.MilestonesTotal);
        Assert.Equal(1, s.MilestonesDone);
        Assert.Equal(50, s.MilestoneCompletionPercentage);
        Assert.Equal(1, s.MilestonesCompletedLast7Days);
    }
}
