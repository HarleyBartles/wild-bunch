# Worker Returns Profile

Portable anti-slop profile for evidence-bearing completion reports.

## Purpose

Catch false-green completion reports and make agent returns evidence-bearing, auditable, and easy to verify.

## Where to Use

Before writing worker completion reports, Codex/agent returns, implementation summaries, PR handoffs, issue closeout notes, or validation reports.

## Slop Patterns to Avoid

- Claiming completion without changed files, branch/PR/commit evidence, or validation output
- Treating `all tests pass` as proof without command names and observed output
- Summarizing intent instead of proving issue-goal conformance
- Calling work ready to merge when PR publication, review, or main-state evidence is absent
- Vague `updated relevant files` claims
- Hiding blockers, skipped validation, or generated artifact spillover

## Required Avoid Rules

Prohibit unsupported completion words such as `done`, `complete`, `ready`, `successfully`, `handled`, and `all tests pass` when they are not paired with exact evidence.

Catch worker reports that omit what changed, how it was validated, what remains unknown, and how the issue goal was met.

## Required Prefer-Instead Rules

- Name changed files or changed surfaces
- Name branch, PR, commit, generated artifact, or durable publication evidence when available
- Include validation commands and observed results
- Separate worker claims from verifier-ready evidence
- State skipped checks, blockers, or uncertainty plainly
- Tie the return back to the issue goal and DOD

## False Positives / Do Not Overapply

Do not require full PR evidence for work that legitimately did not create a PR. Require the evidence appropriate to the work surface.

## Examples

### Before (Avoid)
> Implemented successfully. All tests are passing.

### After (Prefer)
> Changed files: `src/api/users.py`, `tests/test_users.py`. Branch: `feature/user-deletion`. PR: https://github.com/org/repo/pull/123. Validation: `pytest tests/test_users.py` - all 12 tests pass. Issue goal met: user deletion endpoint is implemented and tested.

### Before (Avoid)
> The issue is complete and ready to merge.

### After (Prefer)
> Changed files: `src/auth/session.py`. Branch: `fix/session-timeout`. Commit: `abc123def`. Validation: `pytest tests/test_auth.py::test_session_timeout` passes. Issue goal met: session timeout logic now correctly expires sessions after 30 minutes. Ready for review.

## Acceptance Checks

This profile is acceptable only if a worker return cannot pass while relying on generic completion language without concrete evidence paths and validation output.
