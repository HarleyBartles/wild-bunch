---
description: "WildBunch.GameContent"
trigger: glob
globs:
  - "src/WildBunch.GameContent/**"
---
## Scope

`src/WildBunch.GameContent/**`

When working in this scope:
- **Before touching the seed codec, game-setup pipeline, or starting town rules:** [`.agents/doctrine/game-content-seed-pipeline.md`](../../.agents/doctrine/game-content-seed-pipeline.md) — pipeline, seed-owned/pressure-owned/entropy-owned boundaries, seed-derived town selection, starting town rules, and update rules.
- **Before touching the UUID seed codec, `GameSession`, or persistence:** [`.agents/doctrine/architecture-guardrails.md`](../../.agents/doctrine/architecture-guardrails.md) — architecture stack and UUID Seed Codec section.
- **Before touching entropy, deterministic tests, or dev-overlay seed controls:** [`.agents/doctrine/entropy-and-seed.md`](../../.agents/doctrine/entropy-and-seed.md) — entropy ladder and seed/test policy.
- **Before writing or reviewing tests:** [`.agents/doctrine/validation-policy.md`](../../.agents/doctrine/validation-policy.md) — test kinds and validation commands.
- **Before writing code:** [`.agents/doctrine/coding-discipline.md`](../../.agents/doctrine/coding-discipline.md) — scope and architecture discipline.
