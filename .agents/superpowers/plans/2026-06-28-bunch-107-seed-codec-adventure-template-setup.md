# BUNCH-107: Refactor Seed Codec into SeedWorld Setup

**Status:** Implemented. PR #121 contains the full implementation.
**Resolver contract version:** `resolver-v4`
**Branch:** `harleydbartles/bunch-107-preflight`

## Goal

Refactor the seed codec and game-setup seams so the seed produces a stable world/map template with seed-derived town selection, not final player pressure settings or direct runtime truth. This creates a clean foundation for BUNCH-94 (difficulty controls) and BUNCH-93 (entropy controls) without side-questing into seed codec repairs.

## Implemented Pipeline

```
seed code -> SeedWorld -> DifficultyEnvelope -> EntropyPolicy
-> MysteryTruthResolution -> ResolvedGameSetup -> GameSession
```

With `StartingTownPolicy` as the setup/policy seam between world generation and the final starting town.

## Implemented Seams

### SeedWorld (seed-owned)

`SeedWorld` is a record decoded from the UUID seed code by `SeedWorldResolver.Resolve(Guid)`:

- `SeedCode` (Guid) — the UUID itself
- `WorldVariant` (SeedWorldVariant) — seed-decoded (Canonical/Frontier/Rail)
- `SelectedTownIds` (IReadOnlyList<string>) — which towns are in this seed world
- `Trails` (IReadOnlyList<SeedWorldTrail>) — trail graph with baseline terrain/water/distance
- `AccusationIndex` (int) — seed-decoded default opening accusation
- `DefaultCulpritIndex` (int) — seed-decoded default culprit for Boring replay
- `CashBonus` (int) — raw seed-derived cash bonus (0–8, NOT entropy-capped)

`SeedWorldTrail` is a record holding: trail id, from/to town IDs, risk, terrain, water feature, and ride-day distance. The seed owns the default terrain and trail distances. Later difficulty can modify those values downstream of the seed codec.

### Seed-Derived Town Selection

The seed deterministically derives the world map from the full town catalog (`SeedWorldCatalog.AllTowns` — 8 towns, `SeedWorldCatalog.AllTrails` — 9 trails):

- **Town count**: seed-derived, range 6-8 (minimum 6 for playability, maximum 8 = full catalog).
- **Town selection**: anchor towns (pinecross, redmesa, holloway) are always included to guarantee trail graph connectivity. The remaining towns are seed-selected from the catalog using a deterministic Fisher-Yates-like shuffle.
- **Trail graph**: catalog trails where both endpoints are in the selected town set. Terrain/water/distance come from the catalog indexed by world variant.

This is NOT a pair of canned named sets. Different seeds produce different town counts, different selected towns, and different trail graphs. Same seed + same difficulty produces the same resolved map.

### StartingTownPolicy (player/setup-owned)

- Validates player's chosen starting town exists in the generated world
- If no town supplied, uses safe default (pinecross — a fixed property of the world catalog, NOT a seed-authored selector)
- Future seam: difficulty may constrain eligible starting towns; accusation/black-spot town may become non-stoppable. Difficulty should not redraw the map — it only filters eligibility.

### DifficultyEnvelope (player-selected pressure)

- `GameDifficulty` (enum)
- `BaseCash` (decimal) — difficulty-owned base starting cash
- `StartWithHorse` (bool) — difficulty-owned horse posture (transitional: all difficulties get true)
- `IncludeSaddle` (bool) — difficulty-owned saddle (transitional: all difficulties get true)
- `LoadoutProfile` (enum) — difficulty-owned loadout (transitional: all difficulties get Standard)
- `StartingHealth` (int) — difficulty-owned starting health
- `TravelRulesProfile` (TravelRulesProfile) — difficulty-owned travel rules
- BUNCH-94 will expand this seam.

### EntropyPolicy (player-selected entropy/salt mode)

