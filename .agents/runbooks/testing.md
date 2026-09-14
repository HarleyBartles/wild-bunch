# Testing runbook

## When

Adding, changing, or running Wild Bunch tests and validation gates.

## Required skills

- `/test-driven-development` while constructing behavior changes.
- `/verification-before-completion` before passing or completion claims.

## Composition

Use the owning capability to choose behavior, `/test-driven-development` for
the focused red-green cycle, and `/verification-before-completion` for the
state-bound final gate.

## Doctrine and contracts

- [Validation doctrine](../doctrine/validation-policy.md) owns test lanes and
  repository-specific invariants.
- [Repository command declaration](../contracts/repo-standards-commands.json)
  owns canonical command vectors.

## Local commands and paths

```powershell
.\tools\postgres-dev.ps1 ensure
py -3 tools\run.py ci --check
```

Use `ci --apply` when generated agent surfaces changed and `--diagnostics` only
to collect independent failures. Persistence changes also run `dotnet tool
restore` and `dotnet ef migrations list --project src\WildBunch.Persistence
--startup-project src\WildBunch.Api`. Direct PostgreSQL checks use the shared
service at `localhost:5435`.

## Evidence contract

Report the tested tree, focused behavior result, canonical gate result, and any
documented skips or environment failures.

## Prohibited combinations

- Do not treat a broad green suite as proof of an unexercised behavior.
- Do not run `ci --apply` as the final non-mutating proof.
