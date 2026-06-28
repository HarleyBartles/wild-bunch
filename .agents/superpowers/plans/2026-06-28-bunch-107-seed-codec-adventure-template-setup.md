# BUNCH-107: Refactor Seed Codec into Adventure Template Setup

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the seed codec and game-setup seams so the seed produces a stable adventure/world template, not final player pressure settings or direct runtime truth. This creates a clean foundation for BUNCH-94 (difficulty controls) and BUNCH-93 (entropy controls) without side-questing into seed codec repairs.

**Architecture:** Split the current `StartingWorldDescriptor` — which mixes seed-owned facts (world variant, accusation index) with pressure-owned facts (difficulty, entropy, horse posture, loadout profile, final cash) — into three explicit seams: `AdventureTemplate` (seed-owned), `DifficultyEnvelope` (player-selected pressure), and `EntropyPolicy` (player-selected entropy/salt mode). A `ResolvedGameSetup` record composes the final session-start facts after template + difficulty + entropy are applied. `GameSession.StartNew` consumes `ResolvedGameSetup` instead of reinterpreting the seed during live play.

**Tech Stack:** C# / .NET, EF Core (PostgreSQL), xUnit, React + TypeScript, styled-components.

## Plan Status

- Plan status: preflight complete, plan written, awaiting approval
- Current route state: `preflight_complete`
- This PR is plan-only and contains no implementation.
- Base commit: `6b9fcbf` (BUNCH-106: Add flavourful citizen cast for POI encounters, #118)
- Branch: `harleydbartles/bunch-107-preflight`
- Worktree: `.worktrees/bunch-107`
- After this plan PR is merged, a later implementation worker should execute the checked-in plan from current `main`.
- Implementation must still follow the plan's validation and falsification steps; approval of the plan is not approval to skip verification.

## Global Constraints

- Assumption baseline: current `origin/main` at commit `6b9fcbf` (2026-06-28).
- Do not implement BUNCH-93 entropy controls (Boring/Classic/Adventurous/Wild remix logic beyond the current seam).
- Do not implement BUNCH-94 difficulty controls (the full horse/saddle envelope, loadout envelope, travel harshness, clue pressure, false-lead pressure, consequence severity).
- Do not add new difficulty or entropy levels.
- Do not change the `GameStarted` event shape, snapshot codec shape, or EF schema — the resolved setup feeds the same event fields.
- Do not change `StartGameRequest` or `StartNewGameCommand` parameter shapes — the API contract stays stable.
- Do not change `SetupHuntStep.tsx` or the frontend start-flow UI — the player still selects difficulty, entropy, and seed.
- Keep `GameSession` as the live-play aggregate root. `GameSession.StartNew` still receives the same domain inputs; the refactor changes who computes them, not the event shape.
- Keep hidden culprit truth internal after resolution.
- Keep horse and saddle separate domain concepts. Do not collapse them into a generic loadout blob.
- Keep Wallet and Inventory concrete; do not reintroduce generic supplies.
- Do not normalize runtime session state into many database tables.
- The UUID seed values WILL shift because the codec changes what it encodes. This is expected. Tests that store descriptors (not UUIDs) will adapt; tests that stored UUIDs were already non-compliant per AGENTS.md.
- The seed-derived variety for horse posture and loadout profile WILL be lost in this refactor. That is the intended direction — BUNCH-94 will add difficulty-owned horse/saddle and loadout envelopes. All difficulties currently get the canonical defaults (horse+saddle, Standard loadout) as a transitional state.
- The culprit identity WILL change from fixed `suspect-4` to seed-encoded default culprit index. This is intended — the seed should carry default mystery-truth candidates for Boring replay.

---

## Core Pipeline

The target pipeline, made explicit in source:

```
seed code -> AdventureTemplate -> DifficultyEnvelope -> EntropyPolicy -> ResolvedGameSetup -> GameSession
```

### Seam Definitions (to install in source)

**AdventureTemplate** (seed-owned, shareable replay identity for world setup):
- `SeedCode` (Guid) — the UUID itself
- `WorldVariant` (SeedWorldVariant) — seed-decoded
- `StartingTownSelectionKey` (string) — seed-owned, independent of horse posture
- `AccusationIndex` (int) — seed-decoded default opening accusation
- `DefaultCulpritIndex` (int) — seed-decoded default culprit for Boring replay (NEW)
- `CashBonus` (int) — raw seed-derived cash bonus (0–8, NOT entropy-capped)

**DifficultyEnvelope** (player-selected pressure, applied after template resolution):
- `GameDifficulty` (enum)
- `BaseCash` (decimal) — difficulty-owned base starting cash
- `StartWithHorse` (bool) — difficulty-owned horse posture (transitional: all difficulties get true)
- `IncludeSaddle` (bool) — difficulty-owned saddle (transitional: all difficulties get true)
- `LoadoutProfile` (enum) — difficulty-owned loadout (transitional: all difficulties get Standard)
- `StartingHealth` (int) — difficulty-owned starting health
- `TravelRulesProfile` (TravelRulesProfile) — difficulty-owned travel rules

**EntropyPolicy** (player-selected entropy/salt mode):
- `GameEntropy` (enum)
- `SaltSourceMode` (SaltSourceMode) — Fixed for Boring, Runtime for others
- `CashBonusCap` (int) — max seed cash bonus applied (Boring=0, Classic=2, Adventurous=5, Wild=8)

**ResolvedGameSetup** (final session-start facts after all stages applied):
- `AdventureTemplate` — the seed-owned template (retained for reproducibility)
- `GameDifficulty` — player-selected
- `GameEntropy` — player-selected
- `World` (World) — resolved world domain object
- `StartingTownId` (TownId) — resolved starting town
- `CaseFile` (CaseFile) — resolved case file with final culprit
- `StartingWallet` (Wallet) — final wallet
- `StartingInventory` (Inventory) — final inventory
- `StartingHealth` (int) — final starting health
- `TravelRulesProfile` (TravelRulesProfile) — from difficulty
- `SaltSource` (SaltSource) — from entropy
- `SeedCodeText` (string) — for debugging/reproducibility

---

## Preflight Answers

### Question 1: What does the current seed code encode today?

The current `StartingWorldDescriptor` (in `GameSetupSeed.cs`) mixes seed-decoded facts with pressure inputs:

**Seed-decoded (from UUID via `StartingWorldDescriptorSeedMixer`):**
- `World.Variant` — via `ResolveWorldVariant` (seedValue % 3)
- `Player.StartWithHorse` — via `ResolveStartWithHorse` (seedValue & 1)
- `Player.LoadoutProfile` — via `ResolveLoadoutProfile` (seedValue % 3)
- `Case.AccusationIndex` — via `ResolveAccusationIndex` (seedValue % 7)
- Cash bonus component — via `cashSeed % (maxPolicyBonus + 1)` (entropy-capped)

**Pressure inputs (passed as parameters, stored on descriptor):**
- `GameDifficulty` — passed as `requestedDifficulty`, used for base cash + starting health
- `GameEntropy` — passed as `requestedEntropy`, used for cash bonus cap + salt source mode

**Derived/coupled:**
- `World.StartingTownSelectionKey` — derived from `StartWithHorse` (horse→"world.startingTown.horse", foot→"world.startingTown.foot")
- `Player.Loadout` (Food, HorseFeed, RevolverAmmo, IncludeHorse, IncludeSaddle) — derived from `LoadoutProfile` + `StartWithHorse`
- `Player.StartingCash` — hybrid: `GetBaseStartingCash(difficulty)` + `GetLoadoutProfileBonus(loadoutProfile)` + `horseBonus` + `policyBonus`

**Not in descriptor at all:**
- Starting health — computed in `GameSession.StartingHealthFor(gameDifficulty)`
- Culprit identity — hardcoded to `suspect-4` / `trueCulpritIndex = 3` in `SeedCaseBuilder`
- Salt source — computed in `SeededNewGameFactory.Create` from entropy

**Descriptor signature** (`CreateDescriptorSignature`) includes World+Player+Case but NOT GameDifficulty/GameEntropy. Round-trip search finds a UUID that produces the same World+Player+Case.

### Question 2: Which current fields are pressure-owned and should move out of seed identity?

- `GameDifficulty` — pressure-owned (player-selected). Already a parameter, but stored on descriptor and contaminates cash calculation.
- `GameEntropy` — pressure-owned (player-selected). Already a parameter, but stored on descriptor and contaminates cash cap + salt mode.
- `Player.StartWithHorse` — pressure-owned per execution notes (difficulty owns horse/saddle envelope). Currently seed-decoded.
- `Player.LoadoutProfile` — pressure-owned per execution notes (difficulty owns loadout envelope). Currently seed-decoded.
- `Player.Loadout` (all 5 fields) — pressure-owned, derived from loadout profile + horse posture.
- `Player.StartingCash` (final value) — pressure-owned, hybrid of difficulty base + seed bonus + entropy cap. The seed bonus component stays seed-owned; the final value is resolved-setup.
- Starting health — already pressure-owned (difficulty), not in descriptor. Correct.
- Salt source mode — already pressure-owned (entropy), not in descriptor. Correct.

### Question 3: Which current fields are legitimate seed/adventure-template facts?

- `World.Variant` — seed-owned. Stays.
- `World.StartingTownSelectionKey` — should be seed-owned but currently coupled to horse posture. Must become independent.
- `Case.AccusationIndex` — seed-owned (default mystery-truth candidate). Stays.
- Cash bonus (raw seed value before entropy cap) — seed-owned. Stays, but decoupled from entropy cap.
- **NEW:** `DefaultCulpritIndex` — seed-owned default culprit for Boring replay. Currently hardcoded to 3, must become seed-encoded.

### Question 4: Where should the code represent the pipeline?

In `WildBunch.GameContent/NewGame/`:
- `AdventureTemplate.cs` — new record, pure seed-owned facts
- `AdventureTemplateResolver.cs` — replaces `StartingWorldDescriptorResolver` for seed→template resolution
- `DifficultyEnvelope.cs` — new record, difficulty pressure facts
- `EntropyPolicy.cs` — new record, entropy/salt mode facts
- `ResolvedGameSetup.cs` — new record, final session-start facts
- `GameSetupResolver.cs` — new orchestrator: template + difficulty + entropy → resolved setup
- `SeededNewGameFactory.cs` — refactored to use `GameSetupResolver` instead of directly building from descriptor
- `GameSetupPackageBuilder.cs` — refactored to build from `ResolvedGameSetup` (or absorbed into `GameSetupResolver`)
- `SeedCaseBuilder.cs` — refactored to use seed-encoded culprit index instead of hardcoded `suspect-4`
- `SeedInventoryBuilder.cs` — refactored to build from `DifficultyEnvelope` instead of `StartingWorldDescriptorPlayer`
- `SeedWorldBuilder.cs` — refactored to use `AdventureTemplate` instead of `StartingWorldDescriptor`
- `GameSetupGenerationPlan.cs` — refactored or replaced to carry `AdventureTemplate` + `DifficultyEnvelope` instead of `StartingWorldDescriptor`

### Question 5: How will the implementation avoid doing BUNCH-93/BUNCH-94 while still creating the seam?

- `DifficultyEnvelope.For(GameDifficulty)` preserves the CURRENT difficulty behavior (base cash, starting health, travel rules). The horse/saddle envelope and loadout envelope from the execution notes are documented as the BUNCH-94 target but NOT implemented — all difficulties get canonical defaults (horse+saddle, Standard loadout) as a transitional state.
- `EntropyPolicy.For(GameEntropy)` preserves the CURRENT entropy behavior (Boring=Fixed salt + 0 cash bonus, others=Runtime salt + capped seed bonus). The Classic/Adventurous/Wild remix logic (salted culprit reroll, feature reallocation) is BUNCH-93 — for now, all entropy modes use the seed-encoded default culprit.
- The seams exist as records with factory methods. BUNCH-94 will expand `DifficultyEnvelope.For(...)` to add the full horse/saddle/loadout/travel/clue pressure. BUNCH-93 will expand `EntropyPolicy.For(...)` to add salted remix logic.

### Question 6: What exact tests will prove that BUNCH-93 and BUNCH-94 can build on the refactor?

- **Seed-codec round trip:** same `AdventureTemplate` round-trips through UUID encode/decode.
- **Template determinism:** same seed resolves the same `AdventureTemplate` regardless of difficulty/entropy.
- **Difficulty/entropy absence from seed identity:** `AdventureTemplate` has no `GameDifficulty` or `GameEntropy` field. Grep proof that the codec does not reference difficulty or entropy during `Resolve(Guid)`.
- **Resolved setup behavior:** `ResolvedGameSetup` carries final wallet, inventory, health, case file, and salt source. `GameSession.StartNew` consumes it without reinterpreting the seed.
- **Boring determinism:** same seed + same difficulty + Boring entropy → same session (same culprit, same salt, same world).
- **Classic salted replacement capability:** the `EntropyPolicy` seam exposes `SaltSourceMode` and `CashBonusCap` such that BUNCH-93 can add salted culprit reroll without touching the codec.
- **Difficulty envelope extensibility:** the `DifficultyEnvelope` record has fields for horse/saddle/loadout/health/travel rules such that BUNCH-94 can expand the mapping without touching the codec.

### Question 7: Which repo guidance must change?

- Root `AGENTS.md` "UUID Seed Codec" section: replace "Which one is encoded in the UUID seed" (re: culprit) with the template/resolution distinction.
- Root `AGENTS.md` "Architecture Guardrails" section: update the culprit guardrail to say the seed carries the default culprit for Boring, and non-Boring entropy modes may salt/reroll.
- `WildBunch.GameContent/AGENTS.md`: update codec update checklist to reflect the new `AdventureTemplate` shape and the fact that difficulty/entropy are no longer in the descriptor.
- Create `.agents/docs/setup-pipeline-doctrine.md` — the repo-tracked setup doctrine file that documents the seed → template → difficulty → entropy → resolved setup pipeline. Root `AGENTS.md` should point to it.

---

## Staleness Gate

This plan is written against `origin/main` at commit `6b9fcbf` (2026-06-28). If `main` has advanced before implementation begins, the implementation worker MUST:

1. Refresh the worktree from current `main` (rebase or branch fresh from latest `origin/main`).
2. Check whether any merged PR has changed the seed codec, game setup, or start-flow surfaces.
3. Re-run the surface search (`StartingWorldDescriptor`, `StartingWorldDescriptorResolver`, `SeededNewGameFactory`, `GameSetupPackageBuilder`, `SeedCaseBuilder`, `SeedInventoryBuilder`, `SeedWorldBuilder`, `GameSetupGenerationPlan`, `StartingWorldDescriptorSeedMixer`, `GameSetupDeterministicLabels`) against the refreshed tree.
4. If new surfaces are found, add them to the task list. If existing surfaces have been restructured, follow the new location/name.
5. Stop AMBER if refreshed main changes the work beyond a surface update.

---

## Tasks

### Task 1: Create AdventureTemplate record and AdventureTemplateResolver

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/AdventureTemplate.cs`
- Create: `src/WildBunch.GameContent/NewGame/AdventureTemplateResolver.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupDeterministicLabels.cs`

**Details:**

Create `AdventureTemplate` record:
```csharp
public sealed record AdventureTemplate(
    Guid SeedCode,
    SeedWorldVariant WorldVariant,
    string StartingTownSelectionKey,
    int AccusationIndex,
    int DefaultCulpritIndex,
    int CashBonus)
{
    public string SeedCodeText => SeedCode.ToString("D");
}
```

Create `AdventureTemplateResolver` (replaces the seed-decoding role of `StartingWorldDescriptorResolver`):
- `Resolve(Guid seedCode)` → `AdventureTemplate` — decodes UUID → template using `StartingWorldDescriptorSeedMixer`
- `CreateCanonicalTemplate()` → `AdventureTemplate` — the canonical/default template
- `CreateRepresentativeSeedCode(AdventureTemplate)` → `Guid` — encodes template → UUID via round-trip search
- `TryParseSeedCode`, `FormatSeedCode` — move from `StartingWorldDescriptorResolver`
- `Validate(AdventureTemplate)` — validates seed-owned fields only

Codec changes in the new resolver:
- KEEP: `ResolveWorldVariant` (seedValue % 3)
- KEEP: `ResolveAccusationIndex` (seedValue % 7)
- ADD: `ResolveDefaultCulpritIndex` (seedValue % 7) — NEW seed-encoded field, range 0–6
- ADD: `ResolveCashBonus` (seedValue % 9) — raw 0–8, NOT entropy-capped
- CHANGE: `StartingTownSelectionKey` — now seed-decoded independently via a single label `world.startingTown` (remove the horse/foot coupling)
- REMOVE: `ResolveStartWithHorse`, `ResolveLoadoutProfile` — no longer seed-decoded

Add new label to `GameSetupDeterministicLabels`:
- `WorldStartingTown = "world.startingTown"` — single label, replaces horse/foot pair
- `CaseDefaultCulprit = "case.default-culprit"` — new label for culprit index

Update `CreateDescriptorSignature` equivalent (`CreateTemplateSignature`) to include: WorldVariant, StartingTownSelectionKey, AccusationIndex, DefaultCulpritIndex, CashBonus. Remove all Player fields from the signature.

- [ ] Create `AdventureTemplate.cs`
- [ ] Create `AdventureTemplateResolver.cs` with Resolve, CreateCanonicalTemplate, CreateRepresentativeSeedCode, TryParseSeedCode, FormatSeedCode, Validate
- [ ] Add `WorldStartingTown` and `CaseDefaultCulprit` labels to `GameSetupDeterministicLabels.cs`
- [ ] Write unit tests for AdventureTemplate round-trip, validation, avalanche, determinism

### Task 2: Create DifficultyEnvelope record

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/DifficultyEnvelope.cs`

**Details:**

Create `DifficultyEnvelope` record:
```csharp
public sealed record DifficultyEnvelope(
    GameDifficulty GameDifficulty,
    decimal BaseCash,
    bool StartWithHorse,
    bool IncludeSaddle,
    StartingLoadoutProfile LoadoutProfile,
    int StartingHealth,
    TravelRulesProfile TravelRulesProfile,
    (int Food, int HorseFeed, int RevolverAmmo) LoadoutCounts)
{
    public static DifficultyEnvelope For(GameDifficulty difficulty) => ...
}
```

`DifficultyEnvelope.For(GameDifficulty)` mapping (transitional, preserves current canonical defaults):
- `BaseCash`: Easy=28, Standard=23, Challenging=18, Brutal=13 (current `GetBaseStartingCash`)
- `StartWithHorse`: true for all difficulties (transitional — BUNCH-94 will add Easy=horse, Standard=horse, Challenging=no horse, Brutal=no horse)
- `IncludeSaddle`: true for all difficulties (transitional — BUNCH-94 will add Easy=saddle, Standard=no saddle, Challenging=saddle, Brutal=no saddle)
- `LoadoutProfile`: Standard for all difficulties (transitional — BUNCH-94 will add difficulty-owned loadout envelope)
- `StartingHealth`: Easy=1250, Standard=1000, Challenging=800, Brutal=600 (current `StartingHealthFor`)
- `TravelRulesProfile`: `TravelRulesProfile.For(difficulty)` (current behavior)
- `LoadoutCounts`: from `LoadoutProfile` (current `ResolveLoadoutCounts`)

Add a doc comment: "Transitional mapping. BUNCH-94 will expand this to the full difficulty-owned horse/saddle envelope, loadout envelope, and travel harshness."

- [ ] Create `DifficultyEnvelope.cs` with `For(GameDifficulty)` factory
- [ ] Write unit tests for DifficultyEnvelope.For mapping

### Task 3: Create EntropyPolicy record

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/EntropyPolicy.cs`

**Details:**

Create `EntropyPolicy` record:
```csharp
public sealed record EntropyPolicy(
    GameEntropy GameEntropy,
    SaltSourceMode SaltSourceMode,
    int CashBonusCap)
{
    public static EntropyPolicy For(GameEntropy entropy) => ...
}
```

`EntropyPolicy.For(GameEntropy)` mapping (preserves current behavior):
- `SaltSourceMode`: Boring=Fixed, others=Runtime (current `SeededNewGameFactory` logic)
- `CashBonusCap`: Boring=0, Classic=2, Adventurous=5, Wild=8 (current `maxPolicyBonus`)

Add a doc comment: "Transitional mapping. BUNCH-93 will expand this to add salted culprit reroll, feature reallocation, and Adventurous/Wild variance boundaries."

- [ ] Create `EntropyPolicy.cs` with `For(GameEntropy)` factory
- [ ] Write unit tests for EntropyPolicy.For mapping

### Task 4: Create ResolvedGameSetup record and GameSetupResolver

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/ResolvedGameSetup.cs`
- Create: `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`

**Details:**

Create `ResolvedGameSetup` record:
```csharp
internal sealed record ResolvedGameSetup(
    AdventureTemplate Template,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy,
    World World,
    TownId StartingTownId,
    CaseFile CaseFile,
    Wallet StartingWallet,
    Inventory StartingInventory,
    int StartingHealth,
    TravelRulesProfile TravelRulesProfile,
    SaltSource SaltSource,
    string SeedCodeText);
```

Create `GameSetupResolver` that orchestrates the full pipeline:
```csharp
internal sealed class GameSetupResolver
{
    public ResolvedGameSetup Resolve(
        AdventureTemplate template,
        DifficultyEnvelope difficulty,
        EntropyPolicy entropy,
        TownId? playerChosenStartingTownId = null)
    {
        // 1. Build world from template
        // 2. Build case file from template (use DefaultCulpritIndex)
        // 3. Compute final cash: difficulty.BaseCash + loadoutBonus + horseBonus + min(template.CashBonus, entropy.CashBonusCap)
        // 4. Build inventory from difficulty envelope
        // 5. Build wallet from final cash
        // 6. Resolve salt source from entropy
        // 7. Resolve starting town (player override or seed-derived)
        // 8. Return ResolvedGameSetup
    }
}
```

This absorbs the logic currently spread across `GameSetupPackageBuilder`, `SeedWorldBuilder`, `SeedCaseBuilder`, `SeedInventoryBuilder`, and `SeededNewGameFactory`.

- [ ] Create `ResolvedGameSetup.cs`
- [ ] Create `GameSetupResolver.cs` with full pipeline orchestration
- [ ] Write unit tests for GameSetupResolver

### Task 5: Refactor SeedCaseBuilder to use seed-encoded culprit index

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`

**Details:**

- Replace hardcoded `TrueCulpritId = new SuspectId("suspect-4")` and `trueCulpritIndex: 3` with the seed-encoded `DefaultCulpritIndex` from `AdventureTemplate`.
- The `CreateCaseFile` method should accept `AdventureTemplate` (or the resolved setup plan) and use `template.DefaultCulpritIndex` as the culprit index.
- The canonical case file should use `DefaultCulpritIndex = 3` (preserving the current canonical culprit).
- Keep the `AccusationIndex` from the template as the opening accusation.
- Keep all other case-building logic (roster, features, clues, warrants, turf) unchanged.
- The culprit is still always a gang member (index into the 7-suspect roster). The seed just determines which one is the default.

- [ ] Refactor `SeedCaseBuilder` to accept culprit index from template
- [ ] Update canonical case file to use culprit index 3
- [ ] Verify case file tests still pass

### Task 6: Refactor SeededNewGameFactory to use the new pipeline

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`
- Modify or remove: `src/WildBunch.GameContent/NewGame/GameSetupPackageBuilder.cs`
- Modify or remove: `src/WildBunch.GameContent/NewGame/GameSetupPackage.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupGenerationPlan.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedInventoryBuilder.cs`
- Modify: `src/WildBunch.GameContent/Prologue/PrologueDescriptorResolver.cs`

**Details:**

Refactor `SeededNewGameFactory.Create`:
```csharp
public GameSession Create(
    string playerName,
    GameDifficulty gameDifficulty = GameDifficulty.Standard,
    string? setupSeedCode = null,
    GameEntropy gameEntropy = GameEntropy.Classic,
    string? startingTownId = null)
{
    var seed = ParseOrGenerateSeed(setupSeedCode);
    var template = AdventureTemplateResolver.Resolve(seed);
    var difficulty = DifficultyEnvelope.For(gameDifficulty);
    var entropy = EntropyPolicy.For(gameEntropy);
    var resolvedSetup = _setupResolver.Resolve(template, difficulty, entropy, ParseOptionalTown(startingTownId));

    return GameSession.StartNew(
        playerName,
        resolvedSetup.World,
        resolvedSetup.CaseFile,
        resolvedSetup.StartingTownId,
        resolvedSetup.StartingWallet,
        resolvedSetup.StartingInventory,
        resolvedSetup.GameDifficulty,
        resolvedSetup.SaltSource,
        resolvedSetup.GameEntropy,
        resolvedSetup.SeedCodeText);
}
```

- `GameSetupPackageBuilder` and `GameSetupPackage` may be absorbed into `GameSetupResolver`/`ResolvedGameSetup`, or kept as thin wrappers. The worker should choose the cleanest path but must not leave dead code.
- `GameSetupGenerationPlan` should be refactored to carry `AdventureTemplate` + `DifficultyEnvelope` instead of `StartingWorldDescriptor`, or replaced by `ResolvedGameSetup` if the plan is no longer needed as an intermediate.
- `SeedWorldBuilder` should accept `AdventureTemplate` instead of `StartingWorldDescriptor` for world/starting-town resolution.
- `SeedInventoryBuilder` should accept `DifficultyEnvelope` instead of `StartingWorldDescriptorPlayer` for loadout/wallet building.
- `PrologueDescriptorResolver` should use the new pipeline to resolve the prologue culprit descriptor.

- [ ] Refactor `SeededNewGameFactory.Create` to use new pipeline
- [ ] Refactor or absorb `GameSetupPackageBuilder`/`GameSetupPackage`
- [ ] Refactor or replace `GameSetupGenerationPlan`
- [ ] Refactor `SeedWorldBuilder` to use `AdventureTemplate`
- [ ] Refactor `SeedInventoryBuilder` to use `DifficultyEnvelope`
- [ ] Refactor `PrologueDescriptorResolver` to use new pipeline
- [ ] Verify no dead code remains

### Task 7: Remove or gate old StartingWorldDescriptor

**Files:**
- Modify or remove: `src/WildBunch.GameContent/NewGame/GameSetupSeed.cs`
- Modify or remove: `src/WildBunch.GameContent/NewGame/GameSetupSeedCodec.cs`
- Modify or remove: `src/WildBunch.GameContent/NewGame/StartingWorldDescriptorSeedMixer.cs`
- Modify or remove: `src/WildBunch.GameContent/NewGame/GameSetupSeedCodeValidator.cs`
- Modify: `src/WildBunch.GameContent/DependencyInjection.cs` (if it references old types)

**Details:**

- `StartingWorldDescriptor` record: remove entirely. Its role is split between `AdventureTemplate` (seed-owned) and `DifficultyEnvelope` (pressure-owned).
- `StartingWorldDescriptorResolver`: remove. Replaced by `AdventureTemplateResolver`. Move `TryParseSeedCode`, `FormatSeedCode`, `CreateCanonicalSeedCode`, `GenerateRandomSeedCode` to `AdventureTemplateResolver`.
- `StartingWorldDescriptorSeedMixer`: keep the mixer logic (it's the hash/seed-root infrastructure), but update `CreateDescriptorSignature` → `CreateTemplateSignature` to match the new `AdventureTemplate` shape.
- `StartingWorldDescriptorCodeValidator`: update to reference `AdventureTemplateResolver.TryParseSeedCode`.
- `StartingWorldDescriptorPlayer`, `StartingWorldDescriptorWorld`, `StartingWorldDescriptorLoadout`, `StartingWorldDescriptorCase`, `StartingWorldDescriptorValidationResult`: remove. Their fields are either in `AdventureTemplate`, `DifficultyEnvelope`, or `ResolvedGameSetup`.
- `StartingLoadoutProfile` enum: keep (used by `DifficultyEnvelope`).

- [ ] Remove `StartingWorldDescriptor` and related records
- [ ] Remove `StartingWorldDescriptorResolver` (logic moved to `AdventureTemplateResolver`)
- [ ] Update `StartingWorldDescriptorSeedMixer` to produce template signatures
- [ ] Update `StartingWorldDescriptorCodeValidator` to reference new resolver
- [ ] Update `DependencyInjection.cs` if needed
- [ ] Grep proof: no remaining references to `StartingWorldDescriptor` in production source

### Task 8: Update test helpers and catalogs

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/StartingWorldDescriptorSeedCodeFactory.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalog.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalogGuardrailTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedFixture.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/BoringScenarioBuilder.cs`

**Details:**

`StartingWorldDescriptorSeedCodeFactory` → rename to `AdventureTemplateSeedCodeFactory`:
- Update `CreateSeedCode` parameters to match the new template shape: `worldVariant`, `startingTownKey`, `accusationIndex`, `defaultCulpritIndex`, `cashBonus`, `salt`
- Remove `policy`, `loadoutProfile`, `startWithHorse`, `difficulty` parameters (no longer seed-encoded)
- Update `CreateDescriptor` → `CreateTemplate` to build `AdventureTemplate`
- Update `HasSameSemantics` to compare `AdventureTemplate` fields

`TravelTestSeedCatalog`:
- Replace `StartingWorldDescriptor` entries with `AdventureTemplate` entries + `GameDifficulty`/`GameEntropy` parameters
- The `CreateSession` helper should call `SeededNewGameFactory.Create` with difficulty + entropy + seed code derived from the template
- Hand-built descriptors that specified horse/loadout/cash should be replaced with templates + difficulty envelopes
- The `CanonicalFootBoringLight` entry (no horse, light loadout) can no longer be seed-derived — it should use a difficulty that produces no horse (but since all difficulties currently give horse+saddle, this test profile needs to be either removed or adjusted to use the transitional defaults). The worker should document this as a transitional gap that BUNCH-94 will fill.

`ScenarioSeedCatalog`:
- Replace `StartingWorldDescriptor`-based seed code derivation with `AdventureTemplate`-based derivation
- Update `NoHorseLightEasySeedCode` — this fixture specified no horse + light loadout, which is no longer seed-owned. It should be removed or converted to a difficulty-owned profile. The worker should document this as a transitional gap.
- Update shape signatures that reference horse/saddle/wallet/items counts if they change due to the transitional defaults.

`ScenarioSeedFixture` and `BoringScenarioBuilder`:
- Update any references to `StartingWorldDescriptor` or `StartingWorldDescriptorResolver`.

- [ ] Rename and update `StartingWorldDescriptorSeedCodeFactory` → `AdventureTemplateSeedCodeFactory`
- [ ] Update `TravelTestSeedCatalog` to use `AdventureTemplate` + difficulty/entropy
- [ ] Update `TravelTestSeedCatalogGuardrailTests`
- [ ] Update `ScenarioSeedCatalog` seed code derivation
- [ ] Update `ScenarioSeedFixture` and `BoringScenarioBuilder`
- [ ] Document transitional gaps where horse/loadout variety was lost

### Task 9: Update existing tests for new codec shape

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/StartingWorldDescriptorResolverTests.cs` → rename to `AdventureTemplateResolverTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/GameSetupPackageBuilderTests.cs`

**Details:**

`StartingWorldDescriptorResolverTests` → `AdventureTemplateResolverTests`:
- Round-trip test: `AdventureTemplate` round-trips through UUID
- Multiple UUIDs → same template test
- Validation test: validate template fields only (no difficulty/entropy validation)
- Avalanche test: neighboring UUIDs change multiple template fields
- Determinism test: same seed → same template regardless of difficulty/entropy
- Remove tests that asserted difficulty/entropy behavior on the descriptor (those are now on `DifficultyEnvelope`/`EntropyPolicy`)

`SeededNewGameFactoryTests`:
- `CreatesRicherSeedWorldAndCase`: update cash/inventory assertions for transitional defaults (horse+saddle, Standard loadout for all difficulties). The canonical session should still have 25m cash, 8 items, mounted travel.
- `FrontierDescriptorAddsTownSpecificCivicClues`: update to use new pipeline.
- `SameSeedKeepsTheRosterStable`: update to use new pipeline. Roster/warrant/turf signatures should still be seed-stable.
- `RandomizedNoHorseLightLoadoutSeedCreatesNoHorseOrSaddle`: this test expected seed-derived no-horse/light loadout. After refactor, all seeds get horse+saddle + Standard loadout (transitional). Update or remove this test and document the transitional gap.
- `DefaultAdventureRandomnessStaysRuntimeSaltedAndBoringModeCanOptIntoDeterminism`: update to use new pipeline. Salt source behavior should be preserved.
- `CreateWithPlayerChosenStartingTownOverridesSeedDefault`: update to use new pipeline. Player override should still work.
- `CreateWithNullStartingTownIdUsesSeedDefault`: update to use new pipeline. Seed default should still be pinecross for canonical.

`SeedWorldBuilderTests`:
- Update `BuildSeedWorld` calls to use `AdventureTemplate` instead of `StartingWorldDescriptor`.
- Town/trail snapshot assertions should be unchanged (world catalog is not changing).

`GameSetupPackageBuilderTests`:
- Update or replace with `GameSetupResolverTests` if the package builder is absorbed.
- Tests that asserted loadout profile / horse posture changes should be converted to difficulty envelope tests.

- [ ] Rename and update resolver tests
- [ ] Update factory tests for transitional defaults
- [ ] Update world builder tests
- [ ] Update/replace package builder tests
- [ ] Document all transitional behavior changes in test comments

### Task 10: Update repo guidance

**Files:**
- Modify: `AGENTS.md` (root)
- Modify: `src/WildBunch.GameContent/AGENTS.md`
- Create: `.agents/docs/setup-pipeline-doctrine.md`

**Details:**

Root `AGENTS.md`:
- "UUID Seed Codec" section: replace "Which one is encoded in the UUID seed" with "The seed encodes the default culprit for Boring replay; non-Boring entropy modes may salt/reroll private culprit allocation during setup."
- "UUID Seed Codec" section: update the codec update checklist to reference `AdventureTemplate` instead of `StartingWorldDescriptor`. Update the field list to reflect seed-owned facts (world variant, starting town, accusation index, default culprit index, cash bonus).
- "Architecture Guardrails" section: update the culprit guardrail to say "The seed carries the default culprit for Boring replay. Non-Boring entropy modes may salt/reroll private culprit and identifier allocation during setup. Hidden culprit truth remains internal after resolution."
- Add a pointer to `.agents/docs/setup-pipeline-doctrine.md` in the "UUID Seed Codec" section.

`WildBunch.GameContent/AGENTS.md`:
- Update "UUID ↔ World Descriptor Codec" to "UUID ↔ Adventure Template Codec"
- Update `StartingWorldDescriptorResolver.Resolve(Guid)` → `AdventureTemplateResolver.Resolve(Guid)`
- Update `CreateRepresentativeSeedCode` reference
- Update "When to update this project" checklist to reference `AdventureTemplate`, `AdventureTemplateResolver`, `DifficultyEnvelope`, `EntropyPolicy`
- Update "New difficulty or entropy level" guidance to reference `DifficultyEnvelope.For` and `EntropyPolicy.For`
- Update "Any new starting-world field" to reference `AdventureTemplate` and `CreateTemplateSignature`

Create `.agents/docs/setup-pipeline-doctrine.md`:
- Document the full pipeline: `seed code -> AdventureTemplate -> DifficultyEnvelope -> EntropyPolicy -> ResolvedGameSetup -> GameSession`
- Define each seam's ownership
- List seed-owned facts vs difficulty-owned facts vs entropy-owned facts
- Document the horse/saddle envelope target for BUNCH-94
- Document the entropy/salt contract for BUNCH-93
- State that hidden culprit truth remains internal after resolution

- [ ] Update root `AGENTS.md` UUID Seed Codec section
- [ ] Update root `AGENTS.md` Architecture Guardrails section
- [ ] Update `WildBunch.GameContent/AGENTS.md`
- [ ] Create `.agents/docs/setup-pipeline-doctrine.md`
- [ ] Grep proof: no remaining guidance says UUID seed always directly encodes final culprit truth

### Task 11: Regenerate index mesh and run full validation

**Files:**
- Regenerate: all `INDEX.md` files via `python scripts/generate_index_mesh.py`

**Validation commands:**
- `dotnet build`
- `dotnet test`
- `.\scripts\postgres-dev.ps1 validate` (if PostgreSQL-dependent tests are affected)
- `python scripts/generate_index_mesh.py --check`
- Grep proof: `grep -r "StartingWorldDescriptor" src/` returns no production source hits
- Grep proof: `grep -r "GameDifficulty\|GameEntropy" src/WildBunch.GameContent/NewGame/AdventureTemplate*.cs` returns no hits (seed-owned facts don't reference pressure inputs)

- [ ] Run `dotnet build` — must pass
- [ ] Run `dotnet test` — must pass
- [ ] Run `.\scripts\postgres-dev.ps1 validate` — must pass
- [ ] Run `python scripts/generate_index_mesh.py --check` — must pass
- [ ] Grep proof: no `StartingWorldDescriptor` in production source
- [ ] Grep proof: no `GameDifficulty`/`GameEntropy` in `AdventureTemplate`/`AdventureTemplateResolver`

---

## Validation Expectations

The implementation return must include:

- Targeted backend tests for seed codec/template/resolved setup behavior
- Persistence tests if resolved setup or snapshot/event shapes change (expected: no shape change)
- API/frontend tests if start-game request or setup UI contracts change (expected: no contract change)
- `dotnet build` output
- `dotnet test` output
- EF migration validation (`dotnet ef migrations list`) if persistence shape changes (expected: no change)
- Index mesh regeneration/check
- Grep/source proof that stale seed/difficulty/entropy guidance was removed
- Grep/source proof that `StartingWorldDescriptor` no longer exists in production source
- Grep/source proof that `AdventureTemplate` does not reference `GameDifficulty` or `GameEntropy`

---

## DOD Mapping

| DOD Check | How This Plan Proves It |
|---|---|
| Same seed resolves the same adventure template | Task 1 tests: AdventureTemplate round-trip + determinism |
| Seed codec does not carry selected difficulty or entropy | Task 1+7: AdventureTemplate has no difficulty/entropy fields; grep proof |
| Seed codec does not directly carry final starting health, final horse/saddle state, or direct inventory/loadout facts | Task 1+2: these are in DifficultyEnvelope, not AdventureTemplate |
| Seed-derived cash bonus/multiplier is stable for the same seed | Task 1: CashBonus is seed-decoded, not entropy-capped |
| Boring can preserve template/default mystery truth deterministically | Task 3+5: Boring uses Fixed salt + seed-encoded DefaultCulpritIndex |
| Classic can preserve the same template while resolving salted private truth differently | Task 3: EntropyPolicy exposes SaltSourceMode for BUNCH-93 to add salted reroll |
| Resolved setup is explicit enough that GameSession starts from final setup facts | Task 4+6: ResolvedGameSetup carries all final facts; GameSession.StartNew consumes it |
| Repo guidance no longer teaches that UUID seed is always the final answer key | Task 10: AGENTS.md + doctrine file updated; grep proof |
| BUNCH-93 and BUNCH-94 have clear rebase/plan-repair instructions | See Handoff below |

---

## Handoff to Paused Workers

After this PR lands:

### BUNCH-94 (Difficulty Setup and Controls)
- Rebase and repair plan so difficulty applies pressure after adventure-template resolution.
- Expand `DifficultyEnvelope.For(GameDifficulty)` to implement the full horse/saddle envelope: Easy=horse+saddle, Standard=horse, Challenging=saddle, Brutal=no horse/no saddle.
- Expand `DifficultyEnvelope.For(GameDifficulty)` to add difficulty-owned loadout envelope, travel harshness, clue pressure, false-lead pressure, and consequence severity.
- Add visible difficulty controls to the start-flow UI (the `SetupHuntStep` already has a difficulty selector — wire it to the new envelope).
- Update tests that documented transitional gaps (all difficulties get horse+saddle) to assert the new difficulty-owned behavior.
- The `AdventureTemplate` and `AdventureTemplateResolver` should NOT need changes — difficulty is downstream of the template.

### BUNCH-93 (Entropy Setup and Controls)
- Rebase and repair plan so entropy/salt policy applies after adventure-template resolution.
- Expand `EntropyPolicy.For(GameEntropy)` to add salted culprit reroll for Classic/Adventurous/Wild (using `SaltSource` to reroll `DefaultCulpritIndex` from the template).
- Expand `EntropyPolicy.For(GameEntropy)` to add feature reallocation and Adventurous/Wild variance boundaries.
- Implement the entropy/salt contract: Boring=deterministic, Classic=salted replacement, Adventurous=more variance, Wild=rule-bending.
- Add visible entropy controls to the start-flow UI (the `SetupHuntStep` already has an entropy selector — wire it to the new policy).
- The `AdventureTemplate` and `AdventureTemplateResolver` should NOT need changes — entropy is downstream of the template.
- The `GameSetupResolver` may need a new step between template and resolved setup where entropy applies salted remix. This should be added as a dedicated method, not by expanding the template.
