# Retire Town Service Variation and Telegraph Leads Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Give every generated town the same visible core services, keep Telegraph unusable in 0.1.0, and remove per-town service/source variation and telegraph gang clues.

**Architecture:** Town layout owns visible service presence; the aggregate's available actions own what the player can use. GameContent generates the fixed service buildings, while Domain retains one shared investigation-source policy for the actions that remain. The UUID bit positions stay unchanged and the old service bits remain reserved identity input for the existing deterministic town-name shuffle only.

**Tech Stack:** C#/.NET Domain, GameContent, Application and API; React/TypeScript; xUnit, Vitest/Testing Library, PostgreSQL-backed API integration tests; repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), [PG-003 world map](../../docs/features.md#pg-003-consult-one-world-map-and-navigate-towns), [PG-005 public investigation knowledge](../../docs/features.md#pg-005-gather-public-investigation-knowledge), and [culprit identity/release contract](../investigations/stable-0.1.0/2026-10-07-culprit-identity-and-release-contract.md).

**Execution Strategy:** Native `executing-plans` inline. Town layout, seed bits, event snapshots, domain legality, API actions and browser rendering all express one service-presence/usability boundary; sequential changes need one integrated behavior context and one fresh whole-branch review.

## Global Constraints

- Start from merged `origin/develop` commit `d6d289607f3a3f95cbc3a84fe48aa45d86ca47db` and deliver by PR to `develop`.
- Advance `Directory.Build.props` once from `0.1.0-dev.13` to `0.1.0-dev.14` in this slice; derived web identity remains build-generated.
- Every generated town visibly contains a Saloon, Sheriff, Store and Telegraph. The Trailhead remains navigation infrastructure. Saloon, Sheriff and Store actions remain usable; Telegraph remains visible but has no available action or API command.
- Remove per-town service-presence modeling from generated world facts and remove Town's custom `SourceCatalog` injection. Keep the common retained source definitions and their visit-refresh behavior.
- Remove TelegraphLead as an active source, the `SendTelegram`/`FollowTelegraphLeads` player command path, its API route and client affordance, and its two redundant generated gang clues. The opening culprit clue belongs to the Prologue. Preserve identity facts available through the prologue and retained public notices/posters.
- Keep UUID bit positions 21-23 unchanged. Rename their internal representation as reserved legacy town-name derivation bits and preserve their existing contribution to town IDs and names. They are not a service-availability input: no generated service set, enabled action or clue-selection rule may inspect them. Derived text/location facts may still reflect the town identity selected by the complete seed. Do not advertise or expose the bits as service variability, and do not introduce a new seed field or codec layout.
- The Town layout is the presence representation. Remove duplicate `services` fields from Domain snapshots, Application/API map/session DTOs and browser types where layout or available actions already tell the truthful story. The field report must not display stale service flags.
- Preserve town identities, coordinates, trail topology, prosperity differences, layout determinism, current retained source metadata and per-visit refresh behavior. Do not consolidate LocalRecords with NoticeBoard in this slice; row 11 owns that separate player-surface change.
- No old playthrough history needs retention. Do not add event compatibility or schema migrations for removed town-service facts; preserve migration history and the normal forward-migration strategy for future schema changes.
- Use behavior-first tests and a witnessed RED for generated layout/action and clue provenance. Do not add source-text, enum-count, heading, or file-absence tests.
- Consult ADR-0008 for visit-scoped source refresh and ADR-0021 for the UUID seed boundary. Neither decision requires an amendment: ADR-0008's refresh rule survives, and ADR-0021 already states that the codec bit layout is not a stable contract. Record why no ADR changes in the PR.

## Review Focus

- Every generated town layout contains exactly one each of Saloon, Sheriff, Store and Telegraph, regardless of reserved legacy bit value; the same complete seed still produces identical town IDs, coordinates, layouts and prosperity.
- Telegraph is visible but never actionable. Direct HTTP calls to the removed lead route cannot reveal clues or append events; retained saloon/sheriff/source actions remain usable.
- The opening culprit clue is sourced to Prologue, neither of the retired TelegraphLead gang clues is generated, and the retained public identity information remains available before culprit release.
- Removing `Town.SourceCatalog` does not reset or duplicate visit-scoped source refresh behavior for the common retained sources.
- No current API or browser view claims service availability through the removed per-town flags, and generated town layouts retain the four requested service buildings.

---

### Task 1: Bootstrap this JIT slice and retire its completed predecessor

