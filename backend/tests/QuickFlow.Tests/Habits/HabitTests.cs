using QuickFlow.Domain.Common;
using QuickFlow.Domain.Habits;

namespace QuickFlow.Tests.Habits;

public class HabitTests
{
    private static readonly DateTime Now = new(2026, 1, 14, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 1, 14); // Wednesday

    private static Habit NewHabit(HabitFrequency f = HabitFrequency.Daily) => Habit.Create("Read", null, f, Now);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Name_is_required(string? name)
    {
        var ex = Assert.Throws<DomainValidationException>(() => Habit.Create(name, null, HabitFrequency.Daily, Now));
        Assert.Contains("Name", ex.Errors.Keys);
    }

    [Fact]
    public void Name_max_length_is_150()
    {
        Habit.Create(new string('n', 150), null, HabitFrequency.Daily, Now);
        Assert.Throws<DomainValidationException>(() => Habit.Create(new string('n', 151), null, HabitFrequency.Daily, Now));
    }

    [Fact]
    public void Undefined_frequency_is_rejected()
    {
        var ex = Assert.Throws<DomainValidationException>(() => Habit.Create("x", null, (HabitFrequency)9, Now));
        Assert.Contains("Frequency", ex.Errors.Keys);
    }

    [Fact]
    public void New_habit_is_active()
    {
        var h = NewHabit();
        Assert.True(h.IsActive);
        h.Deactivate();
        Assert.False(h.IsActive);
        h.Activate();
        Assert.True(h.IsActive);
    }

    [Fact]
    public void Only_one_completion_per_date()
    {
        var h = NewHabit();
        h.Complete(Today, Today, Now);
        Assert.Throws<ConflictException>(() => h.Complete(Today, Today, Now));
        Assert.Single(h.Completions);
    }

    [Fact]
    public void Completion_in_the_future_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() => NewHabit().Complete(Today.AddDays(1), Today, Now));
    }

    [Fact]
    public void Uncomplete_missing_date_throws_not_found()
    {
        Assert.Throws<NotFoundException>(() => NewHabit().Uncomplete(Today));
    }

    [Fact]
    public void Daily_streak_counts_consecutive_days_ending_today()
    {
        var p = HabitProgressCalculator.Compute(HabitFrequency.Daily,
            new[] { Today, Today.AddDays(-1), Today.AddDays(-2), Today.AddDays(-4) }, Today);
        Assert.True(p.CompletedToday);
        Assert.Equal(3, p.CurrentStreak);
        Assert.Equal(4, p.TotalCompletions);
        Assert.Equal(Today, p.LastCompletedDate);
    }

    [Fact]
    public void Daily_streak_continues_from_yesterday_when_today_not_done()
    {
        var p = HabitProgressCalculator.Compute(HabitFrequency.Daily, new[] { Today.AddDays(-1), Today.AddDays(-2) }, Today);
        Assert.False(p.CompletedToday);
        Assert.Equal(2, p.CurrentStreak);
    }

    [Fact]
    public void Daily_streak_breaks_after_a_missed_day()
    {
        var p = HabitProgressCalculator.Compute(HabitFrequency.Daily, new[] { Today.AddDays(-2) }, Today);
        Assert.Equal(0, p.CurrentStreak);
    }

    [Fact]
    public void Weekly_habit_counts_iso_weeks()
    {
        // Monday of this week, last week's Friday, and the week before's Sunday.
        var monday = new DateOnly(2026, 1, 12);
        var p = HabitProgressCalculator.Compute(HabitFrequency.Weekly,
            new[] { monday, monday.AddDays(-3), monday.AddDays(-8) }, Today);
        Assert.False(p.CompletedToday);
        Assert.True(p.CompletedThisPeriod);
        Assert.Equal(3, p.CurrentStreak);
    }

    [Fact]
    public void Week_starts_on_monday()
    {
        Assert.Equal(new DateOnly(2026, 1, 12), HabitProgressCalculator.WeekStart(new DateOnly(2026, 1, 18))); // Sunday
        Assert.Equal(new DateOnly(2026, 1, 12), HabitProgressCalculator.WeekStart(new DateOnly(2026, 1, 12))); // Monday
    }
}
