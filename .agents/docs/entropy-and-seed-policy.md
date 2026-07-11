# Entropy and Seed/Test Policy

This document is durable agent-facing guidance for work touching game setup, entropy, travel variance, seeded setup, dev-overlay controls, or tests that need deterministic game scenarios. It is the companion to the entropy ladder implemented in BUNCH-93 and the seed codec policy in `.agents/docs/architecture-guardrails.md`.

## Entropy Ladder

Game entropy is a player-selected axis that controls determinism, randomness, variance, and future rule-bending. It is distinct from difficulty, which controls pressure, lethality, and resource harshness.

| Entropy | Salt Mode | Semantic |
|---------|-----------|----------|
| **Boring** | `SaltSourceMode.Fixed` | Near-deterministic. No salt rolls where entropy controls the decision. Same stable route/session inputs produce the same result. Boring is not "fewer encounters" as its core meaning — it is "no salt-driven surprises." |
| **Classic** | `SaltSourceMode.Runtime` | Standard gameplay entropy. Salted rolls behave as the normal intended game mode. This is the baseline. |
| **Adventurous** | `SaltSourceMode.Runtime` | Bigger luck swings in both directions. Luck can go better or worse than Classic, but this must not become difficulty pressure. |
| **Wild** | `SaltSourceMode.Runtime` | Future rule-bending mode. In BUNCH-93 it is only minimally represented as high volatility (more lucky/unlucky/environmental/Npc, less quiet). Later Wild may allow explicit, named rule bends such as POIs appearing misleadingly similar at first glance or a lawman beating normal trail time through an in-world exception like catching a train. Do not implement vague "Wild means harder" behavior. |

### Entropy Boundary

- **Entropy owns:** determinism, randomness, variance, rule-bending.
- **Difficulty owns:** pressure, lethality, resource harshness, Foe weight.
- **Wild must not increase `Foe` weight** or otherwise behave like hidden Brutal difficulty.
- Entropy weight adjustments in `TravelDayPlanGenerator.Context.cs` may shift Lucky, Unlucky, Environmental, Npc, Quiet, and encounter-count spread. They must not shift Foe weight upward.
- Boring needs no weight adjustment — its determinism comes from `SaltSourceMode.Fixed` (no salt inserted into the seed composition).

### Where entropy lives in the codebase

- `EntropyPolicy.For(GameEntropy)` — maps entropy to salt mode + cash bonus cap (`src/WildBunch.GameContent/NewGame/EntropyPolicy.cs`)
- `TravelDayPlanGenerator.Context.cs` — entropy weight adjustments for encounter count and category weights
- `GameSession.SetDevEntropy` / `DevEntropyChanged` — dev overlay control for runtime entropy changes
- `SetupHuntStep.tsx` — player-facing entropy selection in the setup flow

## Seed/Test Policy

Tests that need deterministic game scenarios must follow these rules. Tests that violate them will break when the seed codec evolves or when entropy weight adjustments change.

### Do not

- **Do not generate random seed GUIDs inside tests.** `Guid.NewGuid()` as a seed makes tests flaky and non-reproducible.
- **Do not hard-code raw UUID seeds in tests.** Stored UUIDs are as flaky as `Random.Guid` because codec changes break them. A UUID that resolved to a specific world yesterday may resolve to a different world after a codec change.
- **Do not brute-force through many seeds or salts to find a scenario that happens to work.** This hides non-determinism behind a search loop and will eventually fail.
- **Do not test private weight-builder internals directly.** If `BuildEncounterCountWeights` or `BuildCategoryWeights` are private, test through the public generator surface or through `GameSession` behavior.

### Do

- **Use the factory plus seed codec round-trip to resolve seed UUIDs/codes.** Build a `SeedWorld` (via `SeedWorldResolver.CreateCanonicalSeedWorld()` or `TravelTestSeedCatalog` entries), then derive the UUID via `SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorld)`. When the codec evolves, the same seed world still resolves to a valid UUID.
- **Use `TravelTestSeedCatalog` for travel/journey tests.** It provides canonical seed world entries with difficulty/entropy combinations and helper methods (`CreateSession`, `FindRouteFromCurrentTown`, `ResolveDestination`) that handle the round-trip.
- **Use dev controls to force the condition under test.** Dev routes are the explicit seams for isolating specific scenarios:
  - `ForceDevSaltSource(SaltSource.CreateFixed(...))` — force a specific salt to isolate entropy weight effects from salt randomness
  - `ForceDevTravelOverride(DevTravelOverride.ForCategory(...))` — force the next encounter category
  - `ForceDevTravelOverride(DevTravelOverride.ForFoe(...))` — force a specific foe profile
  - `SetDevEntropy(...)` — change entropy at runtime
- **Prefer public `GameSession` / factory behavior for game-content tests.** Start a journey via `TravelResolver.PreviewJourney` + `GameSession.StartJourney`, advance days via `GameSession.AdvanceJourneyDay`, and inspect the resulting state. This tests the actual runtime behavior through the public surface.
- **Use `GameEntropy.Classic` (baseline) for tests that verify non-entropy behavior.** If a test verifies canteen consumption, resource tracking, or other mechanics unrelated to entropy, use Classic to avoid entropy weight adjustments or Fixed salt mode changing the deterministic plan output.

### Test location

- Entropy variance tests belong in `WildBunch.GameContent.Tests` because they need access to the seed catalog and factory (`TravelTestSeedCatalog`, `SeededNewGameFactory`, `SeedWorldResolver`).
- Domain-level tests (event-store proof, replay, falsification) belong in `WildBunch.Domain.Tests` because they test aggregate behavior without needing the seed codec.

## References

- `.agents/docs/architecture-guardrails.md` — UUID Seed Codec section (seed-owned vs pressure-owned vs entropy-owned)
- `.agents/docs/game-content-seed-pipeline.md` — game setup pipeline and seed-owned vs pressure-owned boundary
- `.agents/docs/dev-overlay-doctrine.md` — dev overlay state/action boundary
- `TravelTestSeedCatalog.cs` — canonical seed world entries for travel tests
- BUNCH-93 — entropy setup and controls implementation