**Files:** Create this plan; update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; update `Directory.Build.props`; delete `.agents/plans/2026-10-08-consolidate-town-store.md`.

- [x] Record PR #198 as merged to `develop` at `d6d289607f3a3f95cbc3a84fe48aa45d86ca47db`, source head `40aac8f52b9881e1a8eb7db10eebb20c645df968`, and hosted canonical gate run 37838821456.
- [x] Classify the full one-store consolidation scope as shipped from PR #198; retain its durable feature and implementation knowledge in `docs/features.md` and the dated store/inventory investigation outcome, then retire the completed plan and stale roadmap link.
- [x] Update roadmap row 05 to link this plan, record `0.1.0-dev.14`, add PR #198 delivery evidence, and keep row 05 executing for remaining accepted exclusions.
- [x] Advance `Directory.Build.props` exactly once from `0.1.0-dev.13` to `0.1.0-dev.14`.
- [x] Stage only this plan, roadmap, version and predecessor retirement; inspect the staged diff and commit through the check-only hook before implementation.
- [x] After the plan commit creates its SHA, record that exact plan-commit SHA in roadmap row 05 and commit the one-row correction before touching implementation code; do not bump the development version again.

### Task 2: Make town-service presence uniform in seed generation and Domain state

**Files:** `src/WildBunch.Domain/World/WorldModels.cs`; `src/WildBunch.Domain/World/WorldSnapshot.cs`; `src/WildBunch.Domain/World/BuildingKind.cs`; `src/WildBunch.Domain/World/TownSourceModels.cs`; `src/WildBunch.Domain/Game/TownAggregate.cs`; `src/WildBunch.Domain/Game/TownVisitState.cs`; `src/WildBunch.Domain/Game/TownSourceVisitState.cs`; `src/WildBunch.Domain/Actions/ActionAvailabilityResolver.cs`; `src/WildBunch.Domain/Actions/InvestigationSources.cs`; `src/WildBunch.GameContent/NewGame/SeedWorld.cs`; `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`; `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs`; `src/WildBunch.GameContent/NewGame/MapGenerator.cs`; `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`; `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`; `src/WildBunch.Application/Games/Models/GameDtos.cs`; `src/WildBunch.Application/Games/Models/StartingTownDto.cs`; `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs`; `src/WildBunch.Application/Games/Queries/GetStartingTownMapHandler.cs`; `src/WildBunch.Application/Games/Queries/GetStartingTownsHandler.cs`; `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`; `src/WildBunch.Web/src/api/types.ts`; `src/WildBunch.Web/src/components/FieldReportPanel.tsx`; `src/WildBunch.Web/src/ui/formatters.ts`.

- [x] First add a GameContent behavior test that sweeps all eight values of the existing three service bits and verifies each generated town layout contains one Saloon, one Sheriff, one Store and one Telegraph, while deterministic town identity, geometry, prosperity and repeated layout output remain equal for the same seed. Witness failure against the current palette-driven layout.
- [x] Remove `Town.Services`, `TownServices`, `ServicesPalette`, the `TownSourceCatalog` constructor/property from `Town`, and per-town source-definition overrides. Keep one common source catalogue with metadata for the retained actions; remove the no-saloon custom-catalog fixture and its test-only `CreateWithNoSaloon` helper.
- [x] Remove the town-service field from `TownSnapshot`, map DTOs and game-session DTOs. Let the persisted `TownLayout` carry building presence and `AvailableActions` carry usability. Do not add a database migration or old-playthrough upcaster for this removed fact.
- [x] Keep UUID positions 21-23 and preserve the exact current seed-to-town-name shuffle using a clearly named reserved legacy input. Do not resolve or validate a service palette. Prove these bits no longer select services, layouts or actions; preserve only their existing town-ID/name derivation and facts naturally labeled with those town identities.
- [x] Remove `ServicesPalettes`; make the generated town model service-independent; make `TownLayoutGenerator` always emit the four service buildings plus its existing Trailheads; preserve prosperity, deterministic layouts and existing town IDs for a given seed.
- [x] Make the common retained investigation definitions independent of per-town configuration. Preserve source metadata, `TownVisitState` history and per-visit refresh, and remove every custom source-catalog injection route.
- [x] Update affected Domain, GameContent, Application and Integration test fixtures that construct `Town` with old service flags. Replace old service-variation assertions with generated-world behavior; do not retain tests that protect a no-service town shape.
- [x] Run focused Domain, GameContent and Application suites and compare world snapshots and seed-codec round trips for the revised shape.

### Task 3: Retire the disabled Telegraph action and generated gang clues

