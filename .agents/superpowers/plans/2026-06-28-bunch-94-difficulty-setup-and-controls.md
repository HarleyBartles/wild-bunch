# BUNCH-94: Difficulty Setup and Controls — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish difficulty as a first-class game setup/control axis by filling the remaining gaps: a deterministic difficulty-distinction test proof, a dev-overlay difficulty control on the Session dev panel, and short player-facing difficulty copy in the start flow — without touching entropy semantics or reimplementing already-landed setup plumbing.

**Architecture:** Difficulty is already wired end-to-end on current main (domain enum → seed codec → generation plan → travel rules profile → session creation → API/DTO → frontend start flow → persistence/rehydration). The remaining work is (1) a domain-level test proving same-seed/same-entropy/different-difficulty produces different difficulty-shaped facts, (2) a dev-only `ForceDevDifficulty` command following the established `ForceDevSaltSource` pattern (dev event → aggregate method → command handler → dev endpoint → frontend panel control), and (3) short descriptive labels for each difficulty option in `SetupHuntStep`. No new persistence shape is needed — `GameDifficulty` is already in the session snapshot and event stream.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, EF Core, xUnit, React 18, TanStack Query, styled-components, Vitest.

## Plan Status

- Plan status: preflight repaired and rebased onto BUNCH-107, ready for approval
- Current route state: `preflight_repaired_pending_approval`
- Base commit: `a2a88e9` (BUNCH-107: refactor seed codec into SeedWorld setup pipeline, #121)
- Branch: `harleydbartles/bunch-94-difficulty-setup-and-controls`
- Worktree: `.worktrees/bunch-94`
- Repairs applied: (1) fixed Task 1 test to use a single seed with different difficulty parameters, since the seed does not encode difficulty; (2) added event store type mapping for `DevDifficultyForced` in `ResolveEventType` plus explicit event round-trip and rehydration tests; (3) extended Task 5 to require derived travel-rule facts in the Session dev panel after forcing difficulty, and strengthened Task 7 browser proof to require observable envelope change, not just a label swap.
- Rebase onto BUNCH-107 applied: updated all seed-codec references from the pre-BUNCH-107 API (`StartingWorldDescriptorResolver`, `StartingWorldDescriptor`, `GameSetupSeedCodec`, `StartingWorldDescriptorSeedMixer`, `GetBaseStartingCash`) to the post-BUNCH-107 API (`SeedWorldResolver`, `SeedWorld`, `DifficultyEnvelope.For`). The seed codec no longer encodes loadout/horse/saddle — those are now pressure-owned via `DifficultyEnvelope`. Starting cash is now `baseCash + 2m` (horse) across all difficulties, so actual starting cash is Easy=30, Standard=25, Challenging=20, Brutal=15. Task 1's `NotEqual` assertions still hold. No other plan tasks were affected by BUNCH-107.

## Preflight Findings

### What already exists on current main

Inspected at commit `a2a88e9` on `main` (post-BUNCH-107 seed codec refactor):

- **Domain enum** (`src/WildBunch.Domain/Travel/GameDifficulty.cs`): `Standard=0, Easy=1, Challenging=2, Brutal=3` — all 4 values.
- **Entropy enum** (`src/WildBunch.Domain/Travel/GameEntropy.cs`): `Boring=0, Classic=1, Adventurous=2, Wild=3` — all 4 values.
- **Seed codec** (`src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`): the seed UUID encodes the seed-owned world/map layer only — world variant, accusation index, default culprit index, cash bonus, town count, prosperity/services palettes (22 bits used, 106 reserved, direct bit-packing O(1) both directions). **Difficulty, entropy, loadout, horse/saddle, and starting town are NOT encoded in the seed** — difficulty and entropy are caller-supplied parameters to `SeededNewGameFactory.Create`; loadout/horse/saddle are pressure-owned via `DifficultyEnvelope.For(GameDifficulty)`; starting town is player/setup-owned via `StartingTownPolicy`. `SeedWorldResolver.Resolve(Guid)` returns a `SeedWorld`; `SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorld)` encodes it back.
- **Difficulty envelope** (`src/WildBunch.GameContent/NewGame/DifficultyEnvelope.cs`): pressure-owned. `DifficultyEnvelope.For(GameDifficulty)` returns starting cash (baseCash + 2m horse: Easy=30, Standard=25, Challenging=20, Brutal=15), `StartingLoadoutProfile.Standard` (transitional — all difficulties get Standard loadout), `StartWithHorse: true`, `IncludeSaddle: true`, and `TravelRules: TravelRulesProfile.For(difficulty)`. BUNCH-94 is expected to expand this mapping per the file's own TODO comment.
- **Game setup pipeline** (`src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`): orchestrates `SeedWorld → DifficultyEnvelope → EntropyPolicy → MysteryTruthResolver → ResolvedGameSetup → GameSession.StartNew`.
- **Travel rules profile** (`src/WildBunch.Domain/Travel/TravelRulesProfile.cs`): distinct profiles per difficulty — canteen capacity, horse death/lame thresholds, ride day progress, lucky/bad-luck rewards/penalties, encounter health/heat/bribe costs.
- **GameSession** (`src/WildBunch.Domain/Game/GameSession.cs`): `GameDifficulty` property, `TravelRules` derived via `TravelRulesProfile.For(GameDifficulty)`, `StartingHealthFor` (Easy=1250, Standard=1000, Challenging=800, Brutal=600).
- **StartNewGameCommand/Handler**: passes `GameDifficulty` through session creation.
- **StartGameRequest (API)**: passes `GameDifficulty` through.
- **GameSessionDto**: exposes `GameDifficulty` and `GameEntropy`.
- **GameStarted event**: carries `GameDifficulty` and `GameEntropy`.
- **Persistence**: `GameDifficulty` and `GameEntropy` round-trip through JSON snapshot (`GameSessionJsonSerializer.SessionSnapshot.cs`) and event stream. Tested in `GameSessionDifficultyPersistenceTests.cs`.
- **Frontend SetupHuntStep** (`src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx`): exposes all 4 difficulty options (Easy, Standard, Challenging, Brutal) and all 4 entropy options (Boring, Classic, Adventurous, Wild) to players.
- **Session dev panel** (`src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`, BUNCH-101): shows `GameDifficulty` and `GameEntropy` as **inspect-only** values. No dev control to change difficulty mid-session.
- **Dev overlay doctrine** (`.agents/dev-overlay/DOCTRINE.md` §2): "Session dev owns game/session-level setup: difficulty, randomness, entropy/seed posture, current phase, high-level scenario setup."

### Product decision update (this turn)

Harley confirmed: **Easy** (difficulty) and **Boring** (entropy) are both player-facing options now. The code already reflects this — `SetupHuntStep.tsx` exposes all 4 options for both axes. No live doctrine or code needs changing for this; the only stale wording is in the historical BUNCH-104 plan file, which is left as-is per Harley's instruction.

> **BUNCH-93 coordination note:** An earlier BUNCH-93 draft/comment once proposed removing Boring from player-facing setup. Harley has superseded that direction: Boring and Easy are both player-facing today. The current BUNCH-93 plan already reflects this and does not remove Boring. BUNCH-94 must preserve Boring in `SetupHuntStep.tsx`. The remaining BUNCH-93/BUNCH-94 coordination is only mechanical overlap (see the clash map below).

### Gaps this plan fills

1. **Difficulty-distinction test proof**: No dedicated test proves that same seed + same entropy + different difficulty parameter produces different difficulty-shaped outputs (not random variance). The seed does not encode difficulty — it is a caller-supplied parameter to `Resolve`. `TravelRulesProfileTests` covers per-profile tuning values but not the end-to-end "same seed, different difficulty parameter → different difficulty-shaped facts" proof the preflight requires.
2. **Dev overlay difficulty control with observable envelope**: The Session dev panel only inspects difficulty. The doctrine says Session dev owns difficulty setup/control. A dev-only `ForceDevDifficulty` command — following the `ForceDevSaltSource` pattern — lets Harley playtest different difficulty travel-rule envelopes without restarting. This changes `GameDifficulty` on the live session, which changes the derived `TravelRules` going forward. It does not retroactively change starting health/cash (those were set at game start). **The dev panel must show derived travel-rule facts (canteen capacity, mounted ride day progress, encounter fight ammo health loss) after forcing difficulty, so the change is observable as a difficulty envelope, not just a label swap.**
3. **Event store type mapping for `DevDifficultyForced`**: The event store deserializer (`GameSessionJsonSerializer.Events.cs` `ResolveEventType`) has an explicit switch mapping event type names to .NET types. A new `DevDifficultyForced` event type must be added to this switch, or loading events from the store will throw `InvalidOperationException("Unknown domain event type: DevDifficultyForced")`. Snapshot persistence carrying `GameDifficulty` is not by itself proof that the new event type round-trips safely — an explicit event store round-trip test is required.
4. **Frontend difficulty copy**: The difficulty options in `SetupHuntStep` are bare labels ("Easy", "Standard", "Challenging", "Brutal") with no description. Adding short flavor text makes difficulty understandable as game pressure, not randomness.
5. **Browser/playtest proof**: The issue requires at least one browser proof that Harley can observe materially different difficulty envelopes — not just that the Session dev panel label changes. The proof must show either derived travel-rule facts changing in the dev panel after forcing difficulty, or a travel preview whose difficulty-derived values (e.g., expected days, canteen capacity) differ between difficulties.

### Doctrine self-healing (execution-time, out of implementation scope)

BUNCH-107 moved difficulty and entropy OUT of the seed into pressure-owned / entropy-owned / player-owned layers. The vendored `wild-bunch-project-doctrine` skill reference at `.agents/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md` still asserts the pre-BUNCH-107 posture on lines 46-47:

> - Difficulty and entropy are part of seed identity.
> - A seed under a different difficulty or entropy is a different world setup.

These are now false. The seed (`SeedWorld`) owns only the world/map layer; difficulty is pressure-owned (`DifficultyEnvelope.For`); entropy is entropy-owned (`EntropyPolicy` + `MysteryTruthResolver`); starting town is player/setup-owned (`StartingTownPolicy`).

**Execution-time task (not implementation work):** On execution, the worker must create a Linear issue in the MARK project to repair the upstream skill reference in `HarleyBartles/agent-asset-marketplace` and re-sync the vendored copy. The vendored skill files must not be hand-edited in this repo — the fix goes upstream and re-syncs via the standard three-command sequence (`git submodule update --remote .agents/plugins/marketplace-source`, `.\scripts\sync-skills.ps1`, `python scripts\generate_index_mesh.py`). This is tracked as a self-healing follow-up, not a BUNCH-94 implementation deliverable, and does not block the difficulty implementation tasks.

### What is explicitly out of scope

- Do not implement BUNCH-93 entropy variance behavior.
- Do not add new hidden-truth exposure to normal DTOs.
- Do not invent generic supplies or collapse wallet/inventory.
- Do not normalize runtime session state into new database tables.
- Do not change the UUID seed codec, descriptor shape, or round-trip behavior.
- Do not retroactively change starting health/cash when dev-forcing difficulty mid-session.
- Do not add a "clear difficulty" operation — the dev can force back to the original difficulty. Unlike salt (which has a "runtime" default), difficulty always has a specific value.
- Do not hand-edit vendored skill files in this repo. The doctrine self-healing above is upstream + re-sync, not a direct edit.

## Global Constraints

- `GameSession` is the live-play aggregate root; all gameplay mutation flows through it.
- Typed domain events are plain sealed records implementing `IDomainEvent`; `Apply` is the single mutation path.
- Dev endpoints live under `/api/dev/` and are gated by `DevRoleGuard.EnsureDevAccess()`.
- Dev DTOs are separate types from player DTOs (per ADR-0030 §7).
- Normal player APIs must remain clean of dev-only state and must not gain dev mutation powers.
- Do not force normal gameplay actions or final gameplay outcomes (dev-overlay doctrine §1 state/action boundary).
- Changing difficulty mid-session is a state mutation — it changes the travel rules profile going forward. It does not force any gameplay action or result.
- `GameDifficulty` is already persisted in the session snapshot and event stream, so rehydration after a difficulty change requires no new persistence shape.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.
- styled-components for component styling; reference design tokens via `var(--token-name)`. No plain CSS classes.
- Expanded mode must use width (cards/columns), not a tall single column (dev-overlay doctrine §4).

### Difficulty mutation falsification

Tests at both domain aggregate and integration levels must prove that `force-difficulty`:
1. Only changes `GameDifficulty` on the session (which changes the derived `TravelRules`).
2. Only produces the `DevDifficultyForced` dev event — no journey, player, saloon, or gameplay events.
3. Does NOT mutate: journey state, current action context, player state (wallet, inventory, health), journal entries, player-facing DTOs, saloon state, entropy, salt source, or any forced encounter/travel/saloon/gameplay outcome.

---

## File Structure

### Domain layer (src/WildBunch.Domain/)

| File | Responsibility |
|------|----------------|
| `Events/DevDifficultyForced.cs` | **Create.** New typed domain event: dev forced the session difficulty to a new value. Dev-only event. |
| `Game/GameSession.cs` (modify) | Add `ForceDevDifficulty(GameDifficulty)` command method and `Apply(DevDifficultyForced)` method. |
| `Game/GameSessionEventReplay.cs` (modify) | Add `DevDifficultyForced` case to `ApplyEvent` switch. |
| `Game/GameSession.cs` `ApplyProducedEvent` (modify) | Add `DevDifficultyForced` case to the produce-time dispatch switch (after the `DevSaltSourceCleared` case, around line 422). |

### Persistence layer (src/WildBunch.Persistence/)

| File | Responsibility |
|------|----------------|
| `Serialization/GameSessionJsonSerializer.Events.cs` (modify) | Add `nameof(DevDifficultyForced) => typeof(DevDifficultyForced)` to `ResolveEventType` switch. Without this, loading a session with a `DevDifficultyForced` event throws. |

### Application layer (src/WildBunch.Application/)

| File | Responsibility |
|------|----------------|
| `Dev/Commands/ForceDevDifficultyCommand.cs` | **Create.** Command record carrying `GameSessionId` and `GameDifficulty`. |
| `Dev/Commands/ForceDevDifficultyHandler.cs` | **Create.** Handler that loads session, calls `ForceDevDifficulty`, stores/commits via `ExecuteWithRetryAsync`. |
| `Dev/Models/ForceDevDifficultyRequestDto.cs` | **Create.** Dev-only request DTO carrying the new difficulty string value. |
| `Dev/Models/SessionDevContextDto.cs` (modify) | Add `TravelRulesDevDto` nested record and a `TravelRules` **record parameter** (the DTO is a positional record — add `TravelRulesDevDto? TravelRules` as the last constructor parameter, not an init-only property) carrying derived travel-rule facts (canteen capacity, ride day progress, encounter health losses). |
| `Dev/Mapping/SessionDevContextMapper.cs` (modify) | Map `session.TravelRules` facts into the new `TravelRules` DTO field. |

### API layer (src/WildBunch.Api/)

| File | Responsibility |
|------|----------------|
| `Dev/DevEndpoints.cs` (modify) | Add `POST /api/dev/sessions/{id}/session/force-difficulty` endpoint. |

### Frontend (src/WildBunch.Web/src/)

| File | Responsibility |
|------|----------------|
| `dev/types.ts` (modify) | Add `ForceDevDifficultyRequestDto` and `TravelRulesDevDto` interfaces; add `travelRules` to `SessionDevContextDto`. |
| `dev/devApi.ts` (modify) | Add `forceDevDifficulty(gameId, request)` function. |
| `dev/panels/SessionDevPanel.tsx` (modify) | Add difficulty control (segmented toggle) and derived travel-rule facts grid in the "Setup posture" section. |
| `components/start-flow/SetupHuntStep.tsx` (modify) | Add short descriptive text under each difficulty option label. |

### Tests

| File | Responsibility |
|------|----------------|
| `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` (modify) | Add `DifficultyChangesDifficultyShapedFactsNotEntropy` test: same seed + same entropy + different difficulty parameter → different starting cash, health, and travel rules. |
| `tests/WildBunch.Domain.Tests/GameSessionDevDifficultyTests.cs` | **Create.** Domain-level tests for `ForceDevDifficulty`: produces correct event, changes `GameDifficulty` and derived `TravelRules`, falsification proof, event serializer round-trip, `RehydrateFromEvents` proof. |
| `tests/WildBunch.Application.Tests/Dev/ForceDevDifficultyHandlerTests.cs` | **Create.** Application-level handler tests. |
| `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs` (modify) | Assert `TravelRules` DTO field is populated and changes with difficulty. |
| `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs` (modify) | Add `ForceDifficulty_Returns204_AndReflectedInContext` integration test. |
| `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx` (modify) | Add tests for difficulty control rendering, mutation call, and derived travel-rule facts visibility. |
| `src/WildBunch.Web/src/tests/StartFlow.test.tsx` (modify) | Add test asserting difficulty descriptions are visible. |

---

## Task 0: Worktree isolation gate (pre-mutation)

**Required by:** BUNCH-94 Linear issue worktree isolation gate.

Before any mutation, the worker must:

- [ ] Work in a fresh dedicated worktree based on current `main` (or confirm the existing `.worktrees/bunch-94` worktree is on the correct branch and base commit).
- [ ] Report worktree path, branch name, base commit, `git status --short` before mutation, and whether any pre-existing dirty state was present.
- [ ] Do not overwrite pre-existing dirty state. If dirty state exists, stop and report it before proceeding.

**Required Linear docs:** Read the following Linear documents before planning/execution (per BUNCH-94 issue):
- "Preflight — difficulty setup and controls"
- "Execution notes — difficulty setup and controls"

---

## Task 1: Difficulty-distinction test proof

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`

**Interfaces:**
- Consumes: `SeededNewGameFactory.Create`, `SeedWorldResolver.FormatSeedCode`, `TravelRulesProfile.For`
- Produces: `DifficultyChangesDifficultyShapedFactsNotEntropy` test proving difficulty ≠ entropy

**Key insight:** The seed UUID does NOT encode difficulty or entropy (post-BUNCH-107). `SeedWorldResolver.Resolve(Guid)` returns a `SeedWorld` carrying only the seed-owned world/map layer (variant, towns, trails, accusation/default culprit candidates, cash bonus). `SeededNewGameFactory.Create(playerName, gameDifficulty, setupSeedCode, gameEntropy, startingTownId?)` takes difficulty and entropy as caller-supplied parameters; difficulty is applied downstream via `DifficultyEnvelope.For(GameDifficulty)`, which owns starting cash, loadout, horse/saddle, and travel rules. The test uses a single seed code (fixing the world) and passes different difficulty parameters to prove difficulty changes difficulty-shaped facts (cash, health, travel rules) while the seed-derived world stays the same.

- [ ] **Step 1: Write the failing test**

Add to `SeededNewGameFactoryTests.cs`:

```csharp
[Fact]
public void DifficultyChangesDifficultyShapedFactsNotEntropy()
{
    var factory = new SeededNewGameFactory();

    // The seed UUID encodes the seed-owned world/map layer only, NOT difficulty or entropy.
    // Difficulty and entropy are caller-supplied parameters to SeededNewGameFactory.Create.
    // Use one seed code to fix the world, then vary only the difficulty parameter.
    var seedCode = SeedWorldResolver.FormatSeedCode(Guid.NewGuid());

    // Same seed, same entropy, different difficulty parameter
    var easy = factory.Create("Ranger Vale", GameDifficulty.Easy, seedCode, GameEntropy.Classic);
    var standard = factory.Create("Ranger Vale", GameDifficulty.Standard, seedCode, GameEntropy.Classic);
    var challenging = factory.Create("Ranger Vale", GameDifficulty.Challenging, seedCode, GameEntropy.Classic);
    var brutal = factory.Create("Ranger Vale", GameDifficulty.Brutal, seedCode, GameEntropy.Classic);

    // Difficulty-shaped facts differ across difficulties (starting cash, starting health, travel rules)
    Assert.NotEqual(easy.Player.Wallet.Cash, standard.Player.Wallet.Cash);
    Assert.NotEqual(standard.Player.Wallet.Cash, challenging.Player.Wallet.Cash);
    Assert.NotEqual(challenging.Player.Wallet.Cash, brutal.Player.Wallet.Cash);

    Assert.NotEqual(easy.Player.Health, standard.Player.Health);
    Assert.NotEqual(standard.Player.Health, challenging.Player.Health);
    Assert.NotEqual(challenging.Player.Health, brutal.Player.Health);

    // Travel rules profiles differ
    Assert.NotEqual(easy.TravelRules.CanteenCapacity, brutal.TravelRules.CanteenCapacity);
    Assert.NotEqual(easy.TravelRules.MountedRideDayProgress, brutal.TravelRules.MountedRideDayProgress);
    Assert.NotEqual(easy.TravelRules.EncounterFightAmmoHealthLoss, brutal.TravelRules.EncounterFightAmmoHealthLoss);

    // Seed-derived world is the same across all four (difficulty does not change the world)
    Assert.Equal(standard.World.Towns.Count, easy.World.Towns.Count);
    Assert.Equal(standard.World.Towns.Count, challenging.World.Towns.Count);
    Assert.Equal(standard.World.Towns.Count, brutal.World.Towns.Count);
    Assert.Equal(standard.Player.CurrentTownId, easy.Player.CurrentTownId);
    Assert.Equal(standard.Player.CurrentTownId, challenging.Player.CurrentTownId);
    Assert.Equal(standard.Player.CurrentTownId, brutal.Player.CurrentTownId);

    // Entropy is the same across all four (difficulty does not change entropy)
    Assert.Equal(GameEntropy.Classic, easy.GameEntropy);
    Assert.Equal(GameEntropy.Classic, standard.GameEntropy);
    Assert.Equal(GameEntropy.Classic, challenging.GameEntropy);
    Assert.Equal(GameEntropy.Classic, brutal.GameEntropy);

    // Salt posture is the same across all four (difficulty does not change salt posture)
    Assert.Equal(easy.SaltSource.Mode, standard.SaltSource.Mode);
    Assert.Equal(standard.SaltSource.Mode, challenging.SaltSource.Mode);
    Assert.Equal(challenging.SaltSource.Mode, brutal.SaltSource.Mode);
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests --filter "DifficultyChangesDifficultyShapedFactsNotEntropy"`
Expected: PASS (the behavior already exists; this is a characterization/proof test)

- [ ] **Step 3: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs
git commit -m "BUNCH-94: add difficulty-distinction test proving difficulty != entropy"
```

---

## Task 2: Dev difficulty control — domain event, aggregate method, and event store mapping

**Files:**
- Create: `src/WildBunch.Domain/Events/DevDifficultyForced.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`

**Interfaces:**
- Consumes: `GameDifficulty` enum, `IDomainEvent`, `ProduceEvent` pattern, `ResolveEventType` switch
- Produces: `DevDifficultyForced` event, `GameSession.ForceDevDifficulty(GameDifficulty)` method, `Apply(DevDifficultyForced)` method, `ResolveEventType` mapping for `DevDifficultyForced`

**Event store mapping requirement:** The event store deserializer in `GameSessionJsonSerializer.Events.cs` has an explicit `ResolveEventType` switch that maps event type name strings to .NET types. Without adding `nameof(DevDifficultyForced) => typeof(DevDifficultyForced)`, any session that has a `DevDifficultyForced` event in its stream will throw `InvalidOperationException("Unknown domain event type: DevDifficultyForced")` when loaded from the store. Snapshot persistence carrying `GameDifficulty` is not by itself proof that the new event type round-trips safely.

- [ ] **Step 1: Write the failing domain test**

Create `tests/WildBunch.Domain.Tests/GameSessionDevDifficultyTests.cs`:

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Town = WildBunch.Domain.World.Town;
using TownId = WildBunch.Domain.World.TownId;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using World = WildBunch.Domain.World.World;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionDevDifficultyTests
{
    [Fact]
    public void ForceDevDifficulty_ChangesGameDifficultyAndTravelRules()
    {
        var session = CreateSeededSession(GameDifficulty.Standard);

        session.ForceDevDifficulty(GameDifficulty.Brutal);

        Assert.Equal(GameDifficulty.Brutal, session.GameDifficulty);
        Assert.Equal(GameDifficulty.Brutal, session.TravelRules.Difficulty);
        // Brutal canteen capacity is 1, Standard is 10
        Assert.Equal(1, session.TravelRules.CanteenCapacity);
    }

    [Fact]
    public void ForceDevDifficulty_ProducesDevDifficultyForcedEvent()
    {
        var session = CreateSeededSession(GameDifficulty.Standard);

        session.ForceDevDifficulty(GameDifficulty.Challenging);

        var evt = Assert.Single(session.UncommittedEvents.OfType<DevDifficultyForced>());
        Assert.Equal(GameDifficulty.Challenging, evt.ForcedDifficulty);
    }

    [Fact]
    public void ForceDevDifficulty_DoesNotMutateOtherState()
    {
        var session = CreateSeededSession(GameDifficulty.Standard);
        var healthBefore = session.Player.Health;
        var cashBefore = session.Player.Wallet.Cash;
        var entropyBefore = session.GameEntropy;
        var saltBefore = session.SaltSource;
        var statusBefore = session.Status;
        var townBefore = session.CurrentTown.TownId;

        session.ForceDevDifficulty(GameDifficulty.Brutal);

        // Falsification: only GameDifficulty and derived TravelRules change
        Assert.Equal(healthBefore, session.Player.Health);
        Assert.Equal(cashBefore, session.Player.Wallet.Cash);
        Assert.Equal(entropyBefore, session.GameEntropy);
        Assert.Equal(saltBefore, session.SaltSource);
        Assert.Equal(statusBefore, session.Status);
        Assert.Equal(townBefore, session.CurrentTown.TownId);
        // Only one event, and it is the dev difficulty event
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevDifficultyForced>(session.UncommittedEvents[0]);
    }

    [Fact]
    public void ForceDevDifficulty_WithInvalidDifficulty_Throws()
    {
        var session = CreateSeededSession(GameDifficulty.Standard);

        Assert.Throws<ArgumentException>(() => session.ForceDevDifficulty((GameDifficulty)999));
    }

    private static GameSession CreateSeededSession(GameDifficulty difficulty)
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new World(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, difficulty,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "GameSessionDevDifficultyTests"`
Expected: FAIL — `DevDifficultyForced` event and `ForceDevDifficulty` method do not exist yet.

- [ ] **Step 3: Create the DevDifficultyForced event**

Create `src/WildBunch.Domain/Events/DevDifficultyForced.cs`:

```csharp
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced the session difficulty to a new value.
/// This is a dev-only event — it records dev intent to change the difficulty
/// envelope (travel rules profile) for playtesting, not a gameplay outcome.
/// The difficulty is persisted in the session snapshot, so rehydration after
/// a difficulty change requires no new persistence shape.
/// See BUNCH-94 and ADR-0030.
/// </summary>
public sealed record DevDifficultyForced : IDomainEvent
{
    public required GameDifficulty ForcedDifficulty { get; init; }
}
```

- [ ] **Step 4: Add ForceDevDifficulty method and Apply to GameSession**

In `src/WildBunch.Domain/Game/GameSession.cs`, add the command method near `ForceDevSaltSource` (after line ~1170):

```csharp
/// <summary>
/// Dev command: forces the session difficulty to a new value for playtesting.
/// Changes the travel rules profile going forward. Does not retroactively
/// change starting health/cash (those were set at game start).
/// Per dev-overlay doctrine §1 (state/action boundary). See BUNCH-94.
/// </summary>
public void ForceDevDifficulty(GameDifficulty difficulty)
{
    if (!Enum.IsDefined(typeof(GameDifficulty), difficulty))
    {
        throw new ArgumentException("Invalid game difficulty value.", nameof(difficulty));
    }

    ProduceEvent(new DevDifficultyForced
    {
        ForcedDifficulty = difficulty
    });
}
```

Add the Apply method near `Apply(DevSaltSourceForced)` (after line ~759):

```csharp
/// <summary>
/// Applies a DevDifficultyForced event. Changes the session difficulty,
/// which changes the derived TravelRules profile. Dev-only event — does
/// not affect starting health/cash or any other gameplay state directly.
/// See BUNCH-94.
/// </summary>
internal void Apply(DevDifficultyForced e)
{
    GameDifficulty = e.ForcedDifficulty;
    _version++;
}
```

- [ ] **Step 5: Add DevDifficultyForced to event replay switch**

In `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, add to the `ApplyEvent` switch (near the `DevSaltSourceForced` case):

```csharp
case DevDifficultyForced ddf:
    session.Apply(ddf);
    break;
```

- [ ] **Step 6: Add DevDifficultyForced to produce-time dispatch switch**

In `src/WildBunch.Domain/Game/GameSession.cs`, add to the `ApplyProducedEvent` switch (near line 390, after `DevSaltSourceCleared`):

```csharp
case DevDifficultyForced ddf:
    Apply(ddf);
    break;
```

- [ ] **Step 7: Add DevDifficultyForced to event store type mapping**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`, add to the `ResolveEventType` switch (after the `DevSaltSourceCleared` case, before the default throw):

```csharp
nameof(DevDifficultyForced) => typeof(DevDifficultyForced),
```

Without this, loading a session that has a `DevDifficultyForced` event in its stream will throw `InvalidOperationException("Unknown domain event type: DevDifficultyForced")`. Snapshot persistence carrying `GameDifficulty` is not by itself proof that the new event type round-trips safely.

- [ ] **Step 8: Write the event store round-trip test**

Add to `tests/WildBunch.Domain.Tests/GameSessionDevDifficultyTests.cs`:

```csharp
[Fact]
public void DevDifficultyForced_RoundTripsThroughEventSerializer()
{
    var serializer = new WildBunch.Persistence.Serialization.GameSessionJsonSerializer();
    var forced = new DevDifficultyForced
    {
        ForcedDifficulty = GameDifficulty.Brutal
    };

    var json = serializer.SerializeEvent(forced);
    var reloaded = serializer.DeserializeEvent(nameof(DevDifficultyForced), json);

    var roundTripped = Assert.IsType<DevDifficultyForced>(reloaded);
    Assert.Equal(GameDifficulty.Brutal, roundTripped.ForcedDifficulty);
}

[Fact]
public void DevDifficultyForced_RehydratesFromEventStream()
{
    var session = CreateSeededSession(GameDifficulty.Standard);
    session.ForceDevDifficulty(GameDifficulty.Challenging);
    var events = session.UncommittedEvents.ToList();
    session.MarkEventsCommitted();

    var rehydrated = GameSession.RehydrateFromEvents(
        session.Id, session.World, session.CaseFile, events);

    Assert.Equal(GameDifficulty.Challenging, rehydrated.GameDifficulty);
    Assert.Equal(GameDifficulty.Challenging, rehydrated.TravelRules.Difficulty);
}
```

Note: The first test requires a reference to `WildBunch.Persistence` from the domain test project. If that reference does not already exist, add it to `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`. Alternatively, place this test in `tests/WildBunch.Integration.Tests` where the persistence reference already exists.

- [ ] **Step 9: Run domain tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "GameSessionDevDifficultyTests"`
Expected: PASS

- [ ] **Step 10: Run full domain test suite to verify no regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: PASS (all existing tests still pass)

- [ ] **Step 11: Commit**

```bash
git add src/WildBunch.Domain/Events/DevDifficultyForced.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSessionEventReplay.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs tests/WildBunch.Domain.Tests/GameSessionDevDifficultyTests.cs
git commit -m "BUNCH-94: add ForceDevDifficulty domain event, aggregate method, and event store mapping"
```

---

## Task 3: Dev difficulty control — application command and handler

**Files:**
- Create: `src/WildBunch.Application/Dev/Commands/ForceDevDifficultyCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceDevDifficultyHandler.cs`
- Create: `src/WildBunch.Application/Dev/Models/ForceDevDifficultyRequestDto.cs`

**Interfaces:**
- Consumes: `GameSessionCommandHandler.ExecuteWithRetryAsync`, `GameSession.ForceDevDifficulty`, `GameDifficulty`
- Produces: `ForceDevDifficultyCommand`, `ForceDevDifficultyHandler`, `ForceDevDifficultyRequestDto`

- [ ] **Step 1: Write the failing handler test**

Create `tests/WildBunch.Application.Tests/Dev/ForceDevDifficultyHandlerTests.cs`:

```csharp
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Town = WildBunch.Domain.World.Town;
using TownId = WildBunch.Domain.World.TownId;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using World = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class ForceDevDifficultyHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForcesDifficultyAndPersists()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession(GameDifficulty.Standard);
        repository.Seed(session);

        var handler = new ForceDevDifficultyHandler(repository, new InMemoryGameSessionUnitOfWork());

        await handler.HandleAsync(new ForceDevDifficultyCommand(session.Id.Value, GameDifficulty.Brutal));

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(GameDifficulty.Brutal, reloaded!.GameDifficulty);
    }

    [Fact]
    public async Task HandleAsync_DoesNotChangeEntropyOrSalt()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession(GameDifficulty.Standard);
        var entropyBefore = session.GameEntropy;
        var saltBefore = session.SaltSource;
        repository.Seed(session);

        var handler = new ForceDevDifficultyHandler(repository, new InMemoryGameSessionUnitOfWork());

        await handler.HandleAsync(new ForceDevDifficultyCommand(session.Id.Value, GameDifficulty.Challenging));

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.Equal(entropyBefore, reloaded!.GameEntropy);
        Assert.Equal(saltBefore.Mode, reloaded.SaltSource.Mode);
    }

    private static GameSession CreateSeededSession(GameDifficulty difficulty)
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new World(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, difficulty,
            SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "ForceDevDifficultyHandlerTests"`
Expected: FAIL — types do not exist yet.

- [ ] **Step 3: Create the command record**

Create `src/WildBunch.Application/Dev/Commands/ForceDevDifficultyCommand.cs`:

```csharp
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed record ForceDevDifficultyCommand(
    Guid GameSessionId,
    GameDifficulty Difficulty);
```

- [ ] **Step 4: Create the request DTO**

Create `src/WildBunch.Application/Dev/Models/ForceDevDifficultyRequestDto.cs`:

```csharp
namespace WildBunch.Application.Dev.Models;

public sealed record ForceDevDifficultyRequestDto(string Difficulty);
```

- [ ] **Step 5: Create the handler**

Create `src/WildBunch.Application/Dev/Commands/ForceDevDifficultyHandler.cs`:

```csharp
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceDevDifficultyHandler : GameSessionCommandHandler
{
    public ForceDevDifficultyHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceDevDifficultyCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ForceDevDifficulty(command.Difficulty);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 6: Run handler tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "ForceDevDifficultyHandlerTests"`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Application/Dev/Commands/ForceDevDifficultyCommand.cs src/WildBunch.Application/Dev/Commands/ForceDevDifficultyHandler.cs src/WildBunch.Application/Dev/Models/ForceDevDifficultyRequestDto.cs tests/WildBunch.Application.Tests/Dev/ForceDevDifficultyHandlerTests.cs
git commit -m "BUNCH-94: add ForceDevDifficulty application command and handler"
```

---

## Task 4: Dev difficulty control — API endpoint

**Files:**
- Modify: `src/WildBunch.Api/Dev/DevEndpoints.cs`

**Interfaces:**
- Consumes: `ForceDevDifficultyHandler`, `ForceDevDifficultyRequestDto`, `DevRoleGuard`, `GameDifficulty`
- Produces: `POST /api/dev/sessions/{id}/session/force-difficulty` endpoint

- [ ] **Step 1: Write the failing integration test**

Add to `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs`:

```csharp
[Fact]
public async Task ForceDifficulty_Returns204_AndReflectedInContext()
{
    using var factory = new PostgreSqlApiFactory();
    using var client = factory.CreateClient();
    var gameId = await CreateSessionAsync(client);

    var forceResponse = await client.PostAsJsonAsync(
        $"/api/dev/sessions/{gameId}/session/force-difficulty",
        new ForceDevDifficultyRequestDto(Difficulty: "Brutal"));
    Assert.Equal(HttpStatusCode.NoContent, forceResponse.StatusCode);

    var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
        .Content.ReadFromJsonAsync<SessionDevContextDto>();
    Assert.Equal("Brutal", context!.GameDifficulty);
}

[Fact]
public async Task ForceDifficulty_Returns400_ForInvalidDifficulty()
{
    using var factory = new PostgreSqlApiFactory();
    using var client = factory.CreateClient();
    var gameId = await CreateSessionAsync(client);

    var forceResponse = await client.PostAsJsonAsync(
        $"/api/dev/sessions/{gameId}/session/force-difficulty",
        new ForceDevDifficultyRequestDto(Difficulty: "Nightmare"));
    Assert.Equal(HttpStatusCode.BadRequest, forceResponse.StatusCode);
}

[Fact]
public async Task ForceDifficulty_Returns403_InNonDevEnvironment()
{
    using var factory = new NonDevApiFactory();
    using var client = factory.CreateClient();
    var response = await client.PostAsJsonAsync(
        $"/api/dev/sessions/{Guid.NewGuid()}/session/force-difficulty",
        new ForceDevDifficultyRequestDto(Difficulty: "Brutal"));
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "ForceDifficulty"`
Expected: FAIL — endpoint does not exist yet.

- [ ] **Step 3: Add the endpoint to DevEndpoints.cs**

In `src/WildBunch.Api/Dev/DevEndpoints.cs`, add the route registration after the `clear-rng` route (after line 74):

```csharp
dev.MapPost("/sessions/{id:guid}/session/force-difficulty", ForceDevDifficultyAsync)
    .WithName("ForceDevDifficulty")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest);
```

Add the handler method near `ClearRngAsync` (after line ~336):

```csharp
private static async Task<IResult> ForceDevDifficultyAsync(
    Guid id,
    DevRoleGuard guard,
    ForceDevDifficultyHandler handler,
    ForceDevDifficultyRequestDto? request,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        if (request is null || string.IsNullOrWhiteSpace(request.Difficulty))
        {
            return Results.BadRequest("Difficulty is required.");
        }

        if (!Enum.TryParse<GameDifficulty>(request.Difficulty, ignoreCase: true, out var difficulty)
            || !Enum.IsDefined(typeof(GameDifficulty), difficulty))
        {
            return Results.BadRequest($"Invalid difficulty value: {request.Difficulty}");
        }

        await handler.HandleAsync(new ForceDevDifficultyCommand(id, difficulty), cancellationToken);
        return Results.NoContent();
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
}
```

Add the necessary `using` directives at the top of the file if not already present:

```csharp
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Travel;
```

- [ ] **Step 4: Run integration tests to verify they pass**

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "ForceDifficulty"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/Dev/DevEndpoints.cs tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs
git commit -m "BUNCH-94: add force-difficulty dev endpoint"
```

---

## Task 5: Dev difficulty control — backend DTO extension and frontend Session dev panel

**Goal:** The dev panel must show derived travel-rule facts after forcing difficulty, so the change is observable as a difficulty envelope, not just a label swap. This requires extending `SessionDevContextDto` to carry a few derived travel-rule facts from the session's `TravelRules` profile.

**Files:**
- Modify: `src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs`
- Modify: `src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs`
- Modify: `src/WildBunch.Web/src/dev/types.ts`
- Modify: `src/WildBunch.Web/src/dev/devApi.ts`
- Modify: `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`
- Modify: `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx`
- Modify: `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs`

**Interfaces:**
- Consumes: `SessionDevContextDto`, `forceDevDifficulty` API, `SegmentedToggle` component, `TravelRulesProfile`
- Produces: difficulty control + derived travel-rule facts in the "Setup posture" section of `SessionDevPanel`

- [ ] **Step 1: Extend SessionDevContextDto with derived travel-rule facts**

In `src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs`, add a nested DTO for derived travel-rule facts:

```csharp
public sealed record TravelRulesDevDto(
    int CanteenCapacity,
    decimal MountedRideDayProgress,
    decimal FootRideDayProgress,
    int EncounterFightAmmoHealthLoss,
    int EncounterFightUnarmedHealthLoss,
    int EncounterRunFootHealthLoss);
```

Add `TravelRules` as a **record parameter** to `SessionDevContextDto` (it is a positional record — do not use an init-only property):

```csharp
public sealed record SessionDevContextDto(
    Guid SessionId,
    string Status,
    string GameDifficulty,
    string GameEntropy,
    SaltPostureDevDto SaltPosture,
    ClockDevDto Clock,
    string? CurrentTownId,
    string? CurrentTownName,
    string CurrentActionContext,
    bool HasActiveJourney,
    bool SeedCodeRetained,
    string? SeedCodeText,
    TravelRulesDevDto? TravelRules);
```

- [ ] **Step 2: Map derived travel-rule facts in SessionDevContextMapper**

In `src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs`, add the travel-rule facts to the DTO mapping:

```csharp
TravelRules = new TravelRulesDevDto(
    session.TravelRules.CanteenCapacity,
    session.TravelRules.MountedRideDayProgress,
    session.TravelRules.FootRideDayProgress,
    session.TravelRules.EncounterFightAmmoHealthLoss,
    session.TravelRules.EncounterFightUnarmedHealthLoss,
    session.TravelRules.EncounterRunFootHealthLoss),
```

- [ ] **Step 3: Update GetSessionDevContextHandlerTests to assert travel-rule facts**

In `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs`, add assertions to the existing `HandleAsync_ReturnsSessionContext_WithSetupPosture` test:

```csharp
Assert.NotNull(result.TravelRules);
Assert.Equal(session.TravelRules.CanteenCapacity, result.TravelRules!.CanteenCapacity);
Assert.Equal(session.TravelRules.MountedRideDayProgress, result.TravelRules.MountedRideDayProgress);
Assert.Equal(session.TravelRules.EncounterFightAmmoHealthLoss, result.TravelRules.EncounterFightAmmoHealthLoss);
```

Add a new test proving travel-rule facts change with difficulty:

```csharp
[Fact]
public async Task HandleAsync_ReflectsTravelRulesForCurrentDifficulty()
{
    var repository = new InMemoryGameSessionRepository();
    var session = CreateSeededSession(); // Standard difficulty
    repository.Seed(session);

    var handler = new GetSessionDevContextHandler(repository);

    var standardResult = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));
    Assert.Equal(10, standardResult.TravelRules!.CanteenCapacity); // Standard canteen

    session.ForceDevDifficulty(GameDifficulty.Brutal);
    session.MarkEventsCommitted();
    repository.Seed(session);

    var brutalResult = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));
    Assert.Equal(1, brutalResult.TravelRules!.CanteenCapacity); // Brutal canteen
    Assert.NotEqual(standardResult.TravelRules.CanteenCapacity, brutalResult.TravelRules.CanteenCapacity);
}
```

- [ ] **Step 4: Run backend tests to verify the DTO extension**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "GetSessionDevContextHandlerTests"`
Expected: PASS

- [ ] **Step 5: Add the request DTO type to frontend**

In `src/WildBunch.Web/src/dev/types.ts`, add after `LockRngRequestDto`:

```typescript
export interface ForceDevDifficultyRequestDto {
  difficulty: string;
}

export interface TravelRulesDevDto {
  canteenCapacity: number;
  mountedRideDayProgress: number;
  footRideDayProgress: number;
  encounterFightAmmoHealthLoss: number;
  encounterFightUnarmedHealthLoss: number;
  encounterRunFootHealthLoss: number;
}
```

Add `travelRules` to `SessionDevContextDto`:

```typescript
export interface SessionDevContextDto {
  sessionId: string;
  status: string;
  gameDifficulty: string;
  gameEntropy: string;
  saltPosture: SaltPostureDevDto;
  clock: ClockDevDto;
  currentTownId: string | null;
  currentTownName: string | null;
  currentActionContext: string;
  hasActiveJourney: boolean;
  seedCodeRetained: boolean;
  seedCodeText: string | null;
  travelRules: TravelRulesDevDto | null;
}
```

- [ ] **Step 6: Add the API function**

In `src/WildBunch.Web/src/dev/devApi.ts`, add after `clearRng`:

```typescript
export function forceDevDifficulty(gameId: string, request: ForceDevDifficultyRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/session/force-difficulty`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}
```

Add `ForceDevDifficultyRequestDto` to the import from `./types`.

- [ ] **Step 7: Add the difficulty control and derived travel-rule facts to SessionDevPanel**

In `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`:

1. Import `forceDevDifficulty` from `../devApi` (add to existing import on line 5).
2. Import `SegmentedToggle` from `../../components/start-flow/SegmentedToggle`.
3. Add a `difficultyOptions` constant. The dev panel DTO carries `gameDifficulty` as a string (e.g. `"Standard"`), and the `forceDevDifficulty` API accepts a string, so use string values here — not the numeric `GameDifficulty` enum used in `SetupHuntStep`. `SegmentedToggle` is generic over `T extends string | number`, so string values are valid:

```typescript
const difficultyOptions: ReadonlyArray<{ value: string; label: string }> = [
  { value: "Easy", label: "Easy" },
  { value: "Standard", label: "Standard" },
  { value: "Challenging", label: "Challenging" },
  { value: "Brutal", label: "Brutal" },
];
```

4. Add a `handleForceDifficulty` function:

```typescript
const handleForceDifficulty = async (value: string) => {
  setError(null);
  setActionPending(true);
  try {
    await forceDevDifficulty(gameId, { difficulty: value });
    refresh();
  } catch (err) {
    setError(err instanceof Error ? err.message : "Failed to force difficulty.");
  } finally {
    setActionPending(false);
  }
};
```

5. Replace the "Difficulty (inspect):" row in the "Setup posture" section with a control plus derived travel-rule facts:

Replace:
```tsx
<Row>
  <Label>Difficulty (inspect):</Label>
  <Value>{data?.gameDifficulty}</Value>
</Row>
```

With:
```tsx
<Field>
  <Label>Difficulty:</Label>
  <SegmentedToggle
    options={difficultyOptions}
    value={data?.gameDifficulty ?? "Standard"}
    onSelect={handleForceDifficulty}
  />
</Field>
<MutedText>
  Forcing difficulty changes travel rules going forward. It does not change starting health or cash.
</MutedText>
<TravelRulesGrid>
  <Row>
    <Label>Canteen capacity:</Label>
    <Value>{data?.travelRules?.canteenCapacity ?? "—"}</Value>
  </Row>
  <Row>
    <Label>Mounted ride/day:</Label>
    <Value>{data?.travelRules?.mountedRideDayProgress ?? "—"}</Value>
  </Row>
  <Row>
    <Label>Foot ride/day:</Label>
    <Value>{data?.travelRules?.footRideDayProgress ?? "—"}</Value>
  </Row>
  <Row>
    <Label>Encounter fight (ammo) health loss:</Label>
    <Value>{data?.travelRules?.encounterFightAmmoHealthLoss ?? "—"}</Value>
  </Row>
  <Row>
    <Label>Encounter fight (unarmed) health loss:</Label>
    <Value>{data?.travelRules?.encounterFightUnarmedHealthLoss ?? "—"}</Value>
  </Row>
  <Row>
    <Label>Encounter run (foot) health loss:</Label>
    <Value>{data?.travelRules?.encounterRunFootHealthLoss ?? "—"}</Value>
  </Row>
</TravelRulesGrid>
```

6. Add the styled component:

```typescript
const TravelRulesGrid = styled.div`
  display: grid;
  gap: 0.25rem;
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  border-top: 1px solid color-mix(in srgb, var(--border) 50%, transparent);
`;
```

- [ ] **Step 8: Write the frontend tests**

In `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx`, add tests:

```typescript
it("renders difficulty control and calls forceDevDifficulty on select", async () => {
  // Mock the session dev context with Standard difficulty and travel rules
  // Render the panel
  // Assert the difficulty SegmentedToggle is visible
  // Assert travel-rule facts (canteen capacity, etc.) are visible
  // Click "Brutal"
  // Assert forceDevDifficulty was called with { difficulty: "Brutal" }
});

it("shows derived travel-rule facts that change with difficulty", async () => {
  // Mock session dev context with Standard difficulty (canteen capacity 10)
  // Render the panel
  // Assert "10" is visible for canteen capacity
  // Re-mock with Brutal difficulty (canteen capacity 1) after refresh
  // Assert "1" is visible for canteen capacity
});
```

Follow the existing test patterns in `SessionDevPanel.test.tsx` for mocking `useGameSession`, `useQuery`, and the dev API.

- [ ] **Step 9: Run frontend tests**

Run: `cd src/WildBunch.Web && npm test -- --run SessionDevPanel`
Expected: PASS

- [ ] **Step 10: Run typecheck and build**

Run: `cd src/WildBunch.Web && npm run typecheck && npm run build`
Expected: PASS

- [ ] **Step 11: Commit**

```bash
git add src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs src/WildBunch.Web/src/dev/types.ts src/WildBunch.Web/src/dev/devApi.ts src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs
git commit -m "BUNCH-94: add difficulty control and derived travel-rule facts to Session dev panel"
```

---

## Task 6: Frontend difficulty copy in start flow

**Files:**
- Modify: `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx`
- Modify: `src/WildBunch.Web/src/tests/StartFlow.test.tsx`

**Interfaces:**
- Consumes: existing `difficultyOptions` array, `SegmentedToggle` component
- Produces: short descriptive text under each difficulty option

- [ ] **Step 1: Add difficulty descriptions**

In `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx`, the `difficultyOptions` array already exists (lines 23-28) — do NOT re-declare it. Only add a `difficultyDescriptions` map keyed by `GameDifficulty`:

```typescript
const difficultyDescriptions: Record<GameDifficulty, string> = {
  1: "Forgiving trails, generous supplies, softer consequences.",
  0: "A fair chase. The trail bites back but gives ground.",
  2: "Hard riding, thin margins, and costly mistakes.",
  3: "The desert wants you dead. Every ride is a gamble.",
};
```

Add a description line below the difficulty `SegmentedToggle` inside the existing `<FieldGroup>` (the `FieldGroup` and `GroupLabel` styled components already exist at lines 183/188):

```tsx
<FieldGroup>
  <GroupLabel>Difficulty</GroupLabel>
  <SegmentedToggle
    options={difficultyOptions}
    value={gameDifficulty}
    onSelect={onGameDifficultyChange}
  />
  <DifficultyDescription>
    {difficultyDescriptions[gameDifficulty]}
  </DifficultyDescription>
</FieldGroup>
```

Add the styled component (local to this file, per the styling stack rule):

```typescript
const DifficultyDescription = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 55%, transparent);
  font-size: 0.85rem;
  font-style: italic;
`;
```

- [ ] **Step 2: Write the frontend test**

In `src/WildBunch.Web/src/tests/StartFlow.test.tsx`, add a test:

```typescript
it("shows a description for the selected difficulty", async () => {
  primeMocks();
  const user = userEvent.setup();
  renderSurface();

  await screen.findByRole("heading", { name: /set up your hunt/i });

  // Default difficulty is Standard (0) — check its description is visible
  expect(screen.getByText(/a fair chase/i)).toBeInTheDocument();

  // Click "Brutal" and check its description appears
  await user.click(screen.getByRole("button", { name: /brutal/i }));
  expect(screen.getByText(/the desert wants you dead/i)).toBeInTheDocument();
});
```

- [ ] **Step 3: Run frontend tests**

Run: `cd src/WildBunch.Web && npm test -- --run StartFlow`
Expected: PASS

- [ ] **Step 4: Run typecheck and build**

Run: `cd src/WildBunch.Web && npm run typecheck && npm run build`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx src/WildBunch.Web/src/tests/StartFlow.test.tsx
git commit -m "BUNCH-94: add difficulty descriptions to start flow"
```

---

## Task 7: Full validation and browser proof

**Files:**
- No new files — validation and evidence only

- [ ] **Step 1: Run backend build**

Run: `dotnet build`
Expected: PASS with no errors

- [ ] **Step 2: Run PostgreSQL ensure**

Run: `.\scripts\postgres-dev.ps1 ensure`
Expected: service healthy or no-op

- [ ] **Step 3: Run full backend test suite**

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test`
Expected: PASS (all tests pass including new difficulty tests)

- [ ] **Step 4: Run EF migrations list**

Run: `dotnet tool restore; dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: no new migrations needed (no schema change)

- [ ] **Step 5: Run frontend typecheck, test, and build**

Run: `cd src/WildBunch.Web && npm run typecheck && npm test -- --run && npm run build`
Expected: PASS

- [ ] **Step 6: Generate index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: no changes or only expected INDEX.md updates

- [ ] **Step 7: Browser/playtest proof — observable difficulty envelope**

Start the API and frontend dev servers. Open the browser. Take screenshots showing:

1. **Start flow difficulty copy**: The difficulty descriptions in the start flow (Standard and Brutal visible with their flavor text)
2. **Dev panel difficulty control with observable envelope**: Open the Session dev panel. Note the current difficulty (Standard) and the derived travel-rule facts (canteen capacity = 10, mounted ride/day = 1.0, encounter fight ammo health loss = 5). Force difficulty to Brutal. After refresh, observe:
   - The difficulty label changed to "Brutal"
   - The derived travel-rule facts changed (canteen capacity = 1, mounted ride/day = 0.5, encounter fight ammo health loss = higher)
   This proves forcing difficulty changes a materially different difficulty envelope, not just a label.
3. **Dev panel compact AND expanded mode** (dev-overlay doctrine §9 closeout proof): Screenshot the Session dev panel in compact mode (default, with `Expand` button visible) and in expanded mode (after clicking Expand, with `Shrink` button visible). Both must show the difficulty control and derived travel-rule facts. Expanded mode must use width (two columns), not a tall single column (dev-overlay doctrine §4).
4. **Normal start-flow proof** (BUNCH-94 issue goal: "start, observe, and playtest materially different difficulty envelopes from the normal setup flow"): Start two new games from the normal setup flow with the same seed but different difficulties (Standard and Brutal). Screenshot the start-flow difficulty descriptions (Task 6 copy). After each game starts, open the Session dev panel and screenshot the derived travel-rule facts for each. The facts must differ between Standard and Brutal (canteen capacity, ride/day, encounter health losses). This proves the normal setup flow produces materially different difficulty envelopes, not just the dev overlay force path.
5. **Optional — travel preview proof**: If feasible without a long playtest, start a journey and observe that the travel preview reflects the new difficulty's ride-day progress (Brutal's slower mounted progress → more expected days than Standard for the same trail).

Save screenshots to `.agents/superpowers/output/screenshots/` (git-ignored).

The proof must show that derived travel-rule facts change when difficulty is forced — a label-only change is not sufficient evidence for BUNCH-94's goal. Per dev-overlay doctrine §9, closeout proof must include compact and expanded mode screenshots and test results for backend domain/application/API and frontend tests.

- [ ] **Step 8: Grep proof — no stale travel-only names**

Run: `rg -i "TravelDifficulty" src/ tests/`
Expected: no matches (difficulty is `GameDifficulty`, not `TravelDifficulty`)

- [ ] **Step 9: Commit any remaining changes**

```bash
git add -A
git commit -m "BUNCH-94: validation and index mesh"
```

---

## DOD Mapping

| Issue requirement | Plan task | Evidence |
|---|---|---|
| Difficulty is a first-class setup/control axis | Already on main + Tasks 1-6 | Existing code + new tests |
| Harley can start, observe, and playtest materially different difficulty envelopes | Tasks 1, 5, 6, 7 | Distinction test, dev panel control + derived travel-rule facts, start-flow copy, browser proof showing envelope change |
| Backend unit/integration coverage for difficulty effects | Tasks 1, 2, 3, 4 | Domain, application, integration tests |
| Event store round-trip for new `DevDifficultyForced` event | Task 2 (Steps 7-8) | `ResolveEventType` mapping + event serializer round-trip test + `RehydrateFromEvents` test |
| Persistence/rehydration where touched | No new persistence shape needed | `GameDifficulty` already in snapshot; event store mapping added in Task 2 |
| API/DTO checks | Tasks 4, 5 | Integration endpoint tests + DTO extension tests |
| Frontend tests/typecheck/build | Tasks 5, 6 | Vitest tests, typecheck, build |
| Browser/playtest proof — observable envelope | Task 7 | Screenshots showing derived travel-rule facts change when difficulty is forced (dev overlay) AND when starting with different difficulties (normal start flow) |
| Difficulty stayed distinct from entropy | Task 1 | `DifficultyChangesDifficultyShapedFactsNotEntropy` test |
| DOD mapping | This section | This table |

## Return evidence checklist

**Required by:** BUNCH-94 Linear issue return-evidence section. The worker return must include all of the following:

- [ ] Branch name
- [ ] PR URL (if created)
- [ ] Base commit SHA
- [ ] Final head SHA
- [ ] Changed files list
- [ ] Validation commands and results (`dotnet build`, `dotnet test`, `postgres-dev.ps1 validate`, `npm run typecheck`, `npm run build`, `npm test`)
- [ ] Screenshots or browser evidence for the player/dev-facing control (dev overlay force-difficulty + normal start-flow Standard vs Brutal)
- [ ] DOD mapping that proves difficulty stayed distinct from entropy
- [ ] Worktree isolation gate report (Task 0: worktree path, branch, base commit, pre-mutation `git status --short`, dirty state confirmation)
- [ ] GREEN cleanup proof if validation touched local workspace resources (per AGENTS.md GREEN Standard)

## Coordination with BUNCH-93

BUNCH-93 (entropy) may run in parallel. This plan does not depend on unmerged BUNCH-93 work. If BUNCH-93 lands first, rebase onto current main and repair mechanical conflicts. If both touch the same start-flow/dev-overlay files, keep changes minimal and do not overwrite the other axis. The difficulty control in `SessionDevPanel` is in the "Setup posture" section alongside the existing entropy inspect line — they are adjacent but separate controls.

### Boring player-facing status (historical note)

An earlier BUNCH-93 draft/comment once proposed removing Boring from player-facing setup. Harley has superseded that direction: **Boring (entropy) and Easy (difficulty) are both player-facing options today.** They may become dev/test-only in a later product decision, but not now. The current BUNCH-93 plan already reflects this and does not remove Boring. BUNCH-94 must preserve Boring in `SetupHuntStep.tsx`. There is no active contradiction between the two plans.

### Mechanical clash map (all resolvable by rebase)

Both plans modify the same files. All clashes are additive (new switch cases, new routes, new functions) and resolvable by rebase when one lands first:

| File | BUNCH-94 | BUNCH-93 | Clash type |
|------|----------|----------|------------|
| `GameSession.cs` | `ForceDevDifficulty` + `Apply(DevDifficultyForced)` + `ApplyProducedEvent` case | `SetDevEntropy` + `Apply(DevEntropyChanged)` + same switch | Additive cases in same switch |
| `GameSessionEventReplay.cs` | `DevDifficultyForced` case | `DevEntropyChanged` case | Additive cases in same switch |
| `GameSessionJsonSerializer.Events.cs` | `DevDifficultyForced` in `ResolveEventType` | `DevEntropyChanged` in same switch | Additive cases in same switch |
| `DevEndpoints.cs` | `POST .../force-difficulty` | `POST .../set-entropy` | Different routes |
| `SessionDevPanel.tsx` | Replaces "Difficulty (inspect)" row (line 121) with control + travel-rules grid | Replaces "Entropy (inspect)" row (line 125) with control | Same "Setup posture" section, adjacent rows — mechanical conflict on rebase, resolvable by keeping both controls |
| `SetupHuntStep.tsx` | Adds `difficultyDescriptions` in difficulty `FieldGroup` | Updates entropy labels/group label for volatility framing (Boring stays player-facing) | Same file, different sections — low conflict risk |
| `devApi.ts` | Adds `forceDevDifficulty` | Adds `setDevEntropy` | Different functions |
| `dev/types.ts` | Adds `ForceDevDifficultyRequestDto`/`TravelRulesDevDto` | Adds `SetDevEntropyRequestDto` | Different types |
| `SessionDevContextDto.cs` | Adds `TravelRules` record parameter | Unchanged | No clash |
| `SessionDevContextMapper.cs` | Adds travel-rule mapping | Unchanged | No clash |

### Doctrine self-healing ownership

Both plans independently identified the same stale doctrine in `wild-bunch-project-doctrine` (lines 46-47). BUNCH-94 owns the execution-time MARK issue creation (see "Doctrine self-healing" section above). BUNCH-93 defers it. The MARK issue is created once, not twice.
