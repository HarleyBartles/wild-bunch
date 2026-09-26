# Repo Runbook Policy

This file maps Wild Bunch lifecycle stages and topical workflows to the
portable repository composition standard. Workflow procedure remains in the
mapped runbooks and playbooks.

## Standard runbooks

| Standard runbook | Local path | Status |
|---|---|---|
| design.md | `.agents/runbooks/design.md` | required |
| planning.md | `.agents/runbooks/planning.md` | required |
| implementing.md | `.agents/runbooks/implementing.md` | required |
| code-review.md | `.agents/runbooks/code-review.md` | required |
| pr.md | `.agents/runbooks/pr.md` | required |

## Standard playbooks

| Standard playbook | Local path | Status |
|---|---|---|
| code-style.md | `.agents/playbooks/code-style.md` | required |
| testing.md | `.agents/playbooks/testing.md` | required |
| security.md | `.agents/playbooks/security.md` | present |
| marketplace-generation.md | `.agents/playbooks/marketplace-generation.md` | present |
| skill-authoring.md | `.agents/playbooks/skill-authoring.md` | present |

## Additional repository-specific playbooks

- `.agents/playbooks/asset-selection-cut-normalization.md`
- `.agents/playbooks/completing-plans.md`
- `.agents/playbooks/dev-overlay.md`
- `.agents/playbooks/seeded-game-setup.md`
- `.agents/playbooks/town-hub-asset-production.md`
- `.agents/playbooks/ui-browser-check.md`

## Root contributor and review surfaces

- `REVIEW.md` is the review entry point.
- `CONTRIBUTING.md` is the substantive contributor entry point.

## Hook custody

`githooks/pre-commit` is the tracked canonical hook. The apply capability sets
`core.hooksPath=githooks`; hosted CI executes that same hook against the
checked-out commit in parity mode.

## Root router interpretation

Root `AGENTS.md` uses the five-section router defined by the executable
repository-shape contract. Its routing pointers link both workflow inventories;
publication proof remains owned by `.agents/runbooks/pr.md`.
