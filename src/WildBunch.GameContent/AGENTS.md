# WildBunch.GameContent AGENTS.md

This project contains the UUID seed codec and the game-setup pipeline.

## Pipeline

The game-setup pipeline is:

```
seed code -> SeedWorld -> DifficultyEnvelope -> EntropyPolicy
-> MysteryTruthResolution -> ResolvedGameSetup -> GameSession
```

- `SeedWorldResolver.Resolve(Guid)` — UUID → `SeedWorld` (seed-owned world/map layer)
- `SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorld)` — `SeedWorld` → UUID via direct bit-packing (O(1), 24 bits used, 104 reserved)
- `DifficultyEnvelope.For(GameDifficulty)` — player-selected difficulty → pressure-owned envelope (cash, loadout, horse/saddle, travel rules)
- `EntropyPolicy.For(GameEntropy)` — player-selected entropy → entropy policy (salt mode, cash bonus cap)
- `MysteryTruthResolver.Resolve(SeedWorld, EntropyPolicy)` — entropy-applied mystery truth (culprit index, accusation index, salt source).
- `GameSetupResolver.Resolve(...)` — orchestrates the full pipeline, produces `ResolvedGameSetup`
- `StartingTownPolicy.ResolveStartingTown(World, TownId?)` — validates player's chosen starting town against the generated world; provides safe default (slot-0 town of the derived world) if none supplied

## Seed-Owned vs Pressure-Owned

- **Seed-owned** (`SeedWorld`): world variant, selected town IDs, trail graph (with baseline terrain/water/distance), accusation/default culprit candidates, cash bonus. The seed owns the map.
- **Pressure-owned** (`DifficultyEnvelope`): difficulty, starting cash, loadout profile, horse/saddle posture, travel rules profile.
- **Entropy-owned** (`EntropyPolicy` + `MysteryTruthResolver`): salt mode, cash bonus cap, and (future) culprit reroll/feature reallocation. See [`.agents/docs/entropy-and-seed-policy.md`](../../.agents/docs/entropy-and-seed-policy.md) for the full entropy ladder and boundary.
- **Player/setup-owned** (`StartingTownPolicy`): starting town choice. The player can start in any town that exists in the generated world. The seed does NOT choose the starting town.

## Seed-Derived Town Selection

The seed deterministically derives the world map from a 40-entry town-name pool:
- **Town count**: seed-derived, range 5-10 (minimum 5 for playability, maximum 10). The 4-bit field (0-15) is offset-encoded (0-15 → 5-20) and wrapped to 5-10 via modulo for v11.
- **Town selection**: slot-based derivation via xorshift shuffle over the 40-entry name pool. No anchor towns — every town slot is seed-derived.
- **Prosperity/services**: encoded as 3-bit palettes (8 patterns each) applied positionally to the selected towns, wrapped via modulo. Telegraph is encoded separately. Sheriff, saloon, and noticeboard are always present.
- **Trail graph**: slot-based topology guarantees connectivity for any town count in the 5-10 range. Terrain/water/distance come from the catalog indexed by world variant.
- `SeedWorld` holds `SelectedTownIds` and `Trails` (list of `SeedWorldTrail` with terrain/water/distance). The seed owns the default terrain and trail distances. Later difficulty can modify those values downstream of the seed codec.

Design boundary:
- SeedWorld owns the candidate/generated map.
- Same seed + same difficulty should produce the same resolved map.
- Difficulty may later influence map pressure/layout realization (distance bands, terrain harshness, connectivity constraints) downstream of the seed codec, not by hiding difficulty inside the seed.
- Longer term, `SeedWorld + DifficultyEnvelope` may produce the final resolved world/map, while `StartingTownPolicy` validates the player's start choice against that world.

## Starting Town

The starting town is NOT a seed-owned fact. It is a player setup choice validated by `StartingTownPolicy`:
- The player can start in any town that exists in the generated world.
- If no starting town is supplied, the safe default is the slot-0 town of the derived world (the first town produced by the seed's xorshift shuffle), not a fixed catalog property.
- Future seam: difficulty may constrain eligible starting towns (easy allows any except accusation town, standard prefers inner/well-connected towns, harder constrains to outposts). An accusation/black-spot town may become non-stoppable. Difficulty should not redraw the map — it only filters eligibility.

## When to update this project

- **New town or trail**: add to `SeedWorldCatalog.cs`, update `SeedWorldBuilderTests` snapshot assertions, update `SeededNewGameFactoryTests` count assertions.
- **New world variant**: add to `SeedWorldVariant` enum, add variant-specific terrain/water/services to existing town/trail definitions, update `ResolveWorldVariant` in `SeedWorldResolver.cs`, update snapshot tests.
- **New difficulty or entropy level**: update enums, update `DifficultyEnvelope.For` / `EntropyPolicy.For`, update tests.
- **Any new seed-owned field**: add to `SeedWorld`, add to `SeedWorldResolver.Resolve`, update the bit-packing layout in `SeedWorldResolver.CreateRepresentativeSeedCode`, add a round-trip guardrail test.
- **Any new pressure-owned field**: add to `DifficultyEnvelope.For`, update `GameSetupResolver.Resolve`, update tests.

## Do NOT

- Do NOT store UUIDs in test fixtures. Store `SeedWorld` records and derive UUIDs via `SeedWorldResolver.CreateRepresentativeSeedCode`.
- Do NOT bypass the seed system for encounter/journey tests. Use the seed system + `SeededNewGameFactory`.
- Do NOT add compatibility shims for old UUIDs when the codec changes. In this greenfield repo, current codec correctness wins.
- Do NOT mark gang members as `IsTrueCulpritEligible: false`. The culprit is always a gang member, and any gang member can be the culprit.
- Do NOT make the starting town a seed-owned fact. The seed owns the map; the player/setup policy owns the starting town choice.
