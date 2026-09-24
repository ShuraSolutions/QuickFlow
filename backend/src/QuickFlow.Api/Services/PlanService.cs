using Microsoft.EntityFrameworkCore;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Data;
using QuickFlow.Domain.Common;
using QuickFlow.Domain.Learning;
using QuickFlow.Domain.Plans;

namespace QuickFlow.Api.Services;

public class PlanService
{
    private readonly QuickFlowDbContext _db;
    private readonly IClock _clock;

    public PlanService(QuickFlowDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>All plans ordered by priority order, then start; optionally filtered by computed status.</summary>
    public async Task<List<PlanResponse>> ListAsync(PlanStatus? status, CancellationToken ct)
    {
        var plans = await LoadRefreshedAsync(ct);
        return await ToResponsesAsync(plans.Where(p => status is null || p.Status == status), ct);
    }

    /// <summary>Completed plans (all items done or end passed), most recently ended first.</summary>
    public async Task<List<PlanResponse>> HistoryAsync(CancellationToken ct)
    {
        var plans = await LoadRefreshedAsync(ct);
        return await ToResponsesAsync(plans.Where(p => p.Status == PlanStatus.Completed)
            .OrderByDescending(p => p.EndDateTime).ThenByDescending(p => p.Id), ct);
    }

    /// <summary>Plans whose start time has been reached, that have not ended, and whose start was not acknowledged.</summary>
    public async Task<List<PlanResponse>> PendingNotificationsAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var plans = await LoadRefreshedAsync(ct);
        return await ToResponsesAsync(plans.Where(p => p.StartNotifiedAt is null && p.StartDateTime <= now && now < p.EndDateTime), ct);
    }

