# Repo Runbook Policy

This repo follows the `repo-standards` skill. Invoke `using-superpowers-plus`
first to route to the relevant stage skill, then invoke `repo-standards` when
the task touches repo shape, runbook layout, or scaffolds.

## Standard-to-local mapping

| Standard runbook | Local path | Status |
|---|---|---|
| design.md | `.agents/runbooks/design.md` | required |
| planning.md | `.agents/runbooks/planning.md` | required |
| implementing.md | `.agents/runbooks/implementing.md` | required |
| code-review.md | `.agents/runbooks/code-review.md` | required |
| marketplace-generation.md | `.agents/runbooks/marketplace-generation.md` | present |
| skill-authoring.md | `.agents/runbooks/skill-authoring.md` | present |
| security.md | `.agents/runbooks/security.md` | present |
| testing.md | `.agents/runbooks/testing.md` | present |
| pr.md | `.agents/runbooks/pr.md` | required |
| code-style.md | `.agents/runbooks/code-style.md` | present |

## Additional repo-specific runbooks

- <!-- list repo-specific runbooks here -->

## Root contributor and review surfaces

- `REVIEW.md` is the review entry point.
- `CONTRIBUTING.md` is the substantive contributor entry point.

## Exceptions

None.

## Root router interpretation

Root `AGENTS.md` uses the five-section router defined by the executable
repository-shape contract. The 12 canonical topics are coverage requirements
across those five sections and their routed targets; they are not 12 required
root headings. Publication proof is owned by `.agents/runbooks/pr.md`.
