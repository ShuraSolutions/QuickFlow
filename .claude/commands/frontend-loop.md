---
description: Run the frontend-dev loop on a PRD or user story plus a Swagger file (plan → implement → Playwright-verify, phase by phase)
argument-hint: <requirements-path> [swagger-path] [--retry-failed]
---

Run the **frontend-dev** loop.

Arguments: `$ARGUMENTS`

1. Parse the arguments:
   - The first argument is the requirements file path (a full PRD or a single user story).
     It may be quoted if it contains spaces. If it is missing, use
     `project.default_requirements` from `loops/config.md`.
   - The second argument (optional, not starting with `--`) is the Swagger/OpenAPI
     JSON path. If it is missing, use `backend.swagger_export_path` from `loops/config.md`.
   - `--retry-failed`, if present, allows resetting a failed phase (see Step 0 of the loop instructions).
   - If the requirements file does not exist, report that and stop without touching any state.
2. Make sure the Playwright MCP server named in
   `verification.playwright.mcp_server` is available (its `browser_*` tools).
   If it is not, stop and tell the user to enable it (`.mcp.json` at the repo
   root, then restart Claude Code / approve the server). Never replace it with
   unverified manual checks.
3. Read `loops/config.md`, then read and follow
   `loops/frontend-dev/Loop-instructions.md` exactly, from Step 0, with those
   paths. `loops/frontend-dev/task.md` is the acceptance contract.
4. Keep going phase after phase until the loop reaches Step 8 (completed) or
   stops (halted / nothing to do). Do not ask for confirmation between phases
   unless a decision is truly ambiguous in the requirements.
5. When done, report: loop status, phases completed/failed, checks passed,
   total tokens from `loops/frontend-dev/progress.md`, the screenshot folder,
   and the frontend URL with the command that serves it.
