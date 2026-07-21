# Testing Profile

Portable anti-slop profile for behavior-focused testing.

## Purpose

Catch test plans and test code that create confidence theatre instead of behavior proof. Push agents to test observable behavior, negative paths, boundaries, and regression risk.

## Where to Use

Before writing or reviewing test plans, unit tests, integration tests, UI tests, regression tests, validation notes, or coverage claims.

## Slop Patterns to Avoid

- `Add tests` without naming behavior
- Overmocking and asserting implementation calls rather than outcomes
- Snapshot tests used as cheap coverage without reviewing the semantic output
- Vague edge-case language with no named edge cases
- Only happy-path tests
- Validation claims that treat test existence or coverage percentage as proof
- Tests that reproduce implementation structure rather than user/domain behavior

## Required Avoid Rules

Prohibit vague testing instructions such as `new logic`, `edge cases`, `coverage`, `if appropriate`, `assert called`, and `snapshot to cover` when they do not name behavior, inputs, outputs, and failure cases.

Catch tests that validate implementation wiring but not observable behavior.

## Required Prefer-Instead Rules

- Name the behavior under test and why it matters
- Include happy path, negative path, boundary, and regression cases where relevant
- Prefer assertions on externally observable results over internal calls
- Use mocks only at real boundaries or expensive/unavailable dependencies
- Treat snapshots as reviewed artifacts, not automatic proof
- Name exact validation commands and observed output in reports

## False Positives / Do Not Overapply

Do not ban mocks, snapshots, or coverage metrics. Ban using them as substitutes for behavior-specific confidence.

## Examples

### Before (Avoid)
> Add unit tests for the new logic.

### After (Prefer)
> Add unit tests for `calculate_discount()` function. Test cases: (1) 10% discount for orders over $100, (2) no discount for orders under $100, (3) zero discount for zero amount, (4) handles negative amounts correctly.

### Before (Avoid)
> Mock the dependencies and assert the method was called.

### After (Prefer)
> Mock the external payment API and assert that the correct payment amount is passed and the function returns the expected success response. Do not assert internal method calls - assert the observable outcome.

## Acceptance Checks

This profile is acceptable only if it would force a test request from generic coverage language into concrete behavior and failure-case validation.
