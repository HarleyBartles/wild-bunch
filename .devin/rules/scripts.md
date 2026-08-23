---
description: "scripts/ AGENTS.md"
trigger: glob
globs:
  - "scripts/**"
---
## Scope

`scripts/**`

When working in this scope:
- **Use before reporting environmental issues or running ad-hoc commands:** [`scripts/README.md`](README.md) — full script catalog, requirements, and conventions.
- **Use when you need to run CI preflight checks:** `scripts/ci-preflight.sh` or `scripts/ci-preflight.ps1`
- **Use when you need dev servers:** `scripts/dev-servers.sh` or `scripts/dev-servers.ps1`
- **Use when you need PostgreSQL setup/validation:** `scripts/postgres-dev.sh` or `scripts/postgres-dev.ps1`
- **Use when you need image asset processing:** `scripts/image_asset_pipeline.sh` or `scripts/image_asset_pipeline.ps1`
