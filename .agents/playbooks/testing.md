# Testing playbook

## When

Adding, changing, or running Wild Bunch tests and validation gates.

## Required capabilities

- Construct observable behavior through focused, behavior-first tests.
- Verify claims against current repository and hosted evidence.

## Unslop before work
Before work in this scope, follow the [unslop playbook](unslop.md) and its scoped profile selection. Consult the [decision-record playbook](decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Composition

1. Use the owning capability and validation doctrine to select the smallest
   lane that observes the changed behavior.
2. Use behavior-focused test development for the focused red-green cycle; add real
   PostgreSQL or browser evidence only when the boundary requires it.
3. Start shared PostgreSQL with `.\tools\postgres-dev.ps1 ensure` before lanes
   that use `localhost:5435`.
4. When a maintained artifact needs updating, run its named owning maintenance
   command and review the changes it produces. `setup-hooks --apply` configures
   this checkout's Git hook; it is separate from validation. Stage the intended
   tree, then let the normal check-only hook validate it.
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

For hook setup, run `py -3 tools/run.py setup-hooks --apply` explicitly. Use
`py -3 tools/run.py ci --check --diagnostics` only for manual troubleshooting; the pre-commit hook invokes fail-fast `ci --check`, ordered from cheaper to more expensive checks, and never applies repairs or stages corrections. Hook custody is defined in [repo runbook policy](../doctrine/repo-runbook-policy.md).

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
- Do not treat `setup-hooks --apply` as validation proof; use `ci --check` and the
  check-only commit hook.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
- [Code review](../runbooks/code-review.md)
- [Pull request](../runbooks/pr.md)
- [Decision records](decision-records.md) - when an invariant, test boundary, or validation lane becomes a durable repository rule.
