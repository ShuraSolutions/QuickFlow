using QuickFlow.Domain.Plans;

namespace QuickFlow.Api.Contracts;

/// <summary>
/// A plan with server-computed progress. Status, completion and rest time are computed at
/// <c>ServerTime</c>; clients count rest time down locally from <c>RestTimeSeconds</c> or <c>EndDateTime</c>.
/// </summary>
/// <param name="CompletionPercentage">Done items / total items × 100 (2 decimals).</param>
/// <param name="IsRestTimeActive">True only while the plan is in progress (between start and end, items still open).</param>
/// <param name="RestTimeSeconds">Seconds remaining until EndDateTime while rest time is active, else null.</param>
/// <param name="SecondsUntilStart">Seconds until StartDateTime for plans not yet started, else null.</param>
/// <param name="TimeElapsedPercentage">Elapsed share of the start→end window (0–100).</param>
/// <param name="StartNotifiedAt">When the start notification was acknowledged (null = not yet).</param>
public record PlanResponse(
    int Id,
    string Title,
    int EstimatedDurationMinutes,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int PriorityOrder,
    PlanStatus Status,
    DateTime CreatedAt,
    DateTime? StartNotifiedAt,
    List<PlanItemResponse> Items,
    int TotalItems,
    int DoneItems,
    double CompletionPercentage,
    bool HasStarted,
    bool HasEnded,
    bool IsRestTimeActive,
    long? RestTimeSeconds,
    long? SecondsUntilStart,
    double TimeElapsedPercentage,
    DateTime ServerTime);

/// <summary>An item of a plan. <c>SourceTitle</c> is the current title/name of the referenced entity.</summary>
public record PlanItemResponse(int Id, int PlanId, PlanItemSourceType SourceType, int SourceId, string? SourceTitle, bool IsDone);

/// <summary>Body for creating or updating a plan.</summary>
public class PlanRequest
{
    /// <summary>Required, at most 200 characters.</summary>
    public string? Title { get; set; }
    /// <summary>Estimated duration in minutes; required, greater than 0.</summary>
    public int? EstimatedDurationMinutes { get; set; }
    /// <summary>Start date-time (ISO 8601). Values without an offset are taken as server local time.</summary>
    public DateTime? StartDateTime { get; set; }
    /// <summary>End date-time; must be after StartDateTime.</summary>
    public DateTime? EndDateTime { get; set; }
    /// <summary>Rank relative to other plans (lower = higher priority, ≥ 0). Create default: after the last plan; update default: unchanged.</summary>
    public int? PriorityOrder { get; set; }
    /// <summary>At least one existing task, habit or learning resource; no duplicates. On update the list replaces the items (retained items keep IsDone).</summary>
    public List<PlanItemRequest>? Items { get; set; }
}

/// <summary>Reference to an existing entity to include in a plan.</summary>
public class PlanItemRequest
{
    public PlanItemSourceType SourceType { get; set; }
    public int SourceId { get; set; }
}

/// <summary>Body for marking a plan item done / not done.</summary>
public class UpdatePlanItemRequest
{
    public bool IsDone { get; set; }
}
