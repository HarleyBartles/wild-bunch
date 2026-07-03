# Task 9 Report: Unrelated criminal parity system (runtime)

**Branch:** `harleydbartles/bunch-107-preflight`
**Commit:** `d64712e` — feat: add unrelated criminal parity system with spawn/despawn rules
**Issue:** BUNCH-107

## What I implemented

### `UnrelatedCriminalLedger` (`src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs`)
A serializable domain concept that tracks the active pool of unrelated wanted criminals and maintains parity with the number of gang members still available to surface.

**API:**
- `UnrelatedCriminalLedger(int gangMemberCount, int poolSize)` — synthetic roster (`criminal-{i}`), for tests.
- `UnrelatedCriminalLedger(int gangMemberCount, IReadOnlyList<WarrantId> roster)` — real warrant IDs from the case file.
- `ActiveCriminalCount`, `GangMembersAvailable`, `PoolSize`, `ActiveCriminalIds`, `TakenInCriminalIds`, `WarrantCollectedIds`, `RetiredWarrantIds`, `IsSurfacingEligible(WarrantId)`.
- `RecordTakenIn(WarrantId)` — removes an unrelated criminal from the active pool and spawns a replacement from the unused roster when `active < gangAvailable` and the roster is not exhausted. Returns the spawned ID (or null).
- `MarkWarrantCollected(WarrantId)` — tracks that the player collected a warrant (retained preferentially during despawn).
- `Despawn(int count)` — despawns up to `count` active criminals, preferring uncollected ones; retires their warrants from the surfacing pool. Returns despawned IDs.
- `RecordGangMemberTakenIn()` — drops the parity target by one and despawns excess unrelated criminals to match. Clamps at zero.
- `ToSnapshot()` / `FromSnapshot(UnrelatedCriminalLedgerSnapshot)` — JSON-friendly snapshot (record with `WarrantId` arrays + ints).

**Rules enforced (per issue spec):**
- Active pool starts at gang-member parity (7 for 7 gang).
- Spawn-on-take-in only when it would not exceed the parity target.
- Despawn-on-gang-take-in to maintain parity, preferring uncollected warrants.
- Despawning retires the warrant from the surfacing pool (`IsSurfacingEligible` → false).
- Roster must be ≥ 3× gang size (21 for 7) — validated at construction.

### GameSession wiring (`src/WildBunch.Domain/Game/GameSession.cs`)
- New `private readonly UnrelatedCriminalLedger _unrelatedCriminalLedger` field, built in the constructor via `BuildUnrelatedCriminalLedger(caseFile)`.
- `BuildUnrelatedCriminalLedger` derives the roster from `CaseFile.PublicWarrants` (unrelated-criminal warrants) and gang count from `CaseFile.Suspects.Count`, then **replays persisted gang take-ins from `CaseFile.SheriffTurnInSettlements`** so the ledger's gang-side parity matches the persisted state on snapshot load. Partial-roster test fixtures (below the 3x invariant) get a safe no-op ledger (gang count 0).
- `Apply(SheriffTurnInSettled)` now calls `_unrelatedCriminalLedger.RecordGangMemberTakenIn()` — the gang-side parity path. Because GameSession is event-sourced, this reconstructs correctly on full-event-replay loads and advances correctly for post-snapshot events.
- Public read-only `UnrelatedCriminalLedger` property exposed for resolvers/tests.

No constructor signature change → no changes to `GameSessionRehydrator` or the two call sites (StartNew, RehydrateFromEvents).

## What I tested and test results

### `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs` (22 tests)
Unit tests for the ledger in isolation: parity start, 3x invariant validation (throws/accepts), negative-count guards, real-roster constructor + duplicate detection, spawn-on-take-in, repeated-spawn-until-pool-exhausted, non-active-id tolerance, taken-in tracking, warrant-collection tracking, despawn-prefers-uncollected, despawn-can-still-despawn-collected, despawn-retires-warrant, despawn-clamps-to-active, despawn-negative-throws, gang-take-in-despawns-excess, gang-take-in-no-despawn-when-below-parity, gang-take-in-clamps-at-zero, surfacing-eligibility, snapshot round-trip, FromSnapshot-null-throws.

### `tests/WildBunch.Domain.Tests/GameSessionUnrelatedCriminalLedgerWiringTests.cs` (4 tests)
Wiring tests through the real GameSession aggregate: full-roster builds ledger at parity; partial roster builds degenerate no-op ledger; settling a gang turn-in (full saloon→confront→sheriff flow) drops parity and despawns excess; **snapshot-reconstruction** — a fresh session built from a case file that already records a gang turn-in settlement reconstructs the ledger's gang-side parity from the persisted settlements.

### Results
- `dotnet test tests/WildBunch.Domain.Tests` → **Passed: 443, Failed: 0, Skipped: 0** (includes 22 ledger + 4 wiring = 26 new tests).
- `dotnet test tests/WildBunch.Application.Tests` → **Passed: 181, Failed: 0**.
- `dotnet test tests/WildBunch.GameContent.Tests` → **Passed: 83, Failed: 0**.
- `dotnet build src/WildBunch.Persistence --no-dependencies` → **0 errors**.
- `tests/WildBunch.Integration.Tests` could not build because a pre-existing `WildBunch.Api` dev server (PID 29680) locks the output DLLs (MSB3027 file-lock). This is an environment issue, not a code regression — the Integration tests transitively build `WildBunch.Api`, whose bin output is locked by the running process. I did not kill the dev server (shared dev process). The snapshot-reconstruction wiring test covers the persistence-relevant behavior at the domain level.

