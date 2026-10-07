# Test quality investigation for stable 0.1.0

**Status:** Full source audit recorded; findings and remediation scope await discussion. Findings authorize no test or product remediation. This investigation contributes F-008 to the [stable baseline tracker](2026-10-06-stable-0.1.0-investigation.md).

**Source follow-up:** [Application and structural test remediation](2026-10-07-application-test-followup.md) connects the subsequent source findings to exact current assertions and independent behavior scenarios. It distinguishes bad expectations from missing proof and records what to retain, rewrite, strengthen or retire without automatically adding a regression for every fix.

**Scope:** All 234 tracked files under `tests/`, including 209 `*Tests.cs` files and their helpers/configuration; all 42 frontend test files including colocated renderer tests; and all 10 Python test files under `scripts/tests/`. Every selected file was read in full. Supporting source and related tests were followed to distinguish missing behavior from behavior covered elsewhere. The .NET dispositions reconcile exactly with the tracked inventory; the frontend/Python tables account for all 52 files. Repository-wide discovery found no additional tracked test/spec files outside these homes.

**Assessment basis:** Tautologies and change detectors are harmful. Expected failure and reachability tests are valuable. Consolidate TDD sprawl around observable scenarios, preserving distinct failure cases and independently meaningful boundaries. A small test, repeated setup, source inspection, replay comparison or deterministic seed is not automatically defective. A serialization contract or an independently owned validator can legitimately constrain structure. A test title does not prove its claim.

No application or test changes have been made for this audit, and no test suite has been run as part of this source assessment. Findings describe test design and source evidence, not current passing results. Existing retained changes and earlier audit documents remain separate.

The main problem is overstated confidence: initializer echoes, source/IL spelling detectors, empty or nonadversarial negatives, conditional assertions that avoid the named branch, mutable replay baselines, and duplicated happy-path fragments. Useful event replay, real PostgreSQL boundaries, deterministic generation, serialization compatibility and meaningful negative tests already exist and should survive consolidation.

**Proposed baseline priority:** First repair event/replay authority and false reached-state proof, including the dev override replay question and retry isolation. Then remove misleading/obsolete proof, fill known rejection/recovery gaps and consolidate repeated successful scenarios at their owners. Integrate tooling ownership with F-006/F-007. The goal is truthful behavioral confidence and maintainable ownership, not a target test count or a claim that pre-alpha gameplay is complete.

## Frontend findings

### W-01: An object literal is tested against its own assignments

`src/WildBunch.Web/src/tests/types.test.ts`, `PathSegmentDto / stores coordinates`, constructs a TypeScript interface-typed object literal and reads its four assigned values back. No production operation runs, and the interface is erased at runtime. Remove this test. Type checking supplies the relevant type proof. The `TownProsperity` numeric encoding assertions are a separate potential protocol contract; assess against the backend wire contract before retaining or replacing them, rather than treating enum stability as inherently slop.

### W-02: Router tests freeze source spelling and can prove unrelated code

`routingConventions.test.ts` searches for `const <component> = lazy(`, named `rootRoute` parents and any `return {};` anywhere in the source file. A spelling or route-composition refactor can fail without changing behavior; an unrelated empty-object return can satisfy the search-validation assertion while the actual route is wrong. Replace these with route reachability, rendering and search behavior. If deferred loading is an owned performance requirement, prove that boundary independently. Retain the router-factory isolation intent, but distinguish different object identities from isolation of navigation state.

### W-03: Historic deletion assertions and styling rules are mixed together

`stylingEnforcement.test.ts` asserts that old stylesheet files and references remain absent. Retire those historic deletion detectors. Its CSS-import, class and inline-style scans express a potentially legitimate contributor policy, but regex helpers embedded only in a test are not a dependable policy validator and do not cover every equivalent syntax. Reconcile the styling authority identified in the documentation audit, then retain an owned validator only where the policy remains intentional, with valid and invalid examples. Do not simply change the forbidden filenames to the latest layout.

### W-04: Renderer-property absence does not establish the network boundary

`PhaserMapHost.test.tsx`, `PhaserTownHubHost.test.tsx` and `TownHubScene.test.ts` check absent properties such as `api`, `requestJson`, `fetch` and `getGame`. A module can import or call networking without attaching those names to a scene. These assertions do not prove the advertised boundary. Remove them while preserving meaningful fetch observation, callback forwarding, allowed-selection rejection and host lifecycle tests. Where an architectural import boundary is independently owned, enforce the actual dependency boundary through its owner.

`TownHubScene.test.ts`, the scene-key smoke test, checks an imported mocked constructor and constructed type while the mocked `Scene` ignores its key. It never establishes the named key. Remove the misleading smoke proof or test an actual consumer requirement if that key is a contract.

### W-05: A negative matcher permits most forbidden background placements

`components/town-hub/background-building-planner.test.ts`, `excludes the immediate east and west slots beside both trailheads`, negates `arrayContaining` of eight forbidden positions. It passes if just one forbidden position is absent, even when the other seven are present. Require absence of each forbidden slot and include eligible positive controls so an empty result cannot masquerade as correct placement.

The boomtown test claims one or two empty eligible spaces but permits every space to be filled. Reconcile that claim with the intended prosperity rule and tighten the independent bound accordingly. The seed sweep's `observedCounts.size > 0` is guaranteed by adding a value on every iteration; remove that assertion while preserving the useful budget sweep. A nonnegative array length is also redundant, while an independently specified upper bound remains meaningful.

### W-06: Hidden-truth checks use fixtures without the alleged secret

