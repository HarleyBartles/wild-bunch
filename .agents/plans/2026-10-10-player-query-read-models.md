# Route world, store and travel queries through the read model

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the world-map, store-offer and travel-preview Application queries onto the existing query-only session read repository while preserving their current output and failure behavior.

**Architecture:** Each handler will depend on `IGameSessionReadRepository` and derive its existing response from `GameSessionReadModel`. Persistence, routes, DTOs, gameplay rules and stored event/cache behavior remain unchanged; `GetAvailableActionsHandler` stays out because its resolver consumes live town action state not represented by this bounded query migration.

**Tech Stack:** C#/.NET 10, Application query handlers, the existing PostgreSQL read-store loader, xUnit integration and behavior tests.

**Spec:** [Stable 0.1.0 baseline, section 08](../specs/2026-10-07-stable-0.1.0-baseline.md#08-strict-queries-and-truthful-output)

**Execution Strategy:** `executing-plans` - the three handlers consume the same existing read-model port and share one behavior/validation boundary; keeping the small, sequential changes inline avoids artificial PR boundaries and retains one whole-branch review.

## Global Constraints

- `Directory.Build.props` remains the only authored application version source; this PR uses `0.1.0-dev.64`.
- Queries return read models and cannot mutate game state or append events.
- Preserve the current route, response DTOs, status behavior, and town/catalog/travel rules.
- Do not add event types, schema changes, migrations, new repositories, or frontend behavior.
- Every ordinary epic PR targets `develop`, uses a fresh worktree, advances one development version, and retires completed predecessor plans in its first substantive commit.
- Use the existing fail-fast command bus gate; never bypass or mutate through pre-commit.

## Review Focus

- An absent session remains a not-found result for each query; retain and extend current missing-session coverage where a query has no behavior assertion.
- A setup-phase travel preview remains a successful HTTP response carrying `Success=false` and the current explanation; retain the existing PostgreSQL route test.
- Store offers remain available for a non-current town and retain that town's prosperity-based stock; retain both integration behaviors.
- Travel preview continues to use the session difficulty and inventory, while a query leaves the event stream, inventory, and turn unchanged.

---

### Task 1: Move world-map and store-offer reads to the query port

**Files:**
- Modify: `src/WildBunch.Application/Games/Queries/GetWorldMapHandler.cs`
- Modify: `src/WildBunch.Application/Games/Queries/GetTownStoreOffersHandler.cs`
- Test: `tests/WildBunch.Application.Tests/Handlers/GetWorldMapHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Handlers/GetTownStoreOffersHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Guardrails/QueryHandlersAreReadOnlyTests.cs`
- Preserve: `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs`
- Preserve: `tests/WildBunch.Integration.Tests/GameApiStoreOffersTests.cs`

**Interfaces:**
- Consumes: `IGameSessionReadRepository.GetByIdAsync(GameSessionId, CancellationToken)` returning `GameSessionReadModel?`.
- Produces: The same `WorldMapDto` and `TownStoreOffersDto` contracts and the same missing-session/town exceptions as today.

- [ ] Update the two handlers to load `GameSessionReadModel` only. For a null model, throw `GameSessionNotFoundException` with the requested id. Build the world map from its `World`; find store towns in that same world and resolve the same catalog with `TownStoreCatalogResolver`.
- [ ] Extend the read-only behavior test to call both queries and assert no store/commit, no clock-turn change, and no event-history change.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GetWorldMapHandlerTests|FullyQualifiedName~GetTownStoreOffersHandlerTests|FullyQualifiedName~QueryHandlersAreReadOnlyTests"` and confirm map facts, absent sessions, catalog stock, prosperity, non-current towns, unknown towns, and no-write behavior.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~WorldMapEndpointTests|FullyQualifiedName~GameApiStoreOffersTests"` against PostgreSQL; preserve serialized safety and HTTP results.

### Task 2: Move travel preview to the query port

**Files:**
- Modify: `src/WildBunch.Application/Games/Queries/PreviewTravelHandler.cs`
- Modify: `tests/WildBunch.Application.Tests/Handlers/PreviewTravelHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/Guardrails/QueryHandlersAreReadOnlyTests.cs`
- Preserve: `tests/WildBunch.Integration.Tests/SetupPhaseGuardTests.cs`
- Preserve: `tests/WildBunch.Integration.Tests/GameApiTests.cs`

**Interfaces:**
- Consumes: `IGameSessionReadRepository` and the existing `TravelResolver`.
- Produces: The same `TravelPreviewResultDto`; missing sessions still throw `GameSessionNotFoundException` for the API's existing 404 mapping.

- [ ] Change the handler to read `GameSessionReadModel`; map `StartFlowPhase < StartFlowPhase.GameStarted` to the existing setup-phase failure DTO. For a started session, pass its `World`, `Player.CurrentTownId`, `Player.Inventory`, and `TravelRulesProfile.For(GameDifficulty)` to the existing resolver, then map the result exactly as before.
- [ ] Keep the existing mounted-travel behavior test and add the absent-session handler behavior test, which is missing today. Do not duplicate the route-level setup-phase behavior already covered by PostgreSQL integration.
- [ ] Prove the query leaves the read model's inventory, clock turn, and event history unchanged, and run the focused preview tests.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~PreviewTravelHandlerTests|FullyQualifiedName~QueryHandlersAreReadOnlyTests"` and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~SetupPhaseGuardTests|FullyQualifiedName~GameApiTests"`.

### Task 3: Verify durable boundaries and deliver

**Files:**
- Review: `docs/decisions/ADR-0014-ddd-onion-boundaries-cqrs-and-unit-of-work.md`, `docs/decisions/ADR-0015-use-aspnet-core-minimal-apis-as-the-game-http-boundary.md`, and `docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`.
- Review: `docs/features.md` through `.agents/playbooks/feature-matrix.md`.
- Record: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` after delivery.

**Interfaces:**
- Consumes: The completed query handlers, behavior tests, and existing PostgreSQL API tests.
- Produces: A reviewed `.64` PR to `develop` with exact-head and develop-push hosted gates passing.

- [ ] Confirm DI already resolves `IGameSessionReadRepository`; do not add redundant registrations or alter Persistence unless inspection proves a missing implementation.
- [ ] Confirm no durable decision or feature promise changed; record the no-change rationale in the PR. No ADR or feature-matrix edit is expected for a bounded query-port migration that preserves behavior.
- [ ] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`; run focused Application and PostgreSQL integration tests, then `py -3 tools/run.py ci --check` on the final committed tree.
- [ ] Obtain an independent whole-branch review. Resolve any actionable finding with a focused behavior proof, rerun affected checks, and do not publish a changed head until local validation passes.
- [ ] Publish a PR targeting `develop`, verify its exact source head and hosted gate, merge after the gate passes, and verify the develop-push gate.
- [ ] In the successor plan, record the source head, merge commit and both hosted run IDs in row 08; retain this plan through its completing PR.

## Acceptance

- All three handlers depend on the read-only session query port and no longer load a command aggregate.
- Existing HTTP routes, response DTOs, status behavior, generated-world facts, prosperity stock and difficulty/inventory-based travel previews remain unchanged.
- Missing-session and invalid-town behavior remains correctly mapped; a setup-phase travel preview remains a failure DTO rather than a command conflict.
- Repeated reads do not append events, change state, consume resources or advance the clock.
- Relevant Application and PostgreSQL integration behavior tests pass, no migration is generated, and the complete local and hosted gates pass on the reviewed PR head and its develop merge.
- ADR-0014, ADR-0015 and ADR-0028 remain truthful; feature dispositions remain unchanged.

## Explicit Exclusions

Do not migrate `GetAvailableActionsHandler`; its action resolver depends on live town action state and needs a separate design if a query-safe boundary is warranted. Do not redesign `GameSessionReadModel`, add a specialized per-query repository, change the HUD event-read port, alter the event/cache recovery path, revise travel rules, change API or browser contracts, or broaden this into a general read-model rewrite.