**Files:** `src/WildBunch.Domain/Actions/AvailableActionKind.cs`; `src/WildBunch.Domain/Cases/InvestigationSourceKind.cs`; `src/WildBunch.Domain/Game/TownActionContext.cs`; `src/WildBunch.Domain/Game/GameSession.cs`; `src/WildBunch.Domain/Game/InvestigationLoop.cs`; `src/WildBunch.Domain/Game/BeatNarration.cs`; `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`; `src/WildBunch.Api/DependencyInjection.cs`; `src/WildBunch.Api/Games/InvestigationEndpoints.cs`; `src/WildBunch.Application/Games/Commands/FollowTelegraphLeadsCommand.cs`; `src/WildBunch.Application/Games/Commands/FollowTelegraphLeadsHandler.cs`; `src/WildBunch.Application/Projections/FullAuditProjector.cs`; `src/WildBunch.Web/src/api/wildBunchApi.ts`; `src/WildBunch.Web/src/components/AvailableActionsPanel.tsx`; `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`; `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`; `src/WildBunch.Web/src/utils/actionTypePredicates.ts`.

- [x] Add failing tests for the retained behavior: `ActionAvailabilityResolver` never returns a Telegraph action for generated towns; the direct lead HTTP route is not mapped; the Prologue lead carries `Prologue` provenance; generated `KnownClues` and `PublicClues` contain no TelegraphLead entries; and public wanted information still carries the required suspect identities.
- [x] Remove `SendTelegram`, `FollowTelegraphLeads`, the Application command/handler, endpoint, DI registration, web request/mutation/button/predicate and telegraph-specific action-context/narration path. Keep the rendered Telegraph building locked and noninteractive through the existing available-action contract.
- [x] Add a `Prologue` investigation source identity without reusing the retired TelegraphLead numeric value; set the opening culprit clue's provenance to Prologue.
- [x] Remove the two TelegraphLead gang clues from `SeedCaseBuilder` rather than redirecting them or leaving them in an unreachable clue pool. Preserve the opening culprit lead and required public identity via existing prologue/poster sources; do not add automatic deduction or new clue content.
- [x] Retire the source catalog's conditional TelegraphLead entry while preserving the four retained source definitions and their refresh policies. Remove the corresponding telegraph surfacing/action tests and replace them only with the behavior tests above.
- [x] Run focused Domain event/action, GameContent generated-case, API integration and web interaction suites; verify the failed route makes no event or state change and the locked building remains visible.

### Task 4: Reconcile feature truth, validate and deliver

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-culprit-identity-and-release-contract.md`; all implementation and test paths from Tasks 2-3.

- [x] Update PG-003 to state that every town has the same visible core services, TownLayout is the presence authority, and actions determine usability; correct its dependencies so they no longer claim service-presence variation.
- [x] Update PG-005 evidence and disposition to state that the Prologue owns the opening lead, Telegraph remains visible but disabled, and its two redundant gang clues and active command path are retired. Preserve the still-pending noticeboard/sheriff-record surface work for row 11.
- [x] Add a dated implementation outcome to the culprit identity/release contract, preserving its original findings and recording the source/provenance change and retained identity proof.
- [x] Compare the diff with ADR-0008 and ADR-0021 via the decision-record playbook. No ADR changes unless live behavior contradicts a selected decision; record the actual decision boundary in the PR.
- [x] Run focused tests, `py -3 tools/run.py ci --check`, and a rendered browser check of the town hub showing all four services with Telegraph locked. Verify event replay for a generated world, source refresh after town return, no service flags in active contracts, and no change to migrations.
- [x] Review the whole branch against this plan, specification, feature matrix, backend and play-surface unslop profiles, and code-review runbook. Independent reviewer dispatch is unavailable under current runtime policy; disclose the documented self-review fallback in the PR.
- [ ] Open and attach a PR targeting `develop`; verify its source head, successful hosted canonical gate and clean merged state before ending this slice. Keep this plan until the next successor slice records its delivery and retires it.

**Ruling:** Keep the original service-bit positions as reserved legacy input to the existing town-name shuffle so identical seed UUIDs continue to produce identical town IDs. Remove their service semantics completely: no town service set, layout selection or available action reads them. Clue subject selection remains unchanged; location wording may naturally vary with the town identity derived from the complete seed. This preserves town identity and the UUID layout while stopping the bits from advertising or producing service variation. Removing their historical name-shuffle contribution later would be a deliberate seed-output change requiring its own product decision.