## TDD Evidence (RED + GREEN)
- **RED:** Wrote the test file first; ran `dotnet test --filter UnrelatedCriminalLedgerTests` → `error CS0246: The type or namespace name 'UnrelatedCriminalLedger' could not be found` (build failed, type not found). Confirmed tests fail for the right reason.
- **GREEN:** Implemented `UnrelatedCriminalLedger`; re-ran → `Passed: 22, Failed: 0`. Then added wiring tests alongside the GameSession hook → `Passed: 4, Failed: 0`.

## Files changed
- **Created:** `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs`
- **Created:** `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs`
- **Created:** `tests/WildBunch.Domain.Tests/GameSessionUnrelatedCriminalLedgerWiringTests.cs`
- **Modified:** `src/WildBunch.Domain/Game/GameSession.cs` (field, constructor init, `Apply(SheriffTurnInSettled)` hook, public property, `BuildUnrelatedCriminalLedger` helper)
- **Regenerated:** `src/WildBunch.Domain/Cases/INDEX.md`, `tests/WildBunch.Domain.Tests/INDEX.md` (index mesh; `python scripts/generate_index_mesh.py --check` → OK)

## Self-review findings
- **Completeness:** Ledger parity/respawn/despawn logic fully implemented and tested. Gang-side GameSession wiring implemented and tested (including snapshot reconstruction). Unrelated-criminal turn-in side and resolver filtering deferred (see Concerns).
- **Quality:** XML docs on all public members; validation guards (negative counts, duplicates, 3x invariant); serializable snapshot record; no constructor-signature change to GameSession (no blast radius to rehydrator/call sites).
- **YAGNI:** Did not wire the unrelated-criminal turn-in flow (no such flow exists yet — turn-in is SuspectId/gang-based) or modify WantedPosterResolver. The ledger exposes the seams (`IsSurfacingEligible`, `ActiveCriminalIds`, `RecordTakenIn`, `MarkWarrantCollected`) for those follow-ups.
- **Testing discipline:** Real behavior, no mocks. Wiring tests drive the full saloon→confront→sheriff aggregate flow and a snapshot-reconstruction scenario.
- **Brief test-2 note:** The brief's illustrative `TakingInCriminal_DoesNotSpawn_WhenAtGangParity` example appeared internally inconsistent (after 7 take-ins with spawns, an 8th take-in of an active criminal would still spawn back to 7 under the stated rule "spawn only if it wouldn't exceed gang count", yet the example asserted 6). I implemented per the spec's stated rule and instead tested the pool-exhaustion case (`TakingInCriminal_RepeatedlySpawnsUntilUnusedPoolExhausted` + `TakingInCriminal_OfNonActiveId_IsTolerantNoOpAndDoesNotSpawn`) to cover the no-spawn behavior faithfully.

## Concerns
1. **Unrelated-criminal turn-in flow not wired.** The existing sheriff turn-in flow is SuspectId-based (gang members only); unrelated criminals are surfaced as warrants but have no turn-in path yet. `RecordTakenIn`/`MarkWarrantCollected` are tested in isolation but not yet driven by GameSession. This is the deferred wiring; the ledger is ready for it.
2. **WantedPosterResolver does not yet filter by the ledger's active set.** The ledger exposes `IsSurfacingEligible`/`ActiveCriminalIds`, but the resolver still surfaces from all `CaseFile.PublicWarrants` not in `KnownWarrants`. Filtering the resolver against the ledger's active set is a separate, follow-up wiring step.
3. **Integration tests blocked by environment.** A running `WildBunch.Api` dev server (PID 29680) locked the output DLLs, preventing `WildBunch.Integration.Tests` from building. Domain/Application/GameContent tests and the Persistence build all pass. The snapshot-reconstruction behavior is covered by a domain-level wiring test.
4. **Unrelated-side state not persisted across snapshots.** The gang-side parity reconstructs from `CaseFile.SheriffTurnInSettlements` (a persisted component). The unrelated-side state (taken-in/collected/retired IDs) is not yet persisted because those flows are not wired; once they are, the ledger should become a persisted component (like `WantedSuspectPresenceLedger`) or reconstruct from new persisted state. The `ToSnapshot`/`FromSnapshot` API is in place for that.


---

# Task 9 Review Fix Report

## What I fixed

### Fix 1: Add JSON serialization round-trip test (Important)
Added `Snapshot_SurvivesJsonSerializationRoundTrip` test in `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs`. It serializes a populated `UnrelatedCriminalLedgerSnapshot` via `System.Text.Json.JsonSerializer`, deserializes it back, reconstructs the ledger via `FromSnapshot`, and asserts `ActiveCriminalCount`, `GangMembersAvailable`, and `ActiveCriminalIds` match. Added `using System.Text.Json;` to the test file.

### Fix 2: Gate TrySpawnReplacement behind wasActive check (latent bug)
In `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs`, `RecordTakenIn` now only calls `TrySpawnReplacement()` when the criminal ID was actually active (removed). Previously `TrySpawnReplacement()` was called unconditionally, so a take-in of a non-active ID could still spawn a replacement when `active < gangAvailable`, opening a phantom slot. Now a non-active take-in returns `null` without spawning.

## Test results (command run + output)

- `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~UnrelatedCriminalLedgerTests"` -> Passed: 23, Failed: 0, Skipped: 0
- `dotnet test tests/WildBunch.Domain.Tests` -> Passed: 444, Failed: 0, Skipped: 0

## Files changed
- Modified: `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs` (gated TrySpawnReplacement behind wasActive)
- Modified: `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs` (added JSON round-trip test + using directive)
