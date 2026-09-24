# phase-07-dashboard — Dashboard summary

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | phase-02-tasks, phase-03-habits, phase-04-learning, phase-05-plans |
| Requirements covered | US Dashboard 1–6, FR-09, FR-08 (dashboard reflects item changes), §12 Dashboard, §11 unit tests (dashboard metrics match data) |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T13:08:38+03:00 |
| Ended | 2026-09-24T13:11:51+03:00 |

## Goal
One aggregate endpoint that returns everything the dashboard shows, computed in the domain from the underlying data: today's, overdue and completed-today tasks, task completion %, active habits with today's completion, active plans with rest time and progress, and a learning snapshot.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Domain `DashboardCalculator`: task counts (non-archived total, todo, inProgress, done, dueToday, overdue, completedToday) and `taskCompletionPercentage` = done / non-archived total; habit counts (active, completedToday) and `habitCompletionPercentage`; plan counts (inProgress, upcoming, completed); learning snapshot (cards by status, milestones total/done, milestones completed in the last 7 days)
- [x] T2 `DashboardController`: `GET /api/dashboard` returning `summary` metrics + lists `tasksDueToday`, `overdueTasks`, `tasksCompletedToday`, `habitsToday` (active habits with `completedToday`), `activePlans` (in-progress plans with `restTimeSeconds`, `completionPercentage`), `upcomingPlans`, `learningInProgress`
- [x] T3 Unit tests: dashboard metrics match the underlying data

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `GET /api/dashboard` → `200` with `summary.tasks`, `summary.habits`, `summary.plans`, `summary.learning`, `tasksDueToday`, `overdueTasks`, `habitsToday`, `activePlans`
- [x] C2 Task metrics match data: counts and `taskCompletionPercentage` equal values computed from `GET /api/tasks`; a task due today appears in `tasksDueToday`, a past-due one in `overdueTasks`, a completed-today one in `tasksCompletedToday`; archived tasks excluded
- [x] C3 Habit metrics: `summary.habits.active` and `completedToday` match; completing a habit increments `completedToday`
- [x] C4 Plan metrics: an in-progress plan is listed in `activePlans` with `restTimeSeconds > 0` and `completionPercentage`; marking a plan item done is reflected immediately in the dashboard (FR-08)
- [x] C5 Learning snapshot: cards by status and milestones done match `GET /api/learning-cards`

## Verification

### Attempt 1 — 2026-09-24T13:10:46+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`)
**API start:** PASS (configured stop, then configured run command in background; `health 200 after 1 s`)

Check script output (commands prefixed with `$`; `» PASS/FAIL` lines are assertions evaluated on the real response bodies; bodies over 1500 chars trimmed with …):

