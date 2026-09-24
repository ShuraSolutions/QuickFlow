# phase-05-plans — Todo plans, items, progress & rest time

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | phase-02-tasks, phase-03-habits, phase-04-learning |
| Requirements covered | US Todo Plans 1–10, FR-07, FR-08 (backend: status, rest time, completion %, start notification), BR-10..14, NFR Reliability (status from stored times), §12 Todo Plans, §11 unit tests (plan validation, rest time, completion roll-up, lifecycle) |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T12:59:43+03:00 |
| Ended | 2026-09-24T13:05:24+03:00 |

## Goal
Plans can be composed from existing tasks, habits and learning cards with estimated duration, start/end date-time and priority order. Plan items can be marked done (propagating to the source per BR-13). Every plan response carries server-computed status, completion percentage and rest time. Start notifications and plan history are available.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Domain: `Plan` (Id, Title, EstimatedDurationMinutes, StartDateTime, EndDateTime, PriorityOrder, Status, CreatedAt, StartNotifiedAt) + `PlanStatus` (NotStarted, InProgress, Completed); `PlanItem` (Id, PlanId, SourceType, SourceId, IsDone) + `PlanItemSourceType` (Task, Habit, LearningResource)
- [x] T2 Domain rules: title required ≤200; ≥1 item (BR-10); End > Start (BR-11); estimated duration > 0 minutes; priority order ≥ 0; no duplicate (SourceType, SourceId) items
- [x] T3 Domain `PlanProgressCalculator`: completion % = done/total; status from now + items (all done → Completed; now ≥ End → Completed; Start ≤ now < End → InProgress; else NotStarted); rest time = End − now only while Start ≤ now < End (BR-12), otherwise null; `secondsUntilStart`, `hasStarted`, `hasEnded`
- [x] T4 EF mapping (items cascade) + migration `phase-05-plans`; stored Status refreshed from the calculator whenever plans are read or changed
- [x] T5 Service validates that every referenced source exists (400 otherwise); item responses include `sourceTitle`
- [x] T6 `PlansController`: `GET /api/plans` (`?status=`, ordered by priorityOrder then start), `GET /api/plans/{id}`, `POST /api/plans`, `PUT /api/plans/{id}` (replaces items, keeps IsDone of retained items), `DELETE /api/plans/{id}`
- [x] T7 `PATCH /api/plans/{id}/items/{itemId}` `{isDone}`; BR-13 propagation: Task item → task Done/Todo; Habit item → add/remove today's completion; LearningResource item → card Completed/InProgress
- [x] T8 `GET /api/plans/history` (completed/ended plans with completion %, newest end first); `GET /api/plans/notifications` (started, not ended, not acknowledged) and `POST /api/plans/{id}/notifications/ack`
- [x] T9 BR-14: deleting a task/habit/learning card removes the plan items that reference it
- [x] T10 Unit tests: plan validation, rest time, completion roll-up, lifecycle transitions

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `POST /api/plans` with 1 task + 1 habit + 1 learning card, start in the past, end in the future → `201`, `status == "InProgress"`, `totalItems == 3`, `completionPercentage == 0`, `isRestTimeActive == true`, `restTimeSeconds > 0`, items have `sourceTitle`
- [x] C2 `POST /api/plans` with `items: []` → `400` (BR-10); with a non-existent source id → `400`
- [x] C3 `POST /api/plans` with end ≤ start → `400` (BR-11); `estimatedDurationMinutes: 0` → `400`; empty title → `400`; `sourceType:"Note"` → `400`
- [x] C4 Future plan (start tomorrow) → `status == "NotStarted"`, `isRestTimeActive == false`, `restTimeSeconds == null`, `secondsUntilStart > 0`
- [x] C5 [smoke] `PATCH /api/plans/{id}/items/{itemId}` `{isDone:true}` on the task item → `200`, `doneItems == 1`, `completionPercentage == 33.33`; underlying task `status == "Done"` (BR-13)
- [x] C6 Habit item done → habit `completedToday == true`; learning item done → card `status == "Completed"`; all items done → plan `status == "Completed"`, `completionPercentage == 100`
- [x] C7 Un-marking the task item → task back to `Todo`, plan `InProgress` again
- [x] C8 `PUT /api/plans/{id}` updates title, duration, times, priority order and items → `200`, retained item keeps `isDone`
- [x] C9 `GET /api/plans` ordered by `priorityOrder` asc; `?status=NotStarted` filters
- [x] C10 Plan with start and end in the past → `status == "Completed"`, `hasEnded == true`, appears in `GET /api/plans/history` with its `completionPercentage`
- [x] C11 `GET /api/plans/notifications` includes a started, unacknowledged plan; after `POST /api/plans/{id}/notifications/ack` → `200`, `startNotifiedAt` set, plan no longer listed
- [x] C12 `DELETE /api/plans/{id}` → `204`; `GET` → `404`; unknown item id → `404`
- [x] C13 Deleting a task referenced by a plan removes that plan item (`totalItems` decreases) (BR-14)