- `GameEntropy` (enum)
- `SaltSourceMode` (SaltSourceMode) — Fixed for Boring, Runtime for others
- `CashBonusCap` (int) — max seed cash bonus applied (Boring=0, Classic=2, Adventurous=5, Wild=8)
- BUNCH-93 will expand this seam.

### MysteryTruthResolution (entropy-applied mystery-truth seam)

- `ResolvedCulpritIndex` (int) — final culprit index after entropy policy is applied
- `ResolvedAccusationIndex` (int) — final opening accusation index after entropy policy is applied
- `SaltSource` (SaltSource) — the salt source resolved from entropy policy
- `AppliedCashBonus` (int) — seed cash bonus after entropy cap is applied
- This seam is the **single extension point** for BUNCH-93. Transitional behavior: all entropy modes pass through the seed world defaults. BUNCH-93 will expand this seam to add salted culprit reroll, feature reallocation, and Adventurous/Wild variance — without touching `SeedWorld`, `SeedWorldResolver`, or the seed codec.

### ResolvedGameSetup (final session-start facts)

- `SeedWorld` — the seed-owned template (retained for reproducibility)
- `GameDifficulty` — player-selected
- `GameEntropy` — player-selected
- `World` (World) — resolved world domain object
- `StartingTownId` (TownId) — resolved starting town
- `CaseFile` (CaseFile) — resolved case file with final culprit from `MysteryTruthResolution`
- `StartingWallet` (Wallet) — final wallet
- `StartingInventory` (Inventory) — final inventory
- `StartingHealth` (int) — final starting health
- `TravelRulesProfile` (TravelRulesProfile) — from difficulty
- `SaltSource` (SaltSource) — from `MysteryTruthResolution`
- `SeedCodeText` (string) — for debugging/reproducibility

## Design Boundaries

- Starting town is NOT seed-owned. `StartingTownPolicy` validates the player's start choice against the generated world.
- `SeedWorld` owns the candidate/generated map: which towns are selected and the trail graph between them with default terrain/water/distance.
- Same seed + same difficulty should produce the same resolved map.
- Difficulty may later influence map pressure/layout realization (distance bands, terrain harshness, connectivity constraints) downstream of the seed codec, not by hiding difficulty inside the seed.
- Longer term, `SeedWorld + DifficultyEnvelope` may produce the final resolved world/map, while `StartingTownPolicy` validates the player's start choice against that world.

## Validation Evidence

- `dotnet build` — passes
- `dotnet test` — 804 tests pass (395 domain + 180 application + 74 game-content + 155 integration)
- `dotnet ef migrations list` — 8 migrations, no new migration needed
- `python scripts/generate_index_mesh.py --check` — 97 indexes current
- No `TownSetKey`, `WorldTownSetDefault`, `WorldTownSetAlternate`, or `AdventureTemplate` in production source
- No `GameDifficulty`/`GameEntropy` in `SeedWorld`/`SeedWorldResolver`
- `GameSetupResolver` calls `MysteryTruthResolver.Resolve` as an explicit step
- `GameSetupResolver` calls `StartingTownPolicy.ResolveStartingTown` as an explicit step

## Test Proofs

- `DifferentSeedsCanProduceDifferentTownCounts` — different fixed seeds produce different town counts (6-8)
- `DifferentSeedsCanProduceDifferentTownSelections` — different fixed seeds produce different town selections
- `DifferentSeedsCanProduceDifferentTrailSignatures` — different fixed seeds produce different trail graphs
- `SameSeedProducesSameSeedWorld` / `SameSeedProducesSameWorld` — same fixed seed is stable
- `ResolverAlwaysIncludesAnchorTowns` — anchor towns always present
- `CanonicalSeedWorldHasAllEightTowns` — canonical seed world has all 8 towns and 9 trails
- `SelectedStartingTownMustBeInGeneratedWorld` — starting town must be in generated world, not seed-authored