`JournalSurface.test.tsx` asserts absence of `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds` and `killerReleaseState`, none of which is supplied by its fixture. `StorySoFarStep.test.tsx` similarly checks secret identifiers and an unresolved placeholder against already-resolved public copy. These tests cannot detect the advertised filtering or content-resolution failure. Retain actual public rendering and omission checks for fields present in the fixtures, such as the journal town ID and prologue variant metadata. Put hidden-data filtering and template-resolution proof at their real backend boundaries, or supply adversarial metadata where frontend omission is its responsibility. Do not invent a frontend obligation to scrub arbitrary server-authored prose.

### W-07: Starting-town tests do not prove fetched towns or terminal empty state

`StartingTownStep.test.tsx`, `renders towns fetched from the backend` and `renders the Phaser map host`, perform the same host-role assertion. They do not establish that the fetched towns are supplied or selectable. Consolidate around fetched data, actual selection forwarding and disabled selection while pending.

Its empty-response test waits for loading copy already visible before the request resolves, so it can pass without observing the terminal empty response. Source in `components/start-flow/StartingTownStep.tsx` combines loading, error and empty data into the same pending condition. There is no failed-map-fetch test. Agree useful empty/error recovery behavior and prove the settled state with controlled promises; do not encode perpetual loading as the desired failure behavior.

### W-08: Start and archive lifecycle failures are missing

`StartFlow.test.tsx` repeatedly walks the same successful setup, prologue and town flow but does not reject those mutations. `flow/PreSessionSurface.tsx` awaits setup/prologue before advancing and moves to creating before awaiting start, while `useGameSessionMutations.ts` contains explicit error handling. Add behavior scenarios for rejected setup, prologue acknowledgment and start: retain the appropriate draft/session, expose failure, avoid claiming a completed transition and allow recovery. Determine the intended recovery step for failed start before asserting it.

The test named `preserves player name draft through the full start flow` only checks the starting-town payload. Consolidate the richer existing flow's name, difficulty, entropy, session identity and transition assertions instead of retaining a title that overclaims.

`StartOverConfirmation.test.tsx`, `GameSettingsOverlay.test.tsx` and `StartOverRegression.test.tsx` exercise success, cancellation and surface reachability, but do not reject archive. The mutation owner explicitly distinguishes already-archived/409 from other errors: the former clears local session state and the latter retains it and reports failure. Cover those distinct outcomes and retry behavior; preserve the cancellation and busy-state guards.

### W-09: Dev panel tests stop before failure and state recovery

`SessionDevPanel.test.tsx`, `TravelDevPanel.test.tsx`, `SaloonDevPanel.test.tsx` and `TownLayoutDevPanel.test.tsx` mostly prove successful request forwarding. Their owners contain catch/finally error and pending-state behavior that the tests do not exercise. For representative distinct operations, prove rejected mutation, displayed error, unchanged authoritative context, restored controls and retry; combine successful dispatch with observing the refreshed context where that is the scenario contract.

The session RNG-lock assertion uses `objectContaining({})`, which proves no salt payload semantics despite source explicitly distinguishing blank from supplied salts. Add blank/nonblank behavior at the payload boundary instead of another superficial call-count test. Developer diagnostics remain a valid separate capability; this finding does not propose exposing them in production.

### W-10: Transport and travel failures are unexercised

`wildBunchApi.test.ts` tests successful preview URLs but provides no HTTP failure proof. `api/httpClient.ts` has known error extraction for title, error, validation errors, detail, plain text and fallback status, including unreadable-body handling. Add a focused transport scenario table for meaningful error propagation and fetch rejection. Decide the desired successful malformed-body behavior separately; the current cast is not runtime schema validation.

`TravelRoutesPanel.test.tsx` exercises successful previews while its component has per-preview rejection and stale-response guards. Prove one failed preview does not discard another valid route and that an old request cannot overwrite the current session. `TravelPrepSurface.test.tsx` covers presentation but does not exercise preview failure or ride dispatch. `TravelPanel.test.tsx` covers successful/pending flow and session hydration, while `hooks/useTravelPanelState.ts` exposes errors from advance, resolve, acknowledge and refresh. Cover visible failure, no false completed turn, retained server state and usable recovery. Preserve the distinct existing hydration and travel-state cases.

### W-11: Presentation trivia is sometimes mistaken for interaction proof

`SetupHuntStep.test.tsx` freezes thumb markup, transparency and nowrap styling alongside valid value-selection and accessible-label tests. Retain durable interaction and accessibility contracts; use browser evidence for visual requirements rather than asserting an incidental DOM/CSS construction. `TownHubSurface.test.tsx` verifies focus but its focus-ring title promises visual proof it cannot supply. Historic old-copy absence checks in town hub and travel prep should be replaced or retired in favor of current player interactions. Preserve actual alternative-navigation and action-selection behavior.

### W-12: Several action predicates have only positive examples

`actionTypePredicates.test.ts` supplies a mismatching action for the first two predicates, but only matching actions for local records, telegraph leads, gossip and saloon. A predicate changed to always return true would satisfy those four tests. Consolidate a small table of matching and mismatching kinds for all six predicates, tied to their routing responsibility. Do not expand this into speculative malformed-object validation. `beatNarrationHook.test.tsx` also repeats notice setup for a raw-turn-absence assertion whose fixture contains no raw turn; combine the useful composed-notice checks and retain the null-narration fallback.

## Frontend consolidation candidates

