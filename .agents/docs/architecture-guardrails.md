# Architecture Guardrails

Use this reference when making architecture decisions, touching GameSession, modifying persistence, or working with seed codecs.

## Core Architecture Rules
- `GameSession` is the live-play aggregate root.
- Game mutations should flow through `GameSession` or the established aggregate route.
- Travel, journey, and encounter DTO mapping should live in `TravelMapper`; `GameSessionMapper` should delegate rather than duplicate that shape.
- Wallet and Inventory are concrete player state; avoid generic supplies.
- Hidden culprit truth remains internal.
- The culprit is always a gang member. Any gang member can be the culprit. Which one is encoded in the UUID seed. Do not mark gang members as culprit-ineligible unless they are associated characters who are not part of the gang.
- Clue, journal, and wanted-poster flows stay stable unless directly in scope.
- Horse and saddle are separate inventory concepts.
- Mounted travel requires a living/non-lame horse plus saddle.
- Travel advances one trail day at a time; do not reintroduce instant multi-day travel.
- Keep temporary cockpit/debug-shell UI light; do not spend architecture cleanup effort polishing it for its own sake.

## GameSession Child-Component Boundaries
- `GameSession` remains the session aggregate root, command entry point, event-production boundary, apply-dispatch owner, and persistence boundary. It may orchestrate cross-component behavior. It should not directly accumulate all game rules.
- Add behavior directly to `GameSession` when it coordinates across child components, owns a session-level concern (clock, pursuit, player, world, case file), or is the event-production/apply-dispatch/persistence seam.
- Create or extend an internal child domain component when the behavior owns state plus rules, has a clear event family or state family, and can receive narrow context records. A lawful child component is `internal sealed`, lives under `src/WildBunch.Domain/Game/`, receives narrow context records (not the parent aggregate), returns results plus events-to-produce, does NOT reference `GameSession`, does NOT produce events directly, does NOT call `EnterActionContext`, does NOT mutate owners it does not own, and does NOT own infrastructure or persistence. Owned state is restored during snapshot rehydration via a `Restore*` helper on `GameSession` that delegates to the child.
- See `.agents/docs/game-session-decomposition-audit.md` for the current child-component inventory and the decomposition trajectory.

## Persistence / Model Posture
- POCO domain models are fine when they keep the domain plain, composable, and naturally serializable.
- Do not couple domain models to EF/table shape.
- Runtime session persistence is JSON snapshot-oriented today.
- Snapshot codecs belong in `WildBunch.Persistence` and should be split by coherent domain area when they get unwieldy.
- Do not normalize runtime session state into many DB tables unless explicitly directed.
- Persistence adapters may map the domain to JSON now and tables later without forcing domain refactors.
- In this greenfield repo, current mainline model correctness wins over old-save or legacy internal compatibility.
- Dev database drop/recreate is allowed when a current snapshot or schema shape changes and a reset is the cleanest path.
- Do not add compatibility shims for obsolete old saves or internal models unless Harley explicitly asks for one.
- Serializer optionality should exist only for current-domain reasons, not as a default legacy-save support layer.
- When a task calls for replacement, fully replace the old internal model instead of layering a compatibility adapter over it.
- Repo-local database artifacts should live under repo-root `.local/`, never under `src/`.

## UUID Seed Codec
- The game-start UUID encodes the seed-owned world/map layer. Inspect `SeedWorldResolver` source for the current codec layout and what fields are seed-owned.
- The seed does NOT encode difficulty, entropy, loadout, horse/saddle, final starting town, or final cash — those are pressure-owned (`DifficultyEnvelope`), entropy-owned (`EntropyPolicy` + `MysteryTruthResolver`), or player/setup-owned (`StartingTownPolicy`).
- The starting town is NOT a seed-owned fact. The player can start in any town that exists in the generated world. `StartingTownPolicy` validates the choice and provides a safe default. Future seam: difficulty may constrain eligibility.
- The seed deterministically derives the world map from a town-name pool via slot-based derivation. This is NOT a pair of canned named sets — it is true seed-derived town selection. Inspect source for current pool size and derivation parameters.
- `SeedWorld` holds the candidate/generated map. The seed owns default terrain and trail distances. Later difficulty can modify those values downstream of the seed codec.
- Design boundary: SeedWorld owns the candidate/generated map. Same seed + same difficulty should produce the same resolved map. Difficulty may later influence map pressure/layout realization downstream of the seed codec, not by hiding difficulty inside the seed.
- Both encode/decode directions must stay synchronized. Inspect current source before making codec claims. When adding a new seed-owned field, update both directions and verify round-trip behavior.
- Do NOT store UUIDs in test fixtures or libraries. Store `SeedWorld` records and derive UUIDs on the fly via `CreateRepresentativeSeedCode`. Stored UUIDs go stale when the codec evolves; `SeedWorld` records are compile-time checked.
- Do NOT create test sessions by bypassing the seed system with hand-built worlds unless the test is specifically about resource mechanics (canteen math, horse exhaustion). For encounter, trail-event, and journey tests, go through the seed system.
- The UUID has 128 bits of bandwidth. As fields are added, fewer UUIDs map to each seed world shape — this is expected and fine. Inspect current source for the current bit budget and field layout.
