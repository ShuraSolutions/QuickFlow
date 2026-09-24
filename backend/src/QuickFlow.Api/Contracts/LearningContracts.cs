using QuickFlow.Domain.Learning;

namespace QuickFlow.Api.Contracts;

/// <summary>A learning card with its milestones (in creation order) and notes (newest first).</summary>
public record LearningCardResponse(
    int Id,
    string Title,
    string? Description,
    LearningStatus Status,
    DateTime CreatedAt,
    List<LearningMilestoneResponse> Milestones,
    List<LearningNoteResponse> Notes,
    int MilestonesTotal,
    int MilestonesDone,
    double MilestoneProgressPercentage,
    int NotesCount)
{
    public static LearningCardResponse From(LearningCard c) => new(
        c.Id, c.Title, c.Description, c.Status, c.CreatedAt,
        c.Milestones.OrderBy(m => m.Id).Select(LearningMilestoneResponse.From).ToList(),
        c.Notes.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Select(LearningNoteResponse.From).ToList(),
        c.Milestones.Count, c.MilestonesDone, c.MilestoneProgressPercentage, c.Notes.Count);
}

public record LearningMilestoneResponse(int Id, int LearningCardId, string Title, bool IsDone, DateOnly? TargetDate, DateTime? CompletedAt)
{
    public static LearningMilestoneResponse From(LearningMilestone m) => new(m.Id, m.LearningCardId, m.Title, m.IsDone, m.TargetDate, m.CompletedAt);
}

public record LearningNoteResponse(int Id, int LearningCardId, string Text, DateTime CreatedAt)
{
    public static LearningNoteResponse From(LearningNote n) => new(n.Id, n.LearningCardId, n.Text, n.CreatedAt);
}

/// <summary>Body for creating or updating a learning card.</summary>
public class LearningCardRequest
{
    /// <summary>Required, at most 200 characters.</summary>
    public string? Title { get; set; }
    /// <summary>Description or source (link, book, course name); at most 2000 characters.</summary>
    public string? Description { get; set; }
    /// <summary>Defaults to NotStarted on create; unchanged on update when omitted.</summary>
    public LearningStatus? Status { get; set; }
}

/// <summary>Body for adding a milestone.</summary>
public class CreateMilestoneRequest
{
    /// <summary>Required, at most 200 characters.</summary>
    public string? Title { get; set; }
    /// <summary>Optional target date (yyyy-MM-dd).</summary>
    public DateOnly? TargetDate { get; set; }
}

/// <summary>Partial update of a milestone; omitted fields are unchanged.</summary>
public class UpdateMilestoneRequest
{
    public string? Title { get; set; }
    /// <summary>true completes the milestone, false reopens it.</summary>
    public bool? IsDone { get; set; }
    public DateOnly? TargetDate { get; set; }
    /// <summary>true removes the target date.</summary>
    public bool ClearTargetDate { get; set; }
}

/// <summary>Body for adding a note.</summary>
public class CreateNoteRequest
{
    /// <summary>Required, at most 4000 characters.</summary>
    public string? Text { get; set; }
}