| Cluster | Consolidation | Behaviors to retain separately |
|---|---|---|
| Start-over confirmation and settings | One confirmed archive scenario can establish request, cleared storage, notice and closed overlays; one cancellation scenario can establish retained session and no request. | Pending controls, the two error outcomes and reachability from materially different player surfaces. |
| Start flow and starting town | One coherent successful flow can establish payload continuity and legal phase transitions; one loaded-town scenario can establish data, host and selection. | Pending, terminal empty/error, resumed server phase and each failed mutation. |
| Story-so-far | Loaded copy, enabled action and callback can share a scenario; pending copy, disabled action and no callback can share another. | Failure and successful retry; backend hidden-truth and content-resolution boundaries. |
| Dev panels | Each command scenario can establish payload and resulting refreshed state instead of separate caption, call and update tests with identical setup. | Distinct command semantics, mutation failure and pending lockout. |
| Phaser hosts | Combine creation/exactly-one-instance and destruction into a lifecycle scenario; combine initial selection forwarding with the same adapter scenario. | Unknown/disallowed selection, updates, unmount cancellation and distinct renderer responsibilities. |
| Completed trail surface | Heading, acknowledgment affordance and invocation can form one completed-arrival scenario. | Busy/failure and legal arrival transition at the backend boundary. |
| App shell and dev overlay | Keep a real shell-wiring scenario and focused overlay behavior rather than repeating every close interaction at both levels. | Escape/click-away handling where owned, HUD interactions and mutually exclusive overlays. |

Consolidation is not a request for giant tests. Group assertions that describe one scenario, share fixtures deliberately, and keep failures diagnostic. Repeated setup across a domain rule, HTTP contract and PostgreSQL replay boundary can be justified because each observes a different failure.

## Python/tooling findings

### P-01: Wrapper tests preserve the rejected portability policy without proving execution

`scripts/tests/test_script_entrypoints.py` requires paired language filenames, perpetuating F-007's rejected trilingual policy. `test_power_shell_wrappers.py` freezes the image wrapper's existence and runs help without asserting its captured return code or output. An immediate nonzero process failure satisfies that invocation test. Retire these obsolete detectors with the redundant wrappers and retain behavior tests at the surviving implementation owner.

### P-02: Hosted CI strings and private runner steps are weak gate proof

`test_ci_workflow.py` asserts raw YAML substrings and exact command spellings. `test_run.py` freezes internal CI-apply step names/order and the current pytest path. Distinguish independently owned command declarations from incidental implementation vectors and location assertions. Retain useful dispatcher diagnostics and mode rejection; replace stale spelling/order detectors with observable CLI dispatch, failure propagation and mechanical-only apply behavior where appropriate. `test_precommit_candidate.py` already exercises staged candidate repair and is materially stronger than a workflow string search.

Known runner boundaries deserve focused negative proof: a failing target stops the default sequence and propagates the intended exit status; diagnostics gathers independent failures; a denied shared-checkout operation executes no mutation; linked worktrees remain usable. Reconcile these requirements with F-006's selected command-bus standard before creating tests that freeze today's divergent interface.

### P-03: Freshness generation lacks stale-input rejection proof

`test_adr_freshness.py` tests generating metadata and checking the resulting document. That round trip is useful but does not independently prove that the check detects stale metadata, remains nonmutating, or that replacement preserves following content. Add authored stale-input and content-preservation cases at the freshness owner. Do not mistake the round trip itself for a tautology or require preserving an obsolete ADR-index shape.

### P-04: Local-skill assertions contain redundant discovery proof

`test_local_skill_registration.py` calls `is_file()` on paths already obtained by `glob("*/SKILL.md")`; that adds no meaningful discovery proof. The frontmatter/directory-name check is an intentional skill-custody constraint and should be assessed against its authority, not deleted merely because it reads files. Likewise, forbidden marketplace keys need an owned schema/custody rule to justify them. Do not introduce a fixed skill count merely to make an optional inventory nonempty.

### P-05: Structural-validator rejection tests are worth preserving

`test_agent_routers.py`, `test_operating_standards.py` and `test_plugin_subscriptions.py` exercise invalid routes, malformed declarations and subscription failures against actual validator boundaries. These are expected failure tests of declared contracts, not obsolete placement detectors. Preserve them while updating ownership and adopted contracts as separately agreed. No blanket deletion of source or document validation tests follows from this audit.

## .NET assessment

### N-01: Replay proof starts from the action's result

`WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.RehydrateFromEvents_Reconstructs_Investigation_State` calls gossip, then creates `CaseFileGenerated` from the now-investigated case file. The replay begins with the expected known clue already present and the public clue already removed. A no-op investigation Apply satisfies its clue assertions. Replace the reconstructed post-action fixture with actual retained setup events or an immutable pre-action snapshot, then apply the command's emitted events and compare discovery, spent-source and clock state. Preserve genuine command/replay parity elsewhere.

### N-02: Heat reset and arrival tests fail to establish their preconditions

`WildBunch.Domain.Tests/Guardrails/HeatSemanticGuardrailTests.StartingJourney_ResetsHeatToZero` heats one session and starts a different cold session. Removing the journey's heat reset does not falsify the assertion. Heat the same journey-capable session, establish positive heat, require successful start, then assert zero. Related trail/arrival tests stop after a fixed loop without requiring completion or successful acknowledgment; require the claimed transition and destination before asserting its consequences.

### N-03: Conditional assertions accept the untested branch

`WildBunch.Domain.Tests/Projections/TravelDiaryDayProjectorParityTests.Projector_InterruptedJourney_MatchesCommandPathDiaryDays` returns successfully with quiet-day parity if the fixture never interrupts. Require a deterministic interruption through the existing override seam before comparing the independent projection. Retain the parity itself.

`WildBunch.Application.Tests/Handlers/AdvanceTravelDayHandlerTests` places arrival and six-day completion assertions behind `if (JourneyStatus == Completed)`. A journey that never completes can satisfy the remaining assertions. Require the intended states and successful advances, then prove origin before acknowledgment, destination afterwards and complete diary history. Existing domain characterization already covers real arrival and quiet-day behavior; consolidate one meaningful application orchestration scenario rather than reproducing all domain tests.

