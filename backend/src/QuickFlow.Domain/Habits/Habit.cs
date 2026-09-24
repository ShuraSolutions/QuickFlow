using QuickFlow.Domain.Common;

namespace QuickFlow.Domain.Habits;

public enum HabitFrequency { Daily, Weekly }

/// <summary>A recurring habit (FR-03) with its completion events (FR-04).</summary>
public class Habit
{
    public const int NameMaxLength = 150;
    public const int DescriptionMaxLength = 2000;

    private readonly List<HabitCompletion> _completions = new();

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public HabitFrequency Frequency { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<HabitCompletion> Completions => _completions;

    private Habit() { }

    public static Habit Create(string? name, string? description, HabitFrequency frequency, DateTime utcNow)
    {
        Validate(name, description, frequency);
        return new Habit
        {
            Name = name!.Trim(),
            Description = Validator.Normalize(description),
            Frequency = frequency,
            CreatedAt = utcNow,
            IsActive = true,
        };
    }

    public void Update(string? name, string? description, HabitFrequency frequency)
    {
        Validate(name, description, frequency);
        Name = name!.Trim();
        Description = Validator.Normalize(description);
        Frequency = frequency;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public bool IsCompletedOn(DateOnly date) => _completions.Any(c => c.CompletionDate == date);

    /// <summary>
    /// Records a completion for <paramref name="date"/>. The date cannot be in the future and
    /// BR-7 allows only one completion per habit per date (duplicate → <see cref="ConflictException"/>).
    /// Requires <see cref="Completions"/> to be loaded.
    /// </summary>
    public HabitCompletion Complete(DateOnly date, DateOnly today, DateTime utcNow)
    {
        if (date > today)
            throw new DomainValidationException("Date", "Completion date cannot be in the future.");
        if (IsCompletedOn(date))
            throw new ConflictException($"Habit '{Id}' is already completed for {date:yyyy-MM-dd}.");
        var completion = new HabitCompletion(Id, date, utcNow);
        _completions.Add(completion);
        return completion;
    }

    /// <summary>Removes the completion for <paramref name="date"/>; throws <see cref="NotFoundException"/> if there is none.</summary>
    public void Uncomplete(DateOnly date)
    {
        var completion = _completions.FirstOrDefault(c => c.CompletionDate == date)
            ?? throw new NotFoundException("Habit completion", $"{Id}/{date:yyyy-MM-dd}");
        _completions.Remove(completion);
    }

    public HabitProgress Progress(DateOnly today) =>
        HabitProgressCalculator.Compute(Frequency, _completions.Select(c => c.CompletionDate), today);

    private static void Validate(string? name, string? description, HabitFrequency frequency) =>
        new Validator()
            .Required(nameof(Name), name, NameMaxLength)
            .Optional(nameof(Description), description, DescriptionMaxLength)
            .DefinedEnum(nameof(Frequency), frequency)
            .ThrowIfInvalid();
}

/// <summary>A habit was done on a date. Unique per (HabitId, CompletionDate).</summary>
public class HabitCompletion
{
    public int Id { get; private set; }
    public int HabitId { get; private set; }
    public DateOnly CompletionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private HabitCompletion() { }

    internal HabitCompletion(int habitId, DateOnly date, DateTime utcNow)
    {
        HabitId = habitId;
        CompletionDate = date;
        CreatedAt = utcNow;
    }
}
