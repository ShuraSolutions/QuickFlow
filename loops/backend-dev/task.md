# backend-dev — Task Contract

## Purpose
Plan, implement and verify the backend of any application from its
requirements, one phase at a time, and publish the API contract as a
Swagger/OpenAPI file that the frontend-dev loop (or anyone else) can use.

## Inputs
| Input | Required | Source |
|---|---|---|
| Requirements file path (full PRD or a single user story) | yes | `$ARGUMENTS`, else `config.project.default_requirements` |
| Project configuration | yes | `loops/config.md` (`project`, `shell`, `backend`, `verification`) |
| Existing loop state | no | `state/state.json` (for resume/append) |
| Reference docs | no | `config.project.reference_docs` (context only) |

## Outputs
| Output | Location |
|---|---|
| One phase file per phase, tasks as checkboxes marked `[x]` when achieved, plus the curl verification record | `loops/backend-dev/outputs/phase-NN-<slug>.md` |
| Backend source code (and optional unit tests) | `config.backend.folder` |
| Swagger / OpenAPI JSON, exported after every phase | `config.backend.swagger_export_path` |
| Milestone log (start, end, tokens, status, attempts, action items) | `loops/backend-dev/progress.md` |
| Resumable state | `loops/backend-dev/state/state.json` |

## Acceptance criteria (for the loop run)
1. Every backend-relevant requirement in the input maps to at least one phase ("Requirements covered").
2. Each phase is `completed` with `**Result:** PASS` in its latest attempt, **or** the loop halted after `max_attempts` with a filled failure record.
3. Each completed phase has real curl commands with their pasted outputs covering:
   happy path, validation errors (4xx), not found (404), and in-scope filtering, search, sorting and computed values.
4. All business rules in scope are enforced server-side and shown by at least one curl check each.
5. `config.backend.commands.build` succeeds with no errors.
6. The Swagger file exists, is valid JSON, and lists every implemented endpoint with its request/response schemas.
7. CORS allows every origin in `config.backend.cors.allowed_origins`.
8. Data persists across API restarts (database from `config.backend.stack.database`).
9. progress.md has a finished block with tokens for the planning milestone and every phase, and state.json matches the phase files.
10. Optional: unit tests for domain rules pass with `config.backend.commands.test`.

## Out of scope
- Any frontend code or files outside `config.backend.folder`, this loop folder, and the Swagger export path.
- Authentication, unless the requirements ask for it.
