# phase-02-tasks — Tasks resource

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | US Tasks 1–7, FR-01, FR-02, BR-1..5, BR-14, §12 Tasks, §11 unit tests (task validation) |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T12:43:16+03:00 |
| Ended | 2026-09-24T12:52:04+03:00 |

## Goal
Full task management over HTTP: create, read, update, complete, archive, restore, delete, with free-text search, status/priority/due-date filters, overdue detection and sorting. All rules are enforced in the domain project.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Domain: `TaskItem` entity (Id, Title, Description, Status, Priority, DueDate (date), CreatedAt, UpdatedAt, CompletedAt, IsArchived) + `TaskItemStatus` (Todo, InProgress, Done), `TaskPriority` (Low, Medium, High)
- [x] T2 Domain rules: title required, trimmed, ≤200 (BR-1); description optional ≤2000 (BR-2); status/priority must be defined enum values (BR-3); `Complete()` sets status Done + CompletedAt (BR-5); `IsOverdue(today)` = DueDate < today && status != Done && !archived; archive/restore
- [x] T3 EF mapping + migration `phase-02-tasks` (indexes on Status, Priority, DueDate, IsArchived)
- [x] T4 DTOs (`TaskResponse` with computed `isOverdue`, `CreateTaskRequest`, `UpdateTaskRequest`) and query model (`search`, `status`, `priority`, `dueDate`, `dueFrom`, `dueTo`, `overdue`, `archived` = exclude|include|only, `sortBy` = createdAt|dueDate, `sortDir` = asc|desc)
- [x] T5 `TasksController`: `GET /api/tasks`, `GET /api/tasks/{id}`, `POST /api/tasks`, `PUT /api/tasks/{id}`, `POST /api/tasks/{id}/complete`, `POST /api/tasks/{id}/archive`, `POST /api/tasks/{id}/restore`, `DELETE /api/tasks/{id}`
- [x] T6 Default list excludes archived tasks (BR-4); deleted tasks return 404 (BR-14)
- [x] T7 Unit tests for task rules (title/description limits, complete, overdue)

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `POST /api/tasks` valid body → `201`, `Location` header, body has `id`, `status == "Todo"` (default), `priority`, `createdAt`, `isArchived == false`
- [x] C2 [smoke] `GET /api/tasks/{id}` → `200` with the same task
- [x] C3 `POST /api/tasks` with empty/whitespace title → `400` ProblemDetails with `errors.Title`
- [x] C4 `POST /api/tasks` with 201-char title → `400`; 200-char title → `201`
- [x] C5 `POST /api/tasks` with 2001-char description → `400` with `errors.Description`
- [x] C6 `POST /api/tasks` with `status: "Blocked"` → `400`; with `priority: 5` (integer) → `400`
- [x] C7 `PUT /api/tasks/{id}` changes title/status/priority/dueDate → `200`, `updatedAt` > `createdAt`
- [x] C8 `POST /api/tasks/{id}/complete` → `200`, `status == "Done"`, `completedAt` set
- [x] C9 `POST /api/tasks/{id}/archive` → `200` `isArchived == true`; task absent from default `GET /api/tasks`; present with `archived=only`; `POST /restore` → back in default list
- [x] C10 `DELETE /api/tasks/{id}` → `204`; then `GET` → `404` ProblemDetails; `PUT`/`DELETE` unknown id → `404`
- [x] C11 `GET /api/tasks?search=<word>` returns only tasks whose title contains the word (case-insensitive)
- [x] C12 `GET /api/tasks?status=Done&priority=High` returns only matching tasks; `?status=Bogus` → `400`
- [x] C13 `GET /api/tasks?overdue=true` returns only tasks with past due date and not Done (`isOverdue == true`); `?dueDate=<date>` returns tasks due that date
- [x] C14 `GET /api/tasks?sortBy=dueDate&sortDir=asc` ordered by due date ascending; `sortBy=createdAt&sortDir=desc` newest first
- [x] C15 Data persists across API restart (task created before restart still returned after)

## Verification

### Attempt 1 — 2026-09-24T12:49:51+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`)
**API start:** PASS (configured stop, then configured run command in background; `health 200 after 0 s`)

Check script output (commands prefixed with `$`; `» PASS/FAIL` lines are assertions evaluated on the real response bodies; bodies over 1500 chars trimmed with …):