### N-04: Journal equivalence tests never compare the claimed sequence

The three `WildBunch.Domain.Tests/Projections/JournalLogProjectorEquivalenceTests` scenarios assert only nonempty output and entry kinds despite promising exact command-path equality. The encounter case accepts a projector that omits the resolution entry because journey start already produces a travel entry. Replace the smoke with complete expected records/order at the owning projector contract, incorporating existing application projector coverage. Do not preserve obsolete command-side logs merely to keep a historical test name.

### N-05: Concurrency retry proof permits duplicated command effects

`WildBunch.Application.Tests/Execution/GameSessionCommandHandlerTests.ExecuteWithRetryAsync_RetriesOnConcurrencyException` reloads the same mutable object from its fake after a failed purchase store. The next attempt purchases again on already-mutated state; assertions check only result and at least two attempts. Use a fake that reconstructs fresh durable state for each load and assert one durable purchase, resource delta and event set. Exercise store and commit conflicts, retry exhaustion and failed-attempt isolation. The existing exhaustion test remains useful but also shares this aliasing flaw. Real PostgreSQL concurrency and unit orchestration are separate boundaries.

### N-06: Hidden-truth titles do not match their fixtures or operations

`WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.HandleAsync_HiddenTruthDoesNotLeakIntoPlayerDtoSerialization` only obtains a developer DTO and asserts its hidden truth is present. It never produces or serializes a player DTO. Fold that positive assertion into the rich developer-context scenario and retain actual player secrecy tests at mapper/API boundaries.

`Handlers/GetGameSessionHandlerTests.GetGameSessionProjectsOnlyExplicitlyDiscoveredSuspects` excludes a second suspect that its single-suspect factory never created. Supply two real suspects and discover one. The analogous journal test already uses a genuinely adversarial two-suspect fixture; preserve it.

### N-07: Source strings and IL opcodes detect implementation shape

`WildBunch.Application.Tests/Guardrails/ReadStoreLoaderJournalProjectionGuardrailTests` searches particular source files for projector/store strings and absence of a retired table name. Comments satisfy the positive searches and safe refactors break the paths. Remove the detectors, preserving actual PostgreSQL journal projection/read-load equivalence where needed.

`Projections/ProjectionTests.CaseFileViewProjector_PlaythroughArchived_IsHandledAndPreservesSeedView` scans compiled IL to require a specific no-op archive switch arm. Ignoring archive has identical case-view behavior. Preserve the independently owned projection-event completeness obligation where applicable, but enforce explicit event disposition at its owner and prove archive preservation with populated state. Do not freeze an opcode pattern merely to show a switch arm was added.

### N-08: Compiler and initializer smoke sprawls across event/model files

Remove literal-assignment, record-with/equality, marker-interface and enum-existence smoke in the relevant domain world/event tests and content positional-record tests. Examples include domain `World/LayoutSaltsTests`, `World/SeedWorldTests`, `World/TownLayoutLayoutSaltsTests`, `StartingTownSelectedEventTests`, parts of `Events/TypedDomainEventTests`, application `GameSessionDtoProjectionFieldsTests.GameSessionDto_AcceptsProjectionsViaWithExpression`, and content `NewGame/PaletteSpecTests` and `NewGame/ResolvedGameSetupLayoutSaltsTests`. They retest C# storage/compiler behavior without invoking a consumer. Keep payload production, serialization, snapshot reconstruction and independently owned domain/infrastructure boundary contracts. Numeric wire compatibility requires an actual compatibility consumer, not an enum member existence check.

The duplicated `TravelDiaryDayState_HasNoBeatSlotsField` reflection checks in domain mapper/rollup tests are historical property-name detectors. Remove both; preserve snapshot reconstruction and derived projection behavior.

### N-09: Salt and map tests overclaim count/non-null assertions

Application `Integration/DevEnabledActionPatternOrchestrationTests` claims injected/default layout salts are used but checks only Active and nonnull World. Prove the actual effective salt bundles and layout output after prep/inject/start. The handler spy's null-salt case is useful but does not prove a populated bundle is forwarded.

`GetStartingTownMapHandlerTests` claims selectability from a count of eight, complete trail coverage from nonempty output and correct distances from positive values. Replace with a rich explicit world-to-map mapping scenario covering identities, endpoints, coordinates and actual distances. `GetWorldMapHandlerTests` actually exercises the starting-map handler; consolidate its useful missing-session negative into the proper owner. `GetStartingTownsHandlerTests` can consolidate count/ID subsets into complete ID/name/service mapping. Catalog-based adapter comparison is meaningful where the catalog independently owns those facts.

Domain `ClueSurfacingResolverTests.SaltMode_DifferentSaltCanSelectDifferentClue` checks variation within either salt, so ignoring salt while varying town/visit still passes. Compare salts at identical input pairs and require an actual changed result. Keep determinism and exhausted/known-clue behavior.

### N-10: Nonmutation and order claims need independent values

Application `ProjectionTests.Projectors_DoNotMutateInputEvents` projects twice and compares a few output values. A projector can mutate its input once to a stable wrong value and still pass. Snapshot populated nested inputs before projection and compare afterwards, retaining repeatability as a separate claim only if useful.

Domain `DevSaltSourceTests` checks journey nonmutation on null journeys and player nonmutation through item counts/reference values. Use populated immutable before/after values, including quantities and horse/canteen state, while consolidating force/clear state and event assertions. `TravelDiaryTextRendererTests.RenderEntriesDeduplicatesExactStringsWithinADayAndPreservesOrder` establishes distinct membership but never order; strengthen the existing scenario rather than add a separate bug-shaped test.