```text
today=2026-09-24

#### setup: data for today
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Dash due today","dueDate":"2026-09-24","priority":"High"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/16
{"id":16,"title":"Dash due today","description":null,"status":"Todo","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:46.7301891Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Dash overdue","dueDate":"2026-09-23"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/17
{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T10:10:47.4815616Z","updatedAt":"2026-09-24T10:10:47.4815616Z","completedAt":null,"isArchived":false,"isOverdue":true}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Dash completed today"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/18
{"id":18,"title":"Dash completed today","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:10:48.004121Z","updatedAt":"2026-09-24T10:10:48.004121Z","completedAt":null,"isArchived":false,"isOverdue":false}

(completed task 18)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Dash archived","dueDate":"2026-09-24"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/19
{"id":19,"title":"Dash archived","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:49.059446Z","updatedAt":"2026-09-24T10:10:49.059446Z","completedAt":null,"isArchived":false,"isOverdue":false}

(archived task 19)

#### C1 [smoke] dashboard shape
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/dashboard
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"generatedAt":"2026-09-24T10:10:50.1516465Z","today":"2026-09-24","displayName":"Hamada","summary":{"tasks":{"total":11,"todo":7,"inProgress":0,"done":4,"dueToday":2,"overdue":2,"completedToday":4,"completionPercentage":36.36},"habits":{"total":3,"active":3,"completedToday":1,"completionPercentage":33.33},"plans":{"total":2,"inProgress":1,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":50},"learning":{"totalCards":2,"notStarted":0,"inProgress":1,"completed":1,"milestonesTotal":1,"milestonesDone":1,"milestoneCompletionPercentage":100,"milestonesCompletedLast7Days":1}},"tasksDueToday":[{"id":16,"title":"Dash due today","description":null,"status":"Todo","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:46.7301891Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false}],"overdueTasks":[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":"Medium","dueDate":"2026 …
» PASS status (= 200)
» PASS summary.tasks/habits/plans/learning + lists present

#### C2 task metrics match GET /api/tasks
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":18,"title":"Dash completed today","description":null,"status":"Done","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:10:48.004121Z","updatedAt":"2026-09-24T10:10:48.6688378Z","completedAt":"2026-09-24T10:10:48.6688378Z","isArchived":false,"isOverdue":false},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T10:10:47.4815616Z","updatedAt":"2026-09-24T10:10:47.4815616Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":16,"title":"Dash due today","description":null,"status":"Todo","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:46.7301891Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":11,"title":"Plan task A","description":null,"status":"Todo","priority":"High","dueDate":null,"createdAt":"2026-09-24T10:04:04.3366463Z","updatedAt":"2026-09-24T10:04:15.4271457Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":7,"title":"Done late zq1790243391","description":null,"status":"Done","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.8016496Z","updatedAt":"2026-09-24T09:50:06.8016496Z","comple …
(GET /api/tasks returned 11 non-archived tasks)
  computed: {"dash":{"total":11,"todo":7,"inProgress":0,"done":4,"dueToday":2,"overdue":2,"completedToday":4,"completionPercentage":36.36},"fromTasks":{"total":11,"todo":7,"inProgress":0,"done":4,"dueToday":2,"overdue":2,"pct":36.36}}
» PASS counts equal
» PASS taskCompletionPercentage = done/total
» PASS due-today task in tasksDueToday; archived excluded
» PASS overdue task in overdueTasks (all isOverdue)
» PASS completed-today task in tasksCompletedToday; count matches

#### C3 habit metrics
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits?isActive=true
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"name":"Evening run","description":"3 km","frequency":"Weekly","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":false,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-23"},{"id":2,"name":"Weekly review","description":null,"frequency":"Weekly","createdAt":"2026-09-24T09:54:47.0980437Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null},{"id":5,"name":"Plan habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:04:05.5565155Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}]

» PASS active + completedToday match GET /api/habits?isActive=true
  computed: {"dash":{"total":3,"active":3,"completedToday":1,"completionPercentage":33.33},"active":3,"completedToday":1}
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"Dash habit","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/9
{"id":9,"name":"Dash habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:10:52.5810838Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/dashboard
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"generatedAt":"2026-09-24T10:10:53.1184665Z","today":"2026-09-24","displayName":"Hamada","summary":{"tasks":{"total":11,"todo":7,"inProgress":0,"done":4,"dueToday":2,"overdue":2,"completedToday":4,"completionPercentage":36.36},"habits":{"total":4,"active":4,"completedToday":1,"completionPercentage":25},"plans":{"total":2,"inProgress":1,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":50},"learning":{"totalCards":2,"notStarted":0,"inProgress":1,"completed":1,"milestonesTotal":1,"milestonesDone":1,"milestoneCompletionPercentage":100,"milestonesCompletedLast7Days":1}},"tasksDueToday":[{"id":16,"title":"Dash due today","description":null,"status":"Todo","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:46.7301891Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false}],"overdueTasks":[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09 …
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/9/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/9/completions
{"id":7,"habitId":9,"completionDate":"2026-09-24","createdAt":"2026-09-24T10:10:53.5740075Z"}

» PASS complete habit (= 201)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/dashboard
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"generatedAt":"2026-09-24T10:10:53.9800395Z","today":"2026-09-24","displayName":"Hamada","summary":{"tasks":{"total":11,"todo":7,"inProgress":0,"done":4,"dueToday":2,"overdue":2,"completedToday":4,"completionPercentage":36.36},"habits":{"total":4,"active":4,"completedToday":2,"completionPercentage":50},"plans":{"total":2,"inProgress":1,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":50},"learning":{"totalCards":2,"notStarted":0,"inProgress":1,"completed":1,"milestonesTotal":1,"milestonesDone":1,"milestoneCompletionPercentage":100,"milestonesCompletedLast7Days":1}},"tasksDueToday":[{"id":16,"title":"Dash due today","description":null,"status":"Todo","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:46.7301891Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false}],"overdueTasks":[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09 …
» PASS completing a habit increments completedToday by 1

#### C4 plans on dashboard + item change reflected immediately
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Dash plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:40:54Z","endDateTime":"2026-09-24T11:40:54Z","items":[{"sourceType":"Task","sourceId":16},{"sourceType":"Habit","sourceId":9}]}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/plans/5
{"id":5,"title":"Dash plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:40:54Z","endDateTime":"2026-09-24T11:40:54Z","priorityOrder":5,"status":"InProgress","createdAt":"2026-09-24T10:10:54.6071616Z","startNotifiedAt":null,"items":[{"id":11,"planId":5,"sourceType":"Task","sourceId":16,"sourceTitle":"Dash due today","isDone":false},{"id":12,"planId":5,"sourceType":"Habit","sourceId":9,"sourceTitle":"Dash habit","isDone":false}],"totalItems":2,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":5400,"secondsUntilStart":null,"timeElapsedPercentage":25.01,"serverTime":"2026-09-24T10:10:54.6449143Z"}

» PASS create plan (= 201)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/dashboard
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"generatedAt":"2026-09-24T10:10:55.2484961Z","today":"2026-09-24","displayName":"Hamada","summary":{"tasks":{"total":11,"todo":7,"inProgress":0,"done":4,"dueToday":2,"overdue":2,"completedToday":4,"completionPercentage":36.36},"habits":{"total":4,"active":4,"completedToday":2,"completionPercentage":50},"plans":{"total":3,"inProgress":2,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":25},"learning":{"totalCards":2,"notStarted":0,"inProgress":1,"completed":1,"milestonesTotal":1,"milestonesDone":1,"milestoneCompletionPercentage":100,"milestonesCompletedLast7Days":1}},"tasksDueToday":[{"id":16,"title":"Dash due today","description":null,"status":"Todo","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:46.7301891Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false}],"overdueTasks":[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09 …
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":"2026-09-24T10:04:20.2386642Z","items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true}],"totalItems":2,"doneItems":1,"completionPercentage":50,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":85989,"secondsUntilStart":null,"timeElapsedPercentage":4.46,"serverTime":"2026-09-24T10:10:55.6342015Z"},{"id":3,"title":"Yesterday review","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T07:04:04Z","endDateTime":"2026-09-24T09:04:04Z","priorityOrder":4,"status":"Completed","createdAt":"2026-09-24T10:04:18.476645Z","startNotifiedAt":null,"items":[{"id":7,"planId":3,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false}],"totalItems":1,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":true,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":null,"timeElapsedPercentage":100,"serverTime":"2026-09-24T10:10:55.6342015Z"},{"id":5,"title":"Dash plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:40:54Z","endDateTime":"2026-09-24T11:40:54Z","priorityOrder":5,"status":"InProgress","createdAt":"2026-09-24T10:1 …
» PASS plan in activePlans with restTimeSeconds > 0 and completionPercentage 0 (item flags start false)
» PASS plan counts match GET /api/plans
  computed: {"total":3,"inProgress":2,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":25}
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/5/items/11 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":5,"title":"Dash plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:40:54Z","endDateTime":"2026-09-24T11:40:54Z","priorityOrder":5,"status":"InProgress","createdAt":"2026-09-24T10:10:54.6071616Z","startNotifiedAt":null,"items":[{"id":11,"planId":5,"sourceType":"Task","sourceId":16,"sourceTitle":"Dash due today","isDone":true},{"id":12,"planId":5,"sourceType":"Habit","sourceId":9,"sourceTitle":"Dash habit","isDone":false}],"totalItems":2,"doneItems":1,"completionPercentage":50,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":5398,"secondsUntilStart":null,"timeElapsedPercentage":25.03,"serverTime":"2026-09-24T10:10:56.4405612Z"}

» PASS mark plan task item done (= 200)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/dashboard
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"generatedAt":"2026-09-24T10:10:56.9129083Z","today":"2026-09-24","displayName":"Hamada","summary":{"tasks":{"total":11,"todo":6,"inProgress":0,"done":5,"dueToday":2,"overdue":2,"completedToday":5,"completionPercentage":45.45},"habits":{"total":4,"active":4,"completedToday":2,"completionPercentage":50},"plans":{"total":3,"inProgress":2,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":50},"learning":{"totalCards":2,"notStarted":0,"inProgress":1,"completed":1,"milestonesTotal":1,"milestonesDone":1,"milestoneCompletionPercentage":100,"milestonesCompletedLast7Days":1}},"tasksDueToday":[{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":16,"title":"Dash due today","description":null,"status":"Done","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:56.4297981Z","completedAt":"2026-09-24T10:10:56.4297981Z","isArchived":false,"isOverdue":false}],"overdueTasks":[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority":" …
» PASS dashboard reflects item change: plan 50%, task counted as done/completed today

#### C5 learning snapshot
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"Dash course"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/7
{"id":7,"title":"Dash course","description":null,"status":"NotStarted","createdAt":"2026-09-24T10:10:57.4574472Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/7/milestones -H "Content-Type: application/json" -d '{"title":"Dash m1"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/7
{"id":6,"learningCardId":7,"title":"Dash m1","isDone":false,"targetDate":null,"completedAt":null}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/7/milestones -H "Content-Type: application/json" -d '{"title":"Dash m2"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/7
{"id":7,"learningCardId":7,"title":"Dash m2","isDone":false,"targetDate":null,"completedAt":null}

(completed milestone 6)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/dashboard
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"generatedAt":"2026-09-24T10:10:59.2475135Z","today":"2026-09-24","displayName":"Hamada","summary":{"tasks":{"total":11,"todo":6,"inProgress":0,"done":5,"dueToday":2,"overdue":2,"completedToday":5,"completionPercentage":45.45},"habits":{"total":4,"active":4,"completedToday":2,"completionPercentage":50},"plans":{"total":3,"inProgress":2,"upcoming":0,"completed":1,"averageActiveCompletionPercentage":50},"learning":{"totalCards":3,"notStarted":0,"inProgress":2,"completed":1,"milestonesTotal":3,"milestonesDone":2,"milestoneCompletionPercentage":66.67,"milestonesCompletedLast7Days":2}},"tasksDueToday":[{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":16,"title":"Dash due today","description":null,"status":"Done","priority":"High","dueDate":"2026-09-24","createdAt":"2026-09-24T10:10:46.7301891Z","updatedAt":"2026-09-24T10:10:56.4297981Z","completedAt":"2026-09-24T10:10:56.4297981Z","isArchived":false,"isOverdue":false}],"overdueTasks":[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":17,"title":"Dash overdue","description":null,"status":"Todo","priority" …
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":7,"title":"Dash course","description":null,"status":"InProgress","createdAt":"2026-09-24T10:10:57.4574472Z","milestones":[{"id":6,"learningCardId":7,"title":"Dash m1","isDone":true,"targetDate":null,"completedAt":"2026-09-24T10:10:58.8814801Z"},{"id":7,"learningCardId":7,"title":"Dash m2","isDone":false,"targetDate":null,"completedAt":null}],"notes":[],"milestonesTotal":2,"milestonesDone":1,"milestoneProgressPercentage":50,"notesCount":0},{"id":3,"title":"Plan course","description":"course","status":"Completed","createdAt":"2026-09-24T10:04:06.1455496Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0},{"id":1,"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"InProgress","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":true,"targetDate":"2026-10-01","completedAt":"2026-09-24T09:58:22.0651984Z"}],"notes":[{"id":2,"learningCardId":1,"text":"Names reveal intent.","createdAt":"2026-09-24T09:58:27.8381054Z"}],"milestonesTotal":1,"milestonesDone":1,"milestoneProgressPercentage":100,"notesCount":1}]

» PASS cards by status and milestones match GET /api/learning-cards
» PASS recent milestones >= 1 and card listed in learningInProgress
  computed: {"totalCards":3,"notStarted":0,"inProgress":2,"completed":1,"milestonesTotal":3,"milestonesDone":2,"milestoneCompletionPercentage":66.67,"milestonesCompletedLast7Days":2}

FAILS=0
```

