# backend-dev — Loop Instructions

A resumable, phase-by-phase loop that turns a requirements file (a full PRD
**or** a single user story) into a working backend API, a Swagger/OpenAPI
file, and a verified record of every phase.

This file is project-agnostic. Every stack choice, folder, port, URL and
command comes from `loops/config.md` (referred to below as `config.<key>`).
Never hard-code project values here or in `task.md`.

## Files this loop owns

| Path (relative to `loops/backend-dev/`) | Purpose |
|---|---|
| `task.md` | Inputs, outputs, acceptance criteria (read-only contract) |
| `progress.md` | Human-readable milestone log (times, tokens, status, attempts, actions) |
| `state/state.json` | Machine state used to stop and resume |
| `outputs/phase-NN-<slug>.md` | One file per phase: tasks as checkboxes + verification record |

Code goes only into `config.backend.folder`. The Swagger file goes only to
`config.backend.swagger_export_path`. This loop never writes to the frontend folder.

## Invocation

```
/backend-loop <requirements-path> [--retry-failed]
```

- `<requirements-path>`: a PRD or a single user story (markdown/text). If empty,
  use `config.project.default_requirements`.
- `--retry-failed`: resets a phase in `failed` state to `pending` with
  `attempts: 0` (logged in progress.md) so a halted loop can continue.

---

## Algorithm

Run the steps in order. Anything that says "persist" means: write
`state/state.json` (with `updated_at`) **and** the relevant progress.md block
before doing anything else.

### Step 0 — LOAD
1. Read `loops/config.md`, this file, `task.md`, `state/state.json`, and
   `loops/_shared/phase-template.md` + `loops/_shared/progress-entry-template.md`.
2. Compute the input's SHA-256 (`Get-FileHash -Algorithm SHA256 <path>`).
3. Choose the **run mode**:

| Condition | Mode | Next step |
|---|---|---|
| `status == "halted"` and no `--retry-failed` | halted | Report the failed phase, its `last_error`, and how to retry. **Stop.** |
| `status == "halted"` and `--retry-failed` | resume | Reset that phase (`pending`, attempts 0, keep history), set `status: running`. Go to Step 3. |
| `phases` is empty | fresh | Step 1 |
| input path+hash already in `requirements_inputs` and some phase is not `completed` | resume | Step 3 (see *Resume rules*) |
| input path+hash already in `requirements_inputs` and all phases `completed` | done | Re-export Swagger if missing, report "nothing to do". **Stop.** |
| input path or hash not in `requirements_inputs` | append | Step 1 (delta planning) |

### Step 1 — INPUT
1. Read the requirements file in full. Also skim `config.project.reference_docs`
   for context (they are not requirements).
2. Classify it:
   - **full** — several features/user stories/functional requirements.
   - **story** — one user story (optionally with acceptance criteria).
3. Extract the backend-relevant scope: entities and fields, relationships,
   constraints, business rules, operations (CRUD, filters, search, sorting,
   computed/aggregated views), non-functional requirements, and test
   requirements. Ignore purely presentational requirements, but keep any
   data they imply (e.g. a dashboard implies an aggregate endpoint).
4. In **append** mode, compare with the existing phases and code: keep only
   what is new or changed. If the story needs no backend change, record that
   decision (planning milestone in progress.md) and finish at Step 8 with zero new phases.

### Step 2 — PLAN
1. Log the start of the **planning** milestone in progress.md (timestamp + token snapshot).
2. Split the scope into ordered phases. Rules:
   - If `config.backend.folder` has no solution yet, the first phase is
     **scaffold & infrastructure**: solution + projects from `config.backend.projects`,
     database wiring, migrations at startup, Swagger, CORS for
     `config.backend.cors.allowed_origins`, ProblemDetails errors, a health
     endpoint at `config.backend.urls.health`.
   - Then usually one phase per aggregate/resource (entity + its rules + its endpoints).
   - Cross-resource features (references between resources, aggregates,
     dashboards, reports) come after the resources they depend on.
   - Keep phases small: one resource or about 10 endpoints at most. Each phase must be verifiable on its own through HTTP.
   - Every requirement in scope maps to at least one phase (record it in "Requirements covered").
