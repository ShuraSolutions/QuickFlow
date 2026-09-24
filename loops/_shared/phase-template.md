# {{PHASE_ID}} — {{PHASE_TITLE}}

| Field | Value |
|---|---|
| Loop | {{LOOP_NAME}} |
| Status | pending <!-- pending \| in_progress \| completed \| failed --> |
| Attempts | 0 / {{MAX_ATTEMPTS}} |
| Depends on | {{DEPENDS_ON or "—"}} |
| Requirements covered | {{e.g. US Tasks#1-3, FR-01, BR-1..5}} |
| Requirements source | {{REQUIREMENTS_PATH}} |
| Started | — |
| Ended | — |

## Goal
{{One or two sentences: what exists and works when this phase is done.}}

## Tasks
<!-- Mark [x] only when the task is implemented AND covered by a passing check. -->
- [ ] T1 {{task}}
- [ ] T2 {{task}}

## Acceptance checks
<!-- Each check is concrete and observable. Tag one or more checks [smoke]; they are re-run as regression in later phases. -->
- [ ] C1 [smoke] {{check}}
- [ ] C2 {{check}}

## Verification

### Attempt 1 — {{timestamp}}
<!-- Loop-specific format: see the loop's Loop-instructions.md, section "Verification record format". -->

**Result:** {{PASS n/n | FAIL n/n}}

## Unit tests (optional)
- Command: —
- Result: —

## Failure record
<!-- Filled only if the phase ends as failed after max attempts. -->
—

## Notes
—
