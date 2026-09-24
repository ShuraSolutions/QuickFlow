using Microsoft.EntityFrameworkCore;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Data;
using QuickFlow.Domain.Common;
using QuickFlow.Domain.Learning;
using QuickFlow.Domain.Plans;

namespace QuickFlow.Api.Services;

public class LearningService
{
    private readonly QuickFlowDbContext _db;
    private readonly IClock _clock;

    public LearningService(QuickFlowDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<LearningCardResponse>> ListAsync(LearningStatus? status, CancellationToken ct)
    {
        var query = Cards().AsNoTracking();
        if (status is { } s) query = query.Where(c => c.Status == s);
        var cards = await query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).ToListAsync(ct);
        return cards.Select(LearningCardResponse.From).ToList();
    }

    public async Task<LearningCardResponse> GetAsync(int id, CancellationToken ct) =>
        LearningCardResponse.From(await FindAsync(id, ct));

    public async Task<LearningCardResponse> CreateAsync(LearningCardRequest r, CancellationToken ct)
    {
        var card = LearningCard.Create(r.Title, r.Description, r.Status ?? LearningStatus.NotStarted, _clock.UtcNow);
        _db.LearningCards.Add(card);
        await _db.SaveChangesAsync(ct);
        return LearningCardResponse.From(card);
    }

    public async Task<LearningCardResponse> UpdateAsync(int id, LearningCardRequest r, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        card.Update(r.Title, r.Description, r.Status ?? card.Status);
        await _db.SaveChangesAsync(ct);
        return LearningCardResponse.From(card);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        _db.LearningCards.Remove(card);
        await PlanService.RemoveItemsForSourceAsync(_db, PlanItemSourceType.LearningResource, id, _clock.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LearningMilestoneResponse> AddMilestoneAsync(int id, CreateMilestoneRequest r, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        var milestone = card.AddMilestone(r.Title, r.TargetDate);
        await _db.SaveChangesAsync(ct);
        return LearningMilestoneResponse.From(milestone);
    }

    public async Task<LearningMilestoneResponse> UpdateMilestoneAsync(int id, int milestoneId, UpdateMilestoneRequest r, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        var milestone = card.UpdateMilestone(milestoneId, r.Title, r.IsDone, r.TargetDate, r.ClearTargetDate, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return LearningMilestoneResponse.From(milestone);
    }

    public async Task RemoveMilestoneAsync(int id, int milestoneId, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        card.RemoveMilestone(milestoneId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LearningNoteResponse> AddNoteAsync(int id, CreateNoteRequest r, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        var note = card.AddNote(r.Text, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return LearningNoteResponse.From(note);
    }

    public async Task RemoveNoteAsync(int id, int noteId, CancellationToken ct)
    {
        var card = await FindAsync(id, ct);
        card.RemoveNote(noteId);
        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<LearningCard> Cards() =>
        _db.LearningCards.Include(c => c.Milestones).Include(c => c.Notes).AsSplitQuery();

    private async Task<LearningCard> FindAsync(int id, CancellationToken ct) =>
        await Cards().FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Learning card", id);
}