```text
today=2026-09-24 yesterday=2026-09-23 tomorrow=2026-09-25 tag=zq1790243391

#### C1 [smoke] create task
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"zq1790243391 report","description":"Quarterly","priority":"High","dueDate":"2026-09-25"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/1
{"id":1,"title":"zq1790243391 report","description":"Quarterly","status":"Todo","priority":"High","dueDate":"2026-09-25","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:49:51.5095274Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS status (= 201)
» PASS Location header + fields
» PASS Location header present

#### C2 [smoke] get task
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"zq1790243391 report","description":"Quarterly","status":"Todo","priority":"High","dueDate":"2026-09-25","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:49:51.5095274Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS status (= 200)
» PASS same task

#### C3 empty/whitespace title
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"   "}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status (= 400)
» PASS errors.Title
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"description":"no title"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status (= 400)
» PASS errors.Title

#### C4 title length 201 / 200
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title must be at most 200 characters."]}}

» PASS status (= 400)
» PASS errors.Title
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/2
{"id":2,"title":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T09:49:54.8964497Z","updatedAt":"2026-09-24T09:49:54.8964497Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS status (= 201)
» PASS title length 200

#### C5 description 2001 chars
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"x","description":"ddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Description":["Description must be at most 2000 characters."]}}

» PASS status (= 400)
» PASS errors.Description

#### C6 invalid status / integer priority
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"x","status":"Blocked"}'
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"request":["The request field is required."],"$.status":["The JSON value could not be converted to System.Nullable`1[QuickFlow.Domain.Tasks.TaskItemStatus]. Path: $.status | LineNumber: 0 | BytePositionInLine: 31."]},"traceId":"00-a50b6a73c8089fa24ecaea58f3071b99-fdef35c74ebd22c3-00"}

» PASS status (= 400)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"x","priority":5}'
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"request":["The request field is required."],"$.priority":["The JSON value could not be converted to System.Nullable`1[QuickFlow.Domain.Tasks.TaskPriority]. Path: $.priority | LineNumber: 0 | BytePositionInLine: 25."]},"traceId":"00-6f5384949a5b54872d0b072f672fc048-77a9a7273f256aed-00"}

» PASS status (= 400)

#### C7 update
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/tasks/1 -H "Content-Type: application/json" -d '{"title":"zq1790243391 report v2","status":"InProgress","priority":"Low","dueDate":"2026-10-01"}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"zq1790243391 report v2","description":null,"status":"InProgress","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:49:57.9230351Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS status (= 200)
» PASS fields changed, updatedAt > createdAt
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/tasks/1 -H "Content-Type: application/json" -d '{"title":""}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status (invalid update) (= 400)

#### C8 complete
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks/1/complete
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:49:58.7664197Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":false,"isOverdue":false}

» PASS status (= 200)
» PASS Done + completedAt

#### C9 archive / restore
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks/1/archive
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:49:59.2558717Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":true,"isOverdue":false}

» PASS status (= 200)
» PASS isArchived
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?search=zq1790243391
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[]

» PASS absent from default list
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?archived=only
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:49:59.2558717Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":true,"isOverdue":false}]

» PASS present with archived=only (all archived)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks/1/restore
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:50:00.8314803Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":false,"isOverdue":false}

» PASS status (= 200)
» PASS not archived
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?search=zq1790243391
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:50:00.8314803Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":false,"isOverdue":false}]

» PASS back in default list

#### C10 delete / 404
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"zq1790243391 to delete"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/3
{"id":3,"title":"zq1790243391 to delete","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T09:50:01.8226084Z","updatedAt":"2026-09-24T09:50:01.8226084Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/tasks/3
HTTP/1.1 204 No Content

» PASS status (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/3
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Task '3' was not found."}

» PASS status (= 404)
» PASS ProblemDetails
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/tasks/999999 -H "Content-Type: application/json" -d '{"title":"x"}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Task '999999' was not found."}

» PASS status (= 404)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/tasks/999999
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Task '999999' was not found."}

» PASS status (= 404)

#### C11 search
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Buy MILK zq1790243391","priority":"Low"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/4
{"id":4,"title":"Buy MILK zq1790243391","description":null,"status":"Todo","priority":"Low","dueDate":null,"createdAt":"2026-09-24T09:50:03.8094996Z","updatedAt":"2026-09-24T09:50:03.8094996Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Call bank zq1790243391","priority":"High","status":"Done"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/5
{"id":5,"title":"Call bank zq1790243391","description":null,"status":"Done","priority":"High","dueDate":null,"createdAt":"2026-09-24T09:50:04.3140608Z","updatedAt":"2026-09-24T09:50:04.3140608Z","completedAt":"2026-09-24T09:50:04.3140608Z","isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?search=milk%20zq1790243391
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":4,"title":"Buy MILK zq1790243391","description":null,"status":"Todo","priority":"Low","dueDate":null,"createdAt":"2026-09-24T09:50:03.8094996Z","updatedAt":"2026-09-24T09:50:03.8094996Z","completedAt":null,"isArchived":false,"isOverdue":false}]

» PASS status (= 200)
» PASS only titles containing 'milk zq1790243391' (case-insensitive)

#### C12 status + priority filter
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?status=Done&priority=High
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":5,"title":"Call bank zq1790243391","description":null,"status":"Done","priority":"High","dueDate":null,"createdAt":"2026-09-24T09:50:04.3140608Z","updatedAt":"2026-09-24T09:50:04.3140608Z","completedAt":"2026-09-24T09:50:04.3140608Z","isArchived":false,"isOverdue":false}]

» PASS status (= 200)
» PASS all Done+High, includes bank task
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?status=Bogus
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Status":["The value 'Bogus' is not valid for Status."]},"traceId":"00-a93371e54ded083d3aff1385b674a2db-24f5589a5e968dd4-00"}

» PASS status (= 400)

#### C13 overdue + dueDate filter
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Overdue zq1790243391","dueDate":"2026-09-23"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/6
{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true}

» PASS isOverdue true on create
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Done late zq1790243391","dueDate":"2026-09-23","status":"Done"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/7
{"id":7,"title":"Done late zq1790243391","description":null,"status":"Done","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.8016496Z","updatedAt":"2026-09-24T09:50:06.8016496Z","completedAt":"2026-09-24T09:50:06.8016496Z","isArchived":false,"isOverdue":false}

» PASS done task not overdue
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/tasks -H "Content-Type: application/json" -d '{"title":"Due today zq1790243391","dueDate":"2026-09-24"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/tasks/8
{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false}

$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?overdue=true
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true}]

» PASS only overdue (past due, not Done), includes 6, excludes 7/8
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?dueDate=2026-09-24
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false}]

