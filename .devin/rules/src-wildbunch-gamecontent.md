---
description: "WildBunch.GameContent"
trigger: glob
globs:
  - "src/WildBunch.GameContent/**"
---
## Scope

`src/WildBunch.GameContent/**`

When working in this scope:
- **Before touching the seed codec, game-setup pipeline, or starting town rules:** [`.agents/docs/game-content-seed-pipeline.md`](../../.agents/docs/game-content-seed-pipeline.md) — pipeline, seed-owned/pressure-owned/entropy-owned boundaries, seed-derived town selection, starting town rules, and update rules.
- **Before touching the UUID seed codec, `GameSession`, or persistence:** [`.agents/docs/architecture-guardrails.md`](../../.agents/docs/architecture-guardrails.md) — architecture stack and UUID Seed Codec section.
- **Before touching entropy, deterministic tests, or dev-overlay seed controls:** [`.agents/docs/entropy-and-seed-policy.md`](../../.agents/docs/entropy-and-seed-policy.md) — entropy ladder and seed/test policy.
- **Before writing or reviewing tests:** [`.agents/docs/validation-policy.md`](../../.agents/docs/validation-policy.md) — test kinds and validation commands.
- **Before writing code:** [`.agents/docs/coding-discipline.md`](../../.agents/docs/coding-discipline.md) — scope and architecture discipline.
