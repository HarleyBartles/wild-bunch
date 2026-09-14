# Testing runbook

Use `/test-driven-development` for change construction and
`/verification-before-completion` before a passing or complete claim.

## Canonical gate

```powershell
.\tools\postgres-dev.ps1 ensure
py -3 tools\run.py ci --check
```

Use `ci --apply` when authored agent surfaces or generated projections changed;
use `--diagnostics` only to collect independent failures. A normal hooked
commit checks the staged snapshot.

For persistence changes, also restore tools and run `dotnet ef migrations list`
with `src\WildBunch.Persistence` as project and `src\WildBunch.Api` as startup
project. Direct PostgreSQL-backed commands use the shared service at
`localhost:5435`; a refusal is environment evidence to check with
`postgres-dev.ps1 status|ensure`.

See [validation doctrine](../doctrine/validation-policy.md) for lane ownership
and repository-specific test invariants.
