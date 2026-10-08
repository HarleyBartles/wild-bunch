# Testing playbook

## When

Adding, changing, or running Wild Bunch tests and validation gates.

## Required capabilities

- Construct observable behavior through focused, behavior-first tests.
- Verify claims against current repository and hosted evidence.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

## Unslop before work

Before test design or assessment, read [backend guards](../unslop/backend-architecture.md) for domain/API/persistence/replay tests and [play-surface UI guards](../unslop/play-surface-ui.md) for web tests in full; dev tests also require [dev overlay](../unslop/dev-overlay.md). Apply [code-review guards](../unslop/code-review.md) when judging claimed coverage. Record distinct misleading-proof incidents through the [observation loop](../unslop/README.md#record-and-improve), not a log of test runs.

## Composition

1. Use the owning capability and validation doctrine to select the smallest
   lane that observes the changed behavior.
2. Use behavior-focused test development for the focused red-green cycle; add real
   PostgreSQL or browser evidence only when the boundary requires it.
3. Start shared PostgreSQL with `.\tools\postgres-dev.ps1 ensure` before lanes
   that use `localhost:5435`.
4. When a maintained artifact needs updating, run its named owning maintenance
   command and review the changes it produces. `ci --apply` configures the
   repository hook and runs selected checks; it does not refresh generated
   metadata. Stage the intended tree, then let the normal check-only hook
   validate it.
5. Use evidence-based result verification to report focused and canonical proof
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

For hook setup, run `ci --apply` explicitly. Run the named owning command when
refreshing a particular maintained artifact. The pre-commit hook invokes only
`ci --check`; it does not apply changes or stage corrections. Hook custody is
defined in [repo runbook policy](../doctrine/repo-runbook-policy.md).

Use `--diagnostics` only to collect independent failures. Persistence changes also run `dotnet tool
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
- Do not treat `ci --apply` as validation proof; use `ci --check` and the
  check-only commit hook.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
- [Code review](../runbooks/code-review.md)
- [Pull request](../runbooks/pr.md)
- [Decision records](decision-records.md) - when an invariant, test boundary, or validation lane becomes a durable repository rule.
