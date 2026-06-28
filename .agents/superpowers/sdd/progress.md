# BUNCH-75 SDD Progress Ledger

Branch: `harleydbartles/bunch-75-phaser-map-poc-exec`
Worktree: `.worktrees/bunch-75-exec`
Base (origin/main): `76d7e46` BUNCH-103: repair plan Task 9/10 snippets and check off Task 10 (#115)
Plan: `.agents/superpowers/plans/2026-06-27-bunch-75-phaser-backed-world-map-poc-for-starting-town-selection-and-trail-awareness.md`

## Plan-path repairs (in-branch, within approved scope)
- Task 0 Step 1 references `docs/superpowers/plans/2026-06-27-bunch-102-...md`; plans live under `.agents/superpowers/plans/`. Fix path reference when checking off Task 0.
- Task 2 references `tests/WildBunch.Api.Tests/Games/GameSessionEndpointsTests.cs`; no `WildBunch.Api.Tests` project exists. Endpoint tests go in `WildBunch.Integration.Tests` (which already has `GameApiTests.cs` + `PostgreSqlApiFactory`). Do not invent a new test project.

## Task status
- Task 0: Revalidate the BUNCH-102 seam, then compose with it — DONE by controller (gate open, seams verified on `76d7e46`)
- Task 1: Extend the BUNCH-102 setup read model for map coordinates — complete
- Task 2: Reuse the BUNCH-102 setup endpoint and expose map-ready data — complete
- Task 3: Replace the BUNCH-102 `StartingTownStep` body with a Phaser-backed map host — pending
- Task 4: Prove React owns the final confirmation and game creation — pending
- Task 5: Validate the slice and capture browser proof — pending (controller-owned validation)

## Completion log
Task 0: complete (controller-verified on `76d7e46`; BUNCH-102 seams present: StartingTownStep.tsx, PreSessionSurface.tsx handleStartWithTown, GET /api/games/starting-towns, StartGameRequest.StartingTownId)
Task 1: complete (commits 76d7e46..65cc149, review clean — Approved, 3 Minor notes only: CreateWorld rebuilt per call, missing-key throws, unused cancellationToken. All non-blocking.)
Task 2: complete (commits 65cc149..2464ef9, review found 1 Important ordering bug in integration test + 1 Minor trail-count; fixup commit 2464ef9 resolved both. Integration tests need PostgreSQL lane — controller to run in Task 5.)
