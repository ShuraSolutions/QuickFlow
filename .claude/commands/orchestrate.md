---
description: Run backend-dev, then hand its Swagger file to frontend-dev, for a PRD or a single user story
argument-hint: <requirements-path>
---

Orchestrate the two development loops for one requirements input.

Arguments: `$ARGUMENTS`

The orchestrator is **not** a loop. It has no state, no progress log and no
outputs of its own. It only runs the two loops in order and passes the
Swagger file between them. All state lives in each loop's `state/state.json`.

1. **Resolve the input.** The first argument is the requirements file path (a
   full PRD or a single user story, possibly quoted). If it is missing, use
   `project.default_requirements` from `loops/config.md`. If the file does not
   exist, report that and stop.

2. **Run backend-dev.** Read `loops/config.md`, then follow
   `loops/backend-dev/Loop-instructions.md` from Step 0 with the requirements
   path, exactly as `/backend-loop` would, until that loop completes or stops.

3. **Gate.** Read `loops/backend-dev/state/state.json`:
   - `status` must be `completed`, and
   - `artifacts.swagger_file` must point to an existing file that parses as JSON with a `paths` object.

   If either condition fails, **stop**. Report the backend loop status, the failed
   phase and its `last_error`, and do not start the frontend loop.

4. **Run frontend-dev.** Follow `loops/frontend-dev/Loop-instructions.md` from
   Step 0 with the same requirements path and
   `swagger-path = artifacts.swagger_file` from the backend state, exactly as
   `/frontend-loop <requirements-path> <swagger-path>` would. Check the Playwright
   MCP server first, as `/frontend-loop` does.

5. **Report** one combined summary:
   - For each loop: status, phases completed/failed, checks passed, total tokens (from its progress.md).
   - The Swagger file path, the backend URL, and the frontend URL with the commands that run them.
   - Any backend gaps/defects the frontend loop recorded, with the suggested `/backend-loop` follow-up.
