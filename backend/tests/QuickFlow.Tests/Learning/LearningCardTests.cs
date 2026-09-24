using QuickFlow.Domain.Common;
using QuickFlow.Domain.Learning;

namespace QuickFlow.Tests.Learning;

public class LearningCardTests
{
    private static readonly DateTime Now = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

    private static LearningCard NewCard() => LearningCard.Create("Clean Code", "book", LearningStatus.NotStarted, Now);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_is_required(string? title)
    {
        var ex = Assert.Throws<DomainValidationException>(() => LearningCard.Create(title, null, LearningStatus.NotStarted, Now));
        Assert.Contains("Title", ex.Errors.Keys);
    }

    [Fact]
    public void Undefined_status_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() => LearningCard.Create("t", null, (LearningStatus)5, Now));
    }

    [Fact]
    public void Milestone_title_is_required()
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewCard().AddMilestone(" ", null));
        Assert.Contains("Title", ex.Errors.Keys);
    }

    [Fact]
    public void Note_text_is_required()
    {
        var ex = Assert.Throws<DomainValidationException>(() => NewCard().AddNote("", Now));
        Assert.Contains("Text", ex.Errors.Keys);
    }

    [Fact]
    public void Progress_is_done_over_total_milestones()
    {
        var card = NewCard();
        Assert.Equal(0, card.MilestoneProgressPercentage);
        card.AddMilestone("Ch 1", null);
        card.AddMilestone("Ch 2", null);
        card.AddMilestone("Ch 3", null);
        // Milestones are not persisted, so all ids are 0; complete the first via the entity.
        card.UpdateMilestone(0, null, true, null, false, Now);
        Assert.Equal(1, card.MilestonesDone);
        Assert.Equal(33.33, card.MilestoneProgressPercentage);
    }

    [Fact]
    public void Completing_a_milestone_starts_a_not_started_card()
    {
        var card = NewCard();
        card.AddMilestone("Ch 1", null);
        var m = card.UpdateMilestone(0, null, true, null, false, Now);
        Assert.True(m.IsDone);
        Assert.Equal(Now, m.CompletedAt);
        Assert.Equal(LearningStatus.InProgress, card.Status);
    }

    [Fact]
    public void Reopening_a_milestone_clears_completed_at()
    {
        var card = NewCard();
        card.AddMilestone("Ch 1", null);
        card.UpdateMilestone(0, null, true, null, false, Now);
        var m = card.UpdateMilestone(0, null, false, null, false, Now);
        Assert.False(m.IsDone);
        Assert.Null(m.CompletedAt);
    }

    [Fact]
    public void Unknown_milestone_or_note_is_not_found()
    {
        var card = NewCard();
        Assert.Throws<NotFoundException>(() => card.RemoveMilestone(42));
        Assert.Throws<NotFoundException>(() => card.RemoveNote(42));
    }

    [Fact]
    public void Percent_rounds_to_two_decimals()
    {
        Assert.Equal(66.67, Percent.Of(2, 3));
        Assert.Equal(0, Percent.Of(0, 0));
        Assert.Equal(100, Percent.Of(4, 4));
    }
}
