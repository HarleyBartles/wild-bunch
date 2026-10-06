# Code Review Anti-Slop Profile

Read this profile in full before reviewing and maintain encountered patterns through the [observation loop](README.md#record-and-improve). Review the changed behavior against applicable [backend](backend-architecture.md), [web](play-surface-ui.md) and [dev-overlay](dev-overlay.md) guards. Documentation-only changes do not require invented runtime tests, and existing test count is not evidence of a newly covered behavior.

Use this profile when performing code reviews. This profile enforces standards for reviewing code changes, PRs, and worker returns.

## Scratch Artifact Check
- **CRITICAL**: Before creating any scratch files (code reviews, temporary notes, draft documents), check `.agents/doctrine/artifact-custody.md` for placement guidance
- Scratch files must be placed in `Z:\_agent-scratch\wild-bunch\<branch-name>`, never in the repo root
- Files like `*-review*.md`, `*-scratch*.md`, `COMMIT_MSG.txt`, `PR_BODY.md` are scratch artifacts that pollute the tree
- If a committed artifact appears to be scratch, apply the completed-artifact policy before removing it; preserve live, ambiguous and enduring material.

## Review Discipline

### Location-only change detectors

Recognize a test that asserts the old document/file layout without exercising a consumer behavior or current independently owned contract. When an authorized authority or custody change invalidates that assertion, remove the obsolete detector rather than rewriting it to freeze the new filenames. Preserve meaningful structural-checker behavior tests, such as rejecting broken routes or over-budget routers; they exercise a declared validator contract. The concrete near miss and correction are recorded as [U-002](observations.md#u-002-obsolete-profile-placement-detector).
- Review the actual diff, not the PR description or summary
- Verify that the implementation matches the spec/issue requirements
- Check for architectural violations (DDD, CQRS, Event Sourcing)
- Verify test coverage and test quality
- Check for proper error handling and edge cases
- Verify that the change is scoped to the requested work (no opportunistic refactors)
- Check for proper documentation updates (ADR, guides, policy files if needed)

## Review Checklist
- [ ] Scratch artifacts are not committed to repo root
- [ ] Implementation matches spec/issue requirements
- [ ] Architectural patterns are followed (DDD, CQRS, Event Sourcing)
- [ ] Meaningful tests cover changed behavior when needed; no tautological or change-detector tests were introduced
- [ ] Error handling is proper
- [ ] Change is scoped to requested work
- [ ] Documentation is updated if needed
- [ ] No breaking changes without explicit justification
