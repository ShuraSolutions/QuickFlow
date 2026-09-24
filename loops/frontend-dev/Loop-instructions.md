# frontend-dev — Loop Instructions

A resumable, phase-by-phase loop that turns a requirements file (a full PRD
**or** a single user story) plus a backend Swagger/OpenAPI file into a
working frontend. The frontend is served at a local URL, and every phase is
verified in a real browser through the Playwright MCP server.

This file is project-agnostic. Every stack choice, folder, port, URL and
command comes from `loops/config.md` (referred to below as `config.<key>`).
Never hard-code project values here or in `task.md`.

## Files this loop owns

| Path (relative to `loops/frontend-dev/`) | Purpose |
|---|---|
| `task.md` | Inputs, outputs, acceptance criteria (read-only contract) |
| `progress.md` | Human-readable milestone log (times, tokens, status, attempts, actions) |
| `state/state.json` | Machine state used to stop and resume |
| `outputs/phase-NN-<slug>.md` | One file per phase: tasks as checkboxes + Playwright verification record |
| `outputs/screenshots/` | Screenshots from verification (`config.verification.playwright.screenshot_dir`) |

Code goes only into `config.frontend.folder`. This loop **reads** the Swagger
file and may start or stop the backend for verification, but never edits
backend code or the Swagger file.

## Invocation

```
/frontend-loop <requirements-path> [swagger-path] [--retry-failed]
```

- `<requirements-path>`: a PRD or a single user story. If empty, use `config.project.default_requirements`.
- `[swagger-path]`: the OpenAPI JSON. If omitted, use `config.backend.swagger_export_path`.
- `--retry-failed`: resets a phase in `failed` state to `pending` with `attempts: 0` (logged in progress.md).

---

## Algorithm

"Persist" means: write `state/state.json` (with `updated_at`) **and** the
relevant progress.md block before doing anything else.

### Step 0 — LOAD
1. Read `loops/config.md`, this file, `task.md`, `state/state.json`,
   `loops/_shared/phase-template.md` and `loops/_shared/progress-entry-template.md`.
2. **Swagger gate:** the Swagger path must exist and parse as JSON with a
   `paths` object. If not, report "Swagger missing or invalid, run /backend-loop first" and **Stop**
   without changing state.
3. Compute SHA-256 of the requirements file and the Swagger file.
4. Choose the **run mode**:

| Condition | Mode | Next step |
|---|---|---|
| `status == "halted"` and no `--retry-failed` | halted | Report the failed phase, its `last_error`/`failure_kind`, how to retry. **Stop.** |
| `status == "halted"` and `--retry-failed` | resume | Reset that phase (`pending`, attempts 0, keep history), `status: running`. Step 3. |
| `phases` is empty | fresh | Step 1 |
| inputs (requirements path+hash, Swagger hash) unchanged, some phase not `completed` | resume | Step 3 (see *Resume rules*) |
| inputs unchanged, all phases `completed` | done | Report "nothing to do". **Stop.** |
| new requirements path/hash **or** changed Swagger hash | append | Step 1 (delta planning) |

### Step 1 — INPUT
1. Read the requirements file in full (and `config.project.reference_docs` for context).
2. Classify it as **full** (several features/stories) or **story** (one user story).
3. Read the Swagger file. Build an **API map**: for each path and method, its
   parameters, request schema, response schema and status codes.
4. Extract the UI scope: pages/screens, navigation, components, forms and
   their validation, lists with search/filter/sort, empty states,
   notifications, live or time-based displays, and the user flows from
   end-to-end test requirements.
5. Map every UI feature to the API operations it needs. If a feature needs
   an operation that is **not** in the Swagger file, record it as a
   **backend gap** in the planning milestone. Do not invent endpoints.
6. In **append** mode keep only new or changed scope. A changed Swagger file
   may require updating the API client and the affected pages. If nothing in
   the frontend needs to change, record that and go to Step 8.

### Step 2 — PLAN
1. Log the start of the **planning** milestone in progress.md (timestamp + token snapshot).
2. Split the scope into ordered phases. Rules:
   - If `config.frontend.entry_page` does not exist, the first phase is
     **app shell**: the entry page, persistent navigation with every page from the
     requirements, client-side routing, base styles and layout,
     `config.frontend.api_config_file` pointing at `config.backend.urls.base`,
     a shared API client generated from the API map, error/loading/empty-state
     helpers, and vendored libraries from `config.frontend.vendor`.
   - Then usually one phase per page/feature area, in dependency order.
     Pages that combine data from other features (dashboards, summaries,
     builders that select existing items) come after the features they use.
   - Each phase must be demoable and verifiable on its own in the browser.
   - A phase that depends on a backend gap is still planned, but marked
     `blocked_by: ["<gap>"]` in state.