## Verification

### Attempt 1 — 2026-09-24T13:04:03+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`)
**API start:** PASS (configured stop, then configured run command in background; `health 200 after 0 s`)

Check script output (commands prefixed with `$`; `» PASS/FAIL` lines are assertions evaluated on the real response bodies; bodies over 1500 chars trimmed with …):

```text
now(local)=2026-09-24T13:04:04+03:00 start(-1h)=2026-09-24T09:04:04Z end(+2h)=2026-09-24T12:04:04Z tomorrow=2026-09-25T10:04:04Z

#### setup: sources
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Plan task A","priority":"High"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/11
{"id":11,"title":"Plan task A","description":null,"status":"Todo","priority":"High","dueDate":null,"createdAt":"2026-09-24T10:04:04.3366463Z","updatedAt":"2026-09-24T10:04:04.3366463Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Plan task B"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/12
{"id":12,"title":"Plan task B","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:04:05.0561486Z","updatedAt":"2026-09-24T10:04:05.0561486Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"Plan habit","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/5
{"id":5,"name":"Plan habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:04:05.5565155Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"Plan course","description":"course"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/3
{"id":3,"title":"Plan course","description":"course","status":"NotStarted","createdAt":"2026-09-24T10:04:06.1455496Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}


#### C1 [smoke] create in-progress plan with task + habit + learning card
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Morning focus","estimatedDurationMinutes":120,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","priorityOrder":2,"items":[{"sourceType":"Task","sourceId":11},{"sourceType":"Habit","sourceId":5},{"sourceType":"LearningResource","sourceId":3}]}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/plans/1
{"id":1,"title":"Morning focus","estimatedDurationMinutes":120,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","priorityOrder":2,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false},{"id":3,"planId":1,"sourceType":"LearningResource","sourceId":3,"sourceTitle":"Plan course","isDone":false}],"totalItems":3,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7198,"secondsUntilStart":null,"timeElapsedPercentage":33.36,"serverTime":"2026-09-24T10:04:06.9101187Z"}

» PASS status (= 201)
» PASS InProgress, 3 items, 0%, rest time active > 0, source titles

#### C2 BR-10: no items / missing source
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Empty","estimatedDurationMinutes":30,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","items":[]}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Items":["A plan must reference at least one task, habit or learning resource."]}}

» PASS status empty items (= 400)
» PASS errors.Items
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Ghost","estimatedDurationMinutes":30,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","items":[{"sourceType":"Task","sourceId":999999}]}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Items":["Referenced items do not exist: Task 999999."]}}

» PASS status missing source (= 400)
» PASS errors.Items mentions Task 999999

#### C3 BR-11 + field validation
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Backwards","estimatedDurationMinutes":30,"startDateTime":"2026-09-24T12:04:04Z","endDateTime":"2026-09-24T09:04:04Z","items":[{"sourceType":"Task","sourceId":11}]}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"EndDateTime":["EndDateTime must be after StartDateTime."]}}

» PASS status end<start (= 400)
» PASS errors.EndDateTime
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Same","estimatedDurationMinutes":30,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T09:04:04Z","items":[{"sourceType":"Task","sourceId":11}]}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"EndDateTime":["EndDateTime must be after StartDateTime."]}}

» PASS status end==start (= 400)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Zero","estimatedDurationMinutes":0,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","items":[{"sourceType":"Task","sourceId":11}]}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"EstimatedDurationMinutes":["EstimatedDurationMinutes must be greater than 0."]}}

» PASS status duration 0 (= 400)
» PASS errors.EstimatedDurationMinutes
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"","estimatedDurationMinutes":30,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","items":[{"sourceType":"Task","sourceId":11}]}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status empty title (= 400)
» PASS errors.Title
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Bad type","estimatedDurationMinutes":30,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","items":[{"sourceType":"Note","sourceId":1}]}'
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"request":["The request field is required."],"$.items[0].sourceType":["The JSON value could not be converted to QuickFlow.Domain.Plans.PlanItemSourceType. Path: $.items[0].sourceType | LineNumber: 0 | BytePositionInLine: 155."]},"traceId":"00-86f7571aef9f2d39d3da79246f35a0c1-fc6194ecb01a9a34-00"}

» PASS status sourceType Note (= 400)

#### C4 future plan
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Tomorrow block","estimatedDurationMinutes":60,"startDateTime":"2026-09-25T10:04:04Z","endDateTime":"2026-09-25T12:04:04Z","priorityOrder":1,"items":[{"sourceType":"Task","sourceId":12}]}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/plans/2
{"id":2,"title":"Tomorrow block","estimatedDurationMinutes":60,"startDateTime":"2026-09-25T10:04:04Z","endDateTime":"2026-09-25T12:04:04Z","priorityOrder":1,"status":"NotStarted","createdAt":"2026-09-24T10:04:11.4432481Z","startNotifiedAt":null,"items":[{"id":4,"planId":2,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":1,"doneItems":0,"completionPercentage":0,"hasStarted":false,"hasEnded":false,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":86393,"timeElapsedPercentage":0,"serverTime":"2026-09-24T10:04:11.4456168Z"}

» PASS status (= 201)
» PASS NotStarted, no rest time, secondsUntilStart > 0

#### C5 [smoke] mark task item done (BR-13)
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/1/items/1 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus","estimatedDurationMinutes":120,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","priorityOrder":2,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":true},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false},{"id":3,"planId":1,"sourceType":"LearningResource","sourceId":3,"sourceTitle":"Plan course","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7192,"secondsUntilStart":null,"timeElapsedPercentage":33.41,"serverTime":"2026-09-24T10:04:12.2336347Z"}

» PASS status (= 200)
» PASS doneItems 1, 33.33%
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/11
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":11,"title":"Plan task A","description":null,"status":"Done","priority":"High","dueDate":null,"createdAt":"2026-09-24T10:04:04.3366463Z","updatedAt":"2026-09-24T10:04:12.2095007Z","completedAt":"2026-09-24T10:04:12.2095007Z","isArchived":false,"isOverdue":false}

» PASS underlying task Done

#### C6 habit + learning items done -> plan Completed
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/1/items/2 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus","estimatedDurationMinutes":120,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","priorityOrder":2,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":true},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":3,"planId":1,"sourceType":"LearningResource","sourceId":3,"sourceTitle":"Plan course","isDone":false}],"totalItems":3,"doneItems":2,"completionPercentage":66.67,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7191,"secondsUntilStart":null,"timeElapsedPercentage":33.42,"serverTime":"2026-09-24T10:04:13.2996556Z"}

» PASS status habit item (= 200)
» PASS 66.67%
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/5
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":5,"name":"Plan habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:04:05.5565155Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}

» PASS habit completedToday
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/1/items/3 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus","estimatedDurationMinutes":120,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","priorityOrder":2,"status":"Completed","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":true},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":3,"planId":1,"sourceType":"LearningResource","sourceId":3,"sourceTitle":"Plan course","isDone":true}],"totalItems":3,"doneItems":3,"completionPercentage":100,"hasStarted":true,"hasEnded":false,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":null,"timeElapsedPercentage":33.43,"serverTime":"2026-09-24T10:04:14.4266054Z"}

» PASS status learning item (= 200)
» PASS plan Completed 100%, rest time off
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards/3
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":3,"title":"Plan course","description":"course","status":"Completed","createdAt":"2026-09-24T10:04:06.1455496Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

» PASS card Completed

#### C7 un-mark task item
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/1/items/1 -H "Content-Type: application/json" -d '{"isDone":false}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus","estimatedDurationMinutes":120,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-24T12:04:04Z","priorityOrder":2,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":3,"planId":1,"sourceType":"LearningResource","sourceId":3,"sourceTitle":"Plan course","isDone":true}],"totalItems":3,"doneItems":2,"completionPercentage":66.67,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7189,"secondsUntilStart":null,"timeElapsedPercentage":33.44,"serverTime":"2026-09-24T10:04:15.4297271Z"}

» PASS status (= 200)
» PASS plan InProgress again, 66.67%
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/11
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":11,"title":"Plan task A","description":null,"status":"Todo","priority":"High","dueDate":null,"createdAt":"2026-09-24T10:04:04.3366463Z","updatedAt":"2026-09-24T10:04:15.4271457Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS task back to Todo

#### C8 update plan (retained item keeps isDone)
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/plans/1 -H "Content-Type: application/json" -d '{"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"items":[{"sourceType":"Task","sourceId":11},{"sourceType":"Habit","sourceId":5},{"sourceType":"Task","sourceId":12}]}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":5,"planId":1,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":86388,"secondsUntilStart":null,"timeElapsedPercentage":4.01,"serverTime":"2026-09-24T10:04:16.4455379Z"}

» PASS status (= 200)
» PASS fields updated; habit item still done; new task B item not done; card item dropped

#### C9 list order + status filter
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":2,"title":"Tomorrow block","estimatedDurationMinutes":60,"startDateTime":"2026-09-25T10:04:04Z","endDateTime":"2026-09-25T12:04:04Z","priorityOrder":1,"status":"NotStarted","createdAt":"2026-09-24T10:04:11.4432481Z","startNotifiedAt":null,"items":[{"id":4,"planId":2,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":1,"doneItems":0,"completionPercentage":0,"hasStarted":false,"hasEnded":false,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":86388,"timeElapsedPercentage":0,"serverTime":"2026-09-24T10:04:16.9682201Z"},{"id":1,"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":5,"planId":1,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":86388,"secondsUntilStart":null,"timeElapsedPercentage":4.01,"serverTime":"2026-09-24T10:04:16.9682201Z"}]

» PASS status (= 200)
» PASS ordered by priorityOrder asc
2:1:NotStarted 1:3:InProgress
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans?status=NotStarted
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":2,"title":"Tomorrow block","estimatedDurationMinutes":60,"startDateTime":"2026-09-25T10:04:04Z","endDateTime":"2026-09-25T12:04:04Z","priorityOrder":1,"status":"NotStarted","createdAt":"2026-09-24T10:04:11.4432481Z","startNotifiedAt":null,"items":[{"id":4,"planId":2,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":1,"doneItems":0,"completionPercentage":0,"hasStarted":false,"hasEnded":false,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":86387,"timeElapsedPercentage":0,"serverTime":"2026-09-24T10:04:17.5940476Z"}]

» PASS only NotStarted, includes future plan
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans?status=Bogus
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"status":["The value 'Bogus' is not valid."]},"traceId":"00-50654246b51e997615a1b11a6f6473e2-c5f98803c9f20bee-00"}

» PASS invalid status (= 400)

#### C10 past plan -> Completed + history
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"Yesterday review","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T07:04:04Z","endDateTime":"2026-09-24T09:04:04Z","items":[{"sourceType":"Task","sourceId":12},{"sourceType":"Habit","sourceId":5}]}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/plans/3
{"id":3,"title":"Yesterday review","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T07:04:04Z","endDateTime":"2026-09-24T09:04:04Z","priorityOrder":4,"status":"Completed","createdAt":"2026-09-24T10:04:18.476645Z","startNotifiedAt":null,"items":[{"id":6,"planId":3,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false},{"id":7,"planId":3,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false}],"totalItems":2,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":true,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":null,"timeElapsedPercentage":100,"serverTime":"2026-09-24T10:04:18.4789164Z"}

» PASS status (= 201)
» PASS Completed by time, hasEnded, 0%
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/history
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":3,"title":"Yesterday review","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T07:04:04Z","endDateTime":"2026-09-24T09:04:04Z","priorityOrder":4,"status":"Completed","createdAt":"2026-09-24T10:04:18.476645Z","startNotifiedAt":null,"items":[{"id":6,"planId":3,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false},{"id":7,"planId":3,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false}],"totalItems":2,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":true,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":null,"timeElapsedPercentage":100,"serverTime":"2026-09-24T10:04:19.1575099Z"}]

» PASS status (= 200)
» PASS past plan in history with completionPercentage; all Completed

#### C11 start notifications + ack
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/notifications
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":null,"items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":5,"planId":1,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":86385,"secondsUntilStart":null,"timeElapsedPercentage":4.02,"serverTime":"2026-09-24T10:04:19.7083916Z"}]

» PASS status (= 200)
» PASS started unacknowledged plan listed; future and past plans not
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans/1/notifications/ack
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":"2026-09-24T10:04:20.2386642Z","items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true},{"id":5,"planId":1,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":86384,"secondsUntilStart":null,"timeElapsedPercentage":4.02,"serverTime":"2026-09-24T10:04:20.2404806Z"}

» PASS status (= 200)
» PASS startNotifiedAt set
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/notifications
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[]

» PASS no longer listed

#### C12 delete plan / 404
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/1/items/999999 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Plan item '1/999999' was not found."}

» PASS unknown item (= 404)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/plans/2
HTTP/1.1 204 No Content

» PASS status (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/2
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Plan '2' was not found."}

» PASS after delete (= 404)
» PASS ProblemDetails
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/plans/999999
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Plan '999999' was not found."}

» PASS unknown plan (= 404)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/12
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":12,"title":"Plan task B","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:04:05.0561486Z","updatedAt":"2026-09-24T10:04:05.0561486Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS source task kept after plan delete (= 200)

#### C13 BR-14 deleting a referenced task removes its plan item
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/3
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":3,"title":"Yesterday review","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T07:04:04Z","endDateTime":"2026-09-24T09:04:04Z","priorityOrder":4,"status":"Completed","createdAt":"2026-09-24T10:04:18.476645Z","startNotifiedAt":null,"items":[{"id":6,"planId":3,"sourceType":"Task","sourceId":12,"sourceTitle":"Plan task B","isDone":false},{"id":7,"planId":3,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false}],"totalItems":2,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":true,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":null,"timeElapsedPercentage":100,"serverTime":"2026-09-24T10:04:23.2743484Z"}

» PASS past plan has 2 items
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/tasks/12
HTTP/1.1 204 No Content

» PASS delete task (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/3
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":3,"title":"Yesterday review","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T07:04:04Z","endDateTime":"2026-09-24T09:04:04Z","priorityOrder":4,"status":"Completed","createdAt":"2026-09-24T10:04:18.476645Z","startNotifiedAt":null,"items":[{"id":7,"planId":3,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":false}],"totalItems":1,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":true,"isRestTimeActive":false,"restTimeSeconds":null,"secondsUntilStart":null,"timeElapsedPercentage":100,"serverTime":"2026-09-24T10:04:24.2117286Z"}

» PASS past plan now 1 item (habit only)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/plans/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Morning focus v2","estimatedDurationMinutes":90,"startDateTime":"2026-09-24T09:04:04Z","endDateTime":"2026-09-25T10:04:04Z","priorityOrder":3,"status":"InProgress","createdAt":"2026-09-24T10:04:06.8531613Z","startNotifiedAt":"2026-09-24T10:04:20.2386642Z","items":[{"id":1,"planId":1,"sourceType":"Task","sourceId":11,"sourceTitle":"Plan task A","isDone":false},{"id":2,"planId":1,"sourceType":"Habit","sourceId":5,"sourceTitle":"Plan habit","isDone":true}],"totalItems":2,"doneItems":1,"completionPercentage":50,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":86380,"secondsUntilStart":null,"timeElapsedPercentage":4.02,"serverTime":"2026-09-24T10:04:24.7072222Z"}

» PASS plan 1 lost task B item (3 -> 2)

FAILS=0
```

