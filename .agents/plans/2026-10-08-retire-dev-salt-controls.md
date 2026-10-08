# Retire Developer Salt Overrides Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the incomplete developer town-layout salt override and inert session RNG lock/clear controls, including their exclusive prepped-start pipeline, while preserving normal deterministic world/layout generation, player start, and persisted layout facts.

**Architecture:** Keep `SaltSource`, entropy policy, concern-scoped layout salt derivation, generated `TownLayout.LayoutSalts`, and normal player `Go` setup. Remove only developer override state/events and the prepped-snapshot path that exists to feed them; keep the shared map DTO's layout-salt data by moving its type to the game contract. The disabled RNG routes and their incomplete event-backed controls are retired rather than re-enabled.

**Tech Stack:** C#/.NET domain, application, API, GameContent and EF persistence; React/TypeScript playtest overlay; xUnit and Vitest; repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially developer salt-control direction, player-start semantics, event replay and history boundary; [feature matrix](../../docs/features.md), DEV-001; [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 05 and per-plan delivery contract.

**Execution Strategy:** `executing-plans` with Native inline execution. The layout override, prepped-start path, persistence adapters, API routes, browser controls, RNG lock/clear mutations and deterministic fixtures form one narrowly related developer salt-control retirement. These edits share one authority boundary and need one continuous integration context; splitting them into separate PRs would leave dead or misleading halves between slices. The user authorized inline execution for JIT roadmap plans.

## Global Constraints

- Start from the fresh row 05 worktree at the latest merged `develop` commit `ed59233ad84b4302ee1bb0588c09a8a7cf54bba8` and deliver by PR to `develop`.
- Advance the sole authored application version in `Directory.Build.props` exactly once from `0.1.0-dev.10` to `0.1.0-dev.11` for this PR.
- Preserve normal player creation through `CompletePlayerSetupHandler` and the settled Go -> prologue -> starting-town flow.
- Preserve `SaltSource`, normal fixed/runtime entropy semantics, world generation, derived concern-scoped town-layout salts, stored `TownLayout.LayoutSalts`, public map serialization, renderer inputs and replay of established facts.
- Remove the snapshot-only `PrepGameSession` / `StartPrepped` route and only its exclusive consumers; do not replace it with another prepped or layout-editor flow.
- Remove session RNG lock/clear controls and their dev-only mutations rather than re-enable disabled routes. Keep independent read-only session context and existing difficulty/entropy dev controls.
- Do not preserve obsolete dev event or component codecs solely for disposable pre-alpha playtests; the previous row-05 migration established the supported history boundary and preserves the database migration chain.
- Tests must protect retained behavior, witness any intended RED, and avoid route-absence, type/file inventory, or implementation-mirroring tests.
- Use the check-only canonical gate and normal hooked commits. Fix lint/format failures before expensive build or behavior lanes.

## Review Focus

- Normal Go still generates one world with derived layout salts and player start facts in the accepted event history; removing the exclusive dev preparation path must not require a second setup operation.
- Generated layout salts remain serialized in the world/map contract and stay stable on reconstruction; deleting the dev-only component must not delete the real town-layout facts.
- Deterministic gameplay tests remain reproducible through explicit test factory/session inputs after removing the nonfunctional `/lock-rng` request.
- Other working dev surfaces, including session context, difficulty/entropy, saloon and travel controls, keep their current contracts.

---

### Task 1: Bootstrap the JIT slice and retire the completed predecessor

