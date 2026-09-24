# phase-05-plans — Todo plans, items, progress & rest time

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | pending |
| Attempts | 0 / 3 |
| Depends on | phase-02-tasks, phase-03-habits, phase-04-learning |
| Requirements covered | US Todo Plans 1–10, FR-07, FR-08 (backend: status, rest time, completion %, start notification), BR-10..14, NFR Reliability (status from stored times), §12 Todo Plans, §11 unit tests (plan validation, rest time, completion roll-up, lifecycle) |
| Requirements source | docs/Task_PRD.md |
| Started | — |
| Ended | — |

## Goal
Plans can be composed from existing tasks, habits and learning cards with estimated duration, start/end date-time and priority order. Plan items can be marked done (propagating to the source per BR-13). Every plan response carries server-computed status, completion percentage and rest time. Start notifications and plan history are available.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 Domain: `Plan` (Id, Title, EstimatedDurationMinutes, StartDateTime, EndDateTime, PriorityOrder, Status, CreatedAt, StartNotifiedAt) + `PlanStatus` (NotStarted, InProgress, Completed); `PlanItem` (Id, PlanId, SourceType, SourceId, IsDone) + `PlanItemSourceType` (Task, Habit, LearningResource)
- [ ] T2 Domain rules: title required ≤200; ≥1 item (BR-10); End > Start (BR-11); estimated duration > 0 minutes; priority order ≥ 0; no duplicate (SourceType, SourceId) items
- [ ] T3 Domain `PlanProgressCalculator`: completion % = done/total; status from now + items (all done → Completed; now ≥ End → Completed; Start ≤ now < End → InProgress; else NotStarted); rest time = End − now only while Start ≤ now < End (BR-12), otherwise null; `secondsUntilStart`, `hasStarted`, `hasEnded`
- [ ] T4 EF mapping (items cascade) + migration `phase-05-plans`; stored Status refreshed from the calculator whenever plans are read or changed
- [ ] T5 Service validates that every referenced source exists (400 otherwise); item responses include `sourceTitle`
- [ ] T6 `PlansController`: `GET /api/plans` (`?status=`, ordered by priorityOrder then start), `GET /api/plans/{id}`, `POST /api/plans`, `PUT /api/plans/{id}` (replaces items, keeps IsDone of retained items), `DELETE /api/plans/{id}`
- [ ] T7 `PATCH /api/plans/{id}/items/{itemId}` `{isDone}`; BR-13 propagation: Task item → task Done/Todo; Habit item → add/remove today's completion; LearningResource item → card Completed/InProgress
- [ ] T8 `GET /api/plans/history` (completed/ended plans with completion %, newest end first); `GET /api/plans/notifications` (started, not ended, not acknowledged) and `POST /api/plans/{id}/notifications/ack`
- [ ] T9 BR-14: deleting a task/habit/learning card removes the plan items that reference it
- [ ] T10 Unit tests: plan validation, rest time, completion roll-up, lifecycle transitions

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] `POST /api/plans` with 1 task + 1 habit + 1 learning card, start in the past, end in the future → `201`, `status == "InProgress"`, `totalItems == 3`, `completionPercentage == 0`, `isRestTimeActive == true`, `restTimeSeconds > 0`, items have `sourceTitle`
- [ ] C2 `POST /api/plans` with `items: []` → `400` (BR-10); with a non-existent source id → `400`
- [ ] C3 `POST /api/plans` with end ≤ start → `400` (BR-11); `estimatedDurationMinutes: 0` → `400`; empty title → `400`; `sourceType:"Note"` → `400`
- [ ] C4 Future plan (start tomorrow) → `status == "NotStarted"`, `isRestTimeActive == false`, `restTimeSeconds == null`, `secondsUntilStart > 0`
- [ ] C5 [smoke] `PATCH /api/plans/{id}/items/{itemId}` `{isDone:true}` on the task item → `200`, `doneItems == 1`, `completionPercentage == 33.33`; underlying task `status == "Done"` (BR-13)
- [ ] C6 Habit item done → habit `completedToday == true`; learning item done → card `status == "Completed"`; all items done → plan `status == "Completed"`, `completionPercentage == 100`
- [ ] C7 Un-marking the task item → task back to `Todo`, plan `InProgress` again
- [ ] C8 `PUT /api/plans/{id}` updates title, duration, times, priority order and items → `200`, retained item keeps `isDone`
- [ ] C9 `GET /api/plans` ordered by `priorityOrder` asc; `?status=NotStarted` filters
- [ ] C10 Plan with start and end in the past → `status == "Completed"`, `hasEnded == true`, appears in `GET /api/plans/history` with its `completionPercentage`
- [ ] C11 `GET /api/plans/notifications` includes a started, unacknowledged plan; after `POST /api/plans/{id}/notifications/ack` → `200`, `startNotifiedAt` set, plan no longer listed
- [ ] C12 `DELETE /api/plans/{id}` → `204`; `GET` → `404`; unknown item id → `404`
- [ ] C13 Deleting a task referenced by a plan removes that plan item (`totalItems` decreases) (BR-14)

## Verification

_Not run yet._

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
—

## Notes
—
