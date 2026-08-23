# Implementer Brief

Use this template to dispatch an `implementer` subagent for a `node-finding-fix`.

## Finding

- **Lens:** {lens name, e.g., `reviewer-security`}
- **Severity:** {blocking | important | etc.}
- **Text:** {exact finding text}
- **Affected files:** {list of files the finding touches}

## Fix instructions

- {Step-by-step fix instructions}
- {Reference the `lens_checklist` and `diff_slice` as needed}
- {State any consumer constraints, tests, or behavior that must remain passing}

## Out of scope

- {Explicitly list anything the implementer must NOT change}
- {Files, classes, functions, or decisions that are off limits}

## Verification

- {Consumer preflight command to run, e.g., `py -3 tools/run.py ci --check`}
- {Expected passing output or assertion}
- {Any targeted checks to confirm the finding is resolved}

## Outputs

- {Files or commits the implementer must produce}
- {Report file to write back to `review-log-implementer-report.md`}
