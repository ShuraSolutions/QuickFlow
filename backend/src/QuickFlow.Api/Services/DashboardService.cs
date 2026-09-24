using Microsoft.EntityFrameworkCore;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Data;
using QuickFlow.Domain.Common;
using QuickFlow.Domain.Dashboard;
using QuickFlow.Domain.Learning;
using QuickFlow.Domain.Plans;
using QuickFlow.Domain.Settings;

namespace QuickFlow.Api.Services;

public class DashboardService
{
    private readonly QuickFlowDbContext _db;
    private readonly PlanService _plans;
    private readonly IClock _clock;

    public DashboardService(QuickFlowDbContext db, PlanService plans, IClock clock)
    {
        _db = db;
        _plans = plans;
        _clock = clock;
    }

    public async Task<DashboardResponse> GetAsync(CancellationToken ct)
    {
        var today = _clock.Today;
        var tasks = await _db.Tasks.AsNoTracking().Where(t => !t.IsArchived).ToListAsync(ct);
        var habits = await _db.Habits.AsNoTracking().Include(h => h.Completions).ToListAsync(ct);
        var cards = await _db.LearningCards.AsNoTracking().Include(c => c.Milestones).Include(c => c.Notes).AsSplitQuery().ToListAsync(ct);
        var plans = await _plans.ListAsync(null, ct);
        var settings = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(ct);

        var planProgress = plans.Select(p => new PlanProgress(p.Status, p.TotalItems, p.DoneItems, p.CompletionPercentage,
            p.HasStarted, p.HasEnded, p.IsRestTimeActive, p.RestTimeSeconds, p.SecondsUntilStart, p.TimeElapsedPercentage));
        var summary = DashboardCalculator.Compute(tasks, habits, planProgress, cards, today);

        return new DashboardResponse(
            _clock.UtcNow,
            today,
            settings?.DisplayName ?? UserSettings.DefaultDisplayName,
            summary,
            tasks.Where(t => DashboardCalculator.IsDueToday(t, today)).OrderBy(t => t.Status).ThenByDescending(t => t.Priority)
                .Select(t => TaskResponse.From(t, today)).ToList(),
            tasks.Where(t => t.IsOverdue(today)).OrderBy(t => t.DueDate).ThenByDescending(t => t.Priority)
                .Select(t => TaskResponse.From(t, today)).ToList(),
            tasks.Where(t => DashboardCalculator.IsCompletedOn(t, today)).OrderByDescending(t => t.CompletedAt)
                .Select(t => TaskResponse.From(t, today)).ToList(),
            habits.Where(h => h.IsActive).OrderBy(h => h.Id).Select(h => HabitResponse.From(h, today)).ToList(),
            plans.Where(p => p.Status == PlanStatus.InProgress).ToList(),
            plans.Where(p => p.Status == PlanStatus.NotStarted).OrderBy(p => p.StartDateTime).ToList(),
            cards.Where(c => c.Status == LearningStatus.InProgress).OrderByDescending(c => c.CreatedAt)
                .Select(LearningCardResponse.From).ToList());
    }
}