3. For each phase create `outputs/phase-NN-<slug>.md` from the phase
   template. Tasks are T1..Tn. Acceptance checks C1..Cn are **browser-observable**
   ("after submitting the form the new row appears in the list", "empty state
   shows an Add button"). Tag at least one check per phase `[smoke]`.
4. Add the phases to `state.json.phases` (all `pending`), record inputs
   (`requirements_inputs`, `swagger_input` with sha256), `status: running`.
5. Close the planning milestone (end time, tokens) and **persist**.

### Step 3 — START PHASE
1. Pick the first phase with status `pending`/`in_progress` whose `depends_on` are all `completed`. If none: Step 8.
2. If it has unresolved `blocked_by` gaps (still missing in the current
   Swagger): set it `failed` with `failure_kind: "blocked-by-backend"`, fill the
   failure record with the missing operations and a suggested
   `/backend-loop <story>` input, set loop `status: halted`, run Step 7, and
   **Stop**. Retries cannot fix a missing endpoint, so no attempts are used.
3. If `pending`: `in_progress`, `attempts += 1`, `started_at` (first attempt only),
   token snapshot into `token_snapshot_start`, set `current_phase`/`current_attempt`.
4. Create or update the milestone block in progress.md. **Persist.**

### Step 4 — IMPLEMENT
1. Implement only this phase's unchecked tasks, following `config.frontend.stack`
   and `config.frontend.structure_notes`. Do not use frameworks, bundlers or a build step
   unless `config.frontend.stack` allows them.
2. All HTTP calls go through the shared API client, and must match the API map
   exactly (paths, methods, casing, enum values, date formats).
3. Validate forms client-side with the same rules the requirements state
   (the server stays authoritative; show server ProblemDetails messages to the user).
4. Every list has an empty state that leads the user to the relevant "add" action.
5. Mark each finished task `[x]` in the phase file. If the phase fails,
   uncheck any task its failing checks disprove.

### Step 5 — VERIFY with Playwright MCP (never skipped)
Record everything in the phase file under `## Verification → ### Attempt N` (format below).

1. **Backend up:** request `config.backend.urls.health`. If it is not 200,
   start the backend with `config.backend.commands.run` in the background, poll health
   up to `config.backend.startup_timeout_seconds`, and remember that this loop started it.
   If it will not start: FAIL the attempt with `failure_kind: "backend-unavailable"`.
2. **Static server up:** run `config.frontend.commands.stop`, then
   `config.frontend.commands.serve` in the background. Poll `config.frontend.url`
   until 200 or `config.frontend.startup_timeout_seconds` elapses.
3. **Browser checks** using the Playwright MCP server named
   `config.verification.playwright.mcp_server` (tools `browser_navigate`,
   `browser_snapshot`, `browser_click`, `browser_type`, `browser_fill_form`,
   `browser_select_option`, `browser_press_key`, `browser_wait_for`,
   `browser_evaluate`, `browser_handle_dialog`, `browser_console_messages`,
   `browser_network_requests`, `browser_take_screenshot`, `browser_resize`):
   - Set the viewport to `config.verification.playwright.viewport` and navigate to `config.frontend.url` (plus the route).
   - For each acceptance check: perform the user steps, then assert on the
     accessibility snapshot or on `browser_evaluate` results. Do not trust a
     screenshot alone for assertions.
   - Take at least one screenshot per check (for multi-step flows: the key
     states), named `phase-NN-C<k>-<slug>.png`, saved to
     `config.verification.playwright.screenshot_dir`. Confirm the file exists,
     and move it there if the MCP server saved it somewhere else.
   - Time-based behavior (countdowns, scheduled notifications, live
     indicators): create data relative to the current time, observe the value
     twice with a wait in between, and record both readings.
   - Test data: create it through the UI when that is the feature under test.
     Otherwise it may be seeded through the API with curl. Note seeded data in the record.
4. **Console & network:** collect `browser_console_messages` and
   `browser_network_requests`. With `config.verification.playwright.fail_on_console_errors`,
   any console error or failed (4xx/5xx not expected by a check) request fails the attempt.
5. **Smoke regression:** re-run the `[smoke]` checks of every previously completed phase.
6. **Unit tests (optional):** run any frontend tests that exist, or that `config.verification.unit_tests_required` demands, and record the result.
7. **Teardown:** `browser_close`; `config.frontend.commands.stop`; stop the
   backend with `config.backend.commands.stop` **only if** this loop started it.
8. Mark passing checks `[x]`; write `**Result:** PASS n/n` or `FAIL k/n`.

If a failure is caused by the **backend** (wrong status, response not matching
Swagger, CORS rejection) and not by frontend code, confirm it with a direct
curl call. Then fail the phase immediately with `failure_kind: "backend-defect"`,
record the curl evidence, halt, and suggest a `/backend-loop` story. Do not use more attempts.

### Step 6 — DECIDE
- **All checks pass** → `completed`, `ended_at`, `last_error: null`. Step 7, then Step 3.
- **Fail and `attempts < config.verification.max_attempts`** → record
  `last_error` (phase and top level) and the attempt row in progress.md, **persist**, fix, go to
  Step 4 with `attempts += 1` (new `### Attempt N` section; never delete earlier ones).
- **Fail and `attempts == max_attempts`** → phase `failed` (`failure_kind: "verification"`),
  loop `status: halted`, fill `## Failure record` (failed checks, last output, what each
  attempt tried, screenshots, suggested next step). Step 7, then **Stop**.

### Step 7 — RECORD (after every phase, success or failure)
1. Token snapshot → `token_snapshot_end`; `tokens_used = end.total - start.total` and `tokens_new_work = Δinput + Δcache_creation + Δoutput`
   (log both in progress.md, see the progress-entry template)
   (`tokens_method: "estimated"` if the command fails).
2. Finalize the milestone block in progress.md (end, duration, tokens with
   breakdown, status, attempts, action items done, verification summary with
   screenshot links, attempt history), and update the summary table.
3. Update the phase file header table. **Persist.**

### Step 8 — FINISH
If no runnable phase is left and none failed: `status: completed`,
`current_phase: null`. Write a "Run summary" in progress.md (phases done,
total tokens, how to serve: `config.frontend.commands.serve` → `config.frontend.url`).
Report to the user: phases, checks passed, screenshot folder, and the URL.

---

## Resume rules
- `in_progress` phase: **do not** increment attempts. If all tasks are `[x]`, go to Step 5. Otherwise continue Step 4.
- `completed` phases are never re-implemented. They are only re-checked through `[smoke]` checks.
- If state.json and the phase files disagree, the phase files' verification records win. Fix state and log the correction.

## Verification record format

````markdown
### Attempt 1 — 2026-01-01T10:00:00+00:00
**Backend:** already running (health 200) | started by loop
**Static server:** PASS (`<serve command>` → 200 in 2 s)

#### C2 Add item shows it in the list
| # | Action (MCP tool) | Target / input | Observation |
|---|---|---|---|
| 1 | browser_navigate | `<frontend.url>/#/items` | page title "Items", empty state visible |
| 2 | browser_click | "Add item" button | dialog opened |
| 3 | browser_fill_form | Title = "Buy milk" | — |
| 4 | browser_click | "Save" | POST /api/items → 201 |
| 5 | browser_snapshot | list | row "Buy milk" present |

Expected: new row "Buy milk" visible, empty state hidden
Actual: row present (snapshot ref e42), empty state absent
Screenshot: ![C2](screenshots/phase-02-C2-add-item.png)
Result: **PASS**

#### Console & network
- Console errors: 0
- Failed requests: 0 (1 expected 400 in C4)

#### Smoke regression
| Check | Result |
|---|---|
| phase-01 C1 | PASS |

**Unit tests:** not present (optional)
**Result:** PASS 6/6
````

Never invent observations: every row reflects an actual MCP tool call and its result.

## state.json schema

```json
{
  "schema_version": 1,
  "loop": "frontend-dev",
  "status": "idle | running | completed | halted",
  "mode": "fresh | resume | append | null",
  "requirements_inputs": [
    { "path": "docs/x.md", "sha256": "…", "kind": "full | story", "added_at": "ISO" }
  ],
  "swagger_input": { "path": "docs/api/swagger.json", "sha256": "…", "operation_count": 0 },
  "backend_gaps": [],
  "current_phase": null,
  "current_attempt": 0,
  "max_attempts": 3,
  "phases": [
    {
      "id": "phase-01-app-shell",
      "title": "App shell & navigation",
      "depends_on": [],
      "blocked_by": [],
      "status": "pending | in_progress | completed | failed",
      "failure_kind": "verification | blocked-by-backend | backend-defect | backend-unavailable | null",
      "attempts": 0,
      "output_file": "outputs/phase-01-app-shell.md",
      "requirements_covered": [],
      "screenshots": [],
      "started_at": null,
      "ended_at": null,
      "token_snapshot_start": null,
      "token_snapshot_end": null,
      "tokens_used": null,
      "tokens_new_work": null,
      "tokens_method": "measured | estimated | null",
      "last_error": null
    }
  ],
  "artifacts": { "frontend_url": null, "entry_page": null },
  "backend_started_by_loop": false,
  "last_error": null,
  "updated_at": null
}
```

## Hard rules
- Verification through Playwright MCP is never skipped, shortened, or simulated. A check without a real tool observation counts as FAIL.
- At most `config.verification.max_attempts` attempts per phase. After that, record the failure and stop.
- progress.md and state.json are updated after **every** phase and every failed attempt.
- Tokens are logged for every milestone (planning and each phase).
- Never modify backend code or the Swagger file. Report backend problems instead.
- Always stop the static server (and the backend if this loop started it) before ending, including on failure.
