# Safety gate

Use before a destructive or irreversible action.

## Trigger

- Delete, truncate, drop, rewrite history, bulk update, or permission change.
- Any operation where recovery is costly or impossible.

## Green requirements

- The target is explicitly identified.
- The scope is confirmed by source or user authority.
- A backup, dry-run, or rollback path exists when feasible.
- The action is not broader than requested.

## Amber/red signals

- Missing authority for the target or scope.
- No recovery path and no explicit user confirmation.
- The operation would affect data outside the current task scope.

## Mode

- Use `internal_mode` for clearly scoped, recoverable actions.
- Use `interactive_mode` for destructive actions without a clear recovery path.
- Use `blocked_mode` when authority or target evidence is missing.
