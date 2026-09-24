# CLAUDE.md — Global Rules

This repository is built by **Claude Loops**: two reusable, resumable
development loops that can implement any application from a full PRD or a
single user story.

## Layout
| Path | What it is |
|---|---|
| `docs/` | Inputs: requirements (`Task_PRD.md`), assignment brief (`task-description.txt`), exported API spec (`docs/api/`) |
| `loops/config.md` | **The only project-specific file in `loops/`**: stack, folders, ports, URLs, commands |
| `loops/backend-dev/` | Loop 1: requirements → backend code + Swagger, verified with curl |
| `loops/frontend-dev/` | Loop 2: requirements + Swagger → frontend code, verified with Playwright MCP |
| `loops/_shared/` | Phase/progress templates and `token-usage.ps1` |
| `.claude/commands/` | `/backend-loop`, `/frontend-loop`, `/orchestrate` |
| `backend/`, `frontend/` | Generated code (each written only by its own loop) |

## How to run
- `/backend-loop <requirements-path>`: backend loop only.
- `/frontend-loop <requirements-path> [swagger-path]`: frontend loop only.
- `/orchestrate <requirements-path>`: backend loop, then frontend loop with the generated Swagger.
  It is a slash command, not a third loop.
- `<requirements-path>` can be the full PRD or a file holding a single user story.
  A new input is appended as new phases, and an interrupted run resumes from `state/state.json`.

## Global rules (apply to every loop run)
1. **Always update progress and state after every phase**, and after every failed attempt:
   the loop's `progress.md` milestone block **and** `state/state.json`. Do this before starting the next phase or stopping.
2. **Never skip verification.** Every phase is verified with real commands: curl for backend-dev,
   Playwright MCP for frontend-dev. Paste the real outputs into the phase's output file.
   Never mark a task or check `[x]` without evidence, and never fabricate or paraphrase results.
3. **Log tokens used per milestone** (planning and every phase). Take a reading with
   `loops/_shared/token-usage.ps1` at milestone start and end, and log the difference with its breakdown.
   If the script fails, log an estimate and label it `estimated`.
4. **Stop condition:** a phase ends when it is verified successfully, or after
   `verification.max_attempts` (3) failed attempts. After the last failure, record the failure
   (phase file failure record, progress.md, state.json `last_error`), set the loop to `halted`, and **stop**.
   Do not continue to later phases.
5. **Config, not code:** read every stack, folder, port, URL and command from `loops/config.md`.
   Never put project-specific values into `Loop-instructions.md`, `task.md`, templates or slash commands.
6. **Ownership:** backend-dev writes only `backend/`, its loop folder and the Swagger export path.
   frontend-dev writes only `frontend/` and its loop folder. frontend-dev reports backend gaps or defects
   and never fixes them itself.
7. **One phase at a time:** implement only the current phase's tasks. Completed phases are not
   re-implemented, only regression-checked through their `[smoke]` checks.
8. **Clean up processes:** stop any server a loop started (API, static server, browser) before
   the loop ends, including on failure.

## Platform notes
- Windows host. In PowerShell, use `curl.exe`, because `curl` is an alias of Invoke-WebRequest.
  The Bash tool (Git Bash) is also available.
- Long-running servers must run in the background, and their readiness is checked by polling a URL.