» PASS only due today, includes 8

#### C14 sorting
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?search=zq1790243391&sortBy=dueDate&sortDir=asc
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":7,"title":"Done late zq1790243391","description":null,"status":"Done","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.8016496Z","updatedAt":"2026-09-24T09:50:06.8016496Z","completedAt":"2026-09-24T09:50:06.8016496Z","isArchived":false,"isOverdue":false},{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:50:00.8314803Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":false,"isOverdue":false},{"id":4,"title":"Buy MILK zq1790243391","description":null,"status":"Todo","priority":"Low","dueDate":null,"createdAt":"2026-09-24T09:50:03.8094996Z","updatedAt":"2026-09-24T09:50:03.8094996Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":5,"title":"Call bank zq1790243391","description":null,"status":"Done","priority":"High","dueDate":null,"createdAt":"2026-09-24T09:50:04.3140608Z","up …
» PASS dueDate ascending (nulls last)
6:2026-09-23 7:2026-09-23 8:2026-09-24 1:2026-10-01 4:null 5:null
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks?search=zq1790243391&sortBy=createdAt&sortDir=desc
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":8,"title":"Due today zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-24","createdAt":"2026-09-24T09:50:07.4124243Z","updatedAt":"2026-09-24T09:50:07.4124243Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":7,"title":"Done late zq1790243391","description":null,"status":"Done","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.8016496Z","updatedAt":"2026-09-24T09:50:06.8016496Z","completedAt":"2026-09-24T09:50:06.8016496Z","isArchived":false,"isOverdue":false},{"id":6,"title":"Overdue zq1790243391","description":null,"status":"Todo","priority":"Medium","dueDate":"2026-09-23","createdAt":"2026-09-24T09:50:06.1555619Z","updatedAt":"2026-09-24T09:50:06.1555619Z","completedAt":null,"isArchived":false,"isOverdue":true},{"id":5,"title":"Call bank zq1790243391","description":null,"status":"Done","priority":"High","dueDate":null,"createdAt":"2026-09-24T09:50:04.3140608Z","updatedAt":"2026-09-24T09:50:04.3140608Z","completedAt":"2026-09-24T09:50:04.3140608Z","isArchived":false,"isOverdue":false},{"id":4,"title":"Buy MILK zq1790243391","description":null,"status":"Todo","priority":"Low","dueDate":null,"createdAt":"2026-09-24T09:50:03.8094996Z","updatedAt":"2026-09-24T09:50:03.8094996Z","completedAt":null,"isArchived":false,"isOverdue":false},{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","up …
» PASS createdAt descending
8:2026-09-24T09:50:07.4124243Z 7:2026-09-24T09:50:06.8016496Z 6:2026-09-24T09:50:06.1555619Z 5:2026-09-24T09:50:04.3140608Z 4:2026-09-24T09:50:03.8094996Z 1:2026-09-24T09:49:51.5095274Z

FAILS=0

#### C15 persistence across restart (task 1 created before restart)
(API stopped with the configured stop command and restarted with the configured run command; health polled: health 200 after 0 s)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"zq1790243391 report v2","description":null,"status":"Done","priority":"Low","dueDate":"2026-10-01","createdAt":"2026-09-24T09:49:51.5095274Z","updatedAt":"2026-09-24T09:50:00.8314803Z","completedAt":"2026-09-24T09:49:58.7664197Z","isArchived":false,"isOverdue":false}

