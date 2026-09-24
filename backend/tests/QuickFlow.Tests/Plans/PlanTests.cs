using QuickFlow.Domain.Common;
using QuickFlow.Domain.Plans;

namespace QuickFlow.Tests.Plans;

public class PlanTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly PlanItemSpec[] ThreeItems =
    {
        new(PlanItemSourceType.Task, 1),
        new(PlanItemSourceType.Habit, 1),
        new(PlanItemSourceType.LearningResource, 1),
    };

    private static Plan NewPlan(DateTime start, DateTime end, IReadOnlyList<PlanItemSpec>? items = null) =>
        Plan.Create("Deep work", 90, start, end, 1, items ?? ThreeItems, Now);

    [Fact]
    public void Plan_requires_at_least_one_item()
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            Plan.Create("p", 30, Now, Now.AddHours(1), 0, Array.Empty<PlanItemSpec>(), Now));
        Assert.Contains("Items", ex.Errors.Keys);
    }

    [Fact]
    public void End_must_be_after_start()
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewPlan(Now, Now));
        Assert.Contains("EndDateTime", ex.Errors.Keys);
    }

    [Fact]
    public void Title_duration_priority_and_duplicates_are_validated()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Plan.Create(" ", 0, Now, Now.AddHours(1), -1,
            new[] { new PlanItemSpec(PlanItemSourceType.Task, 1), new PlanItemSpec(PlanItemSourceType.Task, 1) }, Now));
        Assert.Contains("Title", ex.Errors.Keys);
        Assert.Contains("EstimatedDurationMinutes", ex.Errors.Keys);
        Assert.Contains("PriorityOrder", ex.Errors.Keys);
        Assert.Contains("Items", ex.Errors.Keys);
    }

    [Fact]
    public void Lifecycle_not_started_in_progress_completed_by_time()
    {
        var start = Now.AddHours(1);
        var end = Now.AddHours(3);

        var before = PlanProgressCalculator.Compute(start, end, 3, 0, Now);
        Assert.Equal(PlanStatus.NotStarted, before.Status);
        Assert.False(before.IsRestTimeActive);
        Assert.Null(before.RestTimeSeconds);
        Assert.Equal(3600, before.SecondsUntilStart);

        var during = PlanProgressCalculator.Compute(start, end, 3, 1, Now.AddHours(2));
        Assert.Equal(PlanStatus.InProgress, during.Status);
        Assert.True(during.IsRestTimeActive);
        Assert.Equal(3600, during.RestTimeSeconds);
        Assert.Equal(50, during.TimeElapsedPercentage);

        var after = PlanProgressCalculator.Compute(start, end, 3, 1, end);
        Assert.Equal(PlanStatus.Completed, after.Status);
        Assert.True(after.HasEnded);
        Assert.False(after.IsRestTimeActive);
        Assert.Null(after.RestTimeSeconds);
        Assert.Equal(33.33, after.CompletionPercentage);
    }

    [Fact]
    public void Rest_time_counts_down_until_end()
    {
        var start = Now.AddMinutes(-10);
        var end = Now.AddMinutes(50);
        Assert.Equal(3000, PlanProgressCalculator.Compute(start, end, 1, 0, Now).RestTimeSeconds);
        Assert.Equal(2940, PlanProgressCalculator.Compute(start, end, 1, 0, Now.AddMinutes(1)).RestTimeSeconds);
    }

    [Fact]
    public void All_items_done_completes_the_plan_early()
    {
        var plan = NewPlan(Now.AddHours(-1), Now.AddHours(1), new[] { new PlanItemSpec(PlanItemSourceType.Task, 1) });
        Assert.Equal(PlanStatus.InProgress, plan.Status);
        // Unsaved items have Id 0.
        plan.SetItemDone(0, true, Now);
        Assert.Equal(PlanStatus.Completed, plan.Status);
        Assert.Equal(100, plan.Progress(Now).CompletionPercentage);
        plan.SetItemDone(0, false, Now);
        Assert.Equal(PlanStatus.InProgress, plan.Status);
    }

    [Fact]
    public void Completion_percentage_is_done_over_total()
    {
        Assert.Equal(0, PlanProgressCalculator.Compute(Now, Now.AddHours(1), 3, 0, Now).CompletionPercentage);
        Assert.Equal(66.67, PlanProgressCalculator.Compute(Now, Now.AddHours(1), 3, 2, Now).CompletionPercentage);
        Assert.Equal(100, PlanProgressCalculator.Compute(Now, Now.AddHours(1), 3, 3, Now).CompletionPercentage);
    }

    [Fact]
    public void Update_keeps_done_flag_of_retained_items()
    {
        var plan = NewPlan(Now.AddHours(-1), Now.AddHours(1), new[] { new PlanItemSpec(PlanItemSourceType.Task, 1) });
        plan.SetItemDone(0, true, Now);
        plan.Update("Renamed", 60, Now.AddHours(-1), Now.AddHours(2), 5,
            new[] { new PlanItemSpec(PlanItemSourceType.Task, 1), new PlanItemSpec(PlanItemSourceType.Habit, 2) }, Now);
        Assert.Equal(2, plan.Items.Count);
        Assert.True(plan.Items.Single(i => i.SourceType == PlanItemSourceType.Task).IsDone);
        Assert.False(plan.Items.Single(i => i.SourceType == PlanItemSourceType.Habit).IsDone);
        Assert.Equal(PlanStatus.InProgress, plan.Status);
    }

    [Fact]
    public void Removing_items_of_a_deleted_source()
    {
        var plan = NewPlan(Now, Now.AddHours(1));
        Assert.Equal(1, plan.RemoveItemsFor(PlanItemSourceType.Habit, 1));
        Assert.Equal(2, plan.Items.Count);
    }

    [Fact]
    public void Start_acknowledgement_is_kept_once()
    {
        var plan = NewPlan(Now, Now.AddHours(1));
        plan.AcknowledgeStart(Now);
        plan.AcknowledgeStart(Now.AddMinutes(5));
        Assert.Equal(Now, plan.StartNotifiedAt);
    }
}
