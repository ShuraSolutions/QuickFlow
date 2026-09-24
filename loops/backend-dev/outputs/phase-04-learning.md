# phase-04-learning — Learning cards, milestones & notes

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | completed |
| Attempts | 1 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | US Learning 1–5, FR-05, FR-06, BR-8, BR-9, BR-14, §12 Learning Resources |
| Requirements source | docs/Task_PRD.md |
| Started | 2026-09-24T12:56:04+03:00 |
| Ended | 2026-09-24T12:59:10+03:00 |

## Goal
Learning cards can be added, updated and removed. Each card holds milestones (add, complete/uncomplete, remove) and free-form notes (add, remove). Card responses expose milestone progress.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [x] T1 Domain: `LearningCard` (Id, Title, Description, Status, CreatedAt) + `LearningStatus` (NotStarted, InProgress, Completed); `LearningMilestone` (Id, LearningCardId, Title, IsDone, TargetDate, CompletedAt); `LearningNote` (Id, LearningCardId, Text, CreatedAt)
- [x] T2 Domain rules: card title required ≤200 (BR-8), description/source optional ≤2000; milestone title required ≤200; note text required ≤4000; a milestone/note belongs to exactly one card (BR-9, required FK); completing a milestone of a `NotStarted` card moves the card to `InProgress`
- [x] T3 EF mapping (cascade delete of milestones/notes with the card) + migration `phase-04-learning`
- [x] T4 `LearningCardsController`: `GET /api/learning-cards` (`?status=`), `GET /api/learning-cards/{id}` (includes milestones + notes), `POST`, `PUT /{id}`, `DELETE /{id}`
- [x] T5 Milestones: `POST /api/learning-cards/{id}/milestones`, `PATCH /api/learning-cards/{id}/milestones/{milestoneId}` (title/isDone/targetDate), `DELETE /api/learning-cards/{id}/milestones/{milestoneId}`
- [x] T6 Notes: `POST /api/learning-cards/{id}/notes`, `DELETE /api/learning-cards/{id}/notes/{noteId}`
- [x] T7 Computed fields on card: `milestonesTotal`, `milestonesDone`, `milestoneProgressPercentage`, `notesCount`
- [x] T8 Unit tests for learning rules and progress computation

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [x] C1 [smoke] `POST /api/learning-cards` `{title, description}` → `201`, `status == "NotStarted"`, `milestones == []`, `notes == []`
- [x] C2 `POST /api/learning-cards` empty title → `400` `errors.Title`; `status:"Paused"` → `400`
- [x] C3 `PUT /api/learning-cards/{id}` updates title/description/status → `200`
- [x] C4 [smoke] `POST /api/learning-cards/{id}/milestones` `{title, targetDate}` → `201`, `isDone == false`, `learningCardId == id`; empty title → `400`
- [x] C5 `PATCH .../milestones/{mid}` `{isDone:true}` → `200` `isDone == true`, `completedAt` set; card `milestonesDone == 1`, `milestoneProgressPercentage == 50` (with 2 milestones), card status moved to `InProgress`
- [x] C6 `DELETE .../milestones/{mid}` → `204`; milestone of another card or unknown id → `404` (BR-9)
- [x] C7 `POST .../notes` `{text}` → `201` with `createdAt`; empty text → `400`; `DELETE .../notes/{nid}` → `204`; unknown → `404`
- [x] C8 `GET /api/learning-cards?status=InProgress` returns only in-progress cards
- [x] C9 `DELETE /api/learning-cards/{id}` → `204`; `GET` → `404`; its milestone/note endpoints → `404`

## Verification

### Attempt 1 — 2026-09-24T12:58:16+03:00
**Build:** PASS (`dotnet build backend/QuickFlow.sln -c Debug` → `Build succeeded. 0 Warning(s) 0 Error(s)`)
**API start:** PASS (configured stop, then configured run command in background; `health 200 after 0 s`)

Check script output (commands prefixed with `$`; `» PASS/FAIL` lines are assertions evaluated on the real response bodies; bodies over 1500 chars trimmed with …):