    public async Task<PlanResponse> AcknowledgeStartAsync(int id, CancellationToken ct)
    {
        var plan = await FindAsync(id, ct);
        plan.AcknowledgeStart(_clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(plan, ct);
    }

    public async Task<PlanResponse> GetAsync(int id, CancellationToken ct)
    {
        var plan = await FindAsync(id, ct);
        if (plan.RefreshStatus(_clock.UtcNow)) await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(plan, ct);
    }

    public async Task<PlanResponse> CreateAsync(PlanRequest r, CancellationToken ct)
    {
        var items = ToSpecs(r.Items);
        await EnsureSourcesExistAsync(items, ct);
        var priority = r.PriorityOrder ?? (await _db.Plans.MaxAsync(p => (int?)p.PriorityOrder, ct) ?? 0) + 1;
        var plan = Plan.Create(r.Title, r.EstimatedDurationMinutes, Time.ToUtc(r.StartDateTime), Time.ToUtc(r.EndDateTime),
            priority, items, _clock.UtcNow);
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(plan, ct);
    }

    public async Task<PlanResponse> UpdateAsync(int id, PlanRequest r, CancellationToken ct)
    {
        var plan = await FindAsync(id, ct);
        var items = ToSpecs(r.Items);
        await EnsureSourcesExistAsync(items, ct);
        plan.Update(r.Title, r.EstimatedDurationMinutes, Time.ToUtc(r.StartDateTime), Time.ToUtc(r.EndDateTime),
            r.PriorityOrder ?? plan.PriorityOrder, items, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(plan, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var plan = await FindAsync(id, ct);
        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Marks a plan item done / not done. BR-13: the item represents its source's own completion action,
    /// so a Task item completes/reopens the task, a Habit item adds/removes today's completion, and a
    /// LearningResource item sets the card to Completed / back to InProgress.
    /// </summary>
    public async Task<PlanResponse> SetItemDoneAsync(int id, int itemId, bool isDone, CancellationToken ct)
    {
        var plan = await FindAsync(id, ct);
        var item = plan.SetItemDone(itemId, isDone, _clock.UtcNow);
        await PropagateToSourceAsync(item, isDone, ct);
        await _db.SaveChangesAsync(ct);
        return await ToResponseAsync(plan, ct);
    }

    /// <summary>BR-14: removes plan items that reference a deleted source (saved with the caller's SaveChanges).</summary>
    public static async Task RemoveItemsForSourceAsync(QuickFlowDbContext db, PlanItemSourceType type, int sourceId,
        DateTime utcNow, CancellationToken ct)
    {
        var plans = await db.Plans.Include(p => p.Items)
            .Where(p => p.Items.Any(i => i.SourceType == type && i.SourceId == sourceId))
            .ToListAsync(ct);
        foreach (var plan in plans)
        {
            plan.RemoveItemsFor(type, sourceId);
            plan.RefreshStatus(utcNow);
        }
    }

    private async Task PropagateToSourceAsync(PlanItem item, bool isDone, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        switch (item.SourceType)
        {
            case PlanItemSourceType.Task:
                var task = await _db.Tasks.FindAsync(new object[] { item.SourceId }, ct);
                if (task is null) break;
                if (isDone) task.Complete(now);
                else if (task.Status == Domain.Tasks.TaskItemStatus.Done) task.Reopen(now);
                break;

            case PlanItemSourceType.Habit:
                var habit = await _db.Habits.Include(h => h.Completions).FirstOrDefaultAsync(h => h.Id == item.SourceId, ct);
                if (habit is null) break;
                var today = _clock.Today;
                if (isDone && !habit.IsCompletedOn(today)) habit.Complete(today, today, now);
                else if (!isDone && habit.IsCompletedOn(today)) habit.Uncomplete(today);
                break;

            case PlanItemSourceType.LearningResource:
                var card = await _db.LearningCards.FindAsync(new object[] { item.SourceId }, ct);
                if (card is null) break;
                if (isDone) card.SetStatus(LearningStatus.Completed);
                else if (card.Status == LearningStatus.Completed) card.SetStatus(LearningStatus.InProgress);
                break;
        }
    }

    private static List<PlanItemSpec>? ToSpecs(List<PlanItemRequest>? items) =>
        items?.Select(i => new PlanItemSpec(i.SourceType, i.SourceId)).ToList();

    /// <summary>BR-10: every referenced task/habit/learning resource must exist.</summary>
    private async Task EnsureSourcesExistAsync(List<PlanItemSpec>? items, CancellationToken ct)
    {
        if (items is null || items.Count == 0) return; // reported by the domain rule
        var titles = await LoadSourceTitlesAsync(items.Select(i => (i.SourceType, i.SourceId)), ct);
        var missing = items.Where(i => !titles.ContainsKey((i.SourceType, i.SourceId)))
            .Select(i => $"{i.SourceType} {i.SourceId}").ToList();
        if (missing.Count > 0)
            throw new DomainValidationException("Items", $"Referenced items do not exist: {string.Join(", ", missing)}.");
    }

    private async Task<Dictionary<(PlanItemSourceType, int), string>> LoadSourceTitlesAsync(
        IEnumerable<(PlanItemSourceType Type, int Id)> refs, CancellationToken ct)
    {
        var list = refs.ToList();
        var taskIds = list.Where(r => r.Type == PlanItemSourceType.Task).Select(r => r.Id).Distinct().ToList();
        var habitIds = list.Where(r => r.Type == PlanItemSourceType.Habit).Select(r => r.Id).Distinct().ToList();
        var cardIds = list.Where(r => r.Type == PlanItemSourceType.LearningResource).Select(r => r.Id).Distinct().ToList();

        var result = new Dictionary<(PlanItemSourceType, int), string>();
        if (taskIds.Count > 0)
            foreach (var t in await _db.Tasks.Where(t => taskIds.Contains(t.Id)).Select(t => new { t.Id, t.Title }).ToListAsync(ct))
                result[(PlanItemSourceType.Task, t.Id)] = t.Title;
        if (habitIds.Count > 0)
            foreach (var h in await _db.Habits.Where(h => habitIds.Contains(h.Id)).Select(h => new { h.Id, h.Name }).ToListAsync(ct))
                result[(PlanItemSourceType.Habit, h.Id)] = h.Name;
        if (cardIds.Count > 0)
            foreach (var c in await _db.LearningCards.Where(c => cardIds.Contains(c.Id)).Select(c => new { c.Id, c.Title }).ToListAsync(ct))
                result[(PlanItemSourceType.LearningResource, c.Id)] = c.Title;
        return result;
    }

    private async Task<List<Plan>> LoadRefreshedAsync(CancellationToken ct)
    {
        var plans = await _db.Plans.Include(p => p.Items).ToListAsync(ct);
        var now = _clock.UtcNow;
        var changed = false;
        foreach (var p in plans) changed |= p.RefreshStatus(now);
        if (changed) await _db.SaveChangesAsync(ct);
        return plans.OrderBy(p => p.PriorityOrder).ThenBy(p => p.StartDateTime).ThenBy(p => p.Id).ToList();
    }

    private async Task<PlanResponse> ToResponseAsync(Plan plan, CancellationToken ct) =>
        (await ToResponsesAsync(new[] { plan }, ct))[0];

    private async Task<List<PlanResponse>> ToResponsesAsync(IEnumerable<Plan> plans, CancellationToken ct)
    {
        var list = plans.ToList();
        var titles = await LoadSourceTitlesAsync(list.SelectMany(p => p.Items).Select(i => (i.SourceType, i.SourceId)), ct);
        var now = _clock.UtcNow;
        return list.Select(p =>
        {
            var g = p.Progress(now);
            var items = p.Items.OrderBy(i => i.Id)
                .Select(i => new PlanItemResponse(i.Id, p.Id, i.SourceType, i.SourceId,
                    titles.GetValueOrDefault((i.SourceType, i.SourceId)), i.IsDone))
                .ToList();
            return new PlanResponse(p.Id, p.Title, p.EstimatedDurationMinutes, p.StartDateTime, p.EndDateTime,
                p.PriorityOrder, g.Status, p.CreatedAt, p.StartNotifiedAt, items, g.TotalItems, g.DoneItems,
                g.CompletionPercentage, g.HasStarted, g.HasEnded, g.IsRestTimeActive, g.RestTimeSeconds,
                g.SecondsUntilStart, g.TimeElapsedPercentage, now);
        }).ToList();
    }

    private async Task<Plan> FindAsync(int id, CancellationToken ct) =>
        await _db.Plans.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new NotFoundException("Plan", id);
}