**Files:** Create this plan; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-08-unrelated-criminal-retirement.md`.

- [x] Verify PR #195 is merged to `develop` at `ed59233ad84b4302ee1bb0588c09a8a7cf54bba8`, the hosted canonical gate passed on the exact reviewed head, and the delivered PG-009-R scope is represented in current source and matrix.
- [x] Classify the full unrelated-criminal plan as shipped by PR #195, preserve its durable history/migration decisions at their current owners, and remove the completed plan from this successor slice.
- [x] Update roadmap row 05 with PR #195, source/merge evidence, hosted run `37811281072`, and `0.1.0-dev.10`; keep row 05 executing and replace its current plan link with this plan. State that this is the next bounded exclusion, not completion of row 05.
- [x] Advance `Directory.Build.props` exactly once from `0.1.0-dev.10` to `0.1.0-dev.11`. Preserve the current pre-1.0 policy: no API/gameplay compatibility promise is made and no future `1.0.0` contract is required or decided in this epic. The later SemVer adoption plan will pin the updated AOM definition current at authoring time.
- [x] Stage the roadmap, version and predecessor-plan retirement; inspect the exact staged diff and commit through the check-only hook. Do not run the complete CI gate immediately before or after this ordinary hooked commit.

### Task 2: Remove town-layout override and its exclusive prepped-start backend

**Files:** Modify `src/WildBunch.Api/Dev/DevEndpoints.cs`, `src/WildBunch.Api/DependencyInjection.cs`, `src/WildBunch.Application/Games/Commands/PrepGameSessionCommand.cs`, `PrepGameSessionHandler.cs`, `StartGameSessionCommand.cs`, `StartGameSessionHandler.cs`, `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`, `src/WildBunch.Domain/Game/GameSession.cs`, `GameSessionEventReplay.cs`, `GameStatus.cs`, `src/WildBunch.GameContent/Abstractions/INewGameFactory.cs`, `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`, `GameSetupResolver.cs`, `ResolvedGameSetup.cs`, `MapGenerator.cs`, `LayoutSaltDeriver.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs`, `EfGameSessionRepository.cs`, `Versioning/PersistedPayloadLoader.cs`, `Serialization/GameSessionRehydrator.cs`, `Serialization/GameSessionJsonSerializer.Components.cs`, `Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`, `Serialization/GameSessionJsonSerializer.Events.cs`, and `tests/WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs`. Move `src/WildBunch.Application/Dev/Models/TownLayoutSaltsDto.cs` to `src/WildBunch.Application/Games/Models/TownLayoutSaltsDto.cs`. Remove the exclusive `TownLayoutDevPanel`, its test, and `tests/WildBunch.Integration.Tests/Dev/TownLayoutDevIntegrationTests.cs`; modify `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`, `src/WildBunch.Web/src/dev/devApi.ts`, `src/WildBunch.Web/src/dev/types.ts` and `src/WildBunch.Web/src/tests/test-utils/setup.ts`. Update/remove focused tests in the Domain, Application, GameContent, Web and Integration owners.

- [ ] Run `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter FullyQualifiedName~CompletePlayerSetupHandlerTests.SetupCreatesSessionAndReturnsDto`, `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~MapGeneratorDevSaltsTests.Generate_PutsEffectiveSaltsOnEveryTownLayout`, and `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~EventSourcingEndToEndTests`. Confirm the supported player start and generated layout facts are exercised without `DevLayoutSalts` before changing the code.
- [ ] Remove the `/api/dev/games/prep`, `/api/dev/games/{id}/start`, town-layout salt GET/set/random routes, their handlers/commands/results/DI registrations, and the exclusive `StartGameSessionHandler` path. Preserve the ordinary setup route and other dev endpoints.
- [ ] Remove `GameSession.StartPrepped`, `StartFromPrepped`, `Prepped` status handling where exclusively used by this path, `DevLayoutSalts` aggregate state/application/replay, and `DevLayoutSaltsForced`. Remove only this developer field from snapshots, component payloads, rehydration and persistence serialization; do not remove normal `World`/`TownSnapshot` layout salts.
- [ ] Remove the optional dev-salt parameter/overload from `INewGameFactory`, `GameSetupResolver`, `ResolvedGameSetup`, `MapGenerator` and `LayoutSaltDeriver`. Keep deterministic derivation from seed, entropy mode, town identity, slot and concern as the only production layout-salt path.
- [ ] Move the shared `TownLayoutSaltsDto` into `Application.Games.Models` so `Games.Models.TownLayoutDto` and `TownLayoutMapper` remain valid without importing a Dev contract. Keep the ordinary API/web world-map `layoutSalts` field and its serialized values.
- [ ] Retire only tests whose sole promise is the prepped-start or manual layout override, including the enum-only `GameStatus_Prepped_Exists` check. Keep and run meaningful normal start, generated-layout derivation, DTO mapping, world persistence and replay behavior tests. Do not add a test whose only assertion is that the retired route/type/component is absent.
- [ ] Add one PostgreSQL-backed scenario to `EventSourcingEndToEndTests` that starts a real seeded world through the normal `SeededNewGameFactory` path, commits its generated layout facts, reconstructs from the persisted event stream, and asserts the known town layout and four generated salt values survive. Establish the expected values from the fixed input independently of the post-write reconstructed object; this protects the retained persistence contract across removal of the separate dev salt component.
- [ ] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`, `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`, `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`, and `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~EventSourcingEndToEndTests|FullyQualifiedName~TownLayoutMapperTests"`. The integration lane uses the repository's shared PostgreSQL service.

### Task 3: Remove session RNG lock/clear mutations and stale client calls

