# phase-03-habits — Habits & habit completions

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | pending |
| Attempts | 0 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | US Habits 1–5, FR-03, FR-04, BR-6, BR-7, BR-14, §9 unique (HabitId, CompletionDate), §12 Habits, §11 unit tests (habit validation) |
| Requirements source | docs/Task_PRD.md |
| Started | — |
| Ended | — |

## Goal
Habits can be created (daily/weekly), updated, deactivated/reactivated and deleted. A habit can be completed for a date exactly once. Responses expose completion progress (completed today / this period, current streak, total completions).

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 Domain: `Habit` (Id, Name, Description, Frequency, CreatedAt, IsActive) + `HabitFrequency` (Daily, Weekly) + `HabitCompletion` (Id, HabitId, CompletionDate, CreatedAt)
- [ ] T2 Domain rules: name required, trimmed, ≤150 (BR-6); description optional ≤2000; frequency must be defined; completion date not in the future; one completion per habit per date (BR-7)
- [ ] T3 Domain `HabitProgressCalculator`: `completedToday`, `completedThisPeriod` (daily = today, weekly = ISO week Mon–Sun), `currentStreak` (consecutive days/weeks ending in the current or the previous period), `totalCompletions`, `lastCompletedDate`
- [ ] T4 EF mapping + unique index (HabitId, CompletionDate), cascade delete of completions; migration `phase-03-habits`
- [ ] T5 `HabitsController`: `GET /api/habits` (`?isActive=`), `GET /api/habits/{id}`, `POST /api/habits`, `PUT /api/habits/{id}`, `POST /api/habits/{id}/deactivate`, `POST /api/habits/{id}/activate`, `DELETE /api/habits/{id}`
- [ ] T6 Completions: `GET /api/habits/{id}/completions`, `POST /api/habits/{id}/completions` (body `{date?}`, default today) → 201 / 409 on duplicate, `DELETE /api/habits/{id}/completions/{date}` → 204
- [ ] T7 Unit tests for habit rules and streak computation

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] `POST /api/habits` `{name, frequency:"Daily"}` → `201`, `isActive == true`, `frequency == "Daily"`, `completedToday == false`, `currentStreak == 0`
- [ ] C2 `POST /api/habits` with `frequency:"Weekly"` → `201`; `frequency:"Monthly"` → `400`
- [ ] C3 `POST /api/habits` empty name → `400` `errors.Name`; 151-char name → `400`; 150-char → `201`
- [ ] C4 [smoke] `POST /api/habits/{id}/completions` `{}` → `201` with `completionDate == today`; `GET /api/habits/{id}` → `completedToday == true`, `currentStreak >= 1`, `totalCompletions == 1`
- [ ] C5 Second `POST /api/habits/{id}/completions` for the same date → `409` ProblemDetails (duplicate prevented)
- [ ] C6 Completion for yesterday and today → `currentStreak == 2`; future date → `400`
- [ ] C7 `GET /api/habits/{id}/completions` lists completions (newest first); `DELETE /api/habits/{id}/completions/{date}` → `204`, `completedToday` false afterwards; unknown date → `404`
- [ ] C8 `PUT /api/habits/{id}` updates name/description/frequency → `200`
- [ ] C9 `POST /api/habits/{id}/deactivate` → `200` `isActive == false`; `GET /api/habits?isActive=true` excludes it; `POST /activate` → `isActive == true`
- [ ] C10 `DELETE /api/habits/{id}` → `204`; `GET` → `404`; completions of deleted habit → `404`; unknown id → `404`

## Verification

_Not run yet._

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
—

## Notes
—