### .NET consolidation candidates already established

| Cluster | Proposed disposition |
|---|---|
| Setup/start and event fields | Combine phase, payload, version and emitted-event facts in a rich successful scenario; preserve legal-phase rejection and rehydrate-then-start behavior. |
| Dev force/clear | Consolidate identical salt branches into theories; retain explicit normalization, populated noninterference, emitted events and consumed-once behavior. |
| Application purchase, posters, records and notice board | Fold projection/privacy output assertions into the richer successful handler case; retain rejected town/offer/journey and no-store negatives. |
| Application encounter resolution | Combine the same forced successful run's prose/frozen-day assertions; preserve run/fight/bribe failure outcomes, which can legitimately consume resources and produce events. |
| Case/world snapshots and mapping | Use populated round trips with real values, trails and links instead of many tiny field-construction/count facts. Preserve null-layout and distinct mapping boundaries. |
| Purchase beat cost and action context | Consolidate overlapping successful purchase clock/context scenarios; retain first versus same-context action behavior and real arrival-to-reset wiring. |
| Bounty, saloon and poster behavior | Remove exact duplicate forced scenarios; retain eligibility, wrong declaration, captured/retired/exhausted, capped fines and idempotent settlement. |
| Serialization registration | Fold registration-only success into the round trip that already uses registration and validates payload; preserve unknown/unregistered-event negatives. |

### N-11: Version completeness and stale diary rebuilding have weak oracles

Integration `Versioning/ProjectionVersionCompletenessTests` freezes diary version 1 and checks a hand-listed component inventory through `ProjectionVersions.ForComponent`, which ignores the name and returns one constant. A nonsense or omitted name satisfies the same check. Remove the constant detector and replace the claimed completeness proof with behavior at the supported payload-loading/rebuilding boundary.

`VersionMismatchBehaviorTests` stale/mixed diary cases use only setup events, so the expected rebuilt diary is empty. Returning empty for every stale diary passes the central count check. Use nonempty journey events and independent expected day/content while retaining mixed-version discard and malformed stored-payload rejection. Preserve current-version JSON loading, future-event rejection, real historical upcasts and the runtime chain-gap guard tests.

### N-12: Fixture readiness labels and ignored requests masquerade as preparation

Integration `TestInfrastructure/ScenarioSeedCatalog` writes literal role/service labels into its purported observed signature and compares a current codec with descriptors initialized from the same current codec. Those subchecks cannot detect role/service/codec drift. Preserve real readiness checks for resources, graph and routes; centralize the needed behavioral prerequisites instead of repeatedly validating the whole fixture in unrelated endpoint tests. `BoringScenarioBuilderTests` also tests wallet/horse/saddle under a service-readiness title and mounted travel under a foot-travel label. Align fixtures, titles and observed behavior.

Six RNG endpoint tests are permanently skipped because `DevEndpoints` actually comments out the routes. Active poster acceptance/API tests nevertheless POST lock-rng and ignore its response. That request cannot establish its claimed preparation; determinism currently comes from the test factory's fixed-salt seam. Remove dead preparation, require success for genuine required setup, and track unimplemented RNG routing explicitly. Do not simply enable dormant tests against absent endpoints.

All three `TownLayoutDevIntegrationTests` are permanently skipped, use wrong prep command field names and do not establish effective layouts. Their expected 400 for non-Prepped mutation is not mapped by the current exception handler. The expected Active status after dev start is correct; the handler's setup-complete comment is stale. Replace their promised proof with the live prep/inject/start lifecycle and observed effective layouts when that slice is authorized; supplying PostgreSQL cannot enable a permanent attribute skip.

### N-13: Secrecy and post-commit tests can pass without observing useful data

Integration `GameApiHiddenTruthTests.PublicApiResponsesDoNotLeakHiddenCulpritMarkers` scans several response bodies without requiring successful responses. Empty 404/500 payloads pass secrecy checks. Pair absence with successful status, positive public behavior and secrets from the actual hidden fixture. Preserve the genuine positive dev-versus-player boundary test and avoid obsolete hard-coded fixture names as secrecy proof.

`ProjectionEndpointTests.ViewPrologue_ReturnedDto_ProjectionsBuiltFromCommittedStream` checks phase/non-null projections even though the event contributes no diary entry. These checks cannot distinguish before versus after commit. Retain the lifecycle response contract and use observable emitted-event effects where post-commit projection behavior needs proof. The complete-start HUD case already observes useful event effects.

### N-14: Preserve full replay and real PostgreSQL boundaries while consolidating duplicates

`FullReplayEqualityTests` genuinely reaches event replay: the repository routes snapshot/stream version mismatch to `LoadFromEventsAsync`. Preserve purchase/journey parity and missing/partial/stale cache cases. Lagging-snapshot tests in `EventStorePersistenceTests` describe incremental replay but follow that same full-replay route; consolidate their overlapping proof and correct the claim rather than report full replay as missing.

Consolidate purchase success across `GameApiPurchaseTests`, `StorePurchaseAcceptanceTests` and purchase-journal read-through while retaining actual HTTP setup, raw payload contract, fresh repository reload and persistence. Similarly consolidate overlapping poster and declared-handle saloon success. Keep repeated-read idempotence, travel to a new town, citizen fine limits and distinct raw serialization assertions within the retained scenarios. Event append/filter/order, uncommitted invisibility, cross-DbContext conflicts and event-only replay are distinct boundaries, even with similar purchase fixtures.

### N-15: Known integration negative gaps are specific and bounded

