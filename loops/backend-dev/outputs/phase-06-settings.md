# phase-06-settings — Settings

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | §4 Settings page, §8 Settings (profile info, notification behavior, default view), NFR Reliability (persist between sessions) |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T13:05:36+03:00 |
| Ended | 2026-09-24T13:08:19+03:00 |

## Goal
A single persisted settings record (user profile + application preferences) that the Settings page can read and update.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Domain: `UserSettings` (Id=1, DisplayName, Email, PlanStartNotificationsEnabled, NotificationLeadMinutes, DefaultView, Theme, UpdatedAt) + `DefaultView` (Dashboard, Tasks, Habits, LearningResources, TodoPlans, Settings) + `ThemePreference` (Light, Dark, System)
- [x] T2 Domain rules: display name ≤100, email optional but valid format and ≤200, notification lead minutes 0..1440, enums defined
- [x] T3 EF mapping + migration `phase-06-settings` with a seeded default row
- [x] T4 `SettingsController`: `GET /api/settings`, `PUT /api/settings`

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `GET /api/settings` → `200` with defaults (`defaultView == "Dashboard"`, `planStartNotificationsEnabled == true`, `theme == "System"`)
- [x] C2 `PUT /api/settings` valid body → `200` with the new values; a later `GET` returns them
- [x] C3 `PUT /api/settings` with `email:"not-an-email"` → `400` `errors.Email`; `defaultView:"Nowhere"` → `400`; `notificationLeadMinutes: -1` → `400`
- [x] C4 Settings persist across API restart

## Verification

### Attempt 1 — 2026-09-24T13:07:11+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`)
**API start:** PASS (configured stop, then configured run command in background; `health 200 after 0 s`)

Check script output (commands prefixed with `$`; `» PASS/FAIL` lines are assertions evaluated on the real response bodies; bodies over 1500 chars trimmed with …):

```text
#### C1 [smoke] defaults
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/settings
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"QuickFlow User","email":null,"planStartNotificationsEnabled":true,"notificationLeadMinutes":0,"defaultView":"Dashboard","theme":"System","updatedAt":"2026-01-01T00:00:00Z"}

» PASS status (= 200)
» PASS defaults

#### C2 update + read back
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/settings -H "Content-Type: application/json" -d '{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Dark"}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Dark","updatedAt":"2026-09-24T10:07:12.4433408Z"}

» PASS status (= 200)
» PASS new values returned
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/settings
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Dark","updatedAt":"2026-09-24T10:07:12.4433408Z"}

» PASS GET returns new values
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/settings -H "Content-Type: application/json" -d '{"theme":"Light"}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Light","updatedAt":"2026-09-24T10:07:13.5482245Z"}

» PASS partial update status (= 200)
» PASS only theme changed

#### C3 validation
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/settings -H "Content-Type: application/json" -d '{"email":"not-an-email"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Email":["Email must be a valid email address."]}}

» PASS status bad email (= 400)
» PASS errors.Email
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/settings -H "Content-Type: application/json" -d '{"defaultView":"Nowhere"}'
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"r":["The r field is required."],"$.defaultView":["The JSON value could not be converted to System.Nullable`1[QuickFlow.Domain.Settings.DefaultView]. Path: $.defaultView | LineNumber: 0 | BytePositionInLine: 24."]},"traceId":"00-28b52725eb646aed8c2090733975ac66-c91ff5a229025a00-00"}

» PASS status bad defaultView (= 400)
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/settings -H "Content-Type: application/json" -d '{"notificationLeadMinutes":-1}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"NotificationLeadMinutes":["NotificationLeadMinutes must be between 0 and 1440."]}}

» PASS status lead -1 (= 400)
» PASS errors.NotificationLeadMinutes
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/settings -H "Content-Type: application/json" -d '{"displayName":"  "}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"DisplayName":["DisplayName is required."]}}

» PASS status blank displayName (= 400)
» PASS errors.DisplayName
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/settings
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Light","updatedAt":"2026-09-24T10:07:13.5482245Z"}