```text
#### C1 [smoke] create card
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"Clean Code","description":"Book by Robert C. Martin"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/1
{"id":1,"title":"Clean Code","description":"Book by Robert C. Martin","status":"NotStarted","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

» PASS status (= 201)
» PASS defaults

#### C2 validation
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"","description":"x"}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status (= 400)
» PASS errors.Title
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"x","status":"Paused"}'
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"request":["The request field is required."],"$.status":["The JSON value could not be converted to System.Nullable`1[QuickFlow.Domain.Learning.LearningStatus]. Path: $.status | LineNumber: 0 | BytePositionInLine: 30."]},"traceId":"00-791bddda7ef98a69a8a928af192a28f9-378e3bbeb176684a-00"}

» PASS status (= 400)

#### C3 update
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/learning-cards/1 -H "Content-Type: application/json" -d '{"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"NotStarted"}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"NotStarted","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

» PASS status (= 200)
» PASS updated
$ curl.exe -s -i --max-time 15 -X PUT http://localhost:5080/api/learning-cards/1 -H "Content-Type: application/json" -d '{"title":" "}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status invalid (= 400)

#### C4 [smoke] add milestones
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/1/milestones -H "Content-Type: application/json" -d '{"title":"Chapters 1-3","targetDate":"2026-10-01"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/1
{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":false,"targetDate":"2026-10-01","completedAt":null}

» PASS status (= 201)
» PASS milestone fields
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/1/milestones -H "Content-Type: application/json" -d '{"title":"Chapters 4-6"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/1
{"id":2,"learningCardId":1,"title":"Chapters 4-6","isDone":false,"targetDate":null,"completedAt":null}

» PASS status (= 201)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/1/milestones -H "Content-Type: application/json" -d '{"title":""}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Title":["Title is required."]}}

» PASS status empty title (= 400)
» PASS errors.Title

#### C5 complete milestone
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/learning-cards/1/milestones/1 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":true,"targetDate":"2026-10-01","completedAt":"2026-09-24T09:58:22.0651984Z"}

» PASS status (= 200)
» PASS done + completedAt
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"InProgress","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":true,"targetDate":"2026-10-01","completedAt":"2026-09-24T09:58:22.0651984Z"},{"id":2,"learningCardId":1,"title":"Chapters 4-6","isDone":false,"targetDate":null,"completedAt":null}],"notes":[],"milestonesTotal":2,"milestonesDone":1,"milestoneProgressPercentage":50,"notesCount":0}

» PASS card progress 1/2 = 50, status InProgress
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/learning-cards/1/milestones/2 -H "Content-Type: application/json" -d '{"title":"Chapters 4-7","clearTargetDate":true}'
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":2,"learningCardId":1,"title":"Chapters 4-7","isDone":false,"targetDate":null,"completedAt":null}

» PASS status rename (= 200)
» PASS renamed, not done

#### C6 remove milestone / BR-9
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards -H "Content-Type: application/json" -d '{"title":"Other card"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/2
{"id":2,"title":"Other card","description":null,"status":"NotStarted","createdAt":"2026-09-24T09:58:23.7915683Z","milestones":[],"notes":[],"milestonesTotal":0,"milestonesDone":0,"milestoneProgressPercentage":0,"notesCount":0}

$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/2/milestones -H "Content-Type: application/json" -d '{"title":"Other ms"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/2
{"id":3,"learningCardId":2,"title":"Other ms","isDone":false,"targetDate":null,"completedAt":null}

$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/1/milestones/3
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Milestone '1/3' was not found."}

» PASS milestone of another card via this card (= 404)
$ curl.exe -s -i --max-time 15 -X PATCH http://localhost:5080/api/learning-cards/1/milestones/3 -H "Content-Type: application/json" -d '{"isDone":true}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Milestone '1/3' was not found."}

» PASS patch milestone of another card (= 404)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/1/milestones/999999
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Milestone '1/999999' was not found."}

» PASS unknown milestone (= 404)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/1/milestones/2
HTTP/1.1 204 No Content

» PASS status (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"InProgress","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":true,"targetDate":"2026-10-01","completedAt":"2026-09-24T09:58:22.0651984Z"}],"notes":[],"milestonesTotal":1,"milestonesDone":1,"milestoneProgressPercentage":100,"notesCount":0}

» PASS one milestone left, 100%