Add duplicate upcaster-registration rejection at the registry constructor, which already rejects duplicate keys. Add blank/invalid forced travel-category HTTP rejection and unchanged persistence to the existing dev travel suite; source owns those validation branches and the saloon suite already demonstrates the analogous negative. Extend existing rejected-purchase cases with read-back state/events/log equality so no persisted mutation is proved beyond returned DTOs and unit no-save checks.

Rename the integration disconnected-town test that actually supplies a nonexistent town; the application suite already proves a real existing town without a trail and zero saves. Do not add a redundant negative under the mistaken claim that it is globally missing. A legacy world without either case-file source already has a fail-closed domain test. A ledger snapshot with events explicitly cleared is useful snapshot proof, but cannot certify replay of taken-in/collected/retired/spawn changes; reconcile that authority before adding event parity.

### N-16: Seed and generator tests sometimes compare the same input or unrelated output

Content `SeedWorldResolverTests.MultipleUuidSeedsCanResolveToTheSameSeedWorld` varies an unused helper argument and consequently resolves the same seed twice. Establish distinct encoded inputs before proving equivalence. `NewGame/SeedWorldResolverCodecTests` includes a representative-code title that never invokes that producer, plus arithmetic/enum self-checks; use fixed independent bit fixtures and real producer/decoder behavior.

`TravelEntropyVarianceTests` claims foe-pressure behavior from difficulty enum comparisons and multi-day determinism from one day's Success/Status. Assert the actual weight/plan/outcome/resource behavior and the reached number of days. Preserve real deterministic graph, planarity, salt and Boring comparisons elsewhere.

`NewGame/TownLayoutGeneratorTests` checks a spur effect through equal building counts and required buildings through total count. Assert actual spur geometry and required kind/uniqueness, folding the independent palette/grid/spawn assertions into useful layout scenarios. Two differently named path tests both assert paths are empty; keep one honest characterization of currently disabled paths. `TrailGraphGeneratorTests` checks minimum degree only for endpoints present in its dictionary, omitting isolated expected towns; include every expected slot and real connectivity.

`CaseCharacterRosterTests` searches seeds until outcomes fit the assertion; use explicit reproducible fixtures rather than outcome-driven sampling for individual scenarios. Retain genuine content-quality/role-concealment negatives and independently authored compatibility budgets; remove arbitrary inventory minima where no owning requirement survives. `TravelTestSeedCatalogGuardrailTests` can consolidate repeated encodings of the same world descriptors and test required readiness through consumers. `SeededNewGameFactoryTests` can fold tiny pool-count cases into the richer factory case; the named next-town civic-clue test never visits another town. `SeedWorldFactoryTests` claims default-first from membership and any-choice from choosing only first; existing nonfirst coverage can supply the richer owner scenario.

### N-17: Dev replay and retired-warrant absence need reached outcomes

Domain `DevSaloonOverrideTests.EventReplay_ForcedThenConsumed_ReconstructsCorrectState` asserts null pending override, but replay postprocessing unconditionally restores pending saloon override to null. This masks whether the consumption event actually reconstructs state and raises a real replay-authority question. Prove forced-but-unconsumed replay preserves the pending override, then consumption reproduces the actual spotted POI. Reconcile any current production deviation before declaring the event stream authoritative for this state.

`GameSessionUnrelatedCriminalLedgerWiringTests.ReadWantedPosters_DoesNotSurfaceRetiredUnrelatedCriminalWarrants` calls `Assert.All` on potentially empty newly surfaced warrants. Require a successful read and a known eligible alternative before checking retired exclusion. Sequential different-town warrant tests can pass simply because the first warrant is removed; consolidate with the existing independent fresh-session town-sensitivity test. Manual town-reset setup is useful narrow unit preparation but cannot prove production arrival invokes the reset.

### N-18: Travel event effects and generator history are not consistently isolated

Domain `TravelResolverTests.ResolveJourneyEncounterRunCanLameMountedHorseAndFallBackToFoot` asserts only nonnull result and sets exhaustion to 4 under Challenging rules, whose death threshold is 4 and lame threshold is 2. It seeds a dead horse rather than proving run causes lameness. Require an initially capable horse, a reached threshold crossing, horse/Foot state, recalculated pacing, inventory synchronization and emitted absolute state. The result-snapshot fallback test also uses null-coalescing defaults that let absent snapshots masquerade as the desired Foot/day values; require the snapshot. Some named food-cache, bad-luck and spooked-horse scenarios accept null events or conditionally check their IDs, and one Lucky scenario accepts unchanged resources. Use a fixed reached event and concrete independent effects, preserving the existing water-seep and forced encounter negatives that do observe their outcomes. Completion proof should use actual quiet travel or normal encounter resolution rather than direct reset/resume repair.

`TravelDayPlanGeneratorTests` changes day and history together, or compares unrelated risk/difficulty/world streams, under repeat-suppression titles. Its HardMiles helper returns the found plan but loses the seed used to find it. Use fixed paired contexts changing only the relevant history, retain actual salt/profile determinism and legality, and remove `Encounters.Count >= 0`. The named Quiet fallback test asserts exclusions without requiring Quiet or reaching the empty eligible pool; establish both. Distribution/reachability requirements remain valid where independently owned.

Replay support factories deserve correction within this slice: `TestSessionFactory.CreateBaselineCaseFileFor` copies current discovery/progress/confrontation/settlement state while reconstructing pools, and `TravelTestFactory.RecaptureSetupEventsForReplay` reads mutable player state and omits entropy. Return original setup events from fixture creation rather than reconstructing a convenient baseline. Wanted-POI replay equality must require a nonnull wanted POI; nullable ordinary-roll equality can otherwise bypass the named branch.

