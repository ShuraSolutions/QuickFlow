using QuickFlow.Domain.Common;

namespace QuickFlow.Domain.Plans;

public enum PlanStatus { NotStarted, InProgress, Completed }

public enum PlanItemSourceType { Task, Habit, LearningResource }

/// <summary>Reference to an existing task, habit or learning resource to include in a plan.</summary>
public record PlanItemSpec(PlanItemSourceType SourceType, int SourceId);

/// <summary>A time-boxed plan built from existing tasks, habits and learning resources (FR-07, FR-08).</summary>
public class Plan
{
    public const int TitleMaxLength = 200;

    private readonly List<PlanItem> _items = new();

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int EstimatedDurationMinutes { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public int PriorityOrder { get; private set; }
    /// <summary>Last computed status; always refreshed from <see cref="PlanProgressCalculator"/> before use.</summary>
    public PlanStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    /// <summary>When the user acknowledged the "plan started" notification.</summary>
    public DateTime? StartNotifiedAt { get; private set; }
    public IReadOnlyCollection<PlanItem> Items => _items;

    private Plan() { }

    public static Plan Create(string? title, int? estimatedDurationMinutes, DateTime? startUtc, DateTime? endUtc,
        int priorityOrder, IReadOnlyList<PlanItemSpec>? items, DateTime utcNow)
    {
        Validate(title, estimatedDurationMinutes, startUtc, endUtc, priorityOrder, items);
        var plan = new Plan { CreatedAt = utcNow };
        plan.Apply(title!, estimatedDurationMinutes!.Value, startUtc!.Value, endUtc!.Value, priorityOrder);
        foreach (var spec in items!) plan._items.Add(new PlanItem(spec.SourceType, spec.SourceId));
        plan.RefreshStatus(utcNow);
        return plan;
    }

    /// <summary>Updates the plan; items are replaced, but retained items keep their IsDone flag.</summary>
    public void Update(string? title, int? estimatedDurationMinutes, DateTime? startUtc, DateTime? endUtc,
        int priorityOrder, IReadOnlyList<PlanItemSpec>? items, DateTime utcNow)
    {
        Validate(title, estimatedDurationMinutes, startUtc, endUtc, priorityOrder, items);
        Apply(title!, estimatedDurationMinutes!.Value, startUtc!.Value, endUtc!.Value, priorityOrder);
        _items.RemoveAll(i => !items!.Any(s => s.SourceType == i.SourceType && s.SourceId == i.SourceId));
        foreach (var spec in items!)
            if (!_items.Any(i => i.SourceType == spec.SourceType && i.SourceId == spec.SourceId))
                _items.Add(new PlanItem(spec.SourceType, spec.SourceId));
        RefreshStatus(utcNow);
    }

    /// <summary>Marks one item done or not done (independent of the plan's overall status).</summary>
    public PlanItem SetItemDone(int itemId, bool isDone, DateTime utcNow)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId) ?? throw new NotFoundException("Plan item", $"{Id}/{itemId}");
        item.IsDone = isDone;
        RefreshStatus(utcNow);
        return item;
    }

    /// <summary>BR-14: drops items whose source was deleted. Returns how many were removed.</summary>
    public int RemoveItemsFor(PlanItemSourceType type, int sourceId) =>
        _items.RemoveAll(i => i.SourceType == type && i.SourceId == sourceId);

    public void AcknowledgeStart(DateTime utcNow) => StartNotifiedAt ??= utcNow;

    public PlanProgress Progress(DateTime utcNow) =>
        PlanProgressCalculator.Compute(StartDateTime, EndDateTime, _items.Count, _items.Count(i => i.IsDone), utcNow);

    /// <summary>Recomputes the stored status from time and item completion. Returns true when it changed.</summary>
    public bool RefreshStatus(DateTime utcNow)
    {
        var status = Progress(utcNow).Status;
        if (status == Status) return false;
        Status = status;
        return true;
    }

    private void Apply(string title, int minutes, DateTime startUtc, DateTime endUtc, int priorityOrder)
    {
        Title = title.Trim();
        EstimatedDurationMinutes = minutes;
        StartDateTime = startUtc;
        EndDateTime = endUtc;
        PriorityOrder = priorityOrder;
    }

    private static void Validate(string? title, int? minutes, DateTime? start, DateTime? end, int priorityOrder,
        IReadOnlyList<PlanItemSpec>? items)
    {
        var v = new Validator()
            .Required(nameof(Title), title, TitleMaxLength)
            .When(minutes is null, "EstimatedDurationMinutes", "EstimatedDurationMinutes is required.")
            .When(minutes is <= 0, "EstimatedDurationMinutes", "EstimatedDurationMinutes must be greater than 0.")
            .When(start is null, nameof(StartDateTime), "StartDateTime is required.")
            .When(end is null, nameof(EndDateTime), "EndDateTime is required.")
            .When(start is not null && end is not null && end <= start, nameof(EndDateTime), "EndDateTime must be after StartDateTime.")
            .When(priorityOrder < 0, nameof(PriorityOrder), "PriorityOrder must be 0 or greater.")
            .When(items is null || items.Count == 0, nameof(Items), "A plan must reference at least one task, habit or learning resource.");
        if (items is not null)
        {
            if (items.Any(i => !Enum.IsDefined(i.SourceType)))
                v.Add(nameof(Items), "Item sourceType must be one of: Task, Habit, LearningResource.");
            if (items.GroupBy(i => (i.SourceType, i.SourceId)).Any(g => g.Count() > 1))
                v.Add(nameof(Items), "The same source cannot be added to a plan twice.");
        }
        v.ThrowIfInvalid();
    }
}

/// <summary>One task, habit or learning resource inside a plan, with its own done flag.</summary>
public class PlanItem
{
    public int Id { get; private set; }
    public int PlanId { get; private set; }
    public PlanItemSourceType SourceType { get; private set; }
    public int SourceId { get; private set; }
    public bool IsDone { get; internal set; }

    private PlanItem() { }

    internal PlanItem(PlanItemSourceType sourceType, int sourceId)
    {
        SourceType = sourceType;
        SourceId = sourceId;
    }
}
