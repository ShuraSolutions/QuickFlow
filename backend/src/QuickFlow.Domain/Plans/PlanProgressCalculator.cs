using QuickFlow.Domain.Common;

namespace QuickFlow.Domain.Plans;

/// <summary>Time- and item-based progress of a plan at one instant.</summary>
/// <param name="Status">NotStarted before start; InProgress between start and end; Completed when all items are done or the end has passed.</param>
/// <param name="CompletionPercentage">Done items / total items × 100, 2 decimals.</param>
/// <param name="IsRestTimeActive">True only while the plan is in progress, i.e. between start and end (BR-12) with items still open.</param>
/// <param name="RestTimeSeconds">Seconds until EndDateTime while rest time is active; otherwise null.</param>
/// <param name="SecondsUntilStart">Seconds until StartDateTime before the plan starts; otherwise null.</param>
/// <param name="TimeElapsedPercentage">Share of the start→end window that has elapsed (0–100).</param>
public record PlanProgress(
    PlanStatus Status,
    int TotalItems,
    int DoneItems,
    double CompletionPercentage,
    bool HasStarted,
    bool HasEnded,
    bool IsRestTimeActive,
    long? RestTimeSeconds,
    long? SecondsUntilStart,
    double TimeElapsedPercentage);

public static class PlanProgressCalculator
{
    public static PlanProgress Compute(DateTime startUtc, DateTime endUtc, int totalItems, int doneItems, DateTime nowUtc)
    {
        var hasStarted = nowUtc >= startUtc;
        var hasEnded = nowUtc >= endUtc;
        var allDone = totalItems > 0 && doneItems >= totalItems;

        var status = allDone || hasEnded ? PlanStatus.Completed
            : hasStarted ? PlanStatus.InProgress
            : PlanStatus.NotStarted;

        var restActive = status == PlanStatus.InProgress;
        long? rest = restActive ? (long)Math.Ceiling((endUtc - nowUtc).TotalSeconds) : null;
        long? untilStart = hasStarted ? null : (long)Math.Ceiling((startUtc - nowUtc).TotalSeconds);

        var window = (endUtc - startUtc).TotalSeconds;
        var elapsed = window <= 0 ? 100
            : Math.Round(Math.Clamp((nowUtc - startUtc).TotalSeconds / window * 100, 0, 100), 2);

        return new PlanProgress(status, totalItems, doneItems, Percent.Of(doneItems, totalItems),
            hasStarted, hasEnded, restActive, rest, untilStart, elapsed);
    }
}
