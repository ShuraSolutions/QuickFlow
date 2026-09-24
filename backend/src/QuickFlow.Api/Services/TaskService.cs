using Microsoft.EntityFrameworkCore;
using QuickFlow.Api.Contracts;
using QuickFlow.Api.Data;
using QuickFlow.Domain.Common;
using QuickFlow.Domain.Plans;
using QuickFlow.Domain.Tasks;

namespace QuickFlow.Api.Services;

public class TaskService
{
    private readonly QuickFlowDbContext _db;
    private readonly IClock _clock;

    public TaskService(QuickFlowDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<List<TaskResponse>> ListAsync(TaskQuery q, CancellationToken ct)
    {
        var today = _clock.Today;
        IQueryable<TaskItem> tasks = _db.Tasks.AsNoTracking();

        tasks = q.Archived switch
        {
            ArchivedFilter.Only => tasks.Where(t => t.IsArchived),
            ArchivedFilter.Include => tasks,
            _ => tasks.Where(t => !t.IsArchived),
        };
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            tasks = tasks.Where(t => t.Title.ToLower().Contains(term));
        }
        if (q.Status is { } status) tasks = tasks.Where(t => t.Status == status);
        if (q.Priority is { } priority) tasks = tasks.Where(t => t.Priority == priority);
        if (q.DueDate is { } due) tasks = tasks.Where(t => t.DueDate == due);
        if (q.DueFrom is { } from) tasks = tasks.Where(t => t.DueDate >= from);
        if (q.DueTo is { } to) tasks = tasks.Where(t => t.DueDate <= to);
        if (q.Overdue is true)
            tasks = tasks.Where(t => t.DueDate < today && t.Status != TaskItemStatus.Done && !t.IsArchived);
        else if (q.Overdue is false)
            tasks = tasks.Where(t => !(t.DueDate < today && t.Status != TaskItemStatus.Done && !t.IsArchived));

        var asc = q.SortDir == SortDirection.Asc;
        tasks = q.SortBy == TaskSortBy.DueDate
            ? (asc
                ? tasks.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ThenBy(t => t.Id)
                : tasks.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate).ThenByDescending(t => t.Id))
            : (asc
                ? tasks.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
                : tasks.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id));

        var list = await tasks.ToListAsync(ct);
        return list.Select(t => TaskResponse.From(t, today)).ToList();
    }

    public async Task<TaskResponse> GetAsync(int id, CancellationToken ct) =>
        TaskResponse.From(await FindAsync(id, ct), _clock.Today);

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest r, CancellationToken ct)
    {
        var task = TaskItem.Create(r.Title, r.Description, r.Status ?? TaskItemStatus.Todo,
            r.Priority ?? TaskPriority.Medium, r.DueDate, _clock.UtcNow);
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);
        return TaskResponse.From(task, _clock.Today);
    }

    public async Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest r, CancellationToken ct)
    {
        var task = await FindAsync(id, ct);
        task.Update(r.Title, r.Description, r.Status ?? task.Status, r.Priority ?? task.Priority, r.DueDate, _clock.UtcNow);
        await _db.SaveChangesAsync(ct);
        return TaskResponse.From(task, _clock.Today);
    }

    public Task<TaskResponse> CompleteAsync(int id, CancellationToken ct) => MutateAsync(id, t => t.Complete(_clock.UtcNow), ct);

    public Task<TaskResponse> ArchiveAsync(int id, CancellationToken ct) => MutateAsync(id, t => t.Archive(_clock.UtcNow), ct);

    public Task<TaskResponse> RestoreAsync(int id, CancellationToken ct) => MutateAsync(id, t => t.Restore(_clock.UtcNow), ct);

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var task = await FindAsync(id, ct);
        _db.Tasks.Remove(task);
        await PlanService.RemoveItemsForSourceAsync(_db, PlanItemSourceType.Task, id, _clock.UtcNow, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<TaskResponse> MutateAsync(int id, Action<TaskItem> change, CancellationToken ct)
    {
        var task = await FindAsync(id, ct);
        change(task);
        await _db.SaveChangesAsync(ct);
        return TaskResponse.From(task, _clock.Today);
    }

    private async Task<TaskItem> FindAsync(int id, CancellationToken ct) =>
        await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Task", id);
}
