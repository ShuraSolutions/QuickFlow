using Microsoft.EntityFrameworkCore;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Data;
using QuickFlow.Domain.Common;
using QuickFlow.Domain.Habits;
using QuickFlow.Domain.Plans;

namespace QuickFlow.Api.Services;

public class HabitService
{
    private readonly QuickFlowDbContext _db;
    private readonly IClock _clock;

    public HabitService(QuickFlowDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<HabitResponse>> ListAsync(bool? isActive, CancellationToken ct)
    {
        var query = _db.Habits.Include(h => h.Completions).AsNoTracking();
        if (isActive is { } active) query = query.Where(h => h.IsActive == active);
        var habits = await query.OrderBy(h => h.Id).ToListAsync(ct);
        return habits.Select(h => HabitResponse.From(h, _clock.Today)).ToList();
    }

    public async Task<HabitResponse> GetAsync(int id, CancellationToken ct) =>
        HabitResponse.From(await FindAsync(id, ct), _clock.Today);

    public async Task<HabitResponse> CreateAsync(HabitRequest r, CancellationToken ct)
    {
        var habit = Habit.Create(r.Name, r.Description, r.Frequency ?? HabitFrequency.Daily, _clock.UtcNow);
        _db.Habits.Add(habit);
        await _db.SaveChangesAsync(ct);
        return HabitResponse.From(habit, _clock.Today);
    }

    public async Task<HabitResponse> UpdateAsync(int id, HabitRequest r, CancellationToken ct)
    {
        var habit = await FindAsync(id, ct);
        habit.Update(r.Name, r.Description, r.Frequency ?? habit.Frequency);
        await _db.SaveChangesAsync(ct);
        return HabitResponse.From(habit, _clock.Today);
    }

    public async Task<HabitResponse> SetActiveAsync(int id, bool active, CancellationToken ct)
    {
        var habit = await FindAsync(id, ct);
        if (active) habit.Activate(); else habit.Deactivate();
        await _db.SaveChangesAsync(ct);
        return HabitResponse.From(habit, _clock.Today);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var habit = await FindAsync(id, ct);
        _db.Habits.Remove(habit);
        await PlanService.RemoveItemsForSourceAsync(_db, PlanItemSourceType.Habit, id, _clock.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<HabitCompletionResponse>> ListCompletionsAsync(int id, CancellationToken ct)
    {
        var habit = await FindAsync(id, ct);
        return habit.Completions.OrderByDescending(c => c.CompletionDate).Select(HabitCompletionResponse.From).ToList();
    }

    /// <summary>Completes a habit for a date (default today). Duplicate → 409 (BR-7).</summary>
    public async Task<HabitCompletionResponse> CompleteAsync(int id, DateOnly? date, CancellationToken ct)
    {
        var habit = await FindAsync(id, ct);
        var completion = habit.Complete(date ?? _clock.Today, _clock.Today, _clock.UtcNow);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index (HabitId, CompletionDate) caught a concurrent duplicate.
            throw new ConflictException($"Habit '{id}' is already completed for {completion.CompletionDate:yyyy-MM-dd}.");
        }
        return HabitCompletionResponse.From(completion);
    }

    public async Task UncompleteAsync(int id, DateOnly date, CancellationToken ct)
    {
        var habit = await FindAsync(id, ct);
        habit.Uncomplete(date);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Habit> FindAsync(int id, CancellationToken ct) =>
        await _db.Habits.Include(h => h.Completions).FirstOrDefaultAsync(h => h.Id == id, ct)
        ?? throw new NotFoundException("Habit", id);
}