| Check | Result |
|---|---|
| C1 [smoke] plan from task + habit + card, start −1h / end +2h → 201, InProgress, 3 items, 0%, rest time active (7198 s), source titles | **PASS** |
| C2 `items: []` → 400 errors.Items (BR-10); unknown source → 400 "Task 999999" | **PASS** |
| C3 end < start and end == start → 400 errors.EndDateTime (BR-11); duration 0 → 400; empty title → 400; sourceType "Note" → 400 | **PASS** |
| C4 plan tomorrow → NotStarted, isRestTimeActive false, restTimeSeconds null, secondsUntilStart > 0 | **PASS** |
| C5 [smoke] task item done → doneItems 1, 33.33%; task Done (BR-13) | **PASS** |
| C6 habit item → habit completedToday; card item → card Completed; plan Completed 100% | **PASS** |
| C7 task item un-marked → task Todo, plan InProgress (66.67%) | **PASS** |
| C8 PUT updates title/duration/end/priority/items; retained habit item keeps isDone | **PASS** |
| C9 list ordered by priorityOrder asc; `?status=NotStarted` filter; invalid status → 400 | **PASS** |
| C10 past plan → Completed, hasEnded, in `/history` with completionPercentage 0 | **PASS** |
| C11 `/notifications` lists started, unacknowledged plan; ack → startNotifiedAt set, no longer listed | **PASS** |
| C12 unknown item → 404; delete plan → 204 then 404; unknown plan → 404; source task kept | **PASS** |
| C13 deleting a referenced task removes its items from both plans (BR-14) | **PASS** |

