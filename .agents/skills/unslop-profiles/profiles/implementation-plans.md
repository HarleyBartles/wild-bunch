# Implementation Plans Profile

Portable anti-slop profile for executable coding plans and worker issues.

## Purpose

Catch plans that sound useful but are not executable by a coding agent. Force plans to name the goal, likely seams, bounded steps, validation, and stop conditions.

## Where to Use

Before writing repo implementation plans, worker issues, coding handoffs, refactor plans, repair plans, migration plans, or follow-up implementation tasks.

## Slop Patterns to Avoid

- Broad `investigate and fix` tasks with no target surface
- Refactor language without files, seams, tests, or acceptance criteria
- `Ensure everything passes` instead of exact validation commands
- Hidden replanning where the worker is told to rediscover the whole problem
- Overbroad cleanup or modernization that can expand indefinitely
- Plans that mention tests but not what behavior must be proven

## Required Avoid Rules

Do not use plan wording such as:
- `relevant files`, `appropriate validation`, `make robust`, `improve architecture`
- `clean up`, `future changes`, `anything related`

unless bounded by specific files, seams, commands, or acceptance checks.

Catch plans where the first real action is broad investigation rather than a small executable slice.

## Required Prefer-Instead Rules

- State one observable goal
- Name likely files, modules, seams, commands, or search anchors
- Break the work into small ordered steps that can be executed without hidden replanning
- Include validation commands and expected signal
- State non-goals and blocked surfaces
- Prefer vertical slices of provable value

## False Positives / Do Not Overapply

Do not require exact file paths when the issue is a legitimate inventory task. In those cases, require exact discovery targets and durable output instead.

## Examples

### Before (Avoid)
> Investigate the current implementation and refactor it to be more robust.

### After (Prefer)
> Refactor the user authentication module in `src/auth/` to separate password hashing from session management. Add unit tests for the new `hash_password()` and `verify_session()` functions. Run `pytest tests/test_auth.py` to verify.

### Before (Avoid)
> Update the relevant files, add tests, and ensure everything passes.

### After (Prefer)
> Update `src/api/users.py` to add the new `delete_user()` endpoint. Add integration tests in `tests/integration/test_users_api.py`. Run `pytest tests/integration/test_users_api.py::test_delete_user` to verify the endpoint returns HTTP 204 on success.

## Acceptance Checks

This profile is acceptable only if it would reject a plan that cannot be started by a worker in the first 10 minutes without rediscovering scope.
