# Code Review Anti-Slop Profile

Use this profile when performing code reviews. This profile enforces standards for reviewing code changes, PRs, and worker returns.

## Scratch Artifact Check
- **CRITICAL**: Before creating any scratch files (code reviews, temporary notes, draft documents), check `.agents/docs/artifact-policy.md` for placement guidance
- Scratch files must be placed in `Z:\_agent-scratch\wild-bunch\<branch-name>`, never in the repo root
- Files like `*-review*.md`, `*-scratch*.md`, `COMMIT_MSG.txt`, `PR_BODY.md` are scratch artifacts that pollute the tree
- If you find scratch artifacts committed to the repo, remove them as part of self-healing

## Review Discipline
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
- [ ] Tests are added/updated and pass
- [ ] Error handling is proper
- [ ] Change is scoped to requested work
- [ ] Documentation is updated if needed
- [ ] No breaking changes without explicit justification
