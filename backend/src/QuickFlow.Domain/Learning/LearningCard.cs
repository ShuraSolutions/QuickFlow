using QuickFlow.Domain.Common;

namespace QuickFlow.Domain.Learning;

public enum LearningStatus { NotStarted, InProgress, Completed }

/// <summary>A learning resource (course, book, topic) with milestones and notes (FR-05, FR-06).</summary>
public class LearningCard
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private readonly List<LearningMilestone> _milestones = new();
    private readonly List<LearningNote> _notes = new();

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public LearningStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<LearningMilestone> Milestones => _milestones;
    public IReadOnlyCollection<LearningNote> Notes => _notes;

    private LearningCard() { }

    public static LearningCard Create(string? title, string? description, LearningStatus status, DateTime utcNow)
    {
        Validate(title, description, status);
        return new LearningCard
        {
            Title = title!.Trim(),
            Description = Validator.Normalize(description),
            Status = status,
            CreatedAt = utcNow,
        };
    }

    public void Update(string? title, string? description, LearningStatus status)
    {
        Validate(title, description, status);
        Title = title!.Trim();
        Description = Validator.Normalize(description);
        Status = status;
    }

    public void SetStatus(LearningStatus status)
    {
        new Validator().DefinedEnum(nameof(Status), status).ThrowIfInvalid();
        Status = status;
    }

    public LearningMilestone AddMilestone(string? title, DateOnly? targetDate)
    {
        var milestone = new LearningMilestone(Id, LearningMilestone.ValidTitle(title), targetDate);
        _milestones.Add(milestone);
        return milestone;
    }

    /// <summary>
    /// Changes a milestone of this card (BR-9: a milestone of another card is not found here).
    /// Completing a milestone of a NotStarted card moves the card to InProgress.
    /// </summary>
    public LearningMilestone UpdateMilestone(int milestoneId, string? title, bool? isDone, DateOnly? targetDate,
        bool clearTargetDate, DateTime utcNow)
    {
        var milestone = FindMilestone(milestoneId);
        if (title is not null) milestone.Rename(LearningMilestone.ValidTitle(title));
        if (clearTargetDate) milestone.SetTargetDate(null);
        else if (targetDate is not null) milestone.SetTargetDate(targetDate);
        if (isDone is { } done)
        {
            milestone.SetDone(done, utcNow);
            if (done && Status == LearningStatus.NotStarted) Status = LearningStatus.InProgress;
        }
        return milestone;
    }

    public void RemoveMilestone(int milestoneId) => _milestones.Remove(FindMilestone(milestoneId));

    public LearningNote AddNote(string? text, DateTime utcNow)
    {
        var note = new LearningNote(Id, LearningNote.ValidText(text), utcNow);
        _notes.Add(note);
        return note;
    }

    public void RemoveNote(int noteId) =>
        _notes.Remove(_notes.FirstOrDefault(n => n.Id == noteId) ?? throw new NotFoundException("Note", $"{Id}/{noteId}"));

    public int MilestonesDone => _milestones.Count(m => m.IsDone);

    public double MilestoneProgressPercentage => Percent.Of(MilestonesDone, _milestones.Count);

    private LearningMilestone FindMilestone(int milestoneId) =>
        _milestones.FirstOrDefault(m => m.Id == milestoneId) ?? throw new NotFoundException("Milestone", $"{Id}/{milestoneId}");

    private static void Validate(string? title, string? description, LearningStatus status) =>
        new Validator()
            .Required(nameof(Title), title, TitleMaxLength)
            .Optional(nameof(Description), description, DescriptionMaxLength)
            .DefinedEnum(nameof(Status), status)
            .ThrowIfInvalid();
}

/// <summary>A step of a learning card. Belongs to exactly one card (BR-9).</summary>
public class LearningMilestone
{
    public const int TitleMaxLength = 200;

    public int Id { get; private set; }
    public int LearningCardId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsDone { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private LearningMilestone() { }

    internal LearningMilestone(int cardId, string title, DateOnly? targetDate)
    {
        LearningCardId = cardId;
        Title = title;
        TargetDate = targetDate;
    }

    internal void Rename(string title) => Title = title;

    internal void SetTargetDate(DateOnly? date) => TargetDate = date;

    internal void SetDone(bool done, DateTime utcNow)
    {
        if (done && !IsDone) CompletedAt = utcNow;
        if (!done) CompletedAt = null;
        IsDone = done;
    }

    internal static string ValidTitle(string? title)
    {
        new Validator().Required(nameof(Title), title, TitleMaxLength).ThrowIfInvalid();
        return title!.Trim();
    }
}

/// <summary>A free-form note attached to a learning card.</summary>
public class LearningNote
{
    public const int TextMaxLength = 4000;

    public int Id { get; private set; }
    public int LearningCardId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private LearningNote() { }

    internal LearningNote(int cardId, string text, DateTime utcNow)
    {
        LearningCardId = cardId;
        Text = text;
        CreatedAt = utcNow;
    }

    internal static string ValidText(string? text)
    {
        new Validator().Required(nameof(Text), text, TextMaxLength).ThrowIfInvalid();
        return text!.Trim();
    }
}