Smoke regression summary: phase-01 C1, C2 · phase-02 C1, C2 · phase-03 C1, C4 · phase-04 C1, C4, all PASS.

#### Smoke regression

```text
## phase-01 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T10:04:25.260184Z"}

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
Location: http://localhost:5080/api/tasks/13
{"id":13,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:04:26.2429439Z","updatedAt":"2026-09-24T10:04:26.2429439Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C1 status (= 201)
» PASS phase-02 C1 fields
## phase-02 C2 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/13
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":13,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:04:26.2429439Z","updatedAt":"2026-09-24T10:04:26.2429439Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C2 status (= 200)
» PASS phase-02 C2 same id
## phase-03 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"smoke habit","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/6
{"id":6,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:04:27.7107191Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS phase-03 C1 status (= 201)
» PASS phase-03 C1 fields
## phase-03 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/6/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/6/completions
{"id":5,"habitId":6,"completionDate":"2026-09-24","createdAt":"2026-09-24T10:04:28.3302494Z"}

» PASS phase-03 C4 status (= 201)
» PASS phase-03 C4 today
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/6
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":6,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:04:27.7107191Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}

» PASS phase-03 C4 progress
## phase-04 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"smoke card","description":"book"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/4
{"id":4,"title":"smoke card","description":"book","status":"NotStarted","createdAt":"2026-09-24T10:04:29.852322Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

» PASS phase-04 C1 status (= 201)
» PASS phase-04 C1 fields
## phase-04 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/4/milestones -H "Content-Type: application/json" -d '{"title":"smoke ms","targetDate":"2030-01-01"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/4
{"id":4,"learningCardId":4,"title":"smoke ms","isDone":false,"targetDate":"2030-01-01","completedAt":null}

» PASS phase-04 C4 status (= 201)
» PASS phase-04 C4 fields
SMOKE_FAILS=0
```

