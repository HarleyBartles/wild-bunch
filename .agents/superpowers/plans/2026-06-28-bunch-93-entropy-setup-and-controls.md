# BUNCH-93 — Entropy setup and controls

**Issue:** [BUNCH-93 — Entropy setup and controls](https://linear.app/harleys-workspace/issue/BUNCH-93/entropy-setup-and-controls)
**Branch:** `harleydbartles/bunch-93-entropy-setup-and-controls`
**Base commit:** `a2a88e9` (== `origin/main` after BUNCH-107 seed codec refactor; rebased from original preflight base `6b9fcbf`)
**Plan type:** Preflight → execution plan (plan-only draft PR first; execution after approval)
**Parallel issue:** BUNCH-94 (difficulty) — may run in parallel; not blocked on it landing. Rebase onto current main if BUNCH-94 lands while this executes; repair mechanical overlap only.

## Repo-skill grounding (read during preflight)

- `.agents/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md` — canonical entropy envelope definitions (Boring/Classic/Adventurous/Wild). Task 1 design aligns with this.
- `.agents/skills/wild-bunch-domain-modeling/SKILL.md` — GameSession aggregate root, travel/journey state, trail-day progression.
- `.agents/skills/wild-bunch-dotnet-architecture/SKILL.md` — domain owns rules, application orchestrates, persistence is JSON snapshot, no early table normalization.
- `.agents/skills/verification-before-completion/SKILL.md` — return evidence + issue-goal conformance + falsification (the plan originally cited a non-existent `wild-bunch-worker-verification` skill; the correct verification skill is `verification-before-completion`).
- `.agents/skills/game-playtest/SKILL.md` — browser playtest + screenshot evidence for the player/dev-facing control proof.
- `.agents/skills/repo-worker-base/SKILL.md` — fresh-main invariant, worktree isolation gate, GREEN gate, required return evidence.

## Preflight investigation summary (current main)

### What is already complete on main (preserve, do not redo)

> **Rebase note (BUNCH-107):** The original preflight was written against `6b9fcbf`, before BUNCH-107 refactored the seed codec into the `SeedWorld` setup pipeline. The references below are verified against the rebased base `a2a88e9`. BUNCH-107 deleted `GameSetupSeedCodec.cs` and `StartingWorldDescriptorSeedMixer.cs` and introduced `SeedWorldResolver`, `SeedWorld`, `EntropyPolicy`, `DifficultyEnvelope`, `MysteryTruthResolver`, `StartingTownPolicy`, `GameSetupResolver`, and `ResolvedGameSetup`. Entropy is no longer seed-owned; it is entropy-owned (`EntropyPolicy` + `MysteryTruthResolver`). The seed codec does NOT encode entropy.

- `GameEntropy` enum (`Boring`, `Classic`, `Adventurous`, `Wild`) — `src/WildBunch.Domain/Travel/GameEntropy.cs`.
- `GameDifficulty` enum — `src/WildBunch.Domain/Travel/GameDifficulty.cs`.
- `EntropyPolicy.For(GameEntropy)` carries the entropy policy (salt mode + cash bonus cap) — `src/WildBunch.GameContent/NewGame/EntropyPolicy.cs`. Boring=Fixed/0, Classic=Runtime/2, Adventurous=Runtime/5, Wild=Runtime/8. The seed does NOT encode entropy; `EntropyPolicy` is applied downstream of `SeedWorld` by `MysteryTruthResolver`.
- `SeedWorld` does NOT carry `GameEntropy` (seed-owned world/map layer only) — `src/WildBunch.GameContent/NewGame/SeedWorld.cs`. This is correct per the post-BUNCH-107 doctrine.
- `MysteryTruthResolver.Resolve` is the entropy-applied mystery-truth seam: salt source selection (Boring=Fixed(seedCodeText), others=factory-produced) + cash bonus cap — `src/WildBunch.GameContent/NewGame/MysteryTruthResolver.cs:51-55`. BUNCH-107 left an explicit BUNCH-93 expansion comment here for setup-time variance (culprit reroll, feature reallocation). **This is a setup-time seam, distinct from the runtime travel variance seam targeted by Task 1.** Task 1 does not duplicate this seam.
- `SeededNewGameFactory.Create` passes entropy through `EntropyPolicy.For` → `GameSetupResolver.Resolve` → `GameSession.StartNew` — `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs:28-51`. Salt selection no longer lives here (moved to `MysteryTruthResolver`).
- `GameSession.StartNew` accepts + stores `GameEntropy` — `src/WildBunch.Domain/Game/GameSession.cs:836,845,870` (constructor stores at line 82; property at line 131).
- `GameStarted` event carries entropy — `src/WildBunch.Domain/Events/GameStarted.cs:21`.
- Event replay restores entropy — `src/WildBunch.Domain/Game/GameSessionEventReplay.cs:68` (passes `gameStarted.GameEntropy` into `GameSession` ctor); `Apply(GameStarted)` sets it at `GameSession.cs:967`.
- Persistence round-trips entropy (snapshot + setup component) — `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs:17,42,82` + `GameSessionJsonSerializer.Setup.cs` (serialize/deserialize `GameEntropy`) + `EfGameSessionRepository.cs:119,276`. Legacy snapshots default to `Classic`.
- API/DTO/mapper pass entropy through — `StartGameRequest.cs:9` (`src/WildBunch.Api/Games/Requests/`), `StartNewGameCommand.cs:9` (`src/WildBunch.Application/Games/Commands/`), `StartNewGameHandler.cs:59` (`src/WildBunch.Application/Games/Commands/`), `GameDtos.cs:15`, `GameSessionMapper.cs:42,68,90` (now at `src/WildBunch.Application/Games/Mapping/` — moved from `Execution/` by BUNCH-107).
- Frontend start-flow entropy selection — `SetupHuntStep.tsx:30-35` (`gameEntropyOptions` includes Boring at line 31), `useStartGameSeed.ts`, `useStartFlow.ts`, `wildBunchApi.ts:151,160-161`.
- Dev overlay exposes entropy as **inspect-only** — `SessionDevContextDto.cs:13` (`GameEntropy` as string), `SessionDevContextMapper.cs`, `SessionDevPanel.tsx:125-126` ("Entropy (inspect):").
- Existing tests cover setup/persistence/round-trip/salt — see Test inventory below.

### The central gap

**`GameEntropy` does NOT yet affect runtime variance behavior.** It is plumbed end-to-end and stored, but no runtime code branches on it to change variance/surprise. `TravelDayGenerationContext` carries `GameEntropy` (line 76) but `TravelDayPlanGenerator.Context.cs` (`BuildCategoryWeights` at line 170, `BuildEncounterCountWeights` at line 103) and `JourneyEncounterResolutionEngine.cs` never read it. The only entropy branches today are setup-time: `EntropyPolicy.For` (cash bonus cap + salt mode) and `MysteryTruthResolver.Resolve` (salt source selection — Boring=Fixed, others=Runtime). BUNCH-107 left an explicit expansion seam in `MysteryTruthResolver` for setup-time variance (culprit reroll, feature reallocation), but that is a **setup-time** seam — it does not make entropy affect the per-travel-day variance that the player experiences during play. Task 1 targets the **runtime travel variance** seam, which is complementary and not duplicated by the `MysteryTruthResolver` seam.

### Secondary gaps

> **Direction change (Harley, this session):** Boring (entropy) and Easy (difficulty) are explicitly player-facing modes today. The original issue text said "Do not make Boring a normal player-facing option unless Harley explicitly changes the issue." Harley has now explicitly changed this direction: Boring stays player-facing for now. Making Boring/Easy dev-only is possible future work, not BUNCH-93 scope. The plan originally included a Task to remove Boring from player-facing setup; that Task has been removed.

1. **Dev overlay has no entropy control** — only inspect. The dev-overlay doctrine (`.agents/dev-overlay/DOCTRINE.md` §2) says "Session dev owns game/session-level setup: difficulty, randomness, entropy/seed posture," so a dev entropy control belongs here, following the existing `ForceDevSaltSource`/`ClearDevSaltSource` pattern.
2. **No test proves entropy affects a variance seam** while difficulty stays separately controlled. All existing entropy tests verify setup/persistence/round-trip, not runtime variance branching.
3. **Frontend entropy labels** say "Boring/Classic/Adventurous/Wild" without framing entropy as volatility/surprise vs. difficulty pressure. All four labels (including Boring) stay player-facing; they need framing as a volatility axis, not removal.

### Test inventory (existing, preserve)

> **Rebase note (BUNCH-107):** Two GameContent test files were renamed by BUNCH-107: `StartingWorldDescriptorResolverTests.cs` → `SeedWorldResolverTests.cs`, and `StartingWorldDescriptorSeedCodeFactory.cs` → `SeedWorldSeedCodeFactory.cs`. The entropy coverage they provided moved with them.

- `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs` — round-trip + legacy default.
- `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` — Boring/Classic salt behavior.
- `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` — default Classic, Boring fixed salt (updated by BUNCH-107 for the new pipeline).
- `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs` — seed world resolution (renamed from `StartingWorldDescriptorResolverTests.cs` by BUNCH-107).
- `tests/WildBunch.GameContent.Tests/SeedWorldSeedCodeFactory.cs` — seed code factory helper (renamed from `StartingWorldDescriptorSeedCodeFactory.cs` by BUNCH-107).
- `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs` — seed world builder snapshots (heavily updated by BUNCH-107).
- `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs` — full setup pipeline resolution (new in BUNCH-107).
- `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs` — event captures + rehydrate restores.
- `tests/WildBunch.Domain.Tests/DevSaltSourceTests.cs` — dev salt does not mutate entropy.
- `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs` — dev context includes entropy.
- `tests/WildBunch.Application.Tests/StartNewGameHandlerTests.cs` — handler passes entropy.
- `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs` — dev endpoint entropy exposure.
- `tests/WildBunch.Web/src/tests/SetupHuntStep.test.tsx` — frontend entropy selection (all four options including Boring stay player-facing; test only needs update if labels change in Task 4).

## Goal (observable repo state)

After execution, Harley can:

1. Start a game with Boring/Classic/Adventurous/Wild entropy and observe **materially different travel variance** (lucky/unlucky/rare/encounter-surprise frequency) — not just different cash or salt. Boring is the dampened/deterministic-feeling baseline; Wild is the high-volatility end.
2. Use the dev overlay Session dev panel to set entropy on a live test session and observe the variance difference immediately.
3. See entropy framed as volatility/surprise in the setup UI (all four options including Boring stay player-facing), distinct from difficulty pressure.
4. Read tests proving entropy changes variance while difficulty stays constant, and vice versa.

## Guardrails (binding)

- Do not rename `GameEntropy` back to journey-only/randomness-policy language.
- Do not collapse entropy and difficulty into one control. Entropy = variance/surprise/story volatility inside coherent rules; difficulty = pressure/harder.
- Do not let Wild bypass hard domain invariants or break game coherence.
- Do not expose hidden culprit truth through normal player APIs.
- Do not move gameplay authority into React state. Dev mutations go through backend commands + dev events.
- Do not normalize live session state into new database tables.
- Do not broaden into BUNCH-94 difficulty behavior except for coordination and compile conflicts.
- Keep temporary cockpit/debug-shell UI light; do not polish it for its own sake.
- Entropy weight changes must be additive adjustments on top of the existing difficulty/risk/terrain/pressure weights, not a replacement of them. Difficulty stays the pressure axis; entropy stays the variance axis.
- Boring (entropy) and Easy (difficulty) are explicitly player-facing modes today. Do not remove them from player-facing setup. Making them dev-only is possible future work, not BUNCH-93 scope.

## Implementation plan

### Task 1: Wire entropy into the travel variance seam (core)

**Files:**
- `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs`

**What:** Add entropy-based weight adjustments to `BuildCategoryWeights` (line 170) and `BuildEncounterCountWeights` (line 103) so that entropy changes variance/surprise without simply increasing difficulty pressure.

**Design (variance, not pressure) — aligned with the canonical entropy doctrine at `.agents/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md`:**
- **Boring:** deterministic by seed and world state. Dampen variance — flatten Lucky/Unlucky spikes, reduce rare-category appearance, lean toward Quiet/Resource. The same action against the same world state should produce the same result. This is the test/reproduction envelope.
- **Classic:** normal play. Baseline (current weights unchanged). Rolls, shuffles, and outcome selection are normally weighted, then shaped by difficulty and feature-specific weights.
- **Adventurous:** increases surprise while preserving the same rules. Rare or unexpected events appear more often. Increase Lucky and Unlucky weights, increase rare-category (Environmental, HorseTrouble, Npc) appearance, slight increase in encounter-count spread. Difficulty still leans the game; adventurous entropy sprinkles rare lucky or unlucky variance into the deck.
- **Wild:** may bend ordinary assumptions while preserving game coherence. Larger Lucky/Unlucky swings, rare events appear more often, wider encounter-count spread, but **do not** increase Foe pressure the way Brutal does. Wild is volatility/story-bending, not lethality. (Doctrine example: a lawman may move unusually fast; a random citizen may look exactly like Elzy Lay. This slice wires the variance seam; later slices can add wild-specific story bends.)

**Constraints:**
- Entropy adjustments are applied AFTER the existing difficulty/risk/terrain/pressure switches, as an additional `switch (context.GameEntropy)` block. Do not modify the existing difficulty branches.
- Keep weights non-negative after adjustment (clamp via the existing `AddWeight` pattern; the `FilterCategoryWeightsForLegality` path already handles zero/negative).
- Do not change `BuildEncounterCountWeights` difficulty branches; add a separate entropy block.
- The variance must be observable in category distribution over a deterministic sample, not just theoretical.

**Checkboxes:**
- [ ] Add `switch (context.GameEntropy)` block to `BuildCategoryWeights` adjusting Lucky/Unlucky/rare categories per the design above.
- [ ] Add `switch (context.GameEntropy)` block to `BuildEncounterCountWeights` adjusting count spread per the design above.
- [ ] Verify no existing difficulty/risk/terrain branches were modified.
- [ ] Verify weights stay legal (non-negative after filter) for all entropy × difficulty combinations.

### Task 2: Tests proving entropy affects variance while difficulty stays separate

**Files:**
- `tests/WildBunch.Domain.Tests/Travel/TravelDayPlanGeneratorEntropyTests.cs` (new)

**What:** Deterministic tests that prove entropy changes the travel-day category distribution while difficulty stays constant, and that difficulty changes it while entropy stays constant. This is the falsification guardrail that proves entropy ≠ difficulty.

**Approach:**
- Use the seed system (`SeededNewGameFactory` / `SeedWorldResolver.CreateRepresentativeSeedCode`) to build sessions — do NOT bypass the seed system (per AGENTS.md UUID Seed Codec rules). Use `SeedWorld` records, not stored UUIDs. (BUNCH-107 renamed `StartingWorldDescriptorResolver` → `SeedWorldResolver` and `StartingWorldDescriptor` → `SeedWorld`.)
- Use `GameEntropy.Boring` + fixed salt for deterministic category sampling across many rolls, OR construct `TravelDayGenerationContext` directly with controlled fields and call the weight builders + a fixed roll sequence to assert category/count differences.
- Assert: holding difficulty constant at `Standard`, the category weight distributions for `Classic` vs `Adventurous` vs `Wild` vs `Boring` are materially different (e.g., Wild has higher Lucky+Unlucky combined weight than Classic, Boring has lower).
- Assert: holding entropy constant at `Classic`, the category weight distributions for `Easy` vs `Brutal` are materially different (existing behavior, proves difficulty still owns pressure).
- Assert: the entropy effect and difficulty effect are independent — changing entropy does not replicate the difficulty pressure pattern (e.g., Wild does not just equal Brutal's Foe weight).

**Checkboxes:**
- [ ] Create `TravelDayPlanGeneratorEntropyTests.cs` with deterministic context construction.
- [ ] Assert entropy changes category/count distribution with difficulty held constant.
- [ ] Assert difficulty changes category/count distribution with entropy held constant.
- [ ] Assert Wild ≠ Brutal pattern (variance vs pressure independence).
- [ ] Do not store UUIDs in fixtures; derive via `SeedWorldResolver.CreateRepresentativeSeedCode` where seed-derived sessions are needed.

### Task 3: Dev overlay entropy control (Session dev panel)

**Files:**
- `src/WildBunch.Domain/Events/DevEntropyChanged.cs` (new)
- `src/WildBunch.Domain/Game/GameSession.cs` — add `SetDevEntropy` method + `Apply(DevEntropyChanged)`
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` — wire Apply
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs` — serialize/deserialize event
- `src/WildBunch.Application/Dev/Commands/SetDevEntropyCommand.cs` (new)
- `src/WildBunch.Application/Dev/Commands/SetDevEntropyHandler.cs` (new)
- `src/WildBunch.Application/Dev/Models/SetDevEntropyRequestDto.cs` (new)
- `src/WildBunch.Api/Dev/DevEndpoints.cs` — map POST `/sessions/{id:guid}/session/set-entropy`
- `src/WildBunch.Api/DependencyInjection.cs` — register handler (if needed)
- `src/WildBunch.Web/src/dev/devApi.ts` — add `setDevEntropy` call
- `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx` — change "Entropy (inspect)" to an editable control
- `src/WildBunch.Web/src/dev/types.ts` — add request type if needed

**What:** Follow the existing `ForceDevSaltSource`/`ClearDevSaltSource` pattern (BUNCH-101) to add a dev-only command that sets `GameSession.GameEntropy` via a dev event. This lets Harley change entropy on a live test session and observe the variance difference immediately without restarting.

**Pattern (from `ForceDevSaltSourceHandler.cs` at `src/WildBunch.Application/Dev/Commands/` + `DevSaltSourceForced.cs` + `GameSession.ForceDevSaltSource` at line 1198):**
1. `DevEntropyChanged` event record carrying `GameEntropy NewEntropy`.
2. `GameSession.SetDevEntropy(GameEntropy entropy)` — validates `Enum.IsDefined`, calls `ProduceEvent(new DevEntropyChanged { NewEntropy = entropy })`.
3. `Apply(DevEntropyChanged e)` — sets `GameEntropy = e.NewEntropy; _version++`.
4. Wire into `GameSessionEventReplay.cs`.
5. Serialize/deserialize in `GameSessionJsonSerializer.Events.cs` (follow the `DevSaltSourceForced` serialization shape).
6. `SetDevEntropyCommand` + `SetDevEntropyHandler` extending `GameSessionCommandHandler`.
7. `SetDevEntropyRequestDto` with `GameEntropy` string field.
8. Dev endpoint POST `/sessions/{id:guid}/session/set-entropy` — guarded by `DevRoleGuard.EnsureDevAccess()`.
9. Frontend `devApi.ts` + `SessionDevPanel.tsx` — replace inspect-only row with a control (select or segmented toggle) that calls the new endpoint and refreshes the dev context.

**Doctrine compliance (`.agents/dev-overlay/DOCTRINE.md`):**
- §1 State/action boundary: setting entropy is setting state (variance posture), not forcing a gameplay outcome. Valid.
- §2 Panel ownership: Session dev owns entropy/seed posture. Valid.
- §7 Backend authority: mutation goes through backend command + dev event, not frontend fabrication. Valid.
- §9 Closeout proof: event-stream proof for dev entropy change.

**Constraints:**
- Do not expose this through normal player APIs. Dev-only, `DevRoleGuard`-guarded.
- The dev event is persisted in the event stream and rehydrated.
- Changing entropy mid-session affects future travel-day generation, not already-generated days.

**Checkboxes:**
- [ ] Create `DevEntropyChanged` event.
- [ ] Add `GameSession.SetDevEntropy` + `Apply(DevEntropyChanged)`.
- [ ] Wire event replay.
- [ ] Add event serialization/deserialization.
- [ ] Create `SetDevEntropyCommand` + `SetDevEntropyHandler` + `SetDevEntropyRequestDto`.
- [ ] Map dev endpoint in `DevEndpoints.cs`.
- [ ] Register handler in `DependencyInjection.cs` if needed.
- [ ] Add `setDevEntropy` to `devApi.ts`.
- [ ] Replace inspect-only entropy row in `SessionDevPanel.tsx` with an editable control.
- [ ] Add backend test for `SetDevEntropyHandler` (entropy changes + event emitted + dev guard).
- [ ] Add dev endpoint integration test.

### Task 4: Frontend setup copy — frame entropy as volatility/surprise

**Files:**
- `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx` — labels/group label

**What:** Update entropy labels and group label so the player understands entropy as variance/surprise/volatility, not pressure. All four options (Boring/Classic/Adventurous/Wild) stay player-facing. Keep it short and in-world; do not over-explain.

**Draft labels:**
- Group label: "Entropy" → keep, or "Story Volatility" if clearer. Prefer keeping "Entropy" with a one-line subtitle if the existing pattern supports it; otherwise keep the single label.
- Option labels: "Boring" / "Classic" / "Adventurous" / "Wild" (already present). Do not add long descriptions to the segmented toggle. If "Boring" reads as a negative judgment rather than a volatility level, consider a clearer in-world label (e.g. "Steady"), but only if it helps the player choose; otherwise keep "Boring".

**Constraints:**
- Follow `src/WildBunch.Web/AGENTS.md` + `.agents/unslop/play-surface-ui.md` — keep player-facing surfaces in-world, not cockpit chrome. Cut labels that don't help the player.
- Do not add a tooltip/help system unless the existing pattern has one.

**Checkboxes:**
- [ ] Review and adjust entropy group label / option labels for volatility framing (only if current labels are misleading; keep minimal).
- [ ] Update `SetupHuntStep.test.tsx` if label assertions change.

### Task 5: Validation

**Commands:**
- `dotnet build`
- `dotnet test` (full suite)
- `.\scripts\postgres-dev.ps1 ensure` then `.\scripts\postgres-dev.ps1 validate` (if persistence/event tests touch PostgreSQL-backed paths; the dev event + entropy tests are likely in-process, but run the validate lane to be safe)
- `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` (only if persistence schema changed — it should NOT, since entropy is JSON snapshot; skip unless a migration was added)
- Frontend: `npm run typecheck` + `npm run build` + `npm test` (from `src/WildBunch.Web`)
- Grep proof: `rg "journey-only|randomness-policy|RandomnessPolicy" src/` returns no reintroduced old names.

**Browser/playtest proof:**
- Start a session with Wild entropy via the normal setup flow; advance several trail days; screenshot the travel/event variety.
- Use the dev overlay Session dev panel to switch entropy from Classic → Wild on a live session; advance trail days; screenshot the variance difference.
- Provide a short explanation of the observed Classic vs Adventurous vs Wild difference in this slice.

**Checkboxes:**
- [ ] `dotnet build` passes.
- [ ] `dotnet test` passes (including new entropy variance tests + dev entropy handler tests).
- [ ] `.\scripts\postgres-dev.ps1 validate` passes (or report why it was skipped if no PostgreSQL-dependent path was touched).
- [ ] Frontend `npm run typecheck` + `npm run build` + `npm test` pass.
- [ ] Grep proof: no old journey-only/randomness-policy names reintroduced.
- [ ] Browser/playtest screenshot: Wild entropy shows more variance than Classic.
- [ ] Browser/playtest screenshot: dev overlay entropy control changes variance on a live session.
- [ ] Short written explanation of Classic vs Adventurous vs Wild observed difference.

### Task 6: Index mesh + cleanup

**Files:**
- `scripts/generate_index_mesh.py` output (run if files were added/removed)
- `.agents/superpowers/plans/INDEX.md` (if the generator covers it)

**Checkboxes:**
- [ ] Run `python scripts/generate_index_mesh.py` and commit updated INDEX.md files if any were added/removed.
- [ ] Ensure no loose agent artifacts at repo root or in product folders.
- [ ] Ensure no screenshots committed to the repo (they go under git-ignored `.agents/superpowers/output/screenshots/`).

## BUNCH-94 coordination

Shared files both issues may touch: `SetupHuntStep.tsx`, `useStartGameSeed.ts`, `SessionDevPanel.tsx`, `EntropyPolicy.cs`, `DifficultyEnvelope.cs`, `MysteryTruthResolver.cs`, `GameSetupResolver.cs`, `SeededNewGameFactory.cs`, `StartGameRequest.cs`, `GameDtos.cs`, `GameSessionMapper.cs` (now at `Games/Mapping/`), snapshot/setup serializers, `EfGameSessionRepository.cs`.

> **Rebase note (BUNCH-107):** `GameSetupSeedCodec.cs` and `StartingWorldDescriptorSeedMixer.cs` no longer exist. The shared setup-pipeline files are now `EntropyPolicy.cs`, `DifficultyEnvelope.cs`, `MysteryTruthResolver.cs`, and `GameSetupResolver.cs`. BUNCH-93 expands `MysteryTruthResolver` (entropy-owned); BUNCH-94 expands `DifficultyEnvelope` (pressure-owned). Keep the two expansions in their respective files.

- This plan touches `SetupHuntStep.tsx` (entropy label framing only — Boring stays player-facing), `SessionDevPanel.tsx` (entropy control), `TravelDayPlanGenerator.Context.cs` (variance seam — BUNCH-94 unlikely to touch), and the dev command/event pattern.
- If BUNCH-94 lands first and conflicts on `SetupHuntStep.tsx` or `SessionDevPanel.tsx`, rebase onto current main and repair mechanical overlap. Keep entropy and difficulty changes in separate regions of the same files where possible.
- Do not overwrite difficulty-axis changes from BUNCH-94.

## DOD mapping (entropy stayed distinct from difficulty)

- Entropy changes variance (lucky/unlucky/rare frequency) — proven by Task 2 tests.
- Difficulty changes pressure (foe/unlucky pressure) — existing behavior, unchanged.
- Wild ≠ Brutal — proven by Task 2 independence assertion.
- Entropy control is dev-only (Session dev panel) — not a normal player API.
- Boring and Easy stay player-facing — not removed from setup (direction change from original issue text).
- No old journey-only/randomness-policy names reintroduced — grep proof.
