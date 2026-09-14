# Repo Runbook Policy

This file records the required repository runbook mapping and accepted local
extensions to the portable repository standard.

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
| completing-plans.md | `.agents/runbooks/completing-plans.md` | present |

## Additional repo-specific runbooks

- `.agents/runbooks/ui-browser-check.md`
- `.agents/runbooks/asset-selection-cut-normalization.md`
- `.agents/runbooks/seeded-game-setup.md`
- `.agents/runbooks/dev-overlay.md`
- `.agents/runbooks/town-hub-asset-production.md`

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
