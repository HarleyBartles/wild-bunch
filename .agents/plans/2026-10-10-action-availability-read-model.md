# Read action availability from the session query model

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the remaining player action-availability query consume the existing query-only session read repository and correct its stale action list while a completed journey awaits arrival acknowledgement.

**Architecture:** `ActionAvailabilityResolver` remains a Domain rule but receives an explicit immutable `ActionAvailabilityContext` containing the start-flow phase, world, current town ID, and optional `TravelJourneySnapshot`. `GetAvailableActionsHandler` reads `IGameSessionReadRepository`, maps the current facts into that context, and returns the same DTOs and missing-session behavior. Source inspection confirms the resolver reads no `TownVisitState`; town-source actions come from the shared static `TownSourceCatalog`. A completed journey remains on the session until the player acknowledges arrival, so its action list must not advertise another travel-day advance; acknowledgement is already a separate command surfaced from the journey status.

**Tech Stack:** C#/.NET 10, Domain action resolver, Application query handler, existing PostgreSQL read-store loader, xUnit and the repository command bus.

**Spec:** [Stable 0.1.0 baseline, section 08](../specs/2026-10-07-stable-0.1.0-baseline.md#architecture-and-state-invariants)

**Execution Strategy:** `executing-plans` - the context contract, resolver, handler, and existing action behavior tests are one tightly coupled query-boundary migration; inline execution preserves the full behavior thread and one independent whole-branch review without artificial PRs.

## Global Constraints

- `Directory.Build.props` remains the only authored application version source; this PR uses `0.1.0-dev.65`.
- Queries use read ports and cannot mutate game state, emit events, or persist gameplay changes.
- Preserve the action set and order for started towns, the no-outgoing-trail case, active travel, and pending encounters; a completed journey awaiting acknowledgement exposes no further action-list travel step.
- Before `StartFlowPhase.GameStarted`, action availability remains empty; a missing session remains `GameSessionNotFoundException` and the API's existing 404 mapping.
- Do not add repositories, persistence schemas, events, API/DTO changes, frontend changes, town-service variation, or a general read-model redesign.
- Each ordinary epic PR targets `develop`, uses a fresh worktree, advances one development version, and retires the completed predecessor plan in its first substantive commit.
- Use the existing fail-fast command-bus gate and normal check-only commit hook; do not bypass the hook or run the full gate immediately before a hooked commit.

## Review Focus

- Setup facts with no selected town still produce an empty action list.
- A started town without outgoing trails does not advertise Travel.
- An active journey suppresses town-only actions and offers AdvanceTravelDay.
- A pending encounter offers ResolveTravelEncounter instead of AdvanceTravelDay.
- A completed journey awaiting acknowledgement does not offer AdvanceTravelDay; the existing acknowledgement command succeeds, then town actions return.
- Repeated action reads do not store, commit, append events, or advance the clock.

---

### Task 1: Retire the completed read-query plan and commit this JIT plan

**Files:**
- Create: `.agents/plans/2026-10-10-action-availability-read-model.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Retire: `.agents/plans/2026-10-10-player-query-read-models.md`

**Interfaces:**
- Consumes: merged PR #250 source `acad81d73e56580a4697c409915c1eecb63c7959`, merge `8659a49a7029f50acee95a7d47b44deb0b6ab2ae`, exact-head gate `38061470049`, and develop-push gate `38061774018`.
- Produces: a committed `.65` plan and roadmap checkpoint from `develop` at `8659a49a7029f50acee95a7d47b44deb0b6ab2ae`, with the completed `.64` plan retired in this first substantive commit.

- [ ] Record PR #250 and its exact source, merge, and hosted gate evidence in row 08; advance the roadmap pointer from `.64` to this `.65` plan and state that the remaining player query is action availability.
- [ ] Remove the fully delivered `.64` plan, set the sole authored version in `Directory.Build.props` to `0.1.0-dev.65`, and commit the plan, roadmap update, version, and predecessor retirement together before source changes.

### Task 2: Move the Domain resolver from aggregate input to explicit read facts

**Files:**
- Create: `src/WildBunch.Domain/Actions/ActionAvailabilityContext.cs`
- Modify: `src/WildBunch.Domain/Actions/ActionAvailabilityResolver.cs`
- Test: `tests/WildBunch.Domain.Tests/ActionAvailabilityResolverTests.cs`

**Interfaces:**
- Consumes: `StartFlowPhase`, `World`, nullable `TownId`, and nullable `TravelJourneySnapshot` from current Domain types.
- Produces: `ActionAvailabilityContext(StartFlowPhase StartFlowPhase, World World, TownId? CurrentTownId, TravelJourneySnapshot? Journey)` and `ActionAvailabilityResolver.Resolve(ActionAvailabilityContext context)` returning the existing `IReadOnlyList<AvailableAction>`.

- [ ] Add one full quiet-journey behavior test using `TravelTestFactory.CreateSixDayQuietJourney()`: reach `JourneyStatus.Completed` through `StartJourney` and repeated `AdvanceJourneyDay`, then assert the current resolver wrongly includes `AdvanceTravelDay`; run the focused suite and confirm this new assertion fails for that action. Do not manually mark the journey completed or add an acknowledgement enum.
- [ ] After recording the behavior RED, refactor the existing resolver tests to construct the explicit context from their started `GameSession` fixtures, preserving their independent action assertions for the canonical town list, no outgoing trail, active journey, and pending encounter. Keep the new completion test's natural journey/acknowledgement lifecycle intact while passing its `TravelJourneySnapshot` to the resolver.
- [ ] Add a setup-phase behavior test showing any phase before `GameStarted` returns no actions without requiring a current town; then run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~ActionAvailabilityResolverTests"` after the resolver change.
- [ ] Change the resolver to use only the context, the shared `TownSourceCatalog.Default`, and `World.ListTrailsFromTown`; preserve the exact current branch and output order. For `JourneyStatus.Completed`, return no travel action while arrival awaits acknowledgement; after the existing acknowledgement clears the journey, normal town actions are available. Do not read or introduce `TownVisitState`.
- [ ] Re-run the focused Domain tests and falsify the setup case by temporarily changing the phase boundary so it returns actions before `GameStarted`; confirm the setup test fails for the unexpected action result. Separately falsify the completed-journey case by temporarily treating a completed journey as advanceable; confirm its `AdvanceTravelDay` assertion fails, restore the correct branch, and rerun the suite.

### Task 3: Route the Application query through the read-only repository

**Files:**
- Modify: `src/WildBunch.Application/Games/Queries/GetAvailableActionsHandler.cs`
- Modify: `tests/WildBunch.Application.Tests/Handlers/GetAvailableActionsHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/Guardrails/QueryHandlersAreReadOnlyTests.cs`
- Preserve: `tests/WildBunch.Integration.Tests/GameApiActionsTests.cs`

**Interfaces:**
- Consumes: `IGameSessionReadRepository.GetByIdAsync(GameSessionId, CancellationToken)`, the new `ActionAvailabilityContext`, and the existing `ActionAvailabilityResolver`.
- Produces: the same `IReadOnlyList<AvailableActionDto>` contract and the same not-found exception used by the current API mapping.

- [ ] Add an Application handler behavior test for a pre-`GameStarted` read model returning no actions, and one active-journey query test proving the snapshot reaches the resolver and town-only actions are absent. Keep the existing missing-session test and exercise the constructor through a statically typed `IGameSessionReadRepository` reference.
- [ ] Change the handler to call the read repository, throw `GameSessionNotFoundException` for a null model, and build the context from `StartFlowPhase`, `World`, `Player.CurrentTownId`, and `Journey`; do not load `GameSession` through the aggregate repository.
- [ ] Keep the action query in `QueryHandlersAreReadOnlyTests` and type its dependency as `IGameSessionReadRepository`; prove store/commit calls remain zero and the turn and event-derived journal count remain unchanged.
- [ ] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GetAvailableActionsHandlerTests|FullyQualifiedName~QueryHandlersAreReadOnlyTests"` and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~GameApiActionsTests"` after `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`; retain the existing endpoint proof for a created game and a missing game.
- [ ] Falsify the Application travel test by temporarily passing `Journey: null` when building the context; confirm it fails because Travel or town-only actions appear, restore the correct mapping, and rerun focused tests.

### Task 4: Reconcile decisions and deliver the completed row 08 slice

**Files:**
- Review: `docs/decisions/ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md`, `docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`, and `docs/features.md` through `.agents/playbooks/feature-matrix.md`.
- Update: `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md` with a dated disposition of the completed-journey availability finding.
- Record: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` in the next successor plan, after merge evidence exists.

**Interfaces:**
- Consumes: the migrated action handler, explicit Domain resolver context, existing persistence read store, and behavior/API test evidence.
- Produces: an independently reviewed `.65` PR to `develop` and a completed row 08 with the PR source, merge, and both hosted gate run IDs recorded in its successor.

- [ ] Confirm that started-town and active-journey actions remain unchanged, the completed-journey action mismatch is corrected under the existing travel lifecycle, the implementation now satisfies live strict CQRS, and no feature promise or dependency changed; no ADR or feature-matrix edit is required.
- [ ] Add a dated disposition to the Domain test follow-up stating that the `.65` action contract reaches completion through normal travel, excludes AdvanceTravelDay until acknowledgement, proves acknowledgement succeeds, and verifies return to town actions; retain the historical finding rather than deleting it.
- [ ] Confirm Persistence already registers `IGameSessionReadRepository`; make no redundant DI registration or persistence/schema edits.
- [ ] Run focused Domain, Application, and PostgreSQL API tests, then commit source changes through the canonical check-only hook. Obtain an independent whole-branch review; resolve actionable findings with focused behavior proof and rerun affected tests.
- [ ] Publish the reviewed PR targeting `develop`, verify hosted CI passes on the exact source SHA, merge it, and verify the develop-push CI run passes on the merge SHA. Keep this plan in-tree through its completing PR; its successor records evidence, marks row 08 complete if inspection confirms no other player query uses the aggregate repository, and retires this plan in the successor's first substantive commit.

## Acceptance

- `GetAvailableActionsHandler` depends on `IGameSessionReadRepository`, and the Domain resolver consumes only explicit query facts rather than `GameSession`.
- All existing action results and ordering remain unchanged for started towns, no outgoing trails, active journeys, and pending encounters; setup remains empty, missing sessions remain 404 at the API, and a completed journey does not advertise another advance before its valid arrival acknowledgement.
- Action availability queries do not store, commit, append events, consume resources, or advance the clock.
- Focused Domain and Application behavior tests, PostgreSQL API tests, the exact-head hosted gate, and the develop-push hosted gate pass; no migration is generated.

## Explicit Exclusions

Do not migrate developer-only context or audit queries in this player action slice; they have separate audience contracts and are not the remaining player action query. Do not change `GameSessionReadModel`, event/read-store recovery, action labels, route behavior, shared town-source definitions, travel rules, API DTOs, or frontend state. If implementation reveals a required fact absent from the existing read model, stop and revise the plan before adding a persistence field or repository.