3. For each phase create `outputs/phase-NN-<slug>.md` from the phase template.
   Continue numbering after existing phases in append mode.
   Fill: goal, tasks (T1..Tn), acceptance checks (C1..Cn) with the
   expected status codes/fields. Tag at least one check per phase `[smoke]`.
4. Add the phases to `state.json.phases` (all `pending`), add the input to
   `requirements_inputs` (path, sha256, kind, added_at), set `status: running`.
5. Close the planning milestone (end time, tokens) and **persist**.

### Step 3 — START PHASE
1. Pick the first phase whose status is `pending` or `in_progress` and whose
   `depends_on` phases are all `completed`. If none: go to Step 8.
2. If the phase is `pending`: set `in_progress`, `attempts += 1`,
   `started_at` (first attempt only), and take a token snapshot into
   `token_snapshot_start`. Set `current_phase`/`current_attempt`.
3. Create or update the milestone block in progress.md from the template. **Persist.**

### Step 4 — IMPLEMENT
1. Implement only this phase's unchecked tasks, following `config.backend.stack`
   and `config.backend.architecture_notes`.
2. Business rules go in the domain project, validated server-side, with a
   4xx ProblemDetails response when violated.
3. Schema changes: follow `config.backend.database_strategy`. When it uses
   migrations, create one per phase with `config.backend.commands.add_migration`
   (`<Name>` = the phase slug), running `config.backend.commands.install_tools` first if the tool is missing.
4. After each finished task, mark it `[x]` in the phase file. A task is
   finished when it is implemented and builds, but it is only final once
   Step 5 passes. If the phase fails, uncheck any task its failing checks disprove.

### Step 5 — VERIFY (never skipped)
Run all of the following and record every command **and its real output** in the
phase file under `## Verification → ### Attempt N` (format below).

1. **Build:** `config.backend.commands.build`. A failure ends the attempt (FAIL).
2. **Start the API** in the background with `config.backend.commands.run`.
   First run `config.backend.commands.stop` to free the port. Poll
   `config.backend.urls.health` until it returns 200 or
   `config.backend.startup_timeout_seconds` elapses (FAIL on timeout, attach the server log tail).
3. **Curl checks:** for every acceptance check of this phase run a real
   HTTP request with `config.shell.curl_binary -s -i --max-time <config.verification.curl_timeout_seconds>`.
   Cover for each resource: happy path (create/read/update/delete as applicable),
   validation errors (400 and rule messages), not found (404), and filtering,
   search, sorting and computed fields where in scope. For state-changing checks, use
   data created inside the same check sequence (capture ids from responses).
4. **Smoke regression:** re-run the `[smoke]` checks of every previously
   completed phase.
5. **Swagger export:** download `config.backend.urls.swagger_json` to
   `config.backend.swagger_export_path` (create folders). Check that it
   is valid JSON and lists every endpoint added in this phase. Record the endpoint count.
6. **Unit tests (optional):** if `config.verification.unit_tests_required`
   is true, or tests exist, run `config.backend.commands.test` and record the result.
   Failing existing tests fail the attempt.
7. **Stop the API** with `config.backend.commands.stop`.
8. Mark each passing acceptance check `[x]`. Write `**Result:** PASS n/n` or `FAIL k/n`.

### Step 6 — DECIDE
- **All checks pass** → phase `completed`, `ended_at`, `last_error: null`. Go to Step 7, then Step 3.
- **A check fails and `attempts < config.verification.max_attempts`** →
  save a short root-cause summary in the phase's `last_error` and top-level `last_error`,
  add the attempt row to progress.md, **persist**, then fix the cause and go to Step 4 with
  `attempts += 1` (a new `### Attempt N` section; never delete earlier attempts).