**Files:** Modify `src/WildBunch.Api/Dev/DevEndpoints.cs`, `src/WildBunch.Api/DependencyInjection.cs`, the `src/WildBunch.Application/Dev/Commands/ForceDevSaltSource*` and `ClearDevSaltSource*` files, `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, `src/WildBunch.Domain/Events/DevSaltSourceForced.cs`, `src/WildBunch.Domain/Events/DevSaltSourceCleared.cs`, and `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`; modify `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`, `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`, `src/WildBunch.Web/src/dev/devApi.ts`, and `src/WildBunch.Web/src/dev/types.ts`; reconcile `GetSessionDevContextHandlerTests`, `SaloonPoiSelectionTests`, `GameApiWantedPostersTests`, `WantedPosterAcceptanceTests`, `DevSessionEndpointTests`, and frontend session-panel tests.

- [ ] Establish deterministic saloon/poster test setup through the existing explicit fixed `SaltSource` fixture/factory seam. Run the affected behavior test(s) and verify their deterministic inputs do not depend on the currently unregistered `/lock-rng` route.
- [ ] Remove the session panel's salt input, lock/clear buttons and calls, `lockRng`/`clearRng` API helpers and request DTO. Retain session status/context reads and the working difficulty/entropy controls.
- [ ] Remove the commented lock/clear route stubs, `ForceDevSaltSource`/`ClearDevSaltSource` application commands and handlers, dependency registrations, aggregate methods/event application/replay, and their event definitions/serializer registrations. Keep the `SaltSource` value object, entropy behavior and established event fields used by player setup and gameplay.
- [ ] Remove skipped tests for permanently unregistered lock/clear routes and remove ignored lock-rng POSTs from wanted-poster tests. Preserve meaningful session-context, poster, saloon selection, difficulty and entropy behavior coverage. Do not replace these tests with checks that routes return 404.
- [ ] Run focused .NET and web behavior tests. Confirm a fixed source in tests produces the expected saloon/poster behavior through normal gameplay and that session context still reports the source established by player genesis.

### Task 4: Reconcile feature truth, ADR history, agent guidance and investigation follow-ups

**Files:** Modify `docs/features.md` DEV-001, `docs/decisions/ADR-0036-dev-enabled-action-pattern.md`, `.agents/doctrine/entropy-and-seed.md`, `.agents/doctrine/dev-overlay.md`, `.agents/unslop/dev-overlay.md`, `.agents/unslop/observations.md`, and the relevant sections of `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, `2026-10-07-domain-test-followup.md`, `2026-10-07-persistence-test-followup.md`, `2026-10-07-web-test-followup.md`, `2026-10-06-dotnet-test-dispositions.md`, and `2026-10-06-test-quality-investigation.md`.

- [ ] Update DEV-001 to record the shipped retirement of the manual town-layout override, exclusive prepped-start path and inert RNG lock/clear mutation controls. Keep the future alternate-salt preview story deferred and preserve the normal generated/persisted layout promise.
- [ ] Add a dated ADR-0036 note stating that this specific developer override/prep implementation was retired for 0.1.0 while its durable separation rule remains: developer controls never enter player command contracts. Remove the stale present-tense assertion that a current prepped developer state remains; do not rewrite the original 2026-07-10 decision.
- [ ] Remove or correct entropy/test guidance that tells agents to use `ForceDevSaltSource` as an active dev control. Explain the retained deterministic test seam using explicit `SaltSource.CreateFixed(...)` at session/factory setup where that is the contract under test.
- [ ] Remove stale dev-overlay examples that claim RNG lock or Town Layout panel as a working control. Preserve the doctrine for remaining supported dev controls and state that each described control must have a working backend consumer before it is advertised.
- [ ] Add a semantic reminder to the dev-overlay unslop profile: every advertised control must reach a registered, guarded server command and its real outcome; a mocked client call, commented endpoint or skipped test does not establish a usable control. Record this concrete observation without claiming independent recurrence, and do not add a structural route-presence test.
- [ ] Add concise dated dispositions beside the affected investigation recommendations so old audit claims remain historical evidence but no longer prescribe implementation of the retired prep, override or lock/clear behavior. Record the positive retained tests that continue to protect normal start, deterministic generation and persistence; do not rewrite unrelated audit findings.
- [ ] Run focused markdown/link/style validation and review all changed documentation against the source behavior.

### Task 5: Verify the complete slice and prepare the develop PR

**Files:** Entire plan scope; update this plan's checklist and roadmap delivery evidence after the actual PR and hosted results are known.

- [ ] Search source, tests, docs and agent guidance for `DevLayoutSalts`, `StartPrepped`, `StartFromPrepped`, `PrepGameSession`, `StartGameSessionHandler`, `ForceDevSaltSource`, `ClearDevSaltSource`, `DevSaltSourceForced`, `DevSaltSourceCleared`, `lock-rng`, `clear-rng` and town-layout dev endpoint/panel names. Inspect each remaining hit and confirm it is an intentional historical mention or an unrelated shared layout fact.
- [ ] Run focused test targets from the testing playbook, then `py -3 tools/run.py ci --check` once on the final candidate. Report PostgreSQL-dependent verification and skipped checks accurately.
- [ ] Review the full diff for preservation of normal player setup, generated/persisted layout facts, other working dev controls and historical ADR truth. Confirm no schema migration or database reset was added for this removal.
- [ ] Commit the implementation and documentation changes through the normal check-only hook. Push the final branch, create a Draft PR targeting `develop`, verify the exact head and required hosted gate, and obtain a fresh whole-branch review before making the PR ready.
- [ ] Merge the approved PR into `develop` under the epic's authorized delivery contract. Record the PR, merge/source heads, hosted gate and resulting development checkpoint in roadmap row 05; keep row 05 executing because other excluded capabilities remain.
- [ ] Retain this plan and the spec through this completing PR. The next substantive successor slice will assess and retire this plan only after confirming the full scope shipped and its durable decisions have current owners.