#### C7 notes
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/1/notes -H "Content-Type: application/json" -d '{"text":"Functions should do one thing."}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/1
{"id":1,"learningCardId":1,"text":"Functions should do one thing.","createdAt":"2026-09-24T09:58:27.1166972Z"}

» PASS status (= 201)
» PASS note fields
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/1/notes -H "Content-Type: application/json" -d '{"text":"Names reveal intent."}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/learning-cards/1
{"id":2,"learningCardId":1,"text":"Names reveal intent.","createdAt":"2026-09-24T09:58:27.8381054Z"}

$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards/1
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":1,"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"InProgress","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":true,"targetDate":"2026-10-01","completedAt":"2026-09-24T09:58:22.0651984Z"}],"notes":[{"id":2,"learningCardId":1,"text":"Names reveal intent.","createdAt":"2026-09-24T09:58:27.8381054Z"},{"id":1,"learningCardId":1,"text":"Functions should do one thing.","createdAt":"2026-09-24T09:58:27.1166972Z"}],"milestonesTotal":1,"milestonesDone":1,"milestoneProgressPercentage":100,"notesCount":2}

» PASS 2 notes, newest first
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/1/notes -H "Content-Type: application/json" -d '{"text":""}'
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"Text":["Text is required."]}}

» PASS status empty (= 400)
» PASS errors.Text
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/1/notes/1
HTTP/1.1 204 No Content

» PASS status delete (= 204)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/1/notes/999999
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Note '1/999999' was not found."}

» PASS unknown note (= 404)

#### C8 status filter
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards?status=InProgress
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
[{"id":1,"title":"Clean Code (2nd ed.)","description":"https://example.com/clean-code","status":"InProgress","createdAt":"2026-09-24T09:58:16.9187723Z","milestones":[{"id":1,"learningCardId":1,"title":"Chapters 1-3","isDone":true,"targetDate":"2026-10-01","completedAt":"2026-09-24T09:58:22.0651984Z"}],"notes":[{"id":2,"learningCardId":1,"text":"Names reveal intent.","createdAt":"2026-09-24T09:58:27.8381054Z"}],"milestonesTotal":1,"milestonesDone":1,"milestoneProgressPercentage":100,"notesCount":1}]

» PASS status (= 200)
» PASS only InProgress, includes card 1, excludes 2
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards?status=Paused
HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=utf-8
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"status":["The value 'Paused' is not valid."]},"traceId":"00-66baefd62c30d99d566e7fdf5a457f92-cfd0460ffb2c0f0b-00"}

» PASS invalid status filter (= 400)

#### C9 delete card
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/2
HTTP/1.1 204 No Content

» PASS status (= 204)
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/learning-cards/2
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Learning card '2' was not found."}

» PASS status after delete (= 404)
» PASS ProblemDetails
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/2/milestones -H "Content-Type: application/json" -d '{"title":"x"}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Learning card '2' was not found."}

» PASS milestones of deleted card (= 404)
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/learning-cards/2/notes -H "Content-Type: application/json" -d '{"text":"x"}'
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Learning card '2' was not found."}

» PASS notes of deleted card (= 404)
$ curl.exe -s -i --max-time 15 -X DELETE http://localhost:5080/api/learning-cards/999999
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Resource not found.","status":404,"detail":"Learning card '999999' was not found."}

» PASS unknown card (= 404)

