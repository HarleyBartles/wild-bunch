# Testing runbook

## When

Adding, changing, or running Wild Bunch tests and validation gates.

## Required skills

- `/test-driven-development` while constructing behavior changes.
- `/verification-before-completion` before passing or completion claims.

## Composition

1. Use the owning capability and validation doctrine to select the smallest
   lane that observes the changed behavior.
2. Use `/test-driven-development` for the focused red-green cycle; add real
   PostgreSQL or browser evidence only when the boundary requires it.
3. Start shared PostgreSQL with `.\tools\postgres-dev.ps1 ensure` before lanes
   that use `localhost:5435`.
4. Apply generated surfaces when needed, stage the intended tree, and let the
   normal hook run the canonical check.
5. Use `/verification-before-completion` to report focused and canonical proof
   for the same tested state.

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

For hook setup or repair, run `ci --apply`; it activates the tracked hook before
checking upstream repository shape. Hook custody is defined in
[repo runbook policy](../doctrine/repo-runbook-policy.md).

Use `ci --apply` when generated agent surfaces changed and `--diagnostics` only
to collect independent failures. Persistence changes also run `dotnet tool
restore` and `dotnet ef migrations list --project src\WildBunch.Persistence
--startup-project src\WildBunch.Api`. Direct PostgreSQL checks use the shared
service at `localhost:5435`.

## Evidence contract

- [ ] The tested tree or commit is identified.
- [ ] Focused behavior proof names its command and result.
- [ ] Required PostgreSQL or browser dependencies were healthy.
- [ ] The canonical gate result belongs to the same staged or committed state.
- [ ] Skips and environment failures are reported rather than counted as proof.

## Prohibited combinations

- Do not treat a broad green suite as proof of an unexercised behavior.
- Do not run `ci --apply` as the final non-mutating proof.
