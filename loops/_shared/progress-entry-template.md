<!--
Template for ONE milestone block in a loop's progress.md.
A milestone is either the planning step ("planning") or one phase.
Create the block when the milestone starts; update it after every attempt;
finalize it when the milestone ends (completed or failed).
Tokens come from `shell.token_usage_command` in loops/config.md:
  tokens used = end.total - start.total (sum over attempts).
If the command returns ok=false, write your best estimate and mark it "estimated".
-->

### {{MILESTONE_ID}} — {{TITLE}}
- **Start:** {{ISO-8601 timestamp}}
- **End:** {{ISO-8601 timestamp | "running"}}
- **Duration:** {{hh:mm:ss}}
- **Tokens used:** {{total}} ({{measured | estimated}}) — input {{n}} · cache write {{n}} · cache read {{n}} · output {{n}}
- **Status:** {{in_progress | completed | failed}}
- **Attempts:** {{n}} / {{max}}
- **Output file:** [{{output file}}](outputs/{{output file}})
- **Action items done:**
  - [x] {{task id}} {{short description}}
- **Verification:** {{e.g. 14/14 curl checks passed; 2 smoke regressions passed; unit tests 12/12}}
- **Attempt history:**

  | # | Start | End | Tokens | Result | Error (short) |
  |---|---|---|---|---|---|
  | 1 | {{t}} | {{t}} | {{n}} | {{pass/fail}} | {{— or message}} |
