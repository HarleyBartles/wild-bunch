---
description: "WildBunch.GameContent"
trigger: glob
globs:
  - "src/WildBunch.GameContent/**"
---
## Scope

`src/WildBunch.GameContent/**`

When working in this scope:
- **For the seed codec, game-setup pipeline, or starting-town rules:** [`.agents/runbooks/seeded-game-setup.md`](../../.agents/runbooks/seeded-game-setup.md) — focused skill composition, local paths, and proof route.
- **For `GameSession` or persistence boundaries:** [`.agents/doctrine/architecture-guardrails.md`](../../.agents/doctrine/architecture-guardrails.md).
- **Before touching entropy, deterministic tests, or dev-overlay seed controls:** [`.agents/doctrine/entropy-and-seed.md`](../../.agents/doctrine/entropy-and-seed.md) — entropy ladder and seed/test policy.
- **For tests:** [`.agents/runbooks/testing.md`](../../.agents/runbooks/testing.md) and [`.agents/doctrine/validation-policy.md`](../../.agents/doctrine/validation-policy.md).
- **Before writing code:** [`.agents/doctrine/coding-discipline.md`](../../.agents/doctrine/coding-discipline.md) — scope and architecture discipline.
