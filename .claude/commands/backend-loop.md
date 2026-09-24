---
description: Run the backend-dev loop on a PRD or a single user story (plan → implement → curl-verify → Swagger, phase by phase)
argument-hint: <requirements-path> [--retry-failed]
---

Run the **backend-dev** loop.

Arguments: `$ARGUMENTS`

1. Parse the arguments:
   - The first argument is the requirements file path (a full PRD or a single user story).
     It may be quoted if it contains spaces. If it is missing, use
     `project.default_requirements` from `loops/config.md`.
   - `--retry-failed`, if present, allows resetting a failed phase (see Step 0 of the loop instructions).
   - If the requirements file does not exist, report that and stop without touching any state.
2. Read `loops/config.md`, then read and follow
   `loops/backend-dev/Loop-instructions.md` exactly, from Step 0, with that
   requirements path. `loops/backend-dev/task.md` is the acceptance contract.
3. Keep going phase after phase until the loop reaches Step 8 (completed) or
   stops (halted / nothing to do). Do not ask for confirmation between phases
   unless a decision is truly ambiguous in the requirements.
4. When done, report: loop status, phases completed/failed, checks passed,
   total tokens from `loops/backend-dev/progress.md`, and the Swagger file path
   (`artifacts.swagger_file` in `loops/backend-dev/state/state.json`).
