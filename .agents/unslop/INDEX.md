# unslop

Repo-wide drift-prevention profiles for Wild Bunch. Agent-facing review/filter material, not human docs.

## Key files

- [backend-architecture.md](backend-architecture.md) - Backend drift-prevention profile for the Onion/DDD/CQRS/event-sourcing/projection posture.
- [dev-overlay.md](dev-overlay.md) - Dev overlay drift-prevention profile for contextual panels, dev-only playtest controls, hidden-truth surfaces, browser proof, and generated evidence.

## Convention

- Repo-wide unslop profiles live under `.agents/unslop/`.
- Project-local unslop profiles live under `{project}/.agents/unslop/`.
- Profile filenames are short lowercase kebab-case scope names.
- Do not include `unslop`, `profile`, or `unslop-profile` in the filename; the folder already says what it is.
- Human docs may point to these profiles, but profiles themselves are agent-facing review/filter material.

Back to [.agents/](../INDEX.md)