### N-19: The large map suite contains both valuable invariants and true tautologies

Content `MapGeneratorBruteForceAnalysisTests.BruteForce_ClusterSeparation_IsReal` classifies pairs as intra-cluster when distance is below 100 and inter-cluster when above 150, then asserts average inter exceeds 1.5 times average intra. That ratio follows from the classification, even for an unclustered scatter. Classify through actual independent cluster membership before measuring separation.

`MapGeneratorTests.Generate_OutlierSlot_NeverProducesDuplicateTownIds` copies naming logic into the test and never invokes the generator. Remove it; retain the real generated-outlier uniqueness case and generator matrix, requiring an outlier actually exists. The named all-towns-valid-clusters test only checks town count; membership already has a genuine owner test.

The analysis collector catches and discards generator exceptions, biasing its statistics toward successful samples. Establish the intended legal sample cardinality and retain unexpected failures. The separate generator matrix already records and fails on exceptions; preserve that meaningful proof and consider sharing generated cases. Keep real bounds, connectivity, planarity, determinism, outlier and independently specified distribution contracts. Narrow or strengthen variant influence that accepts only one differing pair, and the terrain-labelled test that actually checks risk. Rich world repeatability should include the relevant trail graph and layout values, rather than only counts/coordinates.

### N-20: Finish gaps at their existing behavioral owners

The dev travel-context mapper has pending encounter, custom override and no-journey branches not exercised by its scoped handler tests; consolidate a rich interrupted-context mapping scenario and explicit empty-context output rather than another property-per-test file. Domain arrival/context refresh needs one actual departure, arrival and return lifecycle in addition to narrow direct-reset tests. Extend the existing strong application journal projector cases for encounter resolution and trail-event look-ahead rather than retaining the weak domain equivalence smoke. Preserve meaningful role, content-quality and compatibility rules when their authored authority is established; do not turn arbitrary inventory or tuning numbers into permanent fixtures.

Complete per-file .NET dispositions are maintained in the [companion assessment](2026-10-06-dotnet-test-dispositions.md). Frontend and Python dispositions appear below.

## Remediation decisions to discuss

1. Remove no-op, obsolete and demonstrably misleading assertions; preserve their historical removal in the agreed cleanup record rather than rebuilding equivalent detectors.
2. Consolidate repeated happy-path fragments into observable scenarios at their owners, retaining distinct boundary and failure proof.
3. Fill known negative gaps, prioritizing event authority, rejected player transitions, persistence conflicts, projection secrecy and recovery over speculative input matrices.
4. Agree intended behavior before testing currently ambiguous or broken error recovery.
5. Integrate command-bus adoption, wrapper retirement and test custody with F-006/F-007 so the surviving tests enforce the desired contract.
6. Add reusable unslop recognition cues for tests that never invoke production behavior, nonadversarial absence proof, copied expectations and titles that exceed actual assertions. Record this audit as one investigation, without inventing a recurrence count.

## File dispositions

An inventory is a completeness aid for this bounded investigation, not a new permanent repository index or a committed test-run receipt. Each row below represents a full source read, including assertions and setup. Retain means no confirmed harmful pattern was identified in that file; it does not certify comprehensive behavior coverage.

### Frontend test files

Paths are relative to `src/WildBunch.Web/src/`.

