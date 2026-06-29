## BUNCH-107 Preflight: Code Review Fixes + Approved Scope Expansions

### Summary

All critical issues from the code review have been fixed, both approved scope expansions implemented, and minor issues addressed. The branch is now GREEN with full validation.

### Critical Fixes

1. **ISaltSourceFactory bypass (Critical #1)** — `SeededNewGameFactory` was injected with `ISaltSourceFactory` but never used. Salt was always `SaltSource.CreateFixed(seedCodeText)` regardless of entropy mode. Fixed by routing the factory through `GameSetupResolver` → `MysteryTruthResolver`. Non-Boring entropy now uses `RuntimeSaltSourceFactory` in production and `DeterministicSaltSourceFactory` in tests. Failing test written first, then fix applied.

2. **UnrelatedCriminalLedger not persisted (Critical #2)** — The ledger had `ToSnapshot()`/`FromSnapshot()` but they were never called by the persistence layer. On reload, the ledger was reconstructed from a shrinking `PublicWarrants` pool, degrading the roster below the 3x redundancy invariant. Fixed by:
   - Adding a new PostgreSQL component (`unrelatedCriminalLedger`)
   - Adding the ledger to `GameSessionSnapshot` for the JSON snapshot path
   - Adding `GameSessionRehydrator.SetUnrelatedCriminalLedger` to overwrite the constructor's case-file-derived ledger
   - JSON snapshot and PostgreSQL round-trip tests verify gang parity, active count, retired IDs, and collected IDs survive reload

### Approved Scope Expansions

1. **WantedPosterResolver active-set filtering** — Retired/despawned unrelated criminals could still surface on wanted posters after a gang take-in. Fixed by adding an optional `IReadOnlySet<WarrantId>? retiredWarrantIds` parameter to `WantedPosterResolver.Resolve`. `GameSession.ReadWantedPosters` now passes the ledger's `RetiredWarrantIds` and `TakenInCriminalIds` as a combined set.

2. **Unrelated criminal sheriff turn-in** — Players can now turn in unrelated wanted criminals by declaring the warrant (collected from a wanted poster). The sheriff pays the bounty, the ledger records the take-in (spawning a replacement when parity allows), and the warrant is marked as collected. New `UnrelatedCriminalTurnInSettled` event (carries `WarrantId`, not `SuspectId`). New `GameSession.SettleUnrelatedCriminalTurnIn(WarrantId, isAlive)` method with full validation. Event registered in all three switch statements and the persistence event serializer.

### Minor Fixes

- **Stale comments**: Updated GameSession.cs comments that said the unrelated-criminal turn-in flow was "once wired" / "as those flows land" — the flow is now wired.
- **File rename**: `GameSetupSeedCodeValidator.cs` → `StartingWorldDescriptorCodeValidator.cs` (file name now matches class name).
- **xorshift seed=0**: `SeedWorldCatalog.DeriveTownNames` used xorshift32 for the Fisher-Yates shuffle. xorshift32 has 0 as a fixed point. Added a guard: `if seed == 0, seed = 1`. Added a boundary test.

### Doctrine Repairs (committed earlier)

- `AGENTS.md` and `src/WildBunch.GameContent/AGENTS.md` updated with seed codec doctrine.
- `ClueSurfacingResolver.cs` negative modulo bug fix.

### Validation Evidence

- **Build**: `dotnet build` — 0 errors, 5 warnings (pre-existing)
- **Domain.Tests**: 450 passed, 0 failed
- **GameContent.Tests**: 85 passed, 0 failed
- **Application.Tests**: 181 passed, 0 failed
- **Integration.Tests** (PostgreSQL): 157 passed, 0 failed
- **Total**: 873 tests, 0 failures
- **EF migrations**: `dotnet ef migrations list` — 8 migrations, all apply cleanly
- **Index mesh**: `python scripts/generate_index_mesh.py --check` — OK, 98 indexes current
- **Worktree**: clean (no uncommitted changes)

### Commits (this session)

1. `a0a517e` fix: update seed codec doctrine and guard ClueSurfacingResolver negative modulo
2. `7ff0e3e` fix: route ISaltSourceFactory through the setup pipeline for deterministic salt
3. `5e3527d` fix: persist UnrelatedCriminalLedger in JSON snapshot and PostgreSQL component
4. `61c23e3` feat: filter retired warrants from WantedPosterResolver via UnrelatedCriminalLedger active set
5. `390f216` feat: add unrelated criminal sheriff turn-in (by warrant, pay bounty, wire ledger)
6. `0564098` fix: minor code review items (stale comments, file rename, xorshift seed=0 guard)
7. `1b21dad` chore: regenerate index mesh for new files

#### Test plan
- [x] `dotnet build` — 0 errors
- [x] `dotnet test` (all projects) — 873 passed, 0 failed
- [x] `.\scripts\postgres-dev.ps1 validate` — EF migrations + all tests pass
- [x] `python scripts/generate_index_mesh.py --check` — OK
- [x] Worktree clean

Generated with [Devin](https://devin.ai)