| Check | Result |
|---|---|
| C1 [smoke] GET /api/dashboard → 200 with summary.tasks/habits/plans/learning and all lists | **PASS** |
| C2 task counts + completion % (36.36) equal values computed from GET /api/tasks; due-today / overdue / completed-today tasks listed; archived excluded | **PASS** |
| C3 habit active/completedToday match GET /api/habits?isActive=true; completing a habit increments completedToday | **PASS** |
| C4 in-progress plan in activePlans with restTimeSeconds > 0; plan counts match GET /api/plans; marking a plan item done is reflected immediately (50%, task done) | **PASS** |
| C5 learning cards by status + milestones match GET /api/learning-cards; recent milestones counted | **PASS** |

Smoke regression summary: phase-01 C1, C2 · phase-02 C1, C2 · phase-03 C1, C4 · phase-04 C1, C4 · phase-05 C1, C5 · phase-06 C1, all PASS.

#### Smoke regression

```text
## phase-01 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T10:11:00.4137871Z"}

» PASS phase-01 C1 status (= 200)
» PASS phase-01 C1 Healthy/Connected
## phase-01 C2 [smoke]
$ curl.exe -s -i --max-time 15 http://localhost:5080/swagger/v1/swagger.json  (status line + parsed)
HTTP/1.1 200 OK
» PASS phase-01 C2 openapi 3.x
## phase-02 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"smoke task","priority":"Medium"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/20
{"id":20,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:11:01.4412641Z","updatedAt":"2026-09-24T10:11:01.4412641Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C1 status (= 201)
» PASS phase-02 C1 fields
## phase-02 C2 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/20
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":20,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:11:01.4412641Z","updatedAt":"2026-09-24T10:11:01.4412641Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C2 status (= 200)
» PASS phase-02 C2 same id
## phase-03 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"smoke habit","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/10
{"id":10,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:11:03.0500465Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS phase-03 C1 status (= 201)
» PASS phase-03 C1 fields
## phase-03 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/10/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/10/completions
{"id":8,"habitId":10,"completionDate":"2026-09-24","createdAt":"2026-09-24T10:11:03.6996188Z"}

» PASS phase-03 C4 status (= 201)
» PASS phase-03 C4 today
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/10
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":10,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:11:03.0500465Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}

» PASS phase-03 C4 progress
## phase-04 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"smoke card","description":"book"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/8
{"id":8,"title":"smoke card","description":"book","status":"NotStarted","createdAt":"2026-09-24T10:11:05.1000819Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

» PASS phase-04 C1 status (= 201)
» PASS phase-04 C1 fields
## phase-04 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/8/milestones -H "Content-Type: application/json" -d '{"title":"smoke ms","targetDate":"2030-01-01"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/8
{"id":8,"learningCardId":8,"title":"smoke ms","isDone":false,"targetDate":"2030-01-01","completedAt":null}

» PASS phase-04 C4 status (= 201)
» PASS phase-04 C4 fields
## phase-05 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"smoke plan task"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/21
{"id":21,"title":"smoke plan task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:11:06.6983681Z","updatedAt":"2026-09-24T10:11:06.6983681Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"smoke plan habit"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/11
{"id":11,"name":"smoke plan habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:11:07.2373704Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"smoke plan card"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/9
{"id":9,"title":"smoke plan card","description":null,"status":"NotStarted","createdAt":"2026-09-24T10:11:07.7216869Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"smoke plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:11:08Z","endDateTime":"2026-09-24T12:11:08Z","items":[{"sourceType":"Task","sourceId":21},{"sourceType":"Habit","sourceId":11},{"sourceType":"LearningResource","sourceId":9}]}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/plans/6
{"id":6,"title":"smoke plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:11:08Z","endDateTime":"2026-09-24T12:11:08Z","priorityOrder":6,"status":"InProgress","createdAt":"2026-09-24T10:11:08.2863949Z","startNotifiedAt":null,"items":[{"id":13,"planId":6,"sourceType":"Task","sourceId":21,"sourceTitle":"smoke plan task","isDone":false},{"id":14,"planId":6,"sourceType":"Habit","sourceId":11,"sourceTitle":"smoke plan habit","isDone":false},{"id":15,"planId":6,"sourceType":"LearningResource","sourceId":9,"sourceTitle":"smoke plan card","isDone":false}],"totalItems":3,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7200,"secondsUntilStart":null,"timeElapsedPercentage":33.34,"serverTime":"2026-09-24T10:11:08.2891326Z"}

» PASS phase-05 C1 status (= 201)
» PASS phase-05 C1 fields
## phase-05 C5 [smoke]
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/6/items/13 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":6,"title":"smoke plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:11:08Z","endDateTime":"2026-09-24T12:11:08Z","priorityOrder":6,"status":"InProgress","createdAt":"2026-09-24T10:11:08.2863949Z","startNotifiedAt":null,"items":[{"id":13,"planId":6,"sourceType":"Task","sourceId":21,"sourceTitle":"smoke plan task","isDone":true},{"id":14,"planId":6,"sourceType":"Habit","sourceId":11,"sourceTitle":"smoke plan habit","isDone":false},{"id":15,"planId":6,"sourceType":"LearningResource","sourceId":9,"sourceTitle":"smoke plan card","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7199,"secondsUntilStart":null,"timeElapsedPercentage":33.34,"serverTime":"2026-09-24T10:11:09.0376513Z"}

» PASS phase-05 C5 status (= 200)
» PASS phase-05 C5 33.33%
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/21
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":21,"title":"smoke plan task","description":null,"status":"Done","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:11:06.6983681Z","updatedAt":"2026-09-24T10:11:09.0341211Z","completedAt":"2026-09-24T10:11:09.0341211Z","isArchived":false,"isOverdue":false}

» PASS phase-05 C5 task Done
## cleanup of smoke data
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/plans/6
HTTP/1.1 204 No Content

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/tasks/21
HTTP/1.1 204 No Content

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/habits/11
HTTP/1.1 204 No Content

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/9
HTTP/1.1 204 No Content

## phase-06 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/settings
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Light","updatedAt":"2026-09-24T10:07:13.5482245Z"}

» PASS phase-06 C1 status (= 200)
» PASS phase-06 C1 has fields
SMOKE_FAILS=0
```

