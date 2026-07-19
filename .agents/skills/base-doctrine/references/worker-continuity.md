# Worker and subagent continuity

Use this doctrine for controller-launched worker or subagent execution across
repositories and runtimes.

## Core rule

Once a subagent has been dispatched for a bounded task, leave it running until
it returns a terminal status or concrete evidence proves that it is stalled or
unsafe. Waiting for a slow worker is part of execution; it is not a reason to
cancel, replace, or ask the human whether to continue.

Elapsed time, chat silence, token cost, remaining plan tasks, human
unavailability, or a polling timeout alone is never evidence of a stall.
Polling and monitoring must be non-destructive.

Concrete stop evidence is limited to an observed process exit or fatal error,
an explicit `BLOCKED` or unrecoverable infrastructure status, a verified
repeated failure with no state change, or a safety or authorization condition
that requires stopping. Inspect the worker state before acting; do not infer a
stall from waiting duration.

If the worker returns `NEEDS_CONTEXT`, provide the missing context and
re-dispatch it. If it returns `BLOCKED`, follow the owning workflow's blocker
handling. Neither status authorizes silently abandoning the task.

## Controller posture

- Continue waiting when the worker is alive and no concrete stop evidence exists.
- Do not impose an arbitrary controller-side cancellation deadline.
- When stopping or replacing a worker is justified, record the observed evidence
  and preserve the worker's last report for recovery and review.
