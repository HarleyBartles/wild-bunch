# Debugging Profile

Portable anti-slop profile for systematic bug diagnosis.

## Purpose

Catch guess-driven bug work. Force agents to observe symptoms, reproduce where possible, narrow hypotheses, and prove before/after behavior before claiming a fix.

## Where to Use

When diagnosing bugs, test failures, flaky behavior, unexpected output, production incidents, tool failures, or regression reports.

## Slop Patterns to Avoid

- Guessing root cause before observing evidence
- Fix-before-reproduction behavior
- `Probably` and `likely` explanations without logs, traces, test output, or inspected code
- Defensive coding as a substitute for understanding the failure
- Claiming a fix without rerunning the failing case
- Confusing symptom suppression with root-cause repair

## Required Avoid Rules

Prohibit root-cause claims based on `probably`, `likely`, `should`, `seems`, or `flaky` unless paired with observed evidence and a falsifiable hypothesis.

Catch fixes that do not identify the failing input, command, log, stack trace, test case, or reproduction path.

## Required Prefer-Instead Rules

- State the observed symptom and exact evidence
- Reproduce the failure or explain why reproduction is unavailable
- Name the smallest plausible hypothesis and how it was tested
- Change only the surface implicated by evidence
- Rerun the failing case and relevant regression checks
- Report unresolved uncertainty separately from fixed behavior

## False Positives / Do Not Overapply

Do not ban hypotheses. Ban unsupported hypotheses presented as explanation or fix direction. Hypotheses are acceptable when labelled and paired with the next check.

## Examples

### Before (Avoid)
> This is probably caused by a race condition, so we should add a retry.

### After (Prefer)
> Observed symptom: intermittent HTTP 500 errors in `src/api/orders.py` around line 45. Log analysis shows concurrent requests arriving within 10ms of each other. Hypothesis: race condition in order creation. Test: added a delay in test to reproduce. Confirmed race condition. Fix: added database row-level lock. Verification: ran 100 concurrent requests - all succeeded.

### Before (Avoid)
> The issue is likely in the cache layer.

### After (Prefer)
> Observed symptom: stale data returned after updates. Inspected cache layer in `src/cache/redis.py` - cache invalidation is not called after updates. Fix: added cache invalidation call in `update_user()` function. Verification: updated user data is now returned immediately.

## Acceptance Checks

This profile is acceptable only if it would stop an agent from implementing a bug fix before naming the observed failure and the validation that proves the repair.