» PASS status after restart (= 200)
» PASS same task id

#### Smoke regression: phase-01 C1, C2
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T09:50:41.8328215Z"}

» PASS phase-01 C1 status (= 200)
» PASS phase-01 C1 Healthy
$ curl.exe -s -i --max-time 15 http://localhost:5080/swagger/v1/swagger.json (headers + parsed summary)
HTTP/1.1 200 OK
» PASS phase-01 C2 openapi 3.x

FAILS=0

$ powershell -NoProfile -Command "Get-NetTCPConnection -LocalPort 5080 -State Listen | ForEach-Object { Get-Process -Id $_.OwningProcess } | Select-Object Id,ProcessName,StartTime | Format-Table -AutoSize | Out-String"
   Id ProcessName   StartTime            
   -- -----------   ---------            
29008 QuickFlow.Api 9/24/2026 12:50:36 PM
29008 QuickFlow.Api 9/24/2026 12:50:36 PM
```

| Check | Result |
|---|---|
| C1 [smoke] create → 201, Location, defaults | **PASS** |
| C2 [smoke] get by id → 200 | **PASS** |
| C3 empty/whitespace/missing title → 400 errors.Title | **PASS** |
| C4 201 chars → 400, 200 chars → 201 | **PASS** |
| C5 2001-char description → 400 errors.Description | **PASS** |
| C6 status "Blocked" → 400, priority 5 → 400 | **PASS** |
| C7 PUT → 200, fields changed, updatedAt > createdAt (and blank title → 400) | **PASS** |
| C8 complete → Done + completedAt | **PASS** |
| C9 archive hides from default list, archived=only shows it, restore brings it back | **PASS** |
| C10 delete → 204, then 404 ProblemDetails; PUT/DELETE unknown → 404 | **PASS** |
| C11 search (case-insensitive) | **PASS** |
| C12 status+priority filter; status=Bogus → 400 | **PASS** |
| C13 overdue filter + isOverdue; dueDate filter | **PASS** |
| C14 sort by dueDate asc (nulls last) / createdAt desc | **PASS** |
| C15 persists across restart (new process started 12:50:36, task 1 still returned) | **PASS** |

#### Smoke regression
| Check | Result |
|---|---|
| phase-01 C1 | PASS |
| phase-01 C2 | PASS |

**Swagger export / stop / unit tests:**

```text
$ curl.exe -s --max-time 15 http://localhost:5080/swagger/v1/swagger.json -o docs/api/swagger.json
exit 0
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/tasks>'
valid JSON, openapi 3.0.1, 6 paths, 9 operations, schemas: 11
this phase (0):
  
API stopped (configured stop command)
$ dotnet test backend/QuickFlow.sln
Passed!  - Failed:     0, Passed:    13, Skipped:     0, Total:    13, Duration: 43 ms - QuickFlow.Tests.dll (net8.0)
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/tasks>'  (re-run with MSYS_NO_PATHCONV=1)
valid JSON, openapi 3.0.1, 6 paths, 9 operations, schemas: 11
this phase (8):
  GET /api/tasks
  POST /api/tasks
  GET /api/tasks/{id}
  PUT /api/tasks/{id}
  DELETE /api/tasks/{id}
  POST /api/tasks/{id}/complete
  POST /api/tasks/{id}/archive
  POST /api/tasks/{id}/restore
```

**Swagger export:** PASS → `docs/api/swagger.json` (6 paths, 9 operations; all 8 task operations listed)
**Unit tests:** 13 passed, 0 failed
**Result:** PASS 15/15 (45 + 5 assertions, 0 failures)

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 13, Failed 0 (ValidatorTests 3, TaskItemTests 10)

## Failure record
—

## Notes
- `PUT /api/tasks/{id}`: Title required; null Status/Priority keep their current values; Description and DueDate are replaced (null clears them). This is documented in the Swagger schema description.
- Search uses `lower(Title) LIKE`-style matching (`ToLower().Contains`) because SQLite `instr` is case-sensitive.
- Swagger lists query parameter names in PascalCase (`Search`, `Status`, …). ASP.NET Core query binding is case-insensitive, so camelCase (`?search=&status=`) works, as every check above shows.
- Verification tooling: the API is started with the Bash tool's background mode. Starting it inside a piped command made `tee` wait forever, because the API inherits the pipe handle on Windows. That run was stopped before any check executed and did not count as an attempt.
