# BUNCH-69 Implementation Record

**Issue:** BUNCH-69

**Branch:** `codex/bunch-69-extract-bounty-declaration-matching-as-a-domain-policy`

**Starting `main` SHA:** `5eb0e9ae42848b530de5a897eb5bafb3afe2bfc1`

**Final head SHA:** `63637b46c9f28475e6e59aa765249a2aeb9d714f`

**PR URL:** [https://github.com/HarleyBartles/wild-bunch/pull/87](https://github.com/HarleyBartles/wild-bunch/pull/87)

## Exact files changed

- `docs/superpowers/plans/2026-06-16-bunch-69-extract-bounty-declaration-matching.md`
- `docs/superpowers/records/2026-06-16-bunch-69-extract-bounty-declaration-matching.md`
- `src/WildBunch.Domain/Cases/BountyDeclarationMatchPolicy.cs`
- `src/WildBunch.Domain/Game/GameSession.cs`
- `tests/WildBunch.Domain.Tests/BountyDeclarationMatchPolicyTests.cs`

## Generated artifacts

- None

## Validation

- `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~BountyDeclarationMatchPolicyTests` -> passed
- `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~GameSessionSaloonPersonOfInterestTests` -> passed
- `dotnet build` -> passed with existing CA2264 warnings in `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- `powershell -ExecutionPolicy Bypass -File .\scripts\postgres-dev.ps1 validate` -> passed
- `powershell -ExecutionPolicy Bypass -File .\scripts\postgres-dev.ps1 stop` -> passed
- `powershell -ExecutionPolicy Bypass -File .\scripts\postgres-dev.ps1 status` -> cluster exists but is not running on `localhost:5434`
- `Get-NetTCPConnection -LocalPort 5434 -ErrorAction SilentlyContinue` -> no results; port clear
- `git status --short --branch` -> clean worktree on the published branch

## Skipped checks

- Full direct `dotnet test` without the repo-local wrapper was treated as a wrong-route attempt once the PostgreSQL lane became available, because the repo expects the wrapper to inject `ConnectionStrings__WildBunchPostgresDb` for integration coverage.

## Surprises, deviations, follow-ups

- The repo-local PostgreSQL tooling folder was initially missing and later added, so the wrapper validation could be rerun successfully.
- The first bare `dotnet test` run failed in the integration lane because the connection string was not set in that shell. That failure is obsolete relative to the later wrapper-backed validation and should not be treated as the final status.
- BUNCH-70 still owns the fine and settlement extraction seam. This change only isolates declaration matching.
