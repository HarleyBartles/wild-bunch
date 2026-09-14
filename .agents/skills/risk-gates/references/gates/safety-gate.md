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

When the request itself asks for an unauthorized destructive action, the first response
must state that destructive authority is missing and offer a reversible alternative.
Do this before repository inspection: inspection may refine the
alternative, but it must not make the destructive request appear underway.

Recoverability does not grant authority. A backup, bundle, reflog, temporary
branch, or orphan branch can reduce damage but cannot authorize a rewrite. Stop
and wait for explicit destructive authority before any inspection or command
whose purpose is to prepare or perform the rewrite. This includes
`git switch --orphan`, `git checkout --orphan`, reflog expiry, garbage
collection/pruning, branch replacement, and force push.
