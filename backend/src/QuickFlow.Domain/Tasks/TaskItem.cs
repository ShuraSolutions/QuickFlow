using QuickFlow.Domain.Common;

namespace QuickFlow.Domain.Tasks;

public enum TaskItemStatus { Todo, InProgress, Done }

public enum TaskPriority { Low, Medium, High }

/// <summary>A to-do item. All rules from FR-01 and BR-1..5 are enforced here.</summary>
public class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsArchived { get; private set; }

    private TaskItem() { }

    public static TaskItem Create(string? title, string? description, TaskItemStatus status,
        TaskPriority priority, DateOnly? dueDate, DateTime utcNow)
    {
        Validate(title, description, status, priority);
        var task = new TaskItem { CreatedAt = utcNow };
        task.Apply(title!, description, status, priority, dueDate, utcNow);
        return task;
    }

    public void Update(string? title, string? description, TaskItemStatus status,
        TaskPriority priority, DateOnly? dueDate, DateTime utcNow)
    {
        Validate(title, description, status, priority);
        Apply(title!, description, status, priority, dueDate, utcNow);
    }

    /// <summary>BR-5: a completed task has status Done.</summary>
    public void Complete(DateTime utcNow) => SetStatus(TaskItemStatus.Done, utcNow);

    /// <summary>Moves a task back to Todo (used when a plan item is un-marked, BR-13).</summary>
    public void Reopen(DateTime utcNow) => SetStatus(TaskItemStatus.Todo, utcNow);

    public void Archive(DateTime utcNow) { IsArchived = true; UpdatedAt = utcNow; }

    public void Restore(DateTime utcNow) { IsArchived = false; UpdatedAt = utcNow; }

    /// <summary>Overdue = due before today, not done, not archived.</summary>
    public bool IsOverdue(DateOnly today) =>
        DueDate is { } due && due < today && Status != TaskItemStatus.Done && !IsArchived;

    private void Apply(string title, string? description, TaskItemStatus status,
        TaskPriority priority, DateOnly? dueDate, DateTime utcNow)
    {
        Title = title.Trim();
        Description = Validator.Normalize(description);
        Priority = priority;
        DueDate = dueDate;
        SetStatus(status, utcNow);
    }

    private void SetStatus(TaskItemStatus status, DateTime utcNow)
    {
        if (status != TaskItemStatus.Done)
            CompletedAt = null;
        else if (CompletedAt is null)
            CompletedAt = utcNow;
        Status = status;
        UpdatedAt = utcNow;
    }

    private static void Validate(string? title, string? description, TaskItemStatus status, TaskPriority priority) =>
        new Validator()
            .Required(nameof(Title), title, TitleMaxLength)
            .Optional(nameof(Description), description, DescriptionMaxLength)
            .DefinedEnum(nameof(Status), status)
            .DefinedEnum(nameof(Priority), priority)
            .ThrowIfInvalid();
}
