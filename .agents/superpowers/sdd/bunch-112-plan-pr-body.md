## Summary

Plan-only PR for BUNCH-112. Completes the BUNCH-72 `BountyLoopCoordinator` stepping stone by moving all remaining bounty-loop command methods, event `Apply` handlers, dev-saloon-override commands, and saloon-POI eligibility helpers from `GameSession.cs` into `GameSession.BountyLoopCoordinator.cs`.

`GameSession` retains public command entry points (with `IsArchived`/`IsJourneyModal` guards), the `Apply` dispatch switch, and aggregate-wide helpers. The coordinator is a nested `internal sealed` class — aggregate-owned internal cohesion, not aggregate bypass (ADR-0002, ADR-0020, `.agents/unslop/backend-architecture.md` "Aggregate bypass" rule).

No public API, DTO, event payload, message string, persistence shape, or behavior changes. Pure mechanical move + visibility adjustment.

## Route state

- **Mode:** `preflight_complete_pending_approval`
- **Plan path:** `.agents/superpowers/plans/2026-06-30-bunch-112-extract-bountyloop-domain-service.md`
- **Plan PR:** this PR
- **Status:** plan ready for approval; implementation halted pending review

## Architecture decision note

The issue title says "domain service." A standalone `BountyLoopService` outside `GameSession` would violate ADR-0002 (GameSession is the command aggregate root), ADR-0020 (aggregate authority), and the unslop "Aggregate bypass" rule. The plan instead completes the existing nested `BountyLoopCoordinator` extraction — the issue explicitly names it as the stepping stone. This keeps mutation flowing through the aggregate root while consolidating bounty-loop logic in one cohesive file.

## Validation

Plan-only. No source changes in this PR. Implementation tasks (2–7) include per-task `dotnet build` + filtered `dotnet test` gates and a final full-suite `.\scripts\postgres-dev.ps1 validate` run.

Generated with [Devin](https://devin.ai)
