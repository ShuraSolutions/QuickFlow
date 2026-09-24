# phase-02-tasks — Tasks resource

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | pending |
| Attempts | 0 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | US Tasks 1–7, FR-01, FR-02, BR-1..5, BR-14, §12 Tasks, §11 unit tests (task validation) |
| Requirements source | docs/Task_PRD.md |
| Started | — |
| Ended | — |

## Goal
Full task management over HTTP: create, read, update, complete, archive, restore, delete, with free-text search, status/priority/due-date filters, overdue detection and sorting. All rules are enforced in the domain project.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 Domain: `TaskItem` entity (Id, Title, Description, Status, Priority, DueDate (date), CreatedAt, UpdatedAt, CompletedAt, IsArchived) + `TaskItemStatus` (Todo, InProgress, Done), `TaskPriority` (Low, Medium, High)
- [ ] T2 Domain rules: title required, trimmed, ≤200 (BR-1); description optional ≤2000 (BR-2); status/priority must be defined enum values (BR-3); `Complete()` sets status Done + CompletedAt (BR-5); `IsOverdue(today)` = DueDate < today && status != Done && !archived; archive/restore
- [ ] T3 EF mapping + migration `phase-02-tasks` (indexes on Status, Priority, DueDate, IsArchived)
- [ ] T4 DTOs (`TaskResponse` with computed `isOverdue`, `CreateTaskRequest`, `UpdateTaskRequest`) and query model (`search`, `status`, `priority`, `dueDate`, `dueFrom`, `dueTo`, `overdue`, `archived` = exclude|include|only, `sortBy` = createdAt|dueDate, `sortDir` = asc|desc)
- [ ] T5 `TasksController`: `GET /api/tasks`, `GET /api/tasks/{id}`, `POST /api/tasks`, `PUT /api/tasks/{id}`, `POST /api/tasks/{id}/complete`, `POST /api/tasks/{id}/archive`, `POST /api/tasks/{id}/restore`, `DELETE /api/tasks/{id}`
- [ ] T6 Default list excludes archived tasks (BR-4); deleted tasks return 404 (BR-14)
- [ ] T7 Unit tests for task rules (title/description limits, complete, overdue)

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] `POST /api/tasks` valid body → `201`, `Location` header, body has `id`, `status == "Todo"` (default), `priority`, `createdAt`, `isArchived == false`
- [ ] C2 [smoke] `GET /api/tasks/{id}` → `200` with the same task
- [ ] C3 `POST /api/tasks` with empty/whitespace title → `400` ProblemDetails with `errors.Title`
- [ ] C4 `POST /api/tasks` with 201-char title → `400`; 200-char title → `201`
- [ ] C5 `POST /api/tasks` with 2001-char description → `400` with `errors.Description`
- [ ] C6 `POST /api/tasks` with `status: "Blocked"` → `400`; with `priority: 5` (integer) → `400`
- [ ] C7 `PUT /api/tasks/{id}` changes title/status/priority/dueDate → `200`, `updatedAt` > `createdAt`
- [ ] C8 `POST /api/tasks/{id}/complete` → `200`, `status == "Done"`, `completedAt` set
- [ ] C9 `POST /api/tasks/{id}/archive` → `200` `isArchived == true`; task absent from default `GET /api/tasks`; present with `archived=only`; `POST /restore` → back in default list
- [ ] C10 `DELETE /api/tasks/{id}` → `204`; then `GET` → `404` ProblemDetails; `PUT`/`DELETE` unknown id → `404`
- [ ] C11 `GET /api/tasks?search=<word>` returns only tasks whose title contains the word (case-insensitive)
- [ ] C12 `GET /api/tasks?status=Done&priority=High` returns only matching tasks; `?status=Bogus` → `400`
- [ ] C13 `GET /api/tasks?overdue=true` returns only tasks with past due date and not Done (`isOverdue == true`); `?dueDate=<date>` returns tasks due that date
- [ ] C14 `GET /api/tasks?sortBy=dueDate&sortDir=asc` ordered by due date ascending; `sortBy=createdAt&sortDir=desc` newest first
- [ ] C15 Data persists across API restart (task created before restart still returned after)

## Verification

_Not run yet._

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
—

## Notes
—
