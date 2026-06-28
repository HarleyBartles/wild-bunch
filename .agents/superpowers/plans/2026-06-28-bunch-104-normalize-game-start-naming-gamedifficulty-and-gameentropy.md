# BUNCH-104: Normalize Game-Start Naming — GameDifficulty and GameEntropy

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename `TravelDifficulty` to `GameDifficulty` and `AdventureRandomnessPolicy` to `GameEntropy` end-to-end across the repo, wherever these names represent game-start difficulty and game-start entropy/randomness. No behavior, values, levels, or semantics change.

**Architecture:** Pure rename/refactor slice. The enum types, their values, and all derived behavior stay identical. The UUID seed codec's deterministic label strings are hash keys and must not change. The EF column rename is the only schema touch. Backend remains authoritative; the frontend renames its type/property/label to match the new API contract.

**Tech Stack:** C# / .NET, EF Core (PostgreSQL), xUnit, React + TypeScript, styled-components.

## Plan Status

- Plan status: implementation complete (taxonomy addendum + BUNCH-87 folded in, all tests passing, ready for commit)
- Current route state: `implementation_complete`
- Base commit: `a4db54f` (BUNCH-102: Player start-over settings and prologue start loop, #113)
- Branch head: pending commit (taxonomy addendum + BUNCH-87 test fixes)
- The staleness gate was executed after BUNCH-102 merged to `main`. See "Staleness Gate Execution Record" below.
- The taxonomy addendum (Section T) was folded in after the staleness gate. See "Taxonomy Addendum" below.
- Implementation must still follow the plan's validation and falsification steps; approval of the plan is not approval to skip verification.

## Global Constraints

- Assumption baseline: current `origin/main` at commit `73fd9fa` (2026-06-28).
- Do not change difficulty or entropy semantics, values, levels, or behavior.
- Do not add new difficulty/entropy levels.
- Do not bundle seed/start-code UI, setup redesign, or unrelated start-flow work.
- Do not invent frontend-only difficulty or entropy state.
- Keep `GameSession` and existing aggregate/application boundaries intact.
- Do not rename `TravelRulesProfile` — it is a travel-rules concept, not game-start difficulty. Only its `Difficulty` property type changes.
- Do not rename `TravelRandomnessState` — it is journey randomness, not game-start entropy.
- Do not rename the deterministic label string values in `GameSetupDeterministicLabels` — they are codec hash keys (see Critical Scope Decisions below).
- Do not rewrite historical plan files under `.agents/superpowers/plans/` that reference old names — they are audit trails.
- Do not rewrite old EF migration designer files — they are historical schema snapshots. Only the current model snapshot and a new migration change.

---

## Staleness Gate / BUNCH-102 Interaction

This plan was written against `origin/main` at commit `73fd9fa` (2026-06-28). BUNCH-102 is actively changing the start-flow / frontend / API surfaces that BUNCH-104 touches. The preflight surface inventory below is **base-state evidence at the preflight baseline only**, not guaranteed-complete future truth. The implementation worker MUST treat the surface list as a starting point, not a closed set.

### G1: Refresh from current `main` before implementation

Before any implementation edit, the implementation worker MUST:

1. Refresh the worktree from current `main` (rebase or branch fresh from latest `origin/main`).
2. Check whether BUNCH-102 has landed on `main` (search git log / PR list for BUNCH-102 merge).
3. Re-run the naming surface search (`TravelDifficulty`, `AdventureRandomnessPolicy`, `travelDifficulty`, `entropy`, `Entropy`, and the new `GameDifficulty` / `GameEntropy` targets) against the refreshed tree.
4. Diff the refreshed surface set against the preflight inventory below. Any new surface is in scope for this rename and must be added to the task list.

### G2: If BUNCH-102 has landed, include the new start-flow / prologue surfaces

If BUNCH-102 has landed, the worker MUST re-run the naming surface search and include the new start-flow / prologue surfaces, likely including (non-exhaustive — verify against actual BUNCH-102 changes):

- `SetupHuntStep` (and any other setup-step components BUNCH-102 introduces)
- `useStartFlow` (and any other start-flow hooks)
- `useStartGameSeed` (may have been refactored by BUNCH-102)
- `getPrologue` (prologue query / request parameter naming)
- prologue query / request parameter naming surfaces
- updated `StartGameRequest` (BUNCH-102 may have reshaped it)
- updated `api/types.ts` (BUNCH-102 may have added/renamed types)
- new start-flow / setup tests introduced by BUNCH-102
- any new browser proof or setup-flow tests from BUNCH-102

The frontend task (Task 7) and API task (Task 5) must be re-derived from the refreshed tree, not executed verbatim from the preflight inventory.

### G3: Frontend section is base-state evidence, not future truth

The "Frontend (`src/WildBunch.Web/src/`)" surface list under Preflight Answer 2 below documents **surfaces found on the preflight base** (`73fd9fa`). It is not guaranteed complete against a `main` that has absorbed BUNCH-102. The implementation task requires a fresh grep/search before editing any frontend file. If a file listed below no longer exists or has been renamed/restructured by BUNCH-102, follow the new location/name.

### G4: Frontend entropy statement is base-state evidence only

Critical Scope Decision D6 ("Frontend `Entropy` is not consumed today") is **base-state evidence at `73fd9fa` only**. If BUNCH-102 lands first and introduces a frontend entropy setup surface (e.g. an entropy selector in the new start-flow, an `entropy` field on a new start request shape, or prologue entropy parameter naming), that statement is stale and the frontend entropy setup surface MUST be included in the rename. The worker must verify the refreshed frontend tree for any `entropy` / `Entropy` / `adventureRandomnessPolicy` / `AdventureRandomnessPolicy` surface and include it.

### G5: Stop AMBER if refreshed main changes the work beyond a surface update

If the refreshed `main` changes the work beyond a straightforward surface update (e.g. BUNCH-102 restructured the start-flow in a way that makes the rename task list materially wrong, introduced new concepts that overlap with difficulty/entropy naming, or changed aggregate/persistence boundaries in the rename path), the implementation worker MUST **stop AMBER** and report the changed seams rather than improvising. Report the exact files/seams that diverge from this plan and request a plan refresh. Do not silently expand scope.

---

## Staleness Gate Execution Record

**Executed:** 2026-06-28, after BUNCH-102 merged to `main` as PR #113 (`a4db54f`).

**G1 — Refresh from current main:**
- Rebased `harleydbartles/bunch-104-normalize-game-start-naming-gamedifficulty-and-gameentropy` onto `a4db54f`.
- 10 conflict files resolved (BUNCH-102 added `StartingTownId` parameter, prologue endpoint, start-flow components; BUNCH-104 rename applied on top of the new structure).
- New base commit: `a4db54f`. Branch head after rebase + staleness-gate commit: `efc2c5f`.

**G2 — New BUNCH-102 surfaces found and renamed:**
- Backend: `PrologueDescriptorResolver.cs`, `GetPrologueQuery.cs`, `GetPrologueHandler.cs`, `GameSessionEndpoints.cs` (GetPrologueAsync)
- Frontend: `useStartFlow.ts`, `PreSessionSurface.tsx`, `SetupHuntStep.tsx`, `StorySoFarStep.tsx`, `wildBunchApi.ts`
- Frontend tests: `SetupHuntStep.test.tsx`, `StorySoFarStep.test.tsx`, `StartFlow.test.tsx`, `StartOverRegression.test.tsx`, `StartOverConfirmation.test.tsx`, `GameSettingsOverlay.test.tsx`
- Test double: `StubNewGameFactory.RequestedTravelDifficulties` → `RequestedGameDifficulties`

**G3/G4 — Frontend entropy surface:**
- BUNCH-102 introduced entropy state/handlers in `useStartFlow.ts` and `useStartGameSeed.ts`. These were renamed from `AdventureRandomnessPolicy` to `GameEntropy` during the rebase conflict resolution.

**G5 — Scope assessment:**
- The rebase conflicts were straightforward surface updates (BUNCH-102 added new parameters/components using old names; BUNCH-104 rename applied on top). No material scope change. Proceeded with execution.

**Validation after staleness gate:**
- Full solution builds clean (0 errors, 0 warnings).
- 716 backend tests pass (358 Domain + 162 Application + 59 GameContent + 137 Integration).
- 157 frontend tests pass (20 test files).

---

## Taxonomy Addendum (Section T)

**Authorized after BUNCH-102 merged.** The product taxonomy decision folds into the end-to-end game-start taxonomy normalization. This is NOT a frontend-only display-name mask — it changes domain enum values, codec behavior, and all downstream surfaces.

### T1: Difficulty taxonomy

**Current enum:** `Normal=0, Easy=1, Hard=2` (3 values, codec `% 3`)
**New enum:** `Standard=0, Easy=1, Challenging=2, Brutal=3` (4 values, codec `% 4`)

- `Standard` (was `Normal`): playable, value 0 unchanged
- `Easy`: hidden/dev-only, value 1 unchanged
- `Challenging` (was `Hard`): playable, value 2 unchanged
- `Brutal`: new, playable, value 3

**Codec change:** `ResolveDifficulty` modulo changes from 3 to 4. `CreateCanonicalDescriptorShape` and `GetCanonicalSeedCode` gain a `Brutal` case. New canonical seed code: `CanonicalBrutalSeedCode`. Existing canonical seed codes for Standard/Challenging are derived from the renamed values.

**Gameplay parameters for Brutal** (extrapolated from existing pattern):
- `StartingHealthFor`: 600 (pattern: -250 Easy→Standard, -200 Standard→Challenging, -200 Challenging→Brutal)
- `GetBaseStartingCash`: 13 (pattern: -5 per step)
- `CreateCanonicalDescriptorShape` canonical cash: 15

**Round-trip impact:** Adding a 4th value changes what UUIDs resolve to (modulo 3→4). Per AGENTS.md "current codec correctness wins" in greenfield, no compat shim. `CreateRepresentativeSeedCode` search mechanism is preserved. Guardrail tests updated.

### T2: Entropy taxonomy

**Current enum:** `Boring=0, Standard=1, Adventurous=2, Wild=3, Classic=Standard(=1)`
**New enum:** `Boring=0, Classic=1, Adventurous=2, Wild=3` (remove `Standard`, make `Classic` primary)

- `Classic` (was `Standard`): playable, value 1 unchanged
- `Adventurous`: playable, value 2 unchanged
- `Wild`: playable, value 3 unchanged
- `Boring`: dev-only, value 0 unchanged

**Codec change:** `ResolveGameEntropy` maps value 1 to `GameEntropy.Classic` instead of `GameEntropy.Standard`. Modulo stays `% 4`. No round-trip threat — pure rename at same value.

### T3: Frontend player-setup surface

The player-facing setup UI (SetupHuntStep, StartGameOptionsForm) must show only playable values:
- Difficulty: Standard, Challenging, Brutal (hide Easy)
- Entropy: Classic, Adventurous, Wild (hide Boring)

`Easy` and `Boring` remain in the domain enum and are resolvable via the codec, but are not offered in the ordinary player setup flow. They are dev-overlay / future-territory.

### T4: Scope guard

If the codec change (difficulty modulo 3→4) reveals a round-trip incompatibility that cannot be fixed by updating the guardrail tests and codec mapping, stop AMBER and report the exact seam. Per AGENTS.md, no compatibility shims for old UUIDs in this greenfield repo.

---

## Preflight Answers / Source Seams Inspected

### 1. Where are the current difficulty and entropy concepts declared?

- **`TravelDifficulty` enum**: `src/WildBunch.Domain/Travel/TravelDifficulty.cs` — values `Normal=0, Easy=1, Hard=2`.
- **`AdventureRandomnessPolicy` enum**: `src/WildBunch.Domain/Travel/AdventureRandomnessPolicy.cs` — values `Boring=0, Standard=1, Adventurous=2, Wild=3, Classic=Standard`.
- **`StartingWorldDescriptor` record**: `src/WildBunch.GameContent/NewGame/GameSetupSeed.cs` — has `TravelDifficulty Difficulty` and `AdventureRandomnessPolicy AdventureRandomnessPolicy` (property name matches type name).
- **`GameSession` aggregate**: `src/WildBunch.Domain/Game/GameSession.cs` — has `TravelDifficulty TravelDifficulty` (property, line 111) and `AdventureRandomnessPolicy Entropy` (property, line 113).
- **`GameStarted` event**: `src/WildBunch.Domain/Events/GameStarted.cs` — has `TravelDifficulty Difficulty` and `AdventureRandomnessPolicy Entropy`.

### 2. Which domain/application/API/persistence/test/frontend/document surfaces reference them?

**Domain (`src/WildBunch.Domain/`)**:
- `Travel/TravelDifficulty.cs` — enum declaration
- `Travel/AdventureRandomnessPolicy.cs` — enum declaration
- `Travel/TravelRulesProfile.cs` — `TravelDifficulty Difficulty` property, `For(TravelDifficulty)` method, enum value switches
- `Travel/TravelDayGenerationContext.cs` — `TravelDifficulty Difficulty`, `AdventureRandomnessPolicy Entropy` fields
- `Travel/TravelDayPlanGenerator.Context.cs` — `TravelDifficulty.*` enum value references in switches
- `Travel/TrailEventCatalog.cs` — `TravelDifficulty.*` enum value references
- `Travel/JourneyEncounterResolutionEngine.cs` — `TravelDifficulty.*` enum value references
- `Game/GameSession.cs` — property, constructor params, `TravelRulesProfile.For(...)`, `StartingHealthFor(...)`, enum value switches, event payload fields
- `Events/GameStarted.cs` — event payload properties

**GameContent (`src/WildBunch.GameContent/`)**:
- `NewGame/GameSetupSeed.cs` — `StartingWorldDescriptor` record fields
- `NewGame/GameSetupSeedCodec.cs` — 49 references: params, `ResolveDifficulty`, `ResolveAdventureRandomnessPolicy`, `CreateCanonicalDescriptorShape`, enum value switches, `GetCanonicalSeedCode`, `GetBaseStartingCash`, `CreateDescriptorSignature`
- `NewGame/GameSetupDeterministicLabels.cs` — constant names `TravelDifficulty` and `AdventureRandomnessPolicy` (string values are codec hash keys — see Critical Scope Decisions)
- `NewGame/GameSetupPackage.cs` — `TravelDifficulty TravelDifficulty` field
- `NewGame/GameSetupGenerationPlan.cs` — `TravelDifficulty TravelDifficulty` property, `Descriptor.AdventureRandomnessPolicy` access
- `NewGame/GameSetupPackageBuilder.cs` — `descriptor.Difficulty` type reference
- `NewGame/SeededNewGameFactory.cs` — params, enum value references
- `NewGame/StartingWorldDescriptorSeedMixer.cs` — `descriptor.AdventureRandomnessPolicy.ToString()` in signature
- `NewGame/RuntimeTravelRandomnessSource.cs` — `TravelDifficulty` param
- `Abstractions/INewGameFactory.cs` — interface params
- `AGENTS.md` — references `ResolveAdventureRandomnessPolicy` in update guidance

**Application (`src/WildBunch.Application/`)**:
- `Games/Commands/StartNewGameCommand.cs` — command record fields
- `Games/Commands/StartNewGameHandler.cs` — `command.TravelDifficulty` access
- `Games/Models/GameSessionReadModel.cs` — read model record fields
- `Games/Models/GameDtos.cs` — `GameSessionDto` record fields
- `Games/Mapping/GameSessionMapper.cs` — param types, `session.TravelDifficulty` access, `TravelRulesProfile.For(travelDifficulty)` calls
- `Projections/FullAuditProjector.cs` — `gs.Difficulty` (uses event's `Difficulty` property; only the type changes, not the property name)

**API (`src/WildBunch.Api/`)**:
- `Games/Requests/StartGameRequest.cs` — request record fields
- `Games/GameSessionEndpoints.cs` — `validatedRequest.TravelDifficulty` access

**Persistence (`src/WildBunch.Persistence/`)**:
- `GameSessions/GameSessionEntity.cs` — `int TravelDifficulty` EF column property (line 13)
- `GameSessions/GameSessionEntityConfiguration.cs` — `e.TravelDifficulty` EF config (line 26)
- `GameSessions/EfGameSessionRepository.cs` — `entity.TravelDifficulty`, `(TravelDifficulty)store.Envelope.TravelDifficulty`, `AdventureRandomnessPolicy.*` defaults
- `GameSessions/GameSessionReadStoreLoader.cs` — `(TravelDifficulty)store.Envelope.TravelDifficulty`, `AdventureRandomnessPolicy.*` defaults
- `Serialization/GameSessionRehydrator.cs` — `typeof(TravelDifficulty)`, `typeof(AdventureRandomnessPolicy)`, constructor param types
- `Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` — `TravelDifficulty TravelDifficulty`, `AdventureRandomnessPolicy? Entropy` snapshot record fields
- `Serialization/GameSessionJsonSerializer.Setup.cs` — `AdventureRandomnessPolicy` type in `SetupSnapshot` record and method signatures
- `Serialization/GameSessionJsonSerializer.Rehydration.cs` — param types
- `Migrations/WildBunchDbContextModelSnapshot.cs` — `b.Property<int>("TravelDifficulty")` (current model snapshot, line 91)
- `Migrations/20260531154230_PostgresCutoverSync.cs` — column creation `name: "TravelDifficulty"` (historical migration — do not rewrite)
- `Migrations/20260531081955_ComposedSessionPersistence.cs` — column creation `name: "TravelDifficulty"` (historical migration — do not rewrite)
- Multiple historical `.Designer.cs` files reference `TravelDifficulty` — these are historical schema snapshots and stay unchanged.

**Frontend (`src/WildBunch.Web/src/`)** — *surfaces found on the preflight base (`73fd9fa`); see Staleness Gate G3 — re-derive from refreshed `main` before editing*:
- `api/types.ts` — `TravelDifficulty` type alias (line 2), `travelDifficulty` in `StartGameRequest` (line 50), `travelDifficulty` in `GameSessionDto` (line 471). Note: `GameSessionDto` does NOT have an `entropy` property in the frontend type at the preflight base — see D6 / G4 for the staleness caveat on this statement.
- `hooks/useStartGameSeed.ts` — `TravelDifficulty` type, `travelDifficulty` state, `setTravelDifficulty` setter, `handleTravelDifficultyChange` handler
- `components/StartGameOptionsForm.tsx` — `TravelDifficulty` type, `travelDifficulty` prop, `onTravelDifficultyChange` callback, "Travel difficulty" label
- `components/StartGamePanel.tsx` — `travelDifficulty` destructuring, `setTravelDifficulty`, request field
- `ui/formatters.ts` — `formatTravelDifficulty` function (defined but not currently called anywhere in src)
- Tests: `wildBunchApi.test.ts`, `test-utils/factories.ts`, `TravelRoutesPanel.test.tsx`, `TravelPanel.test.tsx`, `StartGamePanel.test.tsx`, `AppShell.test.tsx` — `travelDifficulty` in fixtures and assertions, `/travel difficulty/i` label matchers

**Docs**:
- `docs/adr/ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md` — references `AdventureRandomnessPolicy` in Decision, Detailed Decision Breakdown, Consequences, and Proof sections (lines 33, 57, 91, 110)
- `src/WildBunch.Domain/Travel/INDEX.md` — generated index listing `AdventureRandomnessPolicy.cs` and `TravelDifficulty.cs` (will be regenerated after file renames)
- `src/WildBunch.GameContent/AGENTS.md` — references `ResolveAdventureRandomnessPolicy` in update guidance (line 16)

### 3. Do either names appear in DB columns, migrations, JSON snapshots, API contracts, generated clients, or request/response DTOs?

**Yes — all of the above:**

- **DB column**: `GameSessionEntity.TravelDifficulty` is a real EF `int` column on the `GameSessions` table. Created in migration `20260531081955_ComposedSessionPersistence` and carried through subsequent migrations. Requires a new migration to rename the column.
- **Migrations**: Historical migration files (`20260531081955_ComposedSessionPersistence.cs`, `20260531154230_PostgresCutoverSync.cs`) create the column as `TravelDifficulty`. These are historical and stay unchanged. The current model snapshot (`WildBunchDbContextModelSnapshot.cs`) must be updated to `GameDifficulty`.
- **JSON snapshots**: `GameSessionSnapshot` record has `TravelDifficulty TravelDifficulty` and `AdventureRandomnessPolicy? Entropy`. The persistence serializer uses `JsonSerializerDefaults.Web` (camelCase), so JSON property names are `travelDifficulty` and `entropy`. After rename: `gameDifficulty` and `entropy` (the `Entropy` property name does not change). `SetupSnapshot` has `AdventureRandomnessPolicy? Entropy` → JSON `entropy` (unchanged property name). Per AGENTS.md, greenfield repo allows dev DB drop/recreate — no old-save compatibility shim needed.
- **API contracts**: `StartGameRequest` and `GameSessionDto` carry `TravelDifficulty TravelDifficulty` and `AdventureRandomnessPolicy Entropy`. ASP.NET Core minimal APIs use default camelCase JSON, so the wire contract is `travelDifficulty` and `entropy`. After rename: `gameDifficulty` and `entropy`.
- **Generated/frontend types**: `src/WildBunch.Web/src/api/types.ts` has `TravelDifficulty` type and `travelDifficulty` properties. These are hand-maintained types (not auto-generated), but they mirror the API contract.

### 4. What exact persistence/schema action is required?

**One EF migration to rename the `GameSessions.TravelDifficulty` column to `GameDifficulty`.**

Steps:
1. Rename `GameSessionEntity.TravelDifficulty` property to `GameDifficulty`.
2. Update `GameSessionEntityConfiguration` to reference `e.GameDifficulty`.
3. Update `EfGameSessionRepository` and `GameSessionReadStoreLoader` to use the new property name and cast to `GameDifficulty`.
4. Add a new EF migration (`dotnet ef migrations add RenameTravelDifficultyToGameDifficulty`) that renames the column. Since this is greenfield with dev DB drop/recreate allowed, the migration can use `RenameColumn` or the dev DB can be reset.
5. Update `WildBunchDbContextModelSnapshot.cs` to reflect `GameDifficulty` as the current model state.
6. Historical migration files (`.cs` and `.Designer.cs`) that reference `TravelDifficulty` stay unchanged — they are the audit trail of schema history.

**JSON snapshot property rename**: The `GameSessionSnapshot.TravelDifficulty` record property becomes `GameDifficulty`, which changes the JSON property name from `travelDifficulty` to `gameDifficulty`. Per AGENTS.md, no old-save compatibility shim is needed in this greenfield repo. The `Entropy` property name does not change (it was already `Entropy`).

### 5. Which historical references should intentionally remain?

- **`GameSetupDeterministicLabels` string values**: `"travel.difficulty"` and `"adventure-randomness-policy"` MUST remain unchanged. They are codec hash keys used by `StartingWorldDescriptorSeedMixer.GetFieldSeed`. Renaming them would change the hash, break UUID round-trip, and change every seed's resolution. Only the C# constant names (`TravelDifficulty` → `GameDifficulty`, `AdventureRandomnessPolicy` → `GameEntropy`) change; the string values stay.
- **Old EF migration files**: `20260531081955_ComposedSessionPersistence.*`, `20260531154230_PostgresCutoverSync.*`, and all their `.Designer.cs` files. These are historical schema snapshots.
- **Old plan files under `.agents/superpowers/plans/`**: Several completed plans reference `TravelDifficulty` or `AdventureRandomnessPolicy`. These are audit trails of what was planned at the time and should not be rewritten.
- **Other ADR historical status entries**: Dated `live` entries in ADRs are audit trails. Only the current `Status` line and `Decision` section must match the system today.

### 6. Which validation commands will prove both renames are complete?

- `dotnet build` — compile proves the type/property renames are consistent across all C# projects.
- `dotnet test` — all existing tests pass with renamed types/properties.
- `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` — proves the new migration is registered and the model snapshot is consistent.
- `.\scripts\postgres-dev.ps1 validate` — PostgreSQL-backed validation lane (EF + tests together).
- Frontend: `npm run build` and `npm test` in `src/WildBunch.Web` — proves frontend type/property/label renames are consistent.
- Grep/search proof: `TravelDifficulty` and `AdventureRandomnessPolicy` are absent from all active source/test/docs except intentional historical references (old migrations, old plan files, deterministic label string values).

---

## Critical Scope Decisions

### D1: Deterministic label strings are codec hash keys, not concept names

`GameSetupDeterministicLabels.TravelDifficulty = "travel.difficulty"` and `GameSetupDeterministicLabels.AdventureRandomnessPolicy = "adventure-randomness-policy"` are string labels fed into `StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, label)` which hashes them with FNV1a to derive field-specific seeds. Per ADR-0021, the mixer is "keyed by resolver namespace, resolver version, and field labels." Per root AGENTS.md "UUID Seed Codec" section, both codec directions must stay in sync.

**Decision**: Rename the C# constant names to `GameDifficulty` and `GameEntropy`. Keep the string values `"travel.difficulty"` and `"adventure-randomness-policy"` unchanged. Renaming the strings would break UUID round-trip without any naming benefit — the strings are never seen by users or code outside the codec internals.

### D2: Descriptor property name normalizes to `Entropy`

The `StartingWorldDescriptor` record currently has `AdventureRandomnessPolicy AdventureRandomnessPolicy` — both type and property name are the same. After the type rename to `GameEntropy`, the property name should become `Entropy` (not `GameEntropy`) to normalize with `GameSession.Entropy`, `GameStarted.Entropy`, and DTO `Entropy` which already use `Entropy` as the property name. This is a naming normalization, not a behavior change. The `CreateDescriptorSignature` method uses `.ToString()` on the value, not the property name, so the codec signature is unaffected.

### D3: EF column rename requires a migration

The `GameSessions.TravelDifficulty` column is a real DB column. Renaming the EF entity property requires a new migration. Per AGENTS.md: "Dev database drop/recreate is allowed when a current snapshot or schema shape changes and a reset is the cleanest path." The migration will rename the column; the dev DB can be reset during validation.

### D4: `TravelRulesProfile` type name is NOT in scope

`TravelRulesProfile` is a travel-rules tuning concept, not game-start difficulty. Only its `Difficulty` property type changes from `TravelDifficulty` to `GameDifficulty`. The type name `TravelRulesProfile` stays.

### D5: `TravelRandomnessState` is NOT in scope

`TravelRandomnessState` is journey-level runtime randomness, not game-start entropy. It is a separate concept from `AdventureRandomnessPolicy`/`GameEntropy` and stays unchanged.

### D6: Frontend `Entropy` is not consumed today (base-state evidence only)

At the preflight base (`73fd9fa`), the frontend `GameSessionDto` interface does not have an `entropy` property. The `AdventureRandomnessPolicy`/`GameEntropy` type only appears in the C# API contract. The frontend rename is limited to `TravelDifficulty` → `GameDifficulty` (type, properties, state, label, formatter, tests).

**This is base-state evidence only.** If BUNCH-102 lands first and introduces a frontend entropy setup surface (entropy selector in the new start-flow, `entropy` field on a new start request shape, prologue entropy parameter naming, etc.), this statement is stale and the frontend entropy setup surface MUST be included in the rename. See Staleness Gate G4 — the implementation worker must verify the refreshed frontend tree for any `entropy` / `Entropy` / `adventureRandomnessPolicy` / `AdventureRandomnessPolicy` surface before relying on this decision.

---

## Implementation Tasks

> **Prerequisite — Staleness Gate (see Staleness Gate / BUNCH-102 Interaction section above):** Before starting Task 1, the implementation worker MUST refresh from current `main`, check whether BUNCH-102 has landed, re-run the naming surface search, and diff against the preflight inventory. If BUNCH-102 has landed, Task 5 (API) and Task 7 (frontend) must be re-derived from the refreshed tree. If the refreshed `main` changes the work beyond a straightforward surface update, stop AMBER per G5 and report the changed seams.

### Task 1: Rename domain enum types and files

**Files:**
- Rename: `src/WildBunch.Domain/Travel/TravelDifficulty.cs` → `src/WildBunch.Domain/Travel/GameDifficulty.cs`
- Rename: `src/WildBunch.Domain/Travel/AdventureRandomnessPolicy.cs` → `src/WildBunch.Domain/Travel/GameEntropy.cs`

**Interfaces:**
- Produces: `GameDifficulty` enum (values `Normal=0, Easy=1, Hard=2`), `GameEntropy` enum (values `Boring=0, Standard=1, Adventurous=2, Wild=3, Classic=Standard`)

- [ ] **Step 1: Rename `TravelDifficulty.cs` to `GameDifficulty.cs` and rename the enum.**

Rename the file and change `public enum TravelDifficulty` to `public enum GameDifficulty`. Values stay identical: `Normal = 0, Easy = 1, Hard = 2`.

- [ ] **Step 2: Rename `AdventureRandomnessPolicy.cs` to `GameEntropy.cs` and rename the enum.**

Rename the file and change `public enum AdventureRandomnessPolicy` to `public enum GameEntropy`. Values stay identical: `Boring = 0, Standard = 1, Adventurous = 2, Wild = 3, Classic = Standard`.

### Task 2: Update domain layer references

**Files:**
- Modify: `src/WildBunch.Domain/Travel/TravelRulesProfile.cs`
- Modify: `src/WildBunch.Domain/Travel/TravelDayGenerationContext.cs`
- Modify: `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs`
- Modify: `src/WildBunch.Domain/Travel/TrailEventCatalog.cs`
- Modify: `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Events/GameStarted.cs`

- [ ] **Step 1: Update `TravelRulesProfile.cs`.**

Change `TravelDifficulty Difficulty` → `GameDifficulty Difficulty`. Change `For(TravelDifficulty difficulty)` → `For(GameDifficulty difficulty)`. Change all `TravelDifficulty.Normal/Easy/Hard` → `GameDifficulty.Normal/Easy/Hard`.

- [ ] **Step 2: Update `TravelDayGenerationContext.cs`.**

Change `TravelDifficulty Difficulty` → `GameDifficulty Difficulty`. Change `AdventureRandomnessPolicy Entropy` → `GameEntropy Entropy`.

- [ ] **Step 3: Update `TravelDayPlanGenerator.Context.cs`.**

Change all `TravelDifficulty.Easy/Normal/Hard` → `GameDifficulty.Easy/Normal/Hard`. Change `context.Difficulty` type references if any explicit type annotations exist.

- [ ] **Step 4: Update `TrailEventCatalog.cs` and `JourneyEncounterResolutionEngine.cs`.**

Change all `TravelDifficulty.*` enum value references → `GameDifficulty.*`.

- [ ] **Step 5: Update `GameSession.cs`.**

Change property `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty` (line 111). Change property `AdventureRandomnessPolicy Entropy` → `GameEntropy Entropy` (line 113). Change constructor params (lines 58, 60). Change `TravelRulesProfile.For(TravelDifficulty)` → `TravelRulesProfile.For(GameDifficulty)`. Change `StartingHealthFor(TravelDifficulty)` → `StartingHealthFor(GameDifficulty)`. Change all enum value switches `TravelDifficulty.*` → `GameDifficulty.*`. Change `AdventureRandomnessPolicy.*` → `GameEntropy.*`. Update the comment on line 788 that mentions `TravelDifficulty`.

- [ ] **Step 6: Update `GameStarted.cs`.**

Change `TravelDifficulty Difficulty` → `GameDifficulty Difficulty`. Change `AdventureRandomnessPolicy Entropy` → `GameEntropy Entropy`.

### Task 3: Update GameContent layer (seed codec)

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupSeed.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupSeedCodec.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupDeterministicLabels.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupPackage.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupGenerationPlan.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupPackageBuilder.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`
- Modify: `src/WildBunch.GameContent/NewGame/StartingWorldDescriptorSeedMixer.cs`
- Modify: `src/WildBunch.GameContent/NewGame/RuntimeTravelRandomnessSource.cs`
- Modify: `src/WildBunch.GameContent/Abstractions/INewGameFactory.cs`
- Modify: `src/WildBunch.GameContent/AGENTS.md`

- [ ] **Step 1: Update `GameSetupSeed.cs` — `StartingWorldDescriptor` record.**

Change `TravelDifficulty Difficulty` → `GameDifficulty Difficulty`. Change `AdventureRandomnessPolicy AdventureRandomnessPolicy` → `GameEntropy Entropy` (normalize property name per Critical Scope Decision D2).

- [ ] **Step 2: Update `GameSetupDeterministicLabels.cs` — constant names only.**

Rename constant `TravelDifficulty` → `GameDifficulty` (string value `"travel.difficulty"` stays unchanged). Rename constant `AdventureRandomnessPolicy` → `GameEntropy` (string value `"adventure-randomness-policy"` stays unchanged). See Critical Scope Decision D1.

- [ ] **Step 3: Update `GameSetupSeedCodec.cs`.**

Change all `TravelDifficulty` type references → `GameDifficulty`. Change all `AdventureRandomnessPolicy` type references → `GameEntropy`. Rename method `ResolveAdventureRandomnessPolicy` → `ResolveGameEntropy`. Update `GameSetupDeterministicLabels.TravelDifficulty` → `GameSetupDeterministicLabels.GameDifficulty` and `GameSetupDeterministicLabels.AdventureRandomnessPolicy` → `GameSetupDeterministicLabels.GameEntropy` (constant name references, string values stay). Update `descriptor.AdventureRandomnessPolicy` → `descriptor.Entropy` (property name normalized per D2). Change all enum value switches.

- [ ] **Step 4: Update `GameSetupPackage.cs`.**

Change `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty`.

- [ ] **Step 5: Update `GameSetupGenerationPlan.cs`.**

Change `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty`. Change `Descriptor.AdventureRandomnessPolicy` → `Descriptor.Entropy`.

- [ ] **Step 6: Update `GameSetupPackageBuilder.cs`, `SeededNewGameFactory.cs`, `StartingWorldDescriptorSeedMixer.cs`, `RuntimeTravelRandomnessSource.cs`, `INewGameFactory.cs`.**

Update all type references, param types, enum value switches, and `descriptor.AdventureRandomnessPolicy` → `descriptor.Entropy` accesses.

- [ ] **Step 7: Update `src/WildBunch.GameContent/AGENTS.md`.**

Change `ResolveAdventureRandomnessPolicy` → `ResolveGameEntropy` in the "When to update this project" guidance (line 16).

### Task 4: Update Application layer

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/StartNewGameCommand.cs`
- Modify: `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`
- Modify: `src/WildBunch.Application/Games/Models/GameSessionReadModel.cs`
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Modify: `src/WildBunch.Application/Projections/FullAuditProjector.cs`

- [ ] **Step 1: Update `StartNewGameCommand.cs` and `StartNewGameHandler.cs`.**

Change `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty`. Change `AdventureRandomnessPolicy Entropy` → `GameEntropy Entropy`. Change `command.TravelDifficulty` → `command.GameDifficulty` in handler.

- [ ] **Step 2: Update `GameSessionReadModel.cs` and `GameDtos.cs`.**

Change `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty`. Change `AdventureRandomnessPolicy Entropy` → `GameEntropy Entropy`.

- [ ] **Step 3: Update `GameSessionMapper.cs`.**

Change param types `TravelDifficulty travelDifficulty` → `GameDifficulty gameDifficulty` and `AdventureRandomnessPolicy entropy` → `GameEntropy entropy`. Change `session.TravelDifficulty` → `session.GameDifficulty`. Change `TravelRulesProfile.For(travelDifficulty)` → `TravelRulesProfile.For(gameDifficulty)`.

- [ ] **Step 4: Update `FullAuditProjector.cs`.**

The projector uses `gs.Difficulty` (the event's `Difficulty` property). The property name stays `Difficulty`; only the type changes. No change needed unless there is an explicit type annotation.

### Task 5: Update API layer

> **Re-derive from refreshed `main` first (Staleness Gate G2).** BUNCH-102 may have reshaped `StartGameRequest` or added prologue query/request parameter naming. Verify the API surface list against the refreshed tree and add any new difficulty/entropy-bearing request/response surfaces BUNCH-102 introduced.

**Files (preflight base — verify and extend against refreshed `main`):**
- Modify: `src/WildBunch.Api/Games/Requests/StartGameRequest.cs`
- Modify: `src/WildBunch.Api/Games/GameSessionEndpoints.cs`

- [ ] **Step 1: Update `StartGameRequest.cs`.**

Change `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty`. Change `AdventureRandomnessPolicy Entropy` → `GameEntropy Entropy`.

- [ ] **Step 2: Update `GameSessionEndpoints.cs`.**

Change `validatedRequest.TravelDifficulty` → `validatedRequest.GameDifficulty`.

### Task 6: Update Persistence layer and add EF migration

**Files:**
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionEntityConfiguration.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
- Modify: `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Setup.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Rehydration.cs`
- Modify: `src/WildBunch.Persistence/Migrations/WildBunchDbContextModelSnapshot.cs` (generated by migration add)
- Add: new migration file (generated by `dotnet ef migrations add`)

- [ ] **Step 1: Update `GameSessionEntity.cs` and `GameSessionEntityConfiguration.cs`.**

Change `int TravelDifficulty` → `int GameDifficulty` in entity. Change `e.TravelDifficulty` → `e.GameDifficulty` in configuration.

- [ ] **Step 2: Update `EfGameSessionRepository.cs` and `GameSessionReadStoreLoader.cs`.**

Change `entity.TravelDifficulty` → `entity.GameDifficulty`. Change `(TravelDifficulty)store.Envelope.TravelDifficulty` → `(GameDifficulty)store.Envelope.GameDifficulty`. Change `AdventureRandomnessPolicy.Standard` → `GameEntropy.Standard` defaults.

- [ ] **Step 3: Update serialization files.**

In `GameSessionRehydrator.cs`: change `typeof(TravelDifficulty)` → `typeof(GameDifficulty)`, `typeof(AdventureRandomnessPolicy)` → `typeof(GameEntropy)`, param types. In `GameSessionJsonSerializer.SessionSnapshot.cs`: change `TravelDifficulty TravelDifficulty` → `GameDifficulty GameDifficulty`, `AdventureRandomnessPolicy? Entropy` → `GameEntropy? Entropy`, `session.TravelDifficulty` → `session.GameDifficulty`. In `GameSessionJsonSerializer.Setup.cs`: change `AdventureRandomnessPolicy` → `GameEntropy` in method signatures and `SetupSnapshot` record. In `GameSessionJsonSerializer.Rehydration.cs`: change param types.

- [ ] **Step 4: Add EF migration to rename the column.**

Run `dotnet ef migrations add RenameTravelDifficultyToGameDifficulty --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`. Verify the generated migration renames the `GameSessions.TravelDifficulty` column to `GameDifficulty`. Verify the model snapshot (`WildBunchDbContextModelSnapshot.cs`) now references `GameDifficulty`. If the generated migration does not produce a clean rename, inspect and adjust the migration file to use `migrationBuilder.RenameColumn(...)`.

- [ ] **Step 5: Verify old migration files are unchanged.**

Confirm that `20260531081955_ComposedSessionPersistence.*`, `20260531154230_PostgresCutoverSync.*`, and all their `.Designer.cs` files still reference `TravelDifficulty` as historical schema. Do not rewrite them.

### Task 7: Update frontend

> **Re-derive from refreshed `main` first (Staleness Gate G2/G3/G4).** The file list below is the preflight-base (`73fd9fa`) surface set. If BUNCH-102 has landed, the actual frontend task list must be re-derived from the refreshed tree — add any new start-flow / prologue / setup surfaces BUNCH-102 introduced (e.g. `SetupHuntStep`, `useStartFlow`, `getPrologue`, prologue query/request parameter naming, new start-flow/setup tests, any new browser proof or setup-flow tests). Also re-check whether a frontend entropy setup surface now exists (G4) and include it if so.

**Files (preflight base — verify and extend against refreshed `main`):**
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `src/WildBunch.Web/src/hooks/useStartGameSeed.ts`
- Modify: `src/WildBunch.Web/src/components/StartGameOptionsForm.tsx`
- Modify: `src/WildBunch.Web/src/components/StartGamePanel.tsx`
- Modify: `src/WildBunch.Web/src/ui/formatters.ts`
- Modify: `src/WildBunch.Web/src/tests/wildBunchApi.test.ts`
- Modify: `src/WildBunch.Web/src/tests/test-utils/factories.ts`
- Modify: `src/WildBunch.Web/src/tests/TravelRoutesPanel.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/StartGamePanel.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/AppShell.test.tsx`

- [ ] **Step 1: Update `api/types.ts`.**

Rename type `TravelDifficulty` → `GameDifficulty` (line 2). Rename `travelDifficulty` → `gameDifficulty` in `StartGameRequest` (line 50) and `GameSessionDto` (line 471).

- [ ] **Step 2: Update `hooks/useStartGameSeed.ts`.**

Rename `TravelDifficulty` type import → `GameDifficulty`. Rename `travelDifficulty` state → `gameDifficulty`. Rename `setTravelDifficulty` → `setGameDifficulty`. Rename `handleTravelDifficultyChange` → `handleGameDifficultyChange`. Update the `UseStartGameSeedResult` interface.

- [ ] **Step 3: Update `components/StartGameOptionsForm.tsx`.**

Rename `TravelDifficulty` type → `GameDifficulty`. Rename `travelDifficulty` prop → `gameDifficulty`. Rename `onTravelDifficultyChange` → `onGameDifficultyChange`. Change label "Travel difficulty" → "Game difficulty". Update the `Select` cast.

- [ ] **Step 4: Update `components/StartGamePanel.tsx`.**

Rename `travelDifficulty` → `gameDifficulty` in destructuring and request field. Rename `setTravelDifficulty` → `setGameDifficulty`. Rename `onTravelDifficultyChange` → `onGameDifficultyChange`.

- [ ] **Step 5: Update `ui/formatters.ts`.**

Rename `formatTravelDifficulty` → `formatGameDifficulty`.

- [ ] **Step 6: Update frontend tests.**

In all test files: rename `travelDifficulty` → `gameDifficulty` in fixtures and assertions. In `StartGamePanel.test.tsx`: update label matcher `/travel difficulty/i` → `/game difficulty/i`.

### Task 8: Update docs and ADR

**Files:**
- Modify: `docs/adr/ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md`
- Modify: `docs/adr/INDEX.md` (freshness timestamp for ADR-0021)

- [ ] **Step 1: Update ADR-0021.**

Replace `AdventureRandomnessPolicy` with `GameEntropy` in the Decision section (line 33), Detailed Decision Breakdown (line 57), Consequences (line 91), and Proof of Implementation (line 110). The current `Status` line and `Decision` section must match the system today per the ADR Log Freshness rule. Do not change historical dated status entries.

- [ ] **Step 2: Update `docs/adr/INDEX.md` freshness timestamp.**

Update the "Last checked" timestamp for ADR-0021 to today's date.

- [ ] **Step 3: Regenerate or update generated INDEX.md files.**

Run `python scripts/generate_index_mesh.py` (or the repo's index generation script) to regenerate `src/WildBunch.Domain/Travel/INDEX.md` and any other affected index files so they list `GameDifficulty.cs` and `GameEntropy.cs` instead of the old file names.

### Task 9: Validation and grep proof

- [ ] **Step 1: Run `dotnet build`.**

Verify clean build with no warnings or errors.

- [ ] **Step 2: Run `dotnet test`.**

Verify all tests pass.

- [ ] **Step 3: Run EF migration validation.**

Run `.\scripts\postgres-dev.ps1 ensure` then `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`. Verify the new migration is listed and applies cleanly.

- [ ] **Step 4: Run PostgreSQL-backed validation.**

Run `.\scripts\postgres-dev.ps1 validate` to run EF and test checks together against the shared local PostgreSQL service.

- [ ] **Step 5: Run frontend build and tests.**

In `src/WildBunch.Web`: run `npm run build` and `npm test`. Verify clean build and all tests pass.

- [ ] **Step 6: Grep proof for old names.**

Search the repo for `TravelDifficulty` and `AdventureRandomnessPolicy` in active source/test/docs. Confirm they only remain in:
- `GameSetupDeterministicLabels` string values (`"travel.difficulty"`, `"adventure-randomness-policy"`) — codec hash keys
- Old EF migration files and their `.Designer.cs` — historical schema
- Old plan files under `.agents/superpowers/plans/` — historical audit trails

Search for `GameDifficulty` and `GameEntropy` to confirm they are present in all expected surfaces.

- [ ] **Step 7: Run UUID round-trip guardrail test.**

Verify the seed codec round-trip test still passes (it should, since deterministic label strings are unchanged). This proves the rename did not break the codec.

---

## Return Evidence

Return branch, PR URL, final head SHA, changed files, validation outputs (dotnet build, dotnet test, EF migration list, postgres validate, frontend build/test), persistence/migration decision (column rename migration), and grep/search proof for old and new names.

## Success Criteria

- `GameDifficulty` is the canonical enum type name everywhere `TravelDifficulty` was used for game-start difficulty.
- `GameEntropy` is the canonical enum type name everywhere `AdventureRandomnessPolicy` was used for game-start entropy/randomness.
- The `StartingWorldDescriptor` property for entropy is normalized to `Entropy` (matching `GameSession.Entropy` and DTOs).
- The EF column is renamed to `GameDifficulty` with a clean migration.
- The JSON snapshot and API contract use `gameDifficulty` and `entropy` (camelCase).
- The frontend type, properties, state, label, and formatter use `GameDifficulty` / `gameDifficulty`.
- Deterministic label string values are unchanged (codec hash keys).
- Behavior, values, levels, and semantics are unchanged.
- No stray active-code `TravelDifficulty` or `AdventureRandomnessPolicy` references remain except intentional historical text (old migrations, old plans, codec label strings).
- ADR-0021 is updated to reflect the new names.
- All validation passes: `dotnet build`, `dotnet test`, EF migration list, PostgreSQL validate, frontend build, frontend tests.


---

## BUNCH-87 Addendum (Section B87)

**Folded into this PR during taxonomy addendum work.** BUNCH-87's goal � replacing seed-dependent retry loops in travel tests with ForceDevTravelOverride deterministic control � was accelerated by the taxonomy change. The enum value renames (Normal->Standard, Hard->Challenging) and the Brutal addition (codec modulo 3->4) shifted deterministic seed outputs, breaking seed-dependent test assertions. Fixing those failures with ForceDevTravelOverride completed the BUNCH-87 work in the same pass.

### B87-1: TravelEncounterResolutionCharacterizationTests

- Replaced the AdvanceUntilInterrupted retry loop (up to 10 days) with ForceDevTravelOverride(DevTravelOverride.ForFoe(...)) � a single forced foe encounter with an explicit JourneyFoeProfile(Speed: 3, FightStrength: 3, MinimumBribe: 9m) matching the bribe-cost assertions.
- All 6 characterization tests pass with the forced-foe fixture.

### B87-2: TravelReplayEqualityTests

- Replay_FullJourneyCycle: replaced the unbounded advance loop with ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet)) before each advance, ensuring the journey completes without seed-dependent interruptions.
- Replay_ResolveJourneyEncounter: replaced the retry-until-interrupted loop with ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe)) for a single deterministic interruption.
- All 4 replay tests pass.

### B87-3: Stale seed-catalog comments

- TravelTestSeedCatalog.FrontierFootNormalFoe: reworded the XML doc comment. The old comment claimed the seed profile "produces a foe on day 1" � stale after the taxonomy change shifted seed outputs. New comment clarifies the profile is a route/setup guardrail and foe determinism comes from ForceDevTravelOverride.

### B87-4: Integration tests

- 9 integration tests fixed using ForceDevTravelOverride (via domain seam or dev-travel API endpoint) instead of relying on seed-dependent encounter outcomes. See the integration test subagent's return for details.

### B87-5: Scope note

The CreateForcedFoeJourney helper on TravelTestFactory (originally planned as a BUNCH-87 Task 1 step) was not added as a separate helper because DevTravelOverride.ForFoe(...) + session.ForceDevTravelOverride(...) is already a one-line seam. Adding a wrapper would duplicate the existing dev-travel API without adding clarity. The end-to-end forced-foe dev-travel integration test (BUNCH-87 Task 2) is covered by the existing HighRiskTravelCanPauseResolveAndResumeWithoutSkippingTheTrail integration test, which now uses ForceTravelOverrideRequestDto("Npc", ...) to force a deterministic encounter.