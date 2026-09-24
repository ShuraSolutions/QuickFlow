# phase-03-habits — Habits & habit completions

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | US Habits 1–5, FR-03, FR-04, BR-6, BR-7, BR-14, §9 unique (HabitId, CompletionDate), §12 Habits, §11 unit tests (habit validation) |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T12:52:26+03:00 |
| Ended | 2026-09-24T12:55:50+03:00 |

## Goal
Habits can be created (daily/weekly), updated, deactivated/reactivated and deleted. A habit can be completed for a date exactly once. Responses expose completion progress (completed today / this period, current streak, total completions).

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Domain: `Habit` (Id, Name, Description, Frequency, CreatedAt, IsActive) + `HabitFrequency` (Daily, Weekly) + `HabitCompletion` (Id, HabitId, CompletionDate, CreatedAt)
- [x] T2 Domain rules: name required, trimmed, ≤150 (BR-6); description optional ≤2000; frequency must be defined; completion date not in the future; one completion per habit per date (BR-7)
- [x] T3 Domain `HabitProgressCalculator`: `completedToday`, `completedThisPeriod` (daily = today, weekly = ISO week Mon–Sun), `currentStreak` (consecutive days/weeks ending in the current or the previous period), `totalCompletions`, `lastCompletedDate`
- [x] T4 EF mapping + unique index (HabitId, CompletionDate), cascade delete of completions; migration `phase-03-habits`
- [x] T5 `HabitsController`: `GET /api/habits` (`?isActive=`), `GET /api/habits/{id}`, `POST /api/habits`, `PUT /api/habits/{id}`, `POST /api/habits/{id}/deactivate`, `POST /api/habits/{id}/activate`, `DELETE /api/habits/{id}`
- [x] T6 Completions: `GET /api/habits/{id}/completions`, `POST /api/habits/{id}/completions` (body `{date?}`, default today) → 201 / 409 on duplicate, `DELETE /api/habits/{id}/completions/{date}` → 204
- [x] T7 Unit tests for habit rules and streak computation

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `POST /api/habits` `{name, frequency:"Daily"}` → `201`, `isActive == true`, `frequency == "Daily"`, `completedToday == false`, `currentStreak == 0`
- [x] C2 `POST /api/habits` with `frequency:"Weekly"` → `201`; `frequency:"Monthly"` → `400`
- [x] C3 `POST /api/habits` empty name → `400` `errors.Name`; 151-char name → `400`; 150-char → `201`
- [x] C4 [smoke] `POST /api/habits/{id}/completions` `{}` → `201` with `completionDate == today`; `GET /api/habits/{id}` → `completedToday == true`, `currentStreak >= 1`, `totalCompletions == 1`
- [x] C5 Second `POST /api/habits/{id}/completions` for the same date → `409` ProblemDetails (duplicate prevented)
- [x] C6 Completion for yesterday and today → `currentStreak == 2`; future date → `400`
- [x] C7 `GET /api/habits/{id}/completions` lists completions (newest first); `DELETE /api/habits/{id}/completions/{date}` → `204`, `completedToday` false afterwards; unknown date → `404`
- [x] C8 `PUT /api/habits/{id}` updates name/description/frequency → `200`
- [x] C9 `POST /api/habits/{id}/deactivate` → `200` `isActive == false`; `GET /api/habits?isActive=true` excludes it; `POST /activate` → `isActive == true`
- [x] C10 `DELETE /api/habits/{id}` → `204`; `GET` → `404`; completions of deleted habit → `404`; unknown id → `404`

## Verification

### Attempt 1 — 2026-09-24T12:54:45+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`)
**API start:** PASS (configured stop, then configured run command in background; `health 200 after 0 s`)

Check script output (commands prefixed with `$`; `» PASS/FAIL` lines are assertions evaluated on the real response bodies; bodies over 1500 chars trimmed with …):

```text
today=2026-09-24 yesterday=2026-09-23 tomorrow=2026-09-25

#### C1 [smoke] create daily habit
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"Morning run","description":"5 km","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/1
{"id":1,"name":"Morning run","description":"5 km","frequency":"Daily","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS status (= 201)
» PASS defaults/progress

#### C2 weekly / invalid frequency
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"Weekly review","frequency":"Weekly"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/2
{"id":2,"name":"Weekly review","description":null,"frequency":"Weekly","createdAt":"2026-09-24T09:54:47.0980437Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS status (= 201)
» PASS Weekly
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"x","frequency":"Monthly"}'
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"request":["The request field is required."],"$.frequency":["The JSON value could not be converted to System.Nullable`1[QuickFlow.Domain.Habits.HabitFrequency]. Path: $.frequency | LineNumber: 0 | BytePositionInLine: 33."]},"traceId":"00-fdef7bb101b055c17b8bbdc6141b3cdf-26d0698860208d9d-00"}

» PASS status (= 400)

#### C3 name validation
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"","frequency":"Daily"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Name":["Name is required."]}}

» PASS status (= 400)
» PASS errors.Name
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Name":["Name must be at most 150 characters."]}}

» PASS status 151 (= 400)
» PASS errors.Name
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/3
{"id":3,"name":"mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm","description":null,"frequency":"Daily","createdAt":"2026-09-24T09:54:49.5548297Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS status 150 (= 201)
» PASS len 150

#### C4 [smoke] complete today
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/1/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/1/completions
{"id":1,"habitId":1,"completionDate":"2026-09-24","createdAt":"2026-09-24T09:54:50.4954679Z"}

» PASS status (= 201)
» PASS completionDate today
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"name":"Morning run","description":"5 km","frequency":"Daily","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}

» PASS progress

#### C5 duplicate completion
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/1/completions -H "Content-Type: application/json" -d '{"date":"2026-09-24"}'
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10","title":"Conflict.","status":409,"detail":"Habit '1' is already completed for 2026-09-24."}

» PASS status (= 409)
» PASS ProblemDetails 409

#### C6 streak + future date
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/1/completions -H "Content-Type: application/json" -d '{"date":"2026-09-23"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/1/completions
{"id":2,"habitId":1,"completionDate":"2026-09-23","createdAt":"2026-09-24T09:54:52.219724Z"}

» PASS status (= 201)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"name":"Morning run","description":"5 km","frequency":"Daily","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":2,"totalCompletions":2,"lastCompletedDate":"2026-09-24"}

» PASS currentStreak 2
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/1/completions -H "Content-Type: application/json" -d '{"date":"2026-09-25"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Date":["Completion date cannot be in the future."]}}

» PASS status future (= 400)
» PASS errors.Date

#### C7 list + delete completion
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/1/completions
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"habitId":1,"completionDate":"2026-09-24","createdAt":"2026-09-24T09:54:50.4954679Z"},{"id":2,"habitId":1,"completionDate":"2026-09-23","createdAt":"2026-09-24T09:54:52.219724Z"}]

» PASS status (= 200)
» PASS newest first
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/habits/1/completions/2026-09-24
HTTP/1.1 204 No Content

» PASS status (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"name":"Morning run","description":"5 km","frequency":"Daily","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-23"}

» PASS completedToday false, streak from yesterday
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/habits/1/completions/2001-01-01
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Habit completion '1/2001-01-01' was not found."}

» PASS status unknown date (= 404)

#### C8 update
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/habits/1 -H "Content-Type: application/json" -d '{"name":"Evening run","description":"3 km","frequency":"Weekly"}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"name":"Evening run","description":"3 km","frequency":"Weekly","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":false,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-23"}

» PASS status (= 200)
» PASS updated
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/habits/1 -H "Content-Type: application/json" -d '{"name":""}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Name":["Name is required."]}}

» PASS status invalid (= 400)

#### C9 deactivate / activate
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/2/deactivate
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":2,"name":"Weekly review","description":null,"frequency":"Weekly","createdAt":"2026-09-24T09:54:47.0980437Z","isActive":false,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS status (= 200)
» PASS inactive
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits?isActive=true
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"name":"Evening run","description":"3 km","frequency":"Weekly","createdAt":"2026-09-24T09:54:46.1925923Z","isActive":true,"completedToday":false,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-23"},{"id":3,"name":"mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm","description":null,"frequency":"Daily","createdAt":"2026-09-24T09:54:49.5548297Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}]

» PASS excluded from active list, all active
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits?isActive=false
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":2,"name":"Weekly review","description":null,"frequency":"Weekly","createdAt":"2026-09-24T09:54:47.0980437Z","isActive":false,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}]

» PASS listed as inactive
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/2/activate
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":2,"name":"Weekly review","description":null,"frequency":"Weekly","createdAt":"2026-09-24T09:54:47.0980437Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS status (= 200)
» PASS active again

#### C10 delete / 404
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/habits/3
HTTP/1.1 204 No Content

» PASS status (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/3
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Habit '3' was not found."}

» PASS status after delete (= 404)
» PASS ProblemDetails
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/3/completions
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Habit '3' was not found."}

» PASS completions of deleted (= 404)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/3/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Habit '3' was not found."}

» PASS complete deleted (= 404)
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/habits/999999 -H "Content-Type: application/json" -d '{"name":"x"}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Habit '999999' was not found."}

» PASS unknown PUT (= 404)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/habits/999999
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Habit '999999' was not found."}

» PASS unknown DELETE (= 404)

FAILS=0
```

| Check | Result |
|---|---|
| C1 [smoke] create daily habit → 201, active, no progress | **PASS** |
| C2 Weekly → 201; Monthly → 400 | **PASS** |
| C3 empty name → 400 errors.Name; 151 → 400; 150 → 201 | **PASS** |
| C4 [smoke] complete today → 201; completedToday, streak ≥ 1, total 1 | **PASS** |
| C5 duplicate completion same date → 409 ProblemDetails (BR-7) | **PASS** |
| C6 yesterday + today → streak 2; future date → 400 | **PASS** |
| C7 completions newest first; delete completion → 204; unknown date → 404 | **PASS** |
| C8 PUT updates name/description/frequency → 200 (blank name → 400) | **PASS** |
| C9 deactivate → excluded from isActive=true; activate → active | **PASS** |
| C10 delete → 204; GET/completions/complete → 404; unknown id → 404 | **PASS** |

Smoke regression summary: phase-01 C1, C2 PASS · phase-02 C1, C2 PASS.

#### Smoke regression

```text
## phase-01 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T09:55:01.4534977Z"}

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
Location: http://localhost:5080/api/tasks/9
{"id":9,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T09:55:02.5011748Z","updatedAt":"2026-09-24T09:55:02.5011748Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C1 status (= 201)
» PASS phase-02 C1 fields
## phase-02 C2 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/9
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":9,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T09:55:02.5011748Z","updatedAt":"2026-09-24T09:55:02.5011748Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C2 status (= 200)
» PASS phase-02 C2 same id
SMOKE_FAILS=0
```

**Swagger export / API stop / unit tests:**

```text
$ curl.exe -s --max-time 15 http://localhost:5080/swagger/v1/swagger.json -o docs/api/swagger.json
exit 0
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/habits>'
valid JSON, openapi 3.0.1, 12 paths, 19 operations, schemas: 16
this phase (10):
  GET /api/habits
  POST /api/habits
  GET /api/habits/{id}
  PUT /api/habits/{id}
  DELETE /api/habits/{id}
  POST /api/habits/{id}/deactivate
  POST /api/habits/{id}/activate
  GET /api/habits/{id}/completions
  POST /api/habits/{id}/completions
  DELETE /api/habits/{id}/completions/{date}
API stopped (configured stop command)
$ dotnet test backend/QuickFlow.sln
Passed!  - Failed:     0, Passed:    27, Skipped:     0, Total:    27, Duration: 59 ms - QuickFlow.Tests.dll (net8.0)
```

**Swagger export:** PASS → `docs/api/swagger.json` (12 paths, 19 operations; all 10 habit operations listed)
**Unit tests:** 27 passed, 0 failed
**Result:** PASS 10/10 (41 assertions + 7 smoke assertions, 0 failures)

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 27, Failed 0 (+14 HabitTests)

## Failure record
—

## Notes
- "Today" = the server's local date (`IClock.Today`). A completion with no body/date is recorded for today.
- Weekly habits: `completedThisPeriod` means at least one completion in the current ISO week (Mon–Sun), and `currentStreak` counts consecutive weeks. `completedToday` is still reported for weekly habits.
- Duplicate protection is enforced twice: the domain (`ConflictException` → 409) and the unique index `(HabitId, CompletionDate)`. A concurrent duplicate caught by the index is also mapped to 409.
- Deactivated habits can still be completed. The PRD defines no rule against it.