» PASS invalid updates changed nothing

FAILS=0

#### C4 persistence across restart
(API stopped with the configured stop command and restarted with the configured run command; health polled: health 200 after 0 s)
$ powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort 5080 -State Listen | ForEach-Object { Get-Process -Id $_.OwningProcess } | Select-Object -First 1 Id,ProcessName,StartTime | Format-Table -AutoSize | Out-String"
   Id ProcessName   StartTime           
   -- -----------   ---------           
17772 QuickFlow.Api 9/24/2026 1:07:27 PM
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/settings
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"displayName":"Hamada","email":"hamada@example.com","planStartNotificationsEnabled":false,"notificationLeadMinutes":10,"defaultView":"TodoPlans","theme":"Light","updatedAt":"2026-09-24T10:07:13.5482245Z"}

» PASS status (= 200)
» PASS values from before restart

FAILS=0
```

| Check | Result |
|---|---|
| C1 [smoke] GET defaults (Dashboard, notifications on, System theme) | **PASS** |
| C2 PUT valid body → 200 with new values; GET returns them; partial PUT changes only given fields | **PASS** |
| C3 bad email → 400 errors.Email; defaultView "Nowhere" → 400; lead −1 → 400; blank displayName → 400; nothing changed | **PASS** |
| C4 values persist across restart (new process started 13:07:27) | **PASS** |

Smoke regression summary: phase-01 C1, C2 · phase-02 C1, C2 · phase-03 C1, C4 · phase-04 C1, C4 · phase-05 C1, C5, all PASS.

#### Smoke regression

```text
## phase-01 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T10:07:36.5359057Z"}

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
Location: http://localhost:5080/api/tasks/14
{"id":14,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:07:37.5878431Z","updatedAt":"2026-09-24T10:07:37.5878431Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C1 status (= 201)
» PASS phase-02 C1 fields
## phase-02 C2 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/14
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":14,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:07:37.5878431Z","updatedAt":"2026-09-24T10:07:37.5878431Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C2 status (= 200)
» PASS phase-02 C2 same id
## phase-03 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"smoke habit","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/7
{"id":7,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:07:39.4880527Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS phase-03 C1 status (= 201)
» PASS phase-03 C1 fields
## phase-03 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/7/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/7/completions
{"id":6,"habitId":7,"completionDate":"2026-09-24","createdAt":"2026-09-24T10:07:40.2142799Z"}

» PASS phase-03 C4 status (= 201)
» PASS phase-03 C4 today
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/7
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":7,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:07:39.4880527Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}

» PASS phase-03 C4 progress
## phase-04 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"smoke card","description":"book"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/5
{"id":5,"title":"smoke card","description":"book","status":"NotStarted","createdAt":"2026-09-24T10:07:41.7363789Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

» PASS phase-04 C1 status (= 201)
» PASS phase-04 C1 fields
## phase-04 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/5/milestones -H "Content-Type: application/json" -d '{"title":"smoke ms","targetDate":"2030-01-01"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/5
{"id":5,"learningCardId":5,"title":"smoke ms","isDone":false,"targetDate":"2030-01-01","completedAt":null}

» PASS phase-04 C4 status (= 201)
» PASS phase-04 C4 fields
## phase-05 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"smoke plan task"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/15
{"id":15,"title":"smoke plan task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:07:43.3812678Z","updatedAt":"2026-09-24T10:07:43.3812678Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"smoke plan habit"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/8
{"id":8,"name":"smoke plan habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T10:07:43.8800045Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"smoke plan card"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/6
{"id":6,"title":"smoke plan card","description":null,"status":"NotStarted","createdAt":"2026-09-24T10:07:44.3964156Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/plans -H "Content-Type: application/json" -d '{"title":"smoke plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:07:44Z","endDateTime":"2026-09-24T12:07:44Z","items":[{"sourceType":"Task","sourceId":15},{"sourceType":"Habit","sourceId":8},{"sourceType":"LearningResource","sourceId":6}]}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/plans/4
{"id":4,"title":"smoke plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:07:44Z","endDateTime":"2026-09-24T12:07:44Z","priorityOrder":5,"status":"InProgress","createdAt":"2026-09-24T10:07:45.0516857Z","startNotifiedAt":null,"items":[{"id":8,"planId":4,"sourceType":"Task","sourceId":15,"sourceTitle":"smoke plan task","isDone":false},{"id":9,"planId":4,"sourceType":"Habit","sourceId":8,"sourceTitle":"smoke plan habit","isDone":false},{"id":10,"planId":4,"sourceType":"LearningResource","sourceId":6,"sourceTitle":"smoke plan card","isDone":false}],"totalItems":3,"doneItems":0,"completionPercentage":0,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7199,"secondsUntilStart":null,"timeElapsedPercentage":33.34,"serverTime":"2026-09-24T10:07:45.0968585Z"}

» PASS phase-05 C1 status (= 201)
» PASS phase-05 C1 fields
## phase-05 C5 [smoke]
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/plans/4/items/8 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":4,"title":"smoke plan","estimatedDurationMinutes":60,"startDateTime":"2026-09-24T09:07:44Z","endDateTime":"2026-09-24T12:07:44Z","priorityOrder":5,"status":"InProgress","createdAt":"2026-09-24T10:07:45.0516857Z","startNotifiedAt":null,"items":[{"id":8,"planId":4,"sourceType":"Task","sourceId":15,"sourceTitle":"smoke plan task","isDone":true},{"id":9,"planId":4,"sourceType":"Habit","sourceId":8,"sourceTitle":"smoke plan habit","isDone":false},{"id":10,"planId":4,"sourceType":"LearningResource","sourceId":6,"sourceTitle":"smoke plan card","isDone":false}],"totalItems":3,"doneItems":1,"completionPercentage":33.33,"hasStarted":true,"hasEnded":false,"isRestTimeActive":true,"restTimeSeconds":7199,"secondsUntilStart":null,"timeElapsedPercentage":33.35,"serverTime":"2026-09-24T10:07:45.9186794Z"}

» PASS phase-05 C5 status (= 200)
» PASS phase-05 C5 33.33%
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/15
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":15,"title":"smoke plan task","description":null,"status":"Done","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T10:07:43.3812678Z","updatedAt":"2026-09-24T10:07:45.9079926Z","completedAt":"2026-09-24T10:07:45.9079926Z","isArchived":false,"isOverdue":false}

» PASS phase-05 C5 task Done
## cleanup of smoke data
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/plans/4
HTTP/1.1 204 No Content

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/tasks/15
HTTP/1.1 204 No Content

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/habits/8
HTTP/1.1 204 No Content

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/6
HTTP/1.1 204 No Content

SMOKE_FAILS=0
```

**Swagger export / API stop / unit tests:**

```text
$ curl.exe -s --max-time 15 http://localhost:5080/swagger/v1/swagger.json -o docs/api/swagger.json
exit 0
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/settings>'
valid JSON, openapi 3.0.1, 25 paths, 40 operations, schemas: 35
this phase (2):
  GET /api/settings
  PUT /api/settings
API stopped (configured stop command)
$ dotnet test backend/QuickFlow.sln
Passed!  - Failed:     0, Passed:    52, Skipped:     0, Total:    52, Duration: 58 ms - QuickFlow.Tests.dll (net8.0)
```

**Swagger export:** PASS → `docs/api/swagger.json` (25 paths, 40 operations; both settings operations listed)
**Unit tests:** 52 passed, 0 failed
**Result:** PASS 4/4 (17 assertions + 21 smoke assertions, 0 failures)

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 52, Failed 0 (+4 UserSettingsTests)

## Failure record
—

## Notes
- One settings row (Id 1) is seeded by migration `phase-06-settings`. If the row is missing, GET recreates it with defaults.
- PUT is partial: null or omitted fields keep their value, and `email: ""` clears the email.
- `notificationLeadMinutes` lets the frontend notify ahead of a plan's start. The server's `/api/plans/notifications` still reports plans at their start time.
