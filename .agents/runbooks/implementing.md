# Implementing runbook

Use this local overlay after `using-superpowers-plus` selects the implementation
owner.

## Required local reading

- [Coding discipline](../doctrine/coding-discipline.md) for scope and architecture
  boundaries.
- [Validation doctrine](../doctrine/validation-policy.md) before changing tests.
- [Architecture guardrails](../doctrine/architecture-guardrails.md) before changing
  GameSession, persistence, domain logic, commands, queries, or projections.
- [Frontend standards](../doctrine/frontend-standards.md) before browser work.

## Wild Bunch validation

- Canonical apply: `py -3 tools/run.py ci --apply`
- Canonical fail-fast check: `py -3 tools/run.py ci --check`
- Diagnostic check: `py -3 tools/run.py ci --check --diagnostics`
- Backend focused checks: `dotnet build` and `dotnet test`
- Frontend focused checks in `src/WildBunch.Web`: `npm run typecheck`,
  `npm run test`, and `npm run build`

For a normal commit, stage the intended tree and let the installed pre-commit
hook apply and check that exact staged snapshot.
