# Wild Bunch

## Repository purpose

Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.

## Source-of-truth split

- Repo state (code, docs, ADRs, and generated mesh) is the source of truth for implementation.
- External control planes (GitHub PRs, Linear issues) provide publication and issue facts, but the live repo is the authority for current implementation state.

## Publication proof

Work is published through a dedicated linked worktree, a task branch, and a pull request to `main`. Direct pushes to `main` require explicit authorization.

## Build and test commands

- `py -3 tools/run.py ci --check`
- `dotnet build`
- `dotnet test`
- `npm ci && npm run typecheck && npm run test && npm run build` in `src/WildBunch.Web`

## Routing pointers

- Scoped routing: [.devin/rules/INDEX.md](.devin/rules/INDEX.md)
- Testing instructions: [.agents/guides/testing-guide.md](.agents/guides/testing-guide.md)
- Code style guidelines: [.agents/guides/code-style-guide.md](.agents/guides/code-style-guide.md)
- Review guidelines: [.agents/guides/code-review-guide.md](.agents/guides/code-review-guide.md)
- PR instructions: [.agents/guides/pr-guide.md](.agents/guides/pr-guide.md)
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)
- Security considerations: [.agents/guides/security-guide.md](.agents/guides/security-guide.md)

## Maintenance responsibility

This router and the routed surfaces are maintained by the Wild Bunch core team. Update them when repo conventions, validation commands, or guide locations change.