- **A check fails and `attempts == max_attempts`** → phase `failed`, loop
  `status: halted`. Fill the phase file's `## Failure record` (what failed,
  the last error output, what was tried in each attempt, a suggested next step).
  Go to Step 7, then **Stop**. Do not start later phases.

### Step 7 — RECORD (after every phase, success or failure)
1. Token snapshot → `token_snapshot_end`; `tokens_used = end.total - start.total` and `tokens_new_work = Δinput + Δcache_creation + Δoutput`
   (log both in progress.md, see the progress-entry template).
   If the command returns `ok: false`, estimate and set `tokens_method: "estimated"`.
2. Finalize the milestone block in progress.md: end time, duration, tokens
   (with breakdown), status, attempts, action items done (the `[x]` tasks),
   verification summary, attempt history.
3. Update the summary table at the top of progress.md.
4. Update the phase file's header table (status, attempts, started, ended).
5. Set `artifacts.swagger_file` and `artifacts.swagger_endpoint_count`. **Persist.**

### Step 8 — FINISH
When no runnable phase is left and none failed: `status: completed`,
`current_phase: null`. Make sure the Swagger file is up to date. Write a
final "Run summary" entry in progress.md (phases done, total tokens, Swagger
path). Report to the user: phases, checks passed, Swagger path, and how to run the API.

---

## Resume rules
- `in_progress` phase: **do not** increment attempts. Re-read the phase file.
  If all tasks are `[x]`, go to Step 5. Otherwise continue Step 4 with the unchecked tasks.
- A phase marked `completed` is never re-implemented. It is only re-checked through its `[smoke]` checks.
- If state.json and the phase files disagree, the phase files' verification
  records win. Fix state.json and note the correction in progress.md.

## Verification record format

````markdown
### Attempt 2 — 2026-01-01T10:00:00+00:00
**Build:** PASS (`<config.backend.commands.build>`, 0 warnings, 0 errors)
**API start:** PASS (health 200 after 6 s)

#### C1 [smoke] Create item returns 201 with id
```bash
curl.exe -s -i --max-time 15 -X POST http://localhost:PORT/api/items -H "Content-Type: application/json" -d '{"title":"Buy milk"}'
```
Expected: `201`, body has `id`, `title == "Buy milk"`
Actual:
```
HTTP/1.1 201 Created
Location: /api/items/1
{"id":1,"title":"Buy milk", ...}
```
Result: **PASS**

#### Smoke regression
| Check | Result |
|---|---|
| phase-01 C1 | PASS |

**Swagger export:** PASS → `<swagger_export_path>` (23 paths)
**Unit tests:** 12 passed, 0 failed (optional)
**Result:** PASS 9/9
````

Long bodies may be trimmed to the relevant fields, marked `…`. Never
invent or paraphrase output: paste what the command printed.

## state.json schema

```json
{
  "schema_version": 1,
  "loop": "backend-dev",
  "status": "idle | running | completed | halted",
  "mode": "fresh | resume | append | null",
  "requirements_inputs": [
    { "path": "docs/x.md", "sha256": "…", "kind": "full | story", "added_at": "ISO" }
  ],
  "current_phase": "phase-02-items | null",
  "current_attempt": 0,
  "max_attempts": 3,
  "phases": [
    {
      "id": "phase-01-scaffold",
      "title": "Scaffold & infrastructure",
      "depends_on": [],
      "status": "pending | in_progress | completed | failed",
      "attempts": 0,
      "output_file": "outputs/phase-01-scaffold.md",
      "requirements_covered": ["FR-01"],
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
  "artifacts": { "swagger_file": null, "swagger_endpoint_count": null },
  "last_error": null,
  "updated_at": null
}
```

## Hard rules
- Verification is never skipped, shortened, or simulated. A check without a real, pasted command output counts as FAIL.
- At most `config.verification.max_attempts` attempts per phase. After that, record the failure and stop.
- progress.md and state.json are updated after **every** phase and every failed attempt.
- Tokens are logged for every milestone (planning and each phase).
- Always stop the background API before ending the loop, including on failure.
