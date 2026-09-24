using QuickFlow.Domain.Tasks;

namespace QuickFlow.Api.Contracts;

/// <summary>A task as returned by the API.</summary>
/// <param name="IsOverdue">True when the due date is before today and the task is not Done (and not archived).</param>
public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    bool IsArchived,
    bool IsOverdue)
{
    public static TaskResponse From(TaskItem t, DateOnly today) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority, t.DueDate,
        t.CreatedAt, t.UpdatedAt, t.CompletedAt, t.IsArchived, t.IsOverdue(today));
}

/// <summary>Body for creating a task.</summary>
public class CreateTaskRequest
{
    /// <summary>Required, at most 200 characters.</summary>
    public string? Title { get; set; }
    /// <summary>Optional, at most 2000 characters.</summary>
    public string? Description { get; set; }
    /// <summary>Defaults to Todo.</summary>
    public TaskItemStatus? Status { get; set; }
    /// <summary>Defaults to Medium.</summary>
    public TaskPriority? Priority { get; set; }
    /// <summary>Optional due date (yyyy-MM-dd).</summary>
    public DateOnly? DueDate { get; set; }
}

/// <summary>
/// Body for updating a task. Title is required. Omitted/null Status or Priority keep their
/// current value; Description and DueDate are replaced (null clears them).
/// </summary>
public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateOnly? DueDate { get; set; }
}

public enum ArchivedFilter { Exclude, Include, Only }

public enum TaskSortBy { CreatedAt, DueDate }

public enum SortDirection { Asc, Desc }

/// <summary>Query options for listing tasks.</summary>
public class TaskQuery
{
    /// <summary>Case-insensitive text contained in the title.</summary>
    public string? Search { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    /// <summary>Only tasks due on this date.</summary>
    public DateOnly? DueDate { get; set; }
    /// <summary>Only tasks due on or after this date.</summary>
    public DateOnly? DueFrom { get; set; }
    /// <summary>Only tasks due on or before this date.</summary>
    public DateOnly? DueTo { get; set; }
    /// <summary>true = only overdue tasks, false = only tasks that are not overdue.</summary>
    public bool? Overdue { get; set; }
    /// <summary>Exclude (default) hides archived tasks, Include shows all, Only shows archived tasks.</summary>
    public ArchivedFilter Archived { get; set; } = ArchivedFilter.Exclude;
    /// <summary>CreatedAt (default) or DueDate. Tasks without a due date sort last.</summary>
    public TaskSortBy SortBy { get; set; } = TaskSortBy.CreatedAt;
    /// <summary>Desc (default) or Asc.</summary>
    public SortDirection SortDir { get; set; } = SortDirection.Desc;
}