**Swagger export / API stop / unit tests:**

```text
$ curl.exe -s --max-time 15 http://localhost:5080/swagger/v1/swagger.json -o docs/api/swagger.json
exit 0
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/plans>'
valid JSON, openapi 3.0.1, 24 paths, 38 operations, schemas: 31
this phase (9):
  GET /api/plans
  POST /api/plans
  GET /api/plans/history
  GET /api/plans/notifications
  POST /api/plans/{id}/notifications/ack
  GET /api/plans/{id}
  PUT /api/plans/{id}
  DELETE /api/plans/{id}
  PATCH /api/plans/{id}/items/{itemId}
API stopped (configured stop command)
$ dotnet test backend/QuickFlow.sln
Passed!  - Failed:     0, Passed:    48, Skipped:     0, Total:    48, Duration: 55 ms - QuickFlow.Tests.dll (net8.0)
```

**Swagger export:** PASS → `docs/api/swagger.json` (24 paths, 38 operations; all 9 plan operations listed)
**Unit tests:** 48 passed, 0 failed
**Result:** PASS 13/13 (53 assertions + 16 smoke assertions, 0 failures)

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 48, Failed 0 (+10 PlanTests)

## Failure record
—

## Notes
- **Status rule** (domain `PlanProgressCalculator`): all items done → Completed; now ≥ End → Completed; Start ≤ now < End → InProgress; otherwise NotStarted. The stored `Status` column is refreshed on every read or write, so status is consistent after restarts (NFR Reliability).
- **Rest time** (BR-12): `isRestTimeActive` is true only while the plan is InProgress (inside the start–end window, items still open). `restTimeSeconds` is End − now at `serverTime`. The frontend counts down locally from `endDateTime` (NFR: at least one update per minute).
- **BR-13 propagation:** Task item done/undone → task Done/Todo. Habit item → today's completion added/removed. LearningResource item → card Completed / back to InProgress. Completing a source outside a plan does not change plan items, so plan items keep their own flag.
- **BR-14:** deleting a task, habit or card removes the plan items that reference it (through `Plan.RemoveItemsFor`, which also refreshes the plan status).
- **Start notifications:** `GET /api/plans/notifications` returns started, not-ended plans whose start was not acknowledged. `POST /api/plans/{id}/notifications/ack` sets `startNotifiedAt` once.
- **Dates:** send ISO 8601 with `Z` or an offset. Values without an offset are taken as server local time. All responses are UTC (`Z`).
- `priorityOrder` defaults to (max existing + 1) on create.