**Swagger export / API stop / unit tests:**

```text
$ curl.exe -s --max-time 15 http://localhost:5080/swagger/v1/swagger.json -o docs/api/swagger.json
exit 0
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/dashboard>'
valid JSON, openapi 3.0.1, 26 paths, 41 operations, schemas: 41
this phase (1):
  GET /api/dashboard
API stopped (configured stop command)
$ dotnet test backend/QuickFlow.sln
Passed!  - Failed:     0, Passed:    56, Skipped:     0, Total:    56, Duration: 57 ms - QuickFlow.Tests.dll (net8.0)
```

**Swagger export:** PASS → `docs/api/swagger.json` (26 paths, 41 operations; `GET /api/dashboard` with `DashboardResponse`/`DashboardSummary` schemas)
**Unit tests:** 56 passed, 0 failed
**Result:** PASS 5/5 (17 assertions + 23 smoke assertions, 0 failures)

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 56, Failed 0 (+4 DashboardCalculatorTests)

## Failure record
—

## Notes
- All metrics come from the domain `DashboardCalculator`, run over the same entities the resource endpoints return. The checks compare the dashboard with those endpoints.
- Task metrics cover non-archived tasks. `completedToday` uses the local date of `completedAt`. Habit metrics cover active habits.
- `activePlans` are in-progress plans in priority order, with `restTimeSeconds` at `generatedAt`. The frontend counts down from `endDateTime`.
- No schema change in this phase (`dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration.").