FAILS=0
```

| Check | Result |
|---|---|
| C1 [smoke] create card → 201, NotStarted, empty milestones/notes | **PASS** |
| C2 empty title → 400 errors.Title; status "Paused" → 400 | **PASS** |
| C3 PUT updates title/description/status → 200 (blank title → 400) | **PASS** |
| C4 [smoke] add milestone → 201, isDone false, learningCardId; empty title → 400 | **PASS** |
| C5 complete milestone → completedAt; card 1/2 = 50%, NotStarted → InProgress | **PASS** |
| C6 milestone of another card / unknown → 404 (BR-9); delete → 204 | **PASS** |
| C7 add note → 201 with createdAt; empty → 400; delete → 204; unknown → 404 | **PASS** |
| C8 `?status=InProgress` only in-progress cards (invalid status → 400) | **PASS** |
| C9 delete card → 204; GET and child endpoints → 404 | **PASS** |

Smoke regression summary: phase-01 C1, C2 · phase-02 C1, C2 · phase-03 C1, C4, all PASS.

#### Smoke regression

```text
## phase-01 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/health
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"status":"Healthy","database":"Connected","serverTimeUtc":"2026-09-24T09:58:33.5213852Z"}

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
Location: http://localhost:5080/api/tasks/10
{"id":10,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T09:58:34.5780134Z","updatedAt":"2026-09-24T09:58:34.5780134Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C1 status (= 201)
» PASS phase-02 C1 fields
## phase-02 C2 [smoke]
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/tasks/10
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":10,"title":"smoke task","description":null,"status":"Todo","priority":"Medium","dueDate":null,"createdAt":"2026-09-24T09:58:34.5780134Z","updatedAt":"2026-09-24T09:58:34.5780134Z","completedAt":null,"isArchived":false,"isOverdue":false}

» PASS phase-02 C2 status (= 200)
» PASS phase-02 C2 same id
## phase-03 C1 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits -H "Content-Type: application/json" -d '{"name":"smoke habit","frequency":"Daily"}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/4
{"id":4,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T09:58:36.2619094Z","isActive":true,"completedToday":false,"completedThisPeriod":false,"currentStreak":0,"totalCompletions":0,"lastCompletedDate":null}

» PASS phase-03 C1 status (= 201)
» PASS phase-03 C1 fields
## phase-03 C4 [smoke]
$ curl.exe -s -i --max-time 15 -X POST http://localhost:5080/api/habits/4/completions -H "Content-Type: application/json" -d '{}'
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Location: http://localhost:5080/api/habits/4/completions
{"id":3,"habitId":4,"completionDate":"2026-09-24","createdAt":"2026-09-24T09:58:36.9810086Z"}

» PASS phase-03 C4 status (= 201)
» PASS phase-03 C4 today
$ curl.exe -s -i --max-time 15 -X GET http://localhost:5080/api/habits/4
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
{"id":4,"name":"smoke habit","description":null,"frequency":"Daily","createdAt":"2026-09-24T09:58:36.2619094Z","isActive":true,"completedToday":true,"completedThisPeriod":true,"currentStreak":1,"totalCompletions":1,"lastCompletedDate":"2026-09-24"}

» PASS phase-03 C4 progress
SMOKE_FAILS=0
```

**Swagger export / API stop / unit tests:**

```text
$ curl.exe -s --max-time 15 http://localhost:5080/swagger/v1/swagger.json -o docs/api/swagger.json
exit 0
$ node -e '<validate docs/api/swagger.json; count paths/operations; list operations matching ^/api/learning-cards>'
valid JSON, openapi 3.0.1, 18 paths, 29 operations, schemas: 24
this phase (10):
  GET /api/learning-cards
  POST /api/learning-cards
  GET /api/learning-cards/{id}
  PUT /api/learning-cards/{id}
  DELETE /api/learning-cards/{id}
  POST /api/learning-cards/{id}/milestones
  PATCH /api/learning-cards/{id}/milestones/{milestoneId}
  DELETE /api/learning-cards/{id}/milestones/{milestoneId}
  POST /api/learning-cards/{id}/notes
  DELETE /api/learning-cards/{id}/notes/{noteId}
API stopped (configured stop command)
$ dotnet test backend/QuickFlow.sln
Passed!  - Failed:     0, Passed:    38, Skipped:     0, Total:    38, Duration: 59 ms - QuickFlow.Tests.dll (net8.0)
```

**Swagger export:** PASS → `docs/api/swagger.json` (18 paths, 29 operations; all 10 learning-card operations listed)
**Unit tests:** 38 passed, 0 failed
**Result:** PASS 9/9 (39 assertions + 12 smoke assertions, 0 failures)

## Unit tests (optional)
- Command: `dotnet test backend/QuickFlow.sln`
- Result: Passed 38, Failed 0 (+11 LearningCardTests)

## Failure record
—

## Notes
- Milestone PATCH is a partial update: omitted fields stay unchanged. `clearTargetDate: true` removes the target date, because `null` means "unchanged".
- Card responses always embed milestones (creation order) and notes (newest first), so the card grid can expand without extra calls.
- Completing a milestone of a `NotStarted` card moves the card to `InProgress`. The card is never auto-completed; the user sets `Completed` via PUT (or through a plan item, phase 05).
