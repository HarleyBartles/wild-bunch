---
description: "WildBunch.Assets/scripts AGENTS.md"
trigger: glob
globs:
  - "src/WildBunch.Assets/scripts/**"
---
## Scope

`src/WildBunch.Assets/scripts/**`

When working in this scope:
- Keep asset-pipeline code here when it is specific to the WildBunch.Assets project.
- Treat `image_asset_pipeline.py` here as the canonical implementation for asset staging and promotion.
- Keep the repo-root `scripts/image_asset_pipeline.py` as a compatibility wrapper only.
