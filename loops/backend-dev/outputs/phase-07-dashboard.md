# phase-07-dashboard — Dashboard summary

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | pending |
| Attempts | 0 / 3 |
| Depends on | phase-02-tasks, phase-03-habits, phase-04-learning, phase-05-plans |
| Requirements covered | US Dashboard 1–6, FR-09, FR-08 (dashboard reflects item changes), §12 Dashboard, §11 unit tests (dashboard metrics match data) |
| Requirements source | docs/Task_PRD.md |
| Started | — |
| Ended | — |

## Goal
One aggregate endpoint that returns everything the dashboard shows, computed in the domain from the underlying data: today's, overdue and completed-today tasks, task completion %, active habits with today's completion, active plans with rest time and progress, and a learning snapshot.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 Domain `DashboardCalculator`: task counts (non-archived total, todo, inProgress, done, dueToday, overdue, completedToday) and `taskCompletionPercentage` = done / non-archived total; habit counts (active, completedToday) and `habitCompletionPercentage`; plan counts (inProgress, upcoming, completed); learning snapshot (cards by status, milestones total/done, milestones completed in the last 7 days)
- [ ] T2 `DashboardController`: `GET /api/dashboard` returning `summary` metrics + lists `tasksDueToday`, `overdueTasks`, `tasksCompletedToday`, `habitsToday` (active habits with `completedToday`), `activePlans` (in-progress plans with `restTimeSeconds`, `completionPercentage`), `upcomingPlans`, `learningInProgress`
- [ ] T3 Unit tests: dashboard metrics match the underlying data

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] `GET /api/dashboard` → `200` with `summary.tasks`, `summary.habits`, `summary.plans`, `summary.learning`, `tasksDueToday`, `overdueTasks`, `habitsToday`, `activePlans`
- [ ] C2 Task metrics match data: counts and `taskCompletionPercentage` equal values computed from `GET /api/tasks`; a task due today appears in `tasksDueToday`, a past-due one in `overdueTasks`, a completed-today one in `tasksCompletedToday`; archived tasks excluded
- [ ] C3 Habit metrics: `summary.habits.active` and `completedToday` match; completing a habit increments `completedToday`
- [ ] C4 Plan metrics: an in-progress plan is listed in `activePlans` with `restTimeSeconds > 0` and `completionPercentage`; marking a plan item done is reflected immediately in the dashboard (FR-08)
- [ ] C5 Learning snapshot: cards by status and milestones done match `GET /api/learning-cards`

## Verification

_Not run yet._

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
—

## Notes
—
