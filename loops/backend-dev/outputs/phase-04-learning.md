# phase-04-learning — Learning cards, milestones & notes

| Field | Value |
|---|---|
| Loop | backend-dev |
| Status | pending |
| Attempts | 0 / 3 |
| Depends on | phase-01-scaffold |
| Requirements covered | US Learning 1–5, FR-05, FR-06, BR-8, BR-9, BR-14, §12 Learning Resources |
| Requirements source | docs/Task_PRD.md |
| Started | — |
| Ended | — |

## Goal
Learning cards can be added, updated and removed. Each card holds milestones (add, complete/uncomplete, remove) and free-form notes (add, remove). Card responses expose milestone progress.

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 Domain: `LearningCard` (Id, Title, Description, Status, CreatedAt) + `LearningStatus` (NotStarted, InProgress, Completed); `LearningMilestone` (Id, LearningCardId, Title, IsDone, TargetDate, CompletedAt); `LearningNote` (Id, LearningCardId, Text, CreatedAt)
- [ ] T2 Domain rules: card title required ≤200 (BR-8), description/source optional ≤2000; milestone title required ≤200; note text required ≤4000; a milestone/note belongs to exactly one card (BR-9, required FK); completing a milestone of a `NotStarted` card moves the card to `InProgress`
- [ ] T3 EF mapping (cascade delete of milestones/notes with the card) + migration `phase-04-learning`
- [ ] T4 `LearningCardsController`: `GET /api/learning-cards` (`?status=`), `GET /api/learning-cards/{id}` (includes milestones + notes), `POST`, `PUT /{id}`, `DELETE /{id}`
- [ ] T5 Milestones: `POST /api/learning-cards/{id}/milestones`, `PATCH /api/learning-cards/{id}/milestones/{milestoneId}` (title/isDone/targetDate), `DELETE /api/learning-cards/{id}/milestones/{milestoneId}`
- [ ] T6 Notes: `POST /api/learning-cards/{id}/notes`, `DELETE /api/learning-cards/{id}/notes/{noteId}`
- [ ] T7 Computed fields on card: `milestonesTotal`, `milestonesDone`, `milestoneProgressPercentage`, `notesCount`
- [ ] T8 Unit tests for learning rules and progress computation

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] `POST /api/learning-cards` `{title, description}` → `201`, `status == "NotStarted"`, `milestones == []`, `notes == []`
- [ ] C2 `POST /api/learning-cards` empty title → `400` `errors.Title`; `status:"Paused"` → `400`
- [ ] C3 `PUT /api/learning-cards/{id}` updates title/description/status → `200`
- [ ] C4 [smoke] `POST /api/learning-cards/{id}/milestones` `{title, targetDate}` → `201`, `isDone == false`, `learningCardId == id`; empty title → `400`
- [ ] C5 `PATCH .../milestones/{mid}` `{isDone:true}` → `200` `isDone == true`, `completedAt` set; card `milestonesDone == 1`, `milestoneProgressPercentage == 50` (with 2 milestones), card status moved to `InProgress`
- [ ] C6 `DELETE .../milestones/{mid}` → `204`; milestone of another card or unknown id → `404` (BR-9)
- [ ] C7 `POST .../notes` `{text}` → `201` with `createdAt`; empty text → `400`; `DELETE .../notes/{nid}` → `204`; unknown → `404`
- [ ] C8 `GET /api/learning-cards?status=InProgress` returns only in-progress cards
- [ ] C9 `DELETE /api/learning-cards/{id}` → `204`; `GET` → `404`; its milestone/note endpoints → `404`

## Verification

_Not run yet._

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
—

## Notes
—
