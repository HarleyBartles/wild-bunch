# Code Review Profile

Portable anti-slop profile for evidence-based code review.

## Purpose

Catch shallow review behavior and force reviewers to inspect the actual change, issue goal, validation evidence, and falsification surfaces.

## Where to Use

Before reviewing PRs, worker branches, diffs, implementation reports, generated artifacts, or change summaries.

## Slop Patterns to Avoid

- Approval based on worker summary rather than source evidence
- Performative agreement or politeness without technical judgment
- Style nits that ignore issue-goal conformance
- `Looks good` review with no falsification checks
- Treating tests/checks as complete proof without checking whether they match the goal
- Missing generated artifact, provenance, migration, or manifest review when those surfaces changed

## Required Avoid Rules

Prohibit review conclusions that rely on `seems`, `looks good`, `worker summary`, `small diff`, `tests pass`, or `no obvious issues` without naming inspected evidence.

Reject review comments that do not distinguish implementation evidence from author claims.

## Required Prefer-Instead Rules

- Restate the issue goal as observable state
- Inspect the relevant diff/source surfaces before judging
- Compare changed files to the goal and non-goals
- Name validation evidence and what it does or does not prove
- Call out missing evidence directly instead of approving around it
- Separate blocking defects from follow-up suggestions

## False Positives / Do Not Overapply

Do not force long review essays for tiny safe changes. A short review is acceptable if it names the inspected surface and why the scope is sufficient.

## Examples

### Before (Avoid)
> Looks good overall, just a few minor nits.

### After (Prefer)
> I reviewed the diff in `src/api/users.py`. The changes correctly implement the user deletion endpoint as specified in the issue. Added tests in `tests/test_users.py` cover the success case and the user-not-found case. All tests pass. Ready to merge.

### Before (Avoid)
> This seems reasonable based on the worker summary.

### After (Prefer)
> I inspected the actual code changes in `src/auth/session.py`. The session timeout logic matches the requirements. However, the migration script in `migrations/002_add_timeout.sql` was not included in this PR. That should be added before merge.

## Acceptance Checks

This profile is acceptable only if it would block approval of a PR when the reviewer has only read the worker summary and has not inspected the changed source or relevant validation.
