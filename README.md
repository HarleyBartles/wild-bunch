# Wild Bunch

A C#/.NET Western adventure game with a React/Vite web play surface, Onion/DDD/CQRS/event-sourcing backend, and PostgreSQL persistence.

## For developers

- `AGENTS.md` — auto-injected agent law and routing to repo doctrine. Start here if you are an agent or a contributor working alongside one.
- [INDEX.md](INDEX.md) — generated navigation index for the whole repo.
- [docs/](docs/INDEX.md) — repo documentation and ADR log.

## Setup

- Backend: .NET 10. See `.agents/docs/validation-policy.md` for the validation lane (`dotnet build`, `dotnet test`, `bash scripts/postgres-dev.sh validate` or `.\scripts\postgres-dev.ps1 validate`).
- Frontend: React + Vite in `src/WildBunch.Web`. See `src/WildBunch.Web/package.json` for scripts.

## Repo-local plugin posture

This repo default-installs seven Codex plugins from [HarleyBartles/agent-asset-marketplace](https://github.com/HarleyBartles/agent-asset-marketplace): `repo-worker-pack`, `superpowers-plus`, `wild-bunch-project-pack`, `game-studio`, `dotnet-kit`, `architecture-pack`, and `frontend-pack`. See [`.agents/plugins/marketplace.json`](.agents/plugins/marketplace.json).
