# frontend-dev — Task Contract

## Purpose
Plan, implement and verify the frontend of any application from its
requirements and its backend Swagger/OpenAPI contract, one phase at a time.
Each phase is proven in a real browser via the Playwright MCP server against
the locally served frontend.

## Inputs
| Input | Required | Source |
|---|---|---|
| Requirements file path (full PRD or a single user story) | yes | `$ARGUMENTS` (1st), else `config.project.default_requirements` |
| Swagger / OpenAPI JSON path | yes | `$ARGUMENTS` (2nd), else `config.backend.swagger_export_path` |
| Project configuration | yes | `loops/config.md` (`project`, `shell`, `frontend`, `backend.urls`/`commands` for running the API, `verification`) |
| Running backend (or the ability to start it) | yes, for verification | `config.backend.commands.run` / `config.backend.urls.health` |
| Existing loop state | no | `state/state.json` (for resume/append) |

## Outputs
| Output | Location |
|---|---|
| One phase file per phase, tasks as checkboxes marked `[x]` when achieved, plus the Playwright verification record | `loops/frontend-dev/outputs/phase-NN-<slug>.md` |
| Screenshots from verification | `config.verification.playwright.screenshot_dir` |
| Frontend source code (and optional unit tests) | `config.frontend.folder` |
| Local URL where the app is served | `config.frontend.url` via `config.frontend.commands.serve` |
| Milestone log (start, end, tokens, status, attempts, action items) | `loops/frontend-dev/progress.md` |
| Resumable state | `loops/frontend-dev/state/state.json` |

## Acceptance criteria (for the loop run)
1. Every UI-relevant requirement in the input maps to at least one phase, or is recorded as a backend gap.
2. Each phase is `completed` with `**Result:** PASS` in its latest attempt, **or** the loop halted with a filled failure record (`failure_kind` set).
3. Each completed phase has Playwright MCP checks (actions, observations, screenshots) against `config.frontend.url` with the real backend, covering its acceptance checks.
4. No unexpected console errors or failed network requests during verification.
5. Every API call used by the frontend exists in the Swagger file with matching method, path and payload shape.
6. The app is plain static files in `config.frontend.folder`, served by `config.frontend.commands.serve`, with no build step and only the libraries allowed by `config.frontend.stack`.
7. Every list page supports add and remove from that page and shows a guiding empty state.
8. progress.md has a finished block with tokens for the planning milestone and every phase, and state.json matches the phase files.
9. Optional: frontend unit tests pass if present.

## Out of scope
- Changing backend code or the Swagger file (report gaps/defects instead).
- Deployment beyond the local static server.
