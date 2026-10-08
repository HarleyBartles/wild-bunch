# Repository workflow guidance

This page routes work to Wild Bunch's current lifecycle and concern guides.
The list describes repository practice; it is not a required template or a
minimum inventory. Update links as guidance changes.

Before entering a stage or concern below, read [unslop selection and observations](../unslop/README.md) and the applicable profiles in full. Each stage and topical guide also names the guards required at its own work point. Profile/routing changes require [routing guards](../unslop/routing.md).

## Scoped agent entrypoints

Root `AGENTS.md` is the always-on router. Thin scoped `AGENTS.md` files are placed at the established scripts, web, game-content and asset boundaries, including asset bibles, asset helpers and source families, plus the agent-guidance tree. Each contains one sentence naming its directory-tree scope, the condition for reading linked guidance, and an explicit outside-scope disqualifier. It carries pointers rather than doctrine so retained context cannot make its local instructions apply to unrelated work. Use these conditional pointers across harnesses; no separate Devin rule layer is maintained.

The root budget remains 40 lines and each scoped router's ceiling remains 15 lines; the one-sentence policy is stricter than that ceiling. `scripts/check_agent_routers.py` checks scope/read shape, budgets and links through the existing canonical hook and CI. Semantic review checks the disqualifier, useful placement and harness-independent pointers. Do not add routers per source file or arbitrary directory. Update the scoped pointers when their authority moves.

## Lifecycle stages

| Stage | Runbook |
|---|---|
| Design | [design](../runbooks/design.md) |
| Planning | [planning](../runbooks/planning.md) |
| Implementation | [implementing](../runbooks/implementing.md) |
| Code review | [code review](../runbooks/code-review.md) |
| Pull request | [pull request](../runbooks/pr.md) |

## Cross-stage concerns

- [Decision records](../playbooks/decision-records.md) - when a change makes, changes, corrects, or materially removes a durable decision.
- [Code style](../playbooks/code-style.md)
- [Testing](../playbooks/testing.md)
- [Security](../playbooks/security.md)
- [Asset selection and cut normalization](../playbooks/asset-selection-cut-normalization.md)
- [Completing plans](../playbooks/completing-plans.md)
- [Marketplace and skill changes](../playbooks/marketplace-generation.md)
- [Skill authoring](../playbooks/skill-authoring.md)
- [Seeded game setup](../playbooks/seeded-game-setup.md)
- [Development overlay](../playbooks/dev-overlay.md)
- [Town hub asset production](../playbooks/town-hub-asset-production.md)
- [Browser checks](../playbooks/ui-browser-check.md)

`AGENTS.md` is the concise repository router. It links to this policy,
contribution guidance, and review and publication entry points. Keep detailed
procedure in the relevant runbook and reusable concern guidance in playbooks.