| File | Disposition |
|---|---|
| `components/town-hub/background-building-planner.test.ts` | Strengthen forbidden-slot and prosperity assertions; remove redundant nonempty-set proof; retain independent placement, view and seed-budget behavior (W-05). |
| `components/town-hub/ground-loader.test.ts` | Retain loader behavior and deterministic tile/texture selection; renderer adapter tests do not establish browser visual quality. |
| `components/town-hub/sprite-loader.test.ts` | Retain asset selection/fallback and transform behavior at the loader owner. |
| `tests/AppShell.test.tsx` | Retain real shell wiring; consolidate repeated overlay interactions with focused overlay-owner coverage. |
| `tests/ConfirmDialog.test.tsx` | Retain confirmation, cancellation and pending interaction guards. |
| `tests/DevOverlay.test.tsx` | Retain close interactions and focus/control behavior; avoid duplicating every variant through the full shell. |
| `tests/GameSettingsOverlay.test.tsx` | Consolidate repeated settings/start-over presentation and cancellation; add lifecycle failures at their owner (W-08). |
| `tests/JournalSurface.test.tsx` | Retain grouping and actual public rendering; retire unsupported hidden-marker absence claims (W-06). |
| `tests/PhaserMapHost.test.tsx` | Consolidate mount lifecycle and selection adapter scenarios; preserve invalid selection; remove property-absence boundary detectors (W-04). |
| `tests/PhaserTownHubHost.test.tsx` | Retain host lifecycle/data/callback behavior; remove property-absence networking proof (W-04). |
| `tests/SaloonDevPanel.test.tsx` | Retain force/clear semantics; consolidate context/dispatch scenarios and cover failure recovery (W-09). |
| `tests/SessionAuditDevPanel.test.tsx` | Retain actual diagnostic rendering, loading and failure behavior; dev audit visibility is intentionally distinct from player secrecy. |
| `tests/SessionDevPanel.test.tsx` | Replace empty payload matcher with salt semantics; consolidate caption/call fragments and cover failures (W-09). |
| `tests/SetupHuntStep.test.tsx` | Retain draft/value selection and accessible control semantics; retire incidental thumb/markup/style construction assertions (W-11). |
| `tests/SheriffPlace.test.tsx` | Retain wanted-poster/records interaction and public rendering; coordinate shared notice setup with beat-narration tests. |
| `tests/StartFlow.test.tsx` | Consolidate repeated successful walks; repair overclaimed name-preservation assertion and add failed transition scenarios (W-08). |
| `tests/StartOverConfirmation.test.tsx` | Consolidate confirmation side effects and cancellation; preserve pending interaction and add distinct archive failures (W-08). |
| `tests/StartOverRegression.test.tsx` | Retain materially distinct route/surface reachability; consolidate shared archive lifecycle proof with its owner. |
| `tests/StartingTownStep.test.tsx` | Replace duplicate host assertions with loaded data/selection; settle and test terminal empty/error recovery (W-07). |
| `tests/StorePlaceFeedback.test.tsx` | Retain meaningful purchase feedback and failed-result rendering; distinguish a server-declined result from rejected HTTP transport. |
| `tests/StorySoFarStep.test.tsx` | Retain pending, loaded, retry and actual variant omission; consolidate repeated happy path and remove unsupported secret/placeholder claims (W-06). |
| `tests/TownHubScene.test.ts` | Retain scene data/selection/availability behavior; remove ineffective key smoke and property-absence detectors (W-04). |
| `tests/TownHubScene.tiles.test.ts` | Retain tile placement, overlap and transform behavior; deterministic visual construction has an independently meaningful renderer contract. |
| `tests/TownHubSurface.test.tsx` | Retain accessible alternate navigation and action dispatch; retire historic copy-absence checks and narrow visual-focus claims (W-11). |
| `tests/TownLayoutDevPanel.test.tsx` | Consolidate generate/payload/refreshed salt proof; cover rejected mutation and restored controls (W-09). |
| `tests/TrailFlowSurfaceCompleted.test.tsx` | Consolidate heading, affordance and invocation into completed-arrival behavior; add acknowledgment failure at its owner. |
| `tests/TravelDevPanel.test.tsx` | Retain force/clear behavior; add rejected mutation/recovery and consolidate successful scenario fragments (W-09). |
| `tests/TravelPanel.test.tsx` | Retain journey-state, pending, hydration and request payload proof; add known command/refresh failure scenarios (W-10). |
| `tests/TravelPrepSurface.test.tsx` | Retain meaningful preview presentation; retire historic copy absence and cover dispatch/failure recovery (W-10/W-11). |
| `tests/TravelRoutesPanel.test.tsx` | Retain successful route details and dispatch; add per-route failure and stale response rejection (W-10). |
| `tests/actionTypePredicates.test.ts` | Consolidate matching/mismatching cases for all predicates (W-12). |
| `tests/beatFormatters.test.ts` | Retain labels, fallback/null handling and pluralization; table-driven cases can reduce repetitive declarations without erasing distinct branches. |
| `tests/beatNarrationHook.test.tsx` | Retain real hook-to-notice composition and null fallback; consolidate redundant nonadversarial turn-absence proof (W-12). |
| `tests/formatting.test.ts` | Retain actual money formatting and storage read behavior; table-driven formatting is optional, not a required cleanup. |
| `tests/gameSetupSeedCodec.test.ts` | Retain canonical normalization, random-source forwarding and malformed/legacy rejection. Round-trip proof is useful alongside independent examples. |
| `tests/routingConventions.test.ts` | Replace spelling/topology searches with actual navigation/search/reachability contracts (W-02). |
| `tests/stylingEnforcement.test.ts` | Retire historic deletion detectors; reconcile and separately own any intentional policy validator (W-03). |
| `tests/test-utils/setup.test.ts` | Retain bounded proof that the browser harness's scroll stub avoids a JSDOM error; do not mistake it for game behavior coverage. |
| `tests/types.test.ts` | Remove literal-assignment tautology; separately assess enum wire encoding (W-01). |
| `tests/useDevSurfaceSync.test.tsx` | Retain real route/phase-to-dev-surface behavior; shared fixtures may reduce setup repetition. |
| `tests/usePhaseRouteSync.test.tsx` | Retain redirect, matching phase and pending deep-link behavior; negative no-redirect assertions need settled effects before claiming enduring absence. |
| `tests/wildBunchApi.test.ts` | Retain preview request contract; add actual transport failure proof at httpClient (W-10). |

Shared `tests/test-utils/factories.ts`, `renderHelpers.tsx`, `setup.ts`, the tests README and `vite.config.ts` were also read to assess fixtures, routing and discovery. They are support/configuration, not additional test files.

### Python test files

Paths are relative to `scripts/tests/`.

| File | Disposition |
|---|---|
| `test_adr_freshness.py` | Retain generation round trip; add authored stale-input rejection, nonmutation and content preservation (P-03). |
| `test_agent_routers.py` | Retain invalid router and route rejection behavior (P-05). |
| `test_ci_workflow.py` | Replace incidental raw workflow spelling detectors with proof of the intended hosted gate contract (P-02). |
| `test_local_skill_registration.py` | Remove redundant is-file discovery assertion; retain independently owned skill-custody/schema behavior (P-04). |
| `test_operating_standards.py` | Retain malformed declaration and route rejection behavior (P-05). |
| `test_plugin_subscriptions.py` | Retain subscription validation negatives and custody contract checks (P-05). |
| `test_power_shell_wrappers.py` | Retire wrapper existence and unasserted invocation with rejected redundant wrappers (P-01/F-007). |
| `test_precommit_candidate.py` | Retain staged-candidate behavior, repair/refusal boundaries and hook semantics; these observe real behavior. |
| `test_run.py` | Retain dispatch/diagnostics/mode rejection; consolidate incidental private-order/path proof and add command-bus failure boundaries after F-006 reconciliation (P-02). |
| `test_script_entrypoints.py` | Retire rejected paired-filename policy; test surviving implementation behavior where justified (P-01/F-007). |
