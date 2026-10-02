# Wild Bunch

## Repository purpose

Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.

## Source-of-truth split

- Repo state (code, docs, decisions, contracts, and certifications) is the source of truth for implementation.
- External control planes (GitHub PRs, Linear issues) provide publication and issue facts, but the live repo is the authority for current implementation state.

## Build and test commands

- `py -3 tools/run.py ci --check`
- `dotnet build`
- `dotnet test`
- `npm ci && npm run typecheck && npm run test && npm run build` in `src/WildBunch.Web`

## Routing pointers

- Agent Operating Model adoption: [.agents/contracts/operating-standards.json](.agents/contracts/operating-standards.json) and [standards certification](.agents/contracts/standards-certification.md).
- Lifecycle runbooks: [repo workflow policy](.agents/doctrine/repo-runbook-policy.md)
- Topical playbooks: [testing](.agents/playbooks/testing.md) and [code style](.agents/playbooks/code-style.md)
- Scoped routing: [Devin rules](.devin/rules/scripts.md)
- Testing instructions: [.agents/playbooks/testing.md](.agents/playbooks/testing.md)
- Code style guidelines: [.agents/playbooks/code-style.md](.agents/playbooks/code-style.md)
- Review guidelines: [.agents/runbooks/code-review.md](.agents/runbooks/code-review.md)
- Publication proof and PR instructions: [.agents/runbooks/pr.md](.agents/runbooks/pr.md)
- Contributing: [CONTRIBUTING.md](CONTRIBUTING.md)
- Security considerations: [.agents/playbooks/security.md](.agents/playbooks/security.md)

## Maintenance responsibility

This router and the routed surfaces are maintained by the Wild Bunch core team. Update them when repo conventions, validation commands, or guide locations change.
