---
description: "Completed plans and specs are historical context, not live patterns"
trigger: glob
globs:
  - ".agents/plans/completed/**"
  - ".agents/specs/completed/**"
---
## Scope

`.agents/plans/completed/` and `.agents/specs/completed/`

For the canonical doctrine, read `.agents/doctrine/completed-plans.md`.

This file is a conditional rule trigger. It does not contain the doctrine; it only tells the runtime when to load the doctrine from `.agents/doctrine/`. Do not restate the doctrine here.
