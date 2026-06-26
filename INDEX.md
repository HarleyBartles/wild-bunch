# Wild Bunch

C#/.NET Western adventure game with a React/Vite web play surface, Onion/DDD/CQRS/event-sourcing backend, and PostgreSQL persistence.

## Top-level folders

- [.agents/](.agents/INDEX.md) - Agent doctrine, architecture hygiene, dev-overlay doctrine, repo-wide unslop profiles, repo-local skills index, and superpowers plan/evidence records.
- [.config/](.config/INDEX.md) - Repo-local .NET tool manifest.
- [.github/](.github/INDEX.md) - GitHub workflows and CI configuration.
- [docs/](docs/INDEX.md) - Repo documentation and ADR log.
- [scripts/](scripts/INDEX.md) - Repo-local helper scripts (PostgreSQL dev service).
- [src/](src/INDEX.md) - Solution source projects: Api, Application, Domain, GameContent, Persistence, Web.
- [tests/](tests/INDEX.md) - Test projects: Domain, Application, GameContent, Integration.

## Key files

- [AGENTS.md](AGENTS.md) - Top-level agent law, mesh policy, validation, and worker return format.
- [WildBunch.sln](WildBunch.sln) - .NET solution binding all backend projects.
- [.gitignore](.gitignore) - Git ignore rules.

## Index mesh exclusions

INDEX.md files are not created in: `bin/`, `obj/` (build output), `node_modules/` (dependencies), `.git/` (git internals), `.local/` (local output), and `.agents/skills/` subdirectories (canonical skill-shaped folders where `SKILL.md` is the entrypoint; the skills index lives at `.agents/skills/INDEX.md`).
