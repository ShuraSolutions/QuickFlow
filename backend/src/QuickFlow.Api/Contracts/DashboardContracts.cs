using QuickFlow.Domain.Dashboard;

namespace QuickFlow.Api.Contracts;

/// <summary>Everything the dashboard shows, computed at <c>GeneratedAt</c>.</summary>
/// <param name="Today">The server's local date used for "today" and "overdue".</param>
/// <param name="DisplayName">User's display name from settings (for the greeting).</param>
/// <param name="TasksDueToday">Non-archived tasks due today (any status).</param>
/// <param name="OverdueTasks">Tasks due before today that are not Done.</param>
/// <param name="TasksCompletedToday">Tasks completed today.</param>
/// <param name="HabitsToday">Active habits with today's completion state.</param>
/// <param name="ActivePlans">In-progress plans with live rest time and progress, by priority order.</param>
/// <param name="UpcomingPlans">Not-started plans, soonest start first.</param>
/// <param name="LearningInProgress">Learning cards in progress.</param>
public record DashboardResponse(
    DateTime GeneratedAt,
    DateOnly Today,
    string DisplayName,
    DashboardSummary Summary,
    List<TaskResponse> TasksDueToday,
    List<TaskResponse> OverdueTasks,
    List<TaskResponse> TasksCompletedToday,
    List<HabitResponse> HabitsToday,
    List<PlanResponse> ActivePlans,
    List<PlanResponse> UpcomingPlans,
    List<LearningCardResponse> LearningInProgress);
