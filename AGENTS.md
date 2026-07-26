# Wild Bunch

## Repository purpose

Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.

## Source-of-truth split

- Repo state (code, docs, ADRs, and generated mesh) is the source of truth for implementation.
- External control planes (GitHub PRs, Linear issues) provide publication and issue facts, but the live repo is the authority for current implementation state.

## Publication proof

Work is published through a dedicated linked worktree, a task branch, and a pull request to `main`. Direct pushes to `main` require explicit authorization.

## Build and test commands

- `dotnet build`
- `dotnet test`
- `npm ci && npm run typecheck && npm run test && npm run build` in `src/WildBunch.Web`
- `python .agents/skills/generating-agent-mesh/scripts/generate-index-mesh.py` to regenerate `INDEX.md` files
- `python .agents/skills/refreshing-installed-skills/scripts/refresh_installed_skills.py --check` to validate marketplace skill projection
- `python .agents/skills/repo-standards/scripts/repo_standards.py --check` to validate repo-standards shape

## Routing pointers

- Repository purpose: [AGENTS.md](AGENTS.md)
- Source-of-truth split: [AGENTS.md](AGENTS.md)
- Publication proof: [AGENTS.md](AGENTS.md)
- Build and test commands: [AGENTS.md](AGENTS.md)
- Testing instructions: [.agents/guides/testing-guide.md](.agents/guides/testing-guide.md)
- Code style guidelines: [.agents/guides/code-style-guide.md](.agents/guides/code-style-guide.md)
- Review guidelines: [.agents/guides/code-review-guide.md](.agents/guides/code-review-guide.md)
- PR instructions: [.agents/guides/pr-guide.md](.agents/guides/pr-guide.md)
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)
- Security considerations: [.agents/guides/security-guide.md](.agents/guides/security-guide.md)
- Routing pointers: [AGENTS.md](AGENTS.md)
- Maintenance responsibility: [AGENTS.md](AGENTS.md)

## Maintenance responsibility

This router and the routed surfaces are maintained by the Wild Bunch core team. Update them when repo conventions, validation commands, or guide locations change.
