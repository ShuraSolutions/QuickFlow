using QuickFlow.Domain.Common;
using QuickFlow.Domain.Tasks;

namespace QuickFlow.Tests.Tasks;

public class TaskItemTests
{
    private static readonly DateTime Now = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 1, 10);

    private static TaskItem NewTask(string title = "Write report", DateOnly? due = null,
        TaskItemStatus status = TaskItemStatus.Todo) =>
        TaskItem.Create(title, null, status, TaskPriority.Medium, due, Now);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_is_required(string? title)
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewTask(title!));
        Assert.Contains("Title", ex.Errors.Keys);
    }

    [Fact]
    public void Title_max_length_is_200()
    {
        Assert.Equal(200, NewTask(new string('a', 200)).Title.Length);
        Assert.Throws<DomainValidationException>(() => NewTask(new string('a', 201)));
    }

    [Fact]
    public void Description_max_length_is_2000()
    {
        TaskItem.Create("t", new string('d', 2000), TaskItemStatus.Todo, TaskPriority.Low, null, Now);
        var ex = Assert.Throws<DomainValidationException>(() =>
            TaskItem.Create("t", new string('d', 2001), TaskItemStatus.Todo, TaskPriority.Low, null, Now));
        Assert.Contains("Description", ex.Errors.Keys);
    }

    [Fact]
    public void Undefined_status_or_priority_is_rejected()
    {
        var ex = Assert.Throws<DomainValidationException>(() =>
            TaskItem.Create("t", null, (TaskItemStatus)42, (TaskPriority)7, null, Now));
        Assert.Contains("Status", ex.Errors.Keys);
        Assert.Contains("Priority", ex.Errors.Keys);
    }

    [Fact]
    public void Complete_sets_status_done_and_completed_at()
    {
        var task = NewTask();
        task.Complete(Now.AddHours(1));
        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.Equal(Now.AddHours(1), task.CompletedAt);
    }

    [Fact]
    public void Reopen_clears_completed_at()
    {
        var task = NewTask(status: TaskItemStatus.Done);
        task.Reopen(Now);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Overdue_when_due_before_today_and_not_done()
    {
        Assert.True(NewTask(due: Today.AddDays(-1)).IsOverdue(Today));
        Assert.False(NewTask(due: Today).IsOverdue(Today));
        Assert.False(NewTask(due: null).IsOverdue(Today));
        Assert.False(NewTask(due: Today.AddDays(-1), status: TaskItemStatus.Done).IsOverdue(Today));
    }

    [Fact]
    public void Archived_task_is_not_overdue()
    {
        var task = NewTask(due: Today.AddDays(-3));
        task.Archive(Now);
        Assert.False(task.IsOverdue(Today));
    }
}
