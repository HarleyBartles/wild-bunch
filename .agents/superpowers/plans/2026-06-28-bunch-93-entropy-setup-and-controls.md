# BUNCH-93 — Entropy setup and controls

**Issue:** [BUNCH-93 — Entropy setup and controls](https://linear.app/harleys-workspace/issue/BUNCH-93/entropy-setup-and-controls)
**Branch:** `harleydbartles/bunch-93-entropy-implementation`
**Base commit:** `bb9f5a5` (== `origin/main` after BUNCH-94 difficulty setup and controls)
**Plan type:** Execution plan (approved via PR #119; this is the collapsed implementation PR)

## Repo-skill grounding (read during preflight)

- `.agents/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md` — canonical entropy envelope definitions (Boring/Classic/Adventurous/Wild). Task 1 design aligns with this.
- `.agents/skills/wild-bunch-domain-modeling/SKILL.md` — GameSession aggregate root, travel/journey state, trail-day progression.
- `.agents/skills/wild-bunch-dotnet-architecture/SKILL.md` — domain owns rules, application orchestrates, persistence is JSON snapshot, no early table normalization.
- `.agents/skills/verification-before-completion/SKILL.md` — return evidence + issue-goal conformance + falsification.
- `.agents/skills/game-playtest/SKILL.md` — browser playtest + screenshot evidence for the player/dev-facing control proof.
- `.agents/skills/repo-worker-base/SKILL.md` — fresh-main invariant, worktree isolation gate, GREEN gate, required return evidence.

## Preflight investigation summary (current main post-BUNCH-94)

### What is already complete on main (preserve, do not redo)

- `GameEntropy` enum (`Boring`, `Classic`, `Adventurous`, `Wild`) — `src/WildBunch.Domain/Travel/GameEntropy.cs`.
- `GameDifficulty` enum — `src/WildBunch.Domain/Travel/GameDifficulty.cs`.
- `EntropyPolicy.For(GameEntropy)` carries the entropy policy (salt mode + cash bonus cap) — `src/WildBunch.GameContent/NewGame/EntropyPolicy.cs`.
- `SeedWorld` does NOT carry `GameEntropy` (seed-owned world/map layer only) — `src/WildBunch.GameContent/NewGame/SeedWorld.cs`.
- `MysteryTruthResolver.Resolve` is the entropy-applied mystery-truth seam: salt source selection + cash bonus cap — `src/WildBunch.GameContent/NewGame/MysteryTruthResolver.cs`.
- `SeededNewGameFactory.Create` passes entropy through `EntropyPolicy.For` → `GameSetupResolver.Resolve` → `GameSession.StartNew`.
- `GameSession.StartNew` accepts + stores `GameEntropy` — `src/WildBunch.Domain/Game/GameSession.cs`.
- `GameStarted` event carries entropy — `src/WildBunch.Domain/Events/GameStarted.cs`.
- Event replay restores entropy — `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`.
- Persistence round-trips entropy (snapshot + setup component) — `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` + `GameSessionJsonSerializer.Setup.cs`.
- API/DTO/mapper pass entropy through — `StartGameRequest.cs`, `StartNewGameCommand.cs`, `StartNewGameHandler.cs`, `GameDtos.cs`, `GameSessionMapper.cs`.
- Frontend start-flow entropy selection — `SetupHuntStep.tsx` (all four options including Boring stay player-facing).
- Dev overlay exposes entropy as **inspect-only** — `SessionDevContextDto.cs`, `SessionDevContextMapper.cs`, `SessionDevPanel.tsx`.
- BUNCH-94 added `DevDifficultyForced` event, `ForceDevDifficulty` command/handler/endpoint, and difficulty control in SessionDevPanel. BUNCH-93 follows the same pattern for entropy.

### The central gap

**`GameEntropy` does NOT yet affect runtime variance behavior.** It is plumbed end-to-end and stored, but no runtime code branches on it to change variance/surprise. `TravelDayGenerationContext` carries `GameEntropy` but `TravelDayPlanGenerator.Context.cs` (`BuildCategoryWeights`, `BuildEncounterCountWeights`) never reads it. The only entropy branches today are setup-time: `EntropyPolicy.For` (cash bonus cap + salt mode) and `MysteryTruthResolver.Resolve` (salt source selection). Task 1 targets the **runtime travel variance** seam.

### Secondary gaps

1. **Dev overlay has no entropy control** — only inspect. Following the BUNCH-94 `ForceDevDifficulty` pattern, we need `DevEntropyChanged` event, `SetDevEntropy` command/handler/endpoint, and editable entropy control in SessionDevPanel.
2. **No test proves entropy affects a variance seam** while difficulty stays separately controlled.
3. **Frontend entropy labels** need framing as volatility/surprise. All four labels (including Boring) stay player-facing.

## Plan scope (6 tasks)

### Task 1: Wire entropy into the travel variance seam

**File:** `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs`

Add entropy-weighted adjustments to `BuildEncounterCountWeights` and `BuildCategoryWeights`:

- **Boring:** More weight on 0 encounters, more Quiet category weight, fewer Lucky/Unlucky/Foe.
- **Classic:** Baseline (no adjustment).
- **Adventurous:** Less Quiet, more Lucky/Unlucky, slight Foe increase.
- **Wild:** Much less Quiet, much more Lucky/Unlucky, more Foe.

Entropy affects variance only — difficulty stays separate (no cross-contamination). The adjustments are additive weight deltas, not difficulty overrides.

### Task 2: Tests proving entropy affects variance while difficulty stays separate

**File:** `tests/WildBunch.Domain.Tests/Travel/TravelDayPlanGeneratorEntropyTests.cs` (new)

Add 5 tests:

1. `Entropy_AffectsEncounterCountWeights` — Boring has more zero-encounter weight than Wild.
2. `Entropy_AffectsCategoryWeights` — Boring has more Quiet weight, Wild has more Lucky/Unlucky weight.
3. `Entropy_DoesNotAffectDifficultyWeights` — Difficulty-based weights are the same regardless of entropy (prove separation).
4. `Entropy_Adventurous_HasIntermediateWeights` — Adventurous is between Classic and Wild.
5. `Entropy_DifferentEntropyProducesDifferentDistributions` — Generate 100 plans each with Boring vs Wild entropy; prove distribution differences.

### Task 3: Dev overlay entropy control (following BUNCH-94 pattern)

**Domain:**
- New event: `DevEntropyChanged` (carries `GameEntropy NewEntropy`) — `src/WildBunch.Domain/Events/DevEntropyChanged.cs`.
- `GameSession.SetDevEntropy(GameEntropy)` — validates `Enum.IsDefined`, produces event — `src/WildBunch.Domain/Game/GameSession.cs`.
- `GameSession.Apply(DevEntropyChanged)` — sets `GameEntropy`, increments version — `src/WildBunch.Domain/Game/GameSession.cs`.
- All 3 event switches updated:
  - `ApplyProducedEvent` (GameSession.cs ~422) — add `case DevEntropyChanged dec: Apply(dec); break;`
  - `ApplyEvent` (GameSessionEventReplay.cs ~161) — add `case DevEntropyChanged dec: session.Apply(dec); break;`
  - `ResolveEventType` (GameSessionJsonSerializer.Events.cs ~59) — add `nameof(DevEntropyChanged) => typeof(DevEntropyChanged),`

**Application:**
- `SetDevEntropyCommand(Guid, GameEntropy)` — `src/WildBunch.Application/Dev/Commands/SetDevEntropyCommand.cs`.
- `SetDevEntropyHandler` following `GameSessionCommandHandler` pattern — `src/WildBunch.Application/Dev/Commands/SetDevEntropyHandler.cs`.
- `SetDevEntropyRequestDto(string?)` for endpoint binding — `src/WildBunch.Application/Dev/Models/SetDevEntropyRequestDto.cs`.

**API:**
- `POST /api/dev/sessions/{id}/session/set-entropy` endpoint — `src/WildBunch.Api/Dev/DevEndpoints.cs`.
- DevRoleGuard-protected, 400 for invalid entropy, 403 non-dev, 404 not found.
- Handler registered in `DependencyInjection.cs` — `src/WildBunch.Api/DependencyInjection.cs`.

**Frontend:**
- `setDevEntropy` in `devApi.ts` — `src/WildBunch.Web/src/dev/devApi.ts`.
- `SetDevEntropyRequestDto` in `types.ts` — `src/WildBunch.Web/src/dev/types.ts`.
- `SessionDevPanel.tsx` entropy row changed from inspect-only to editable SegmentedToggle — `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`.

**Tests (event-store proof matching BUNCH-94 standard):**
- `GameSessionDevEntropyTests.cs` (new, 9 tests):
  - `SetDevEntropy_ChangesGameEntropy`
  - `SetDevEntropy_ProducesDevEntropyChangedEvent`
  - `SetDevEntropy_DoesNotMutateOtherState` (falsification: only GameEntropy changes, not difficulty/salt/health/cash/status/town)
  - `SetDevEntropy_WithInvalidEntropy_Throws`
  - `SetDevEntropy_CanBeReplayedFromEvents` (rehydration proof)
  - `DevEntropyChanged_RoundTripsThroughEventSerializer` (serializer round-trip proof)
  - `ResolveEventType_KnowsDevEntropyChanged` (event store mapping proof)
  - `SetDevEntropy_DoesNotMutateHiddenCulpritTruth`
- `SetDevEntropyHandlerTests.cs` (new, 3 tests):
  - `SetDevEntropy_ChangesEntropyAndPersists`
  - `SetDevEntropy_DoesNotChangeDifficulty`
  - `SetDevEntropy_DoesNotChangeSalt`
- `DevSessionEndpointTests.cs` (add 3 tests):
  - `SetEntropy_Returns204_AndReflectedInContext`
  - `SetEntropy_Returns400_ForInvalidEntropy`
  - `SetEntropy_Returns403_InNonDevEnvironment`
- `SessionDevPanel.test.tsx` (add 2 tests):
  - `renders entropy control`
  - `calls setDevEntropy when entropy option is clicked`

### Task 4: Frontend setup copy

**File:** `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx`

Add `entropyDescriptions` map keyed by `GameEntropy` enum with short thematic copy:

- Boring: "Calm trails. Fewer surprises, more quiet days."
- Classic: "Balanced variance. The standard trail rhythm."
- Adventurous: "More lucky breaks and bad luck. Livelier trails."
- Wild: "Big swings. Frequent windfalls and mishaps."

Show description below the entropy SegmentedToggle (reuse `DifficultyDescription` styled component). Add test in `StartFlow.test.tsx` proving description updates when entropy changes.

### Task 5: Validation

- Backend build: `dotnet build`
- Backend tests: `dotnet test` (all test projects)
- PostgreSQL lane: `.\scripts\postgres-dev.ps1 validate` (or `ensure` + targeted tests)
- Frontend typecheck: `npm run typecheck`
- Frontend build: `npm run build`
- Frontend tests: `npm test`
- Index mesh: `python scripts/generate_index_mesh.py --check`
- Grep proof: no old `RandomnessPolicy`/`JourneyOnly`/`ITravelRandomnessPolicy` names reintroduced
- Browser playtest: setup flow entropy descriptions render and update correctly

### Task 6: Index mesh + cleanup

- Regenerate all INDEX.md files via `python scripts/generate_index_mesh.py`
- Clean worktree
- No screenshots committed (use git-ignored `.agents/superpowers/output/screenshots/` if needed)
- PostgreSQL shared service on :5434 left running (not stopped per policy)

## Guardrails

- Entropy stays distinct from difficulty (variance vs pressure).
- No old journey-only/randomness-policy names reintroduced.
- Dev control is dev-only (`DevRoleGuard`-guarded), backend-authority.
- `DevEntropyChanged` event round-trips through the event store — proven by serializer round-trip test + rehydrate-from-events test (matches BUNCH-94 standard).
- `SetDevEntropy` changes only `GameEntropy` — falsification proof confirms no mutation of difficulty, salt, journey/player, hidden culprit truth, saloon state, or gameplay outcomes.
- Boring and Easy stay player-facing; do not remove them from player-facing setup.
- Coordinates with BUNCH-94 (difficulty) on shared files; rebase + repair mechanical overlap if it lands first (it did — this is the rebase pass).

## DOD mapping

| BUNCH-93 requirement | Evidence |
|---|---|
| Entropy wired into travel variance | `TravelDayPlanGenerator.Context.cs` + 5 entropy variance tests |
| Difficulty stays separate | Falsification tests prove no cross-contamination |
| Dev overlay entropy control | `DevEntropyChanged` event, all 3 switches, handler, endpoint, frontend select |
| Event-store proof | Serializer round-trip test, rehydrate-from-events test |
| Falsification | `SetDevEntropy_DoesNotMutateOtherState`, `SetDevEntropy_DoesNotMutateHiddenCulpritTruth` |
| Frontend setup copy | Entropy descriptions in `SetupHuntStep.tsx`, verified via browser |
| Boring/Easy stay player-facing | All 4 entropy labels in setup flow + dev panel |
| Index mesh | `generate_index_mesh.py --check` passes |

## Rebase note (BUNCH-94 merge)

BUNCH-94 landed at commit `bb9f5a5` with difficulty setup and controls. This implementation is rebased onto that commit, preserving both BUNCH-94 (difficulty) and BUNCH-93 (entropy) additions in shared files:

- `src/WildBunch.Api/DependencyInjection.cs` — both `ForceDevDifficultyHandler` and `SetDevEntropyHandler` registered
- `src/WildBunch.Api/Dev/DevEndpoints.cs` — both `force-difficulty` and `set-entropy` endpoints
- `src/WildBunch.Domain/Game/GameSession.cs` — both `ForceDevDifficulty` and `SetDevEntropy` methods, both `Apply` methods, both switch cases
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — both switch cases
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs` — both `ResolveEventType` cases
- `src/WildBunch.Web/src/dev/devApi.ts` — both `forceDevDifficulty` and `setDevEntropy` functions
- `src/WildBunch.Web/src/dev/types.ts` — both DTOs
- `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx` — both SegmentedToggle controls
- `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx` — both difficulty and entropy descriptions
- `src/WildBunch.Web/src/components/start-flow/SegmentedToggle.tsx` — description field added to options type (shared by both)

No product decision changed. Boring/Easy stay player-facing. Issue scope unchanged.
