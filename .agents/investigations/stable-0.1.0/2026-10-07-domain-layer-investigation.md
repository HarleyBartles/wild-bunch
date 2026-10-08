# Domain layer investigation

**Status:** Static source investigation for the interactive stable `0.1.0` baseline. Findings and proposed dispositions do not authorize remediation. No application code, tests, persisted history or services were changed or exercised. Confirmed source contradictions are distinguished from malformed-data risks, dormant features and unresolved game rules.

**Scope:** All 134 tracked files in `src/WildBunch.Domain`, read in full across the investigation: Actions 4, Cases 22, Economy 3, Events 33, Game 29, Inventory 7, Journal 2, World 10, WantedPosters 1, Properties 1, aggregate marker and project file. Local `bin/` and `obj/` output is excluded. Supporting Application, Persistence, GameContent and browser callers were traced where needed. Findings extend the [Application](2026-10-07-application-layer-investigation.md) and [Persistence](2026-10-07-persistence-layer-investigation.md) audits; the [baseline tracker](2026-10-06-stable-0.1.0-investigation.md) owns agreed scope.

**Test follow-up:** [The Domain test assessment](2026-10-07-domain-test-followup.md) maps these findings to existing assertions and fixtures, missing invariant proof, conditional feature retirement and behavioral consolidation. Strict CQRS remains a baseline requirement.

**Dated disposition, 2026-10-08:** The developer salt override events and zero-event prepped-start path described in this static snapshot were retired from the 0.1.0 candidate. Their findings and file inventory rows remain historical evidence, not current implementation recommendations; see the dated [Domain test follow-up](2026-10-07-domain-test-followup.md) and [developer control feature record](../../../docs/features.md#dev-001-developer-salt-controls). Normal event-backed setup and persisted generated layout facts remain supported.

## Patterns and ownership to protect

The repository's domain/.NET/seed skills, backend and developer unslop profiles, gameplay invariants, architecture guardrails, seed pipeline and event-sourcing doctrine informed this audit. The selected architecture remains appropriate: `GameSession` owns external commands and event production, cohesive internal children own narrow rules and state, Application orchestrates, and Persistence stores history and reconstructs caches. The user's stricter target is that every session fact has event authority; the current zero-event preparation exception is recorded debt.

Primary-source research supports the particular boundaries being assessed. Events must reconstruct authoritative state, snapshots are expendable optimizations, and retries reevaluate against reloaded state. Historical events need explicit schema transitions rather than silent replacement. See [Microsoft's event-sourcing guidance](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing). Replay must preserve the inputs that influenced past behavior rather than obtain new answers from time or external sources; see [Fowler's event-sourcing explanation](https://martinfowler.com/eaaDev/EventSourcing.html). Rich domain behavior and cohesive aggregate boundaries are supported by [Microsoft's domain-model guidance](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model). These sources inform recognition; they do not prescribe this game's missing rules or justify introducing brokers, services or additional aggregates.

| Owner | Assessment |
|---|---|
| `Game` | Session lifecycle and internal journey, bounty, investigation, store and context children form a coherent boundary. File size warrants clearer responsibilities during correction, but does not establish a need for independently persisted aggregates. `TownAggregate` is a session-owned town wrapper despite its name; no separate town repository is warranted. |
| `Cases` | Case facts, suspects, warrants, declaration/settlement policies and surfacing belong to Domain. Fine-grained typed results are useful; public mutation seams and dormant progression obscure actual authority. |
| `Travel` | Route, upkeep, encounter, generation and diary-state types belong to the travel domain. Partial deterministic generation is reasonable. Correct conflicting resources/pacing before cosmetic moves. Presentation-only narration deserves an owner review when it invents causes, as already recorded in AP-19. |
| `Inventory`, `Economy` | Concrete inventory, horse/canteen state, wallet and store rules are legitimate domain concepts. Preserve their names and distinct rules. Duplicated stackability validation and an unused misleading predicate need correction or retirement. |
| `World` | World topology, town sources and layout values are real game state. Typed event snapshots can live beside the values they describe; they are not EF envelopes. Layout representation alone does not justify moving all of it out of Domain. Some comments assert stronger immutability/determinism than the types establish. |
| `Actions`, `WantedPosters`, `Journal` | Action and poster resolution express domain-facing behavior. The journal snapshot is a read contract composed from domain facts and projected logs, with production Persistence and test consumers. Reassess Application ownership of that contract/resolver during projection consolidation; do not delete it as dead code or expose its raw domain fields to players. |
| `Events`, marker, project and `Properties` | Typed facts and an infrastructure-independent project are worth preserving. `InternalsVisibleTo` supports named adapters/tests but does not enforce aggregate authority by itself. Do not replace typed events with generic payload envelopes. |

## Findings and proposed dispositions

**Baseline triage:** Source findings identify what is false or incomplete; they do not imply that every affected feature deserves completion. The user explicitly distinguished bugs in retained behavior from unfinished, non-compliant features that may be removed and implemented properly later. Unrelated criminals is a candidate for removal/deferment, not an agreed repair project. No feature removal is authorized by this record.

| Baseline decision | Candidates and consequence |
|---|---|
| Retain and correct | Ordinary saloon confrontation, deterministic salt/event behavior and legal travel resources/progression are candidates for core behavior correction. Their inclusion remains subject to the agreed minimum playable scope. |
| Remove/defer an unfinished feature | Unrelated-criminal parity/settlement is the clearest candidate: partial events and snapshots do not establish complete event authority. Distinct NPC interactions, dormant killer-release advancement and unused refresh-policy variants also need a deliberate scope decision rather than automatic completion. |
| Remove obsolete scaffolding | Dead branches, unused convenience APIs, dispatcher drift and task comments can be retired where consumer review establishes that they serve no retained contract. |
| Preserve or explicitly retire history | Removing a live feature must account for its event types, snapshots, projections, callers and historical playthroughs. Preserve replay/compatibility where promised or explicitly invalidate affected pre-alpha playthroughs under the selected migration policy. Keep applied migrations and record material removals truthfully in the ADR log. A database reset is not the default migration strategy. |

### DN-01: Clearing the RNG lock invents a different salt on every replay

**Confirmed event-authority defect.** `Game/GameSession.cs:799-802` applies `DevSaltSourceCleared` by calling `SaltSource.CreateRuntime()`. `Events/DevSaltSourceCleared.cs` carries no resulting salt. Applying the same committed history therefore creates a new authoritative salt each time, affecting future deterministic travel/saloon rolls. Already-recorded outcomes can still reconstruct; subsequent play from the reconstructed state can diverge. The Application clear handler exists but has no current HTTP route, so this is a supported Domain/use-case defect rather than a claim that the public browser currently invokes it.

Generate the new salt at command decision time and record it in the fact. Establish how existing payloadless events are supported; an upcaster cannot recover entropy that was never recorded. `GameSessionEventReplay.cs:68-70` also falls back to runtime randomness when both start/world salt facts are absent. Treat supported legacy genesis and malformed-history policy explicitly, alongside PS-05, without inventing historical values.

### DN-02: A normally spotted suspect cannot pass the saloon presence gate

**Confirmed live flow contradiction.** `GameSession.Apply(SaloonPersonOfInterestSpotted)` (`GameSession.cs:485-498`) records an active suspect in the town visit but never establishes availability. The presence ledger starts empty, missing state means `Unavailable` (`Game/WantedSuspectPresenceLedger.cs:35-52`), and `BountyLoop.ConfrontWantedSuspectInSaloon` rejects that state (`Game/BountyLoop.cs:128-139`). Source searches found no production caller of `SetWantedSuspectPresenceState`; remaining event-driven presence writers run after confrontation and set secured/fled states, not initial availability.

A player can learn the matching warrant and spot its suspect, yet receive “no longer in the saloon” when trying to confront them. Decide whether the active spotted POI establishes confrontation eligibility itself or its event must update presence. Keep both command and replay legal, and prove the ordinary learn/spot/confront/settle sequence without a test-only presence setter.

### DN-03: Full replay loses the unrelated-criminal ledger's history

**Confirmed replay divergence.** Full reconstruction creates a placeholder empty case and therefore a zero-roster ledger (`GameSessionEventReplay.cs:80-105`; `GameSession.cs:90-94`). Applying unrelated turn-ins calls `MarkWarrantCollected` and `RecordTakenIn` on that empty ledger (`BountyLoop.cs:769-773`), producing no ledger effect. After all events, reconstruction replaces it with `BuildUnrelatedCriminalLedger` (`GameSessionEventReplay.cs:120-132`), which reconstructs gang settlements from the final case but omits unrelated turn-ins (`GameSession.cs:1849-1878`). Already-paid unrelated criminals can return to the active pool; replacement and collected/retired state diverge from command/snapshot state.

If retained, initialize the ledger when its authoritative roster becomes available and apply subsequent ledger-affecting events in order. Rebuilding only the final gang count cannot preserve the sequence of take-ins, replacements and preferential retirement. Audit how warrant collection enters that history as part of the same correction. Alternatively, remove the unfinished unrelated-criminal feature from the live baseline and defer a coherent implementation, with explicit disposition of callers, content, old events, snapshots and projections. Existing settlement events mean it is partially integrated, not wholly absent from event sourcing; the missing authoritative lifecycle is the reason for triage. The final restore with a null pending override does **not** clear a replayed developer saloon override: its setter is conditional on a non-null argument. That suspicion was checked and rejected.

### DN-04: Bribe retaliation restores the food or feed it stole

**Confirmed live outcome defect.** Retaliation can steal Food/HorseFeed (`Travel/JourneyEncounterResolutionEngine.cs:374-390`). `Game/JourneyLoop.cs:567-577` adjusts runtime ammunition only, leaving journey food/feed unchanged. `GameSession.Apply(JourneyEncounterResolved)` removes the stolen item, then `SyncPlayerFromJourneySnapshot` restores food/feed to those unchanged absolute reserves (`GameSession.cs:629-647, 677-687`). The emitted theft fact and resulting resource state contradict each other.

Make the decision's resulting journey resources and stolen-item fact agree. Both command and replay currently produce the same wrong resource state, so a replay-equality test alone cannot detect this. Prove observable loss for Food and HorseFeed, retention of unrelated resources, and replay of that correct outcome; consolidate with encounter settlement coverage.

### DN-05: Losing mounted travel can discard remaining route distance

**Confirmed travel progression defect.** `TravelJourney.RecalculatePacing` changes only mode (`Travel/TravelJourney.cs:129-131`). `AdvanceOneDay` subtracts distance at the new rate but decrements the old day counter and completes at zero days (`134-147`). Horse upkeep or a trail event can switch mode (`JourneyLoop.cs:748-790, 1001-1007`). The ordinary completion branch uses that stale result and `MarkCompleted` zeroes remaining distance.

For a Standard route with three ride-day units, a change to foot before the first day's progress can still end it after three half-unit days, discarding 1.5 units. Reconcile distance, pacing and delay semantics at their domain owner. Preserve legitimate delay days while preventing arrival with distance left. Strengthen the travel progression scenario across a real horse-loss transition; checking the enum or final zeroed distance is insufficient.

### DN-06: Generated dust loss can throw during ordinary low-food travel

**Confirmed reachable failure condition; no particular seed was reproduced.** The unlucky candidate pool includes dust food loss without checking remaining food (`Travel/TravelDayPlanGenerator.Context.cs:696-784`). Brutal loses two food (`Travel/TravelRulesProfile.cs:138-155`); even Standard loses one. Daily upkeep consumes food before generation. A dust event with less than its cost then calls `TravelJourney.AdjustFood`, which throws on underflow (`Game/JourneyLoop.cs:969-972`; `TravelJourney.cs:287-295`). Multiple events in one plan can also change resources after candidate selection.

Choose whether actual loss is capped, the outcome changes, or an inapplicable event is excluded. Ensure every selected plan is legal at application time and records the actual effects. Verify low and zero reserves through real generation/application, including earlier same-day events, rather than simply testing that the low-level value object rejects negatives. The aggregate advances its clock before asking the child for outcomes (`GameSession.cs:1304-1306`), and the child changes journey state while assembling the plan; an exception can leave the in-memory object changed without produced events. The Application pipeline does not save an unsuccessful thrown operation, so no partial database commit is alleged.

### DN-07: Prepped start adds direct facts that its event does not apply

**Confirmed extension of AP-01/PS-01.** `StartFromPrepped` directly assigns world, case, seed, salt and Active status (`GameSession.cs:1506-1510`), then emits `WorldGenerated`. Its Apply sets world/case/salt/entropy but not seed or status (`1174-1189`). The stream still lacks the replay constructor's required setup/start genesis. These are extra transition mismatches within the already-recorded preparation problem, not separate evidence that the exception is an acceptable permanent design.

Correct preparation and transition authority together. Preserve unknown setup facts until known, establish event-backed genesis, and require event-only reconstruction through every supported phase. Do not fix EF null serialization by inventing a world or silently retaining snapshot-only truth.

### DN-08: The killer-release advancement path has no production trigger

**Confirmed dormant feature; intended rule unresolved.** `Cases/CaseFile.cs:191-205, 287-336, 467-469` exposes release progress behind an optional flag that defaults false. No production source call passes true. Investigation Apply reveals by ID through methods that retain that default (`399-421`; `GameSession.cs:1253-1261`). New content starts with a positive threshold and zero progress (`GameContent/NewGame/SeedCaseBuilder.cs:104`). Ordinary clue discovery therefore never opens this gate, and the culprit remains excluded from saloon/poster eligibility by that mechanism.

Confirm which findings or actions should advance it, or explicitly classify release as unfinished for `0.1.0`. Do not invent “every clue counts” from the method name, nor protect a permanently locked gate by seeding release progress in tests. Any implemented progress must follow an authoritative event/application path.

### DN-09: Public child and convenience mutators permit unrecorded state

**Confirmed authority exposure; no production external mutation caller established.** Session exposes mutable Player/Inventory, Clock, PursuitState, CaseFile, town visit and unrelated ledger objects. Public methods include cash/health/items/town changes, clock advancement, case discovery/accusation/settlement, town source/POI changes and ledger take-ins. `SetWantedSuspectPresenceState` and diary append/update methods also bypass events/version (`GameSession.cs:1272-1273, 2434-2444`). Tests use several seams; they do not establish production legitimacy.

Distinguish event Apply, deliberate restoration and fixture construction from live commands. Restrict alternate mutation routes without introducing repositories for children. Read-only collection interfaces alone do not freeze mutable objects or caller-owned lists/arrays, including event snapshots and layout grids. This is an encapsulation concern, not a claim of a current HTTP mutation bypass. Trace and retain legitimate adapter consumers before narrowing visibility.

### DN-10: Ledger restoration's validation claim exceeds its implementation

**Confirmed validation gap; corruption impact conditional.** `UnrelatedCriminalLedger.FromSnapshot` (`Cases/UnrelatedCriminalLedger.cs:225-238`) validates starting roster size/uniqueness through the constructor, then assigns active/taken/collected/retired sets, parity and spawn index without validating their coherence. Its comment says roster and parity invariants are revalidated. Contradictory/out-of-roster active IDs and invalid indices can enter runtime state. This extends PS-04's malformed-current-cache concern.

Validate the supported snapshot invariants and recover derived state from intact history, after DN-03 makes that history complete. Define actual allowed overlaps rather than indiscriminately requiring every set to be disjoint: collected warrants can legitimately refer to taken-in criminals. A green constructor test does not establish restored-state validity.

### DN-11: World snapshots silently replace custom town-source catalogs

**Confirmed supported-shape loss; current generated-world impact not found.** `World/WorldModels.cs:25-31` accepts a custom `SourceCatalog`; `TownSnapshot.FromDomain/ToDomain` omit it (`World/WorldSnapshot.cs:17-33`), restoring Default instead. Event and component world serialization both use this mapping. A world constructed with a no-saloon catalog therefore returns with saloon support after reconstruction. Such a fixture exists; current `SeedWorldFactory` constructs default catalogs.

Decide whether custom catalogs are supported authoritative world data or intentionally test-only configuration. Preserve them through the real boundary if supported, or narrow the misleading API. Do not allege data loss in today's default-only generated worlds, and do not add event compatibility machinery without a declared historical contract.

### DN-12: Available actions advertise a command that completed journeys reject

**Confirmed read/command mismatch.** `Actions/ActionAvailabilityResolver.cs:37-58` treats any journey without a pending encounter as advanceable. Completed journeys remain present until arrival acknowledgement, while `JourneyLoop.AdvanceJourneyDay` rejects non-Active status (`JourneyLoop.cs:212-221`). The available-action response can advertise “Advance travel day” after arrival. The browser has a separate arrival workflow, so this is not a claim that arrival is impossible.

Resolve actions by legal journey phase and account for the existing acknowledgement operation. Preserve modal town blocking until acknowledgement if intended. The same resolver has no terminal-session-status check; determine archive/unfinished terminal visibility as part of the action contract rather than assuming enum presence proves implemented failure/completion behavior.

### DN-13: Event occurrence getters return read time

**Confirmed false derived fact, extending AP-14.** `CaseFileGenerated`, `WorldGenerated` and `StartingTownSelected` define `OccurredAt => DateTimeOffset.UtcNow`. The same event reports different occurrence times on repeated access. Persistence supplies its own stored envelope timestamp; the getters do not change that stored column, and no production read of these getters was established.

Retire the misleading unused properties or use a genuinely captured fact when domain timing is required. Resolve audit occurrence time at its real envelope owner, alongside the Application projector that invents current time. Do not pretend access time was historical occurrence or add timing fields to every event solely for uniformity.

### DN-14: Dormant branches and stale seams obscure the live contract

**Confirmed maintenance debt with bounded impact.** The second identical zero-encounter branch in `TravelDayPlanGenerator.Context.cs:34-37` is unreachable. `GameSessionEventReplay.cs:39` assigns an unused first-event local. `JourneyLoop.cs:41-42` still says methods will be filled by numbered tasks. `Inventory.GetHorseStateOrNull` duplicates `GetHorseState` and has no caller; `AdvanceHorseState` is test-used legacy convenience. Citizen reveal helpers have no production caller. `TownSourceVisitState.RefreshForVisit` collapses both refresh policies and default to the same operation; all current definitions use PerVisit, so no current OnTownReturn discrepancy is alleged.

`Inventory.CanAddItem` validates shape only, while duplicate nonstackable rejection happens later in AddHorse/AddCanteen/AddNonStackable. It can report true for a duplicate that AddItem rejects, and its catch-all hides any validation exception. No caller was found in source/tests. Retire it if unused or make the predicate genuinely match addition. StoreLoop already performs its own duplicate checks, so this is not a claim that normal store purchases currently bypass them.

`ApplyProducedEvent` claims to mirror replay but lacks four setup event cases supported by the replay dispatcher (`GameSession.cs:332-433`; `GameSessionEventReplay.cs:145-166`). Current setup methods manually apply/record them, so this is dispatcher drift, not a currently failing ProduceEvent call. Share responsibility or document the intentional distinction; do not create tests that freeze two switch inventories instead of behavior.

Remove obsolete code when nearby remediation establishes its consumers. Record intentional omissions and semantic distinctions in live guidance rather than retaining task scaffolding. Do not invent replacement abstractions for every removed helper.

### DN-15: Accepted-value and deterministic-index edges need narrower contracts

**Conditional input defects, not reproduced ordinary-player failures.** `Cases/CaseWarrants.cs:18-43` accepts negative bounty amounts; settlement policies propagate signed cash effects. Current content has no negative example established by this audit. Define nonnegative bounty at the owning contract, or document a real upstream guarantee and reconstruction policy. Likewise `Wallet`'s public positional constructor can accept negative cash despite its guarded adjustment methods; current generated wallets are valid. `PathSegment.Create` validates coordinates but the public record constructor bypasses it, and layout dimensions/grids are permissive. Avoid treating a helper factory as a universally enforced invariant.

`BountyLoop.StableSaloonRollHash` and `CitizenCast.StableHash` call `Math.Abs` on unchecked signed hashes (`BountyLoop.cs:1000-1010`; `CitizenCast.cs:205-219`). The `int.MinValue` result throws. No concrete generated seed/input reaching that result was established. Resolve overflow-safe index conversion if arbitrary salt/content inputs are supported, preserving explicitly selected deterministic compatibility rather than casually changing all existing roll sequences.

### DN-16: Incomplete encounter and legacy-repair semantics require decisions

**Questions/conditional paths, not asserted new gameplay rules.** Generated Npc encounters share run/fight/bribe and default foe resolution (`TravelDayPlanGenerator.Context.cs:625-636`; `JourneyLoop.cs:357-380`). Confirm whether these are hostile contacts or an unfinished distinct interaction. Food warnings exist, but travel consumes food only when available and no player starvation rule was established. Decide intended shortage behavior before writing a test that invents it.

**Dated disposition, 2026-10-08:** The accepted stable 0.1.0 baseline classifies generated interactive friendly trail encounters as an unfinished interaction and retires their category, choices, messages, developer option and exclusive tests. Existing hostile Foe encounters and shared travel resolution remain. This resolves only the NPC question; starvation semantics were settled separately in the baseline specification.

Encounter resolution can replace/recover an old pending foe profile before validating choice or funds (`JourneyLoop.cs:303-342`), then return no events on rejection. This is an unrecorded mutation only for the malformed/legacy shapes that enter that repair branch; current generated foes have profiles. Establish whether those shapes are supported and perform deterministic repair at an explicit compatibility boundary. Other impossible-looking null recovery after an earlier null return should be assessed as dormant defensive scaffolding, not praised as exercised recovery.

Bribe resolution still reports nonzero heat increase in its plan/diary metadata while current pursuit heat remains unchanged (`JourneyEncounterResolutionEngine.cs:358-387`). This extends AP-19's narration honesty concern; do not reactivate retired trail heat merely to make old fields meaningful. Apply methods also assume valid event ordering and town identity: normal command production supplies them, but malformed-stream semantic rejection is not comprehensively specified. Settle the supported-history failure boundary without re-running commands or RNG during Apply.

## Behavioral proof direction

First decide which features remain in the minimum baseline. For retained behavior, prioritize ordinary saloon reachability; event-backed setup and salt transitions; correct travel resource settlement; and distance/pace/delay legality under real horse loss and low reserves. Ordered ledger reconstruction after mixed gang/unrelated take-ins is needed only if that feature is retained; removal instead needs behavioral proof that the removed feature is unavailable and that its agreed history/migration policy works. Extend existing independent scenarios and meaningful negatives. A source-string guard, enum snapshot, event count or replay equality of an already-wrong result does not prove these behaviors. Pure helper retirement, comments and moves need no new runtime test. The separate test follow-up remains an interactive next step; this source audit does not claim every current test was re-assessed here.

## Per-file dispositions

Every tracked Domain file has a row below. “Keep” means its role is justified and no separate defect was established in this static pass, not a correctness certification. DN references identify corrections/questions to carry into agreed remediation; existing AP/PS concerns are retained without counting them as new independent incidents.

| File | Disposition |
|---|---|
| [Actions/ActionAvailabilityResolver.cs](../../../src/WildBunch.Domain/Actions/ActionAvailabilityResolver.cs) | Keep role; correct completed-journey action mismatch, DN-12. |
| [Actions/AvailableAction.cs](../../../src/WildBunch.Domain/Actions/AvailableAction.cs) | Keep typed action result. |
| [Actions/AvailableActionKind.cs](../../../src/WildBunch.Domain/Actions/AvailableActionKind.cs) | Keep action identifiers; enum membership does not establish reachability. |
| [Actions/InvestigationSources.cs](../../../src/WildBunch.Domain/Actions/InvestigationSources.cs) | Keep default-catalog accessors; custom-catalog question DN-11. |
| [Cases/BountyDeclarationMatchPolicy.cs](../../../src/WildBunch.Domain/Cases/BountyDeclarationMatchPolicy.cs) | No independent issue found; exact ordinal warrant-ID match is explicit. |
| [Cases/BountySettlementPolicy.cs](../../../src/WildBunch.Domain/Cases/BountySettlementPolicy.cs) | No separate issue found; see DN-15 for unchecked bounty input. |
| [Cases/CaseFile.cs](../../../src/WildBunch.Domain/Cases/CaseFile.cs) | See DN-08 and DN-09; constructor clamps threshold/progress, with progress-above-threshold accepted as a malformed-state policy question. |
| [Cases/CaseFileSnapshot.cs](../../../src/WildBunch.Domain/Cases/CaseFileSnapshot.cs) | Explicit domain/snapshot mapping; enum strings parse fail-closed. No independent defect established. |
| [Cases/CaseModels.cs](../../../src/WildBunch.Domain/Cases/CaseModels.cs) | Typed values and trait normalization; no independent issue found. |
| [Cases/CaseProgress.cs](../../../src/WildBunch.Domain/Cases/CaseProgress.cs) | Threshold comparison is direct; advancement policy noted in DN-08. |
| [Cases/CaseWarrants.cs](../../../src/WildBunch.Domain/Cases/CaseWarrants.cs) | See DN-15; other constructor normalization is explicit. |
| [Cases/ClueSurfacingResolver.cs](../../../src/WildBunch.Domain/Cases/ClueSurfacingResolver.cs) | Deterministic stable hash and signed-safe modulo; no independent issue found. |
| [Cases/FeatureLanguage.cs](../../../src/WildBunch.Domain/Cases/FeatureLanguage.cs) | Value/content records; no independent issue found. |
| [Cases/FeatureLanguageService.cs](../../../src/WildBunch.Domain/Cases/FeatureLanguageService.cs) | Exhaustive category switch fails on unsupported category; no independent issue found. |
| [Cases/InvestigationResult.cs](../../../src/WildBunch.Domain/Cases/InvestigationResult.cs) | Result factories preserve success/session-change fields; no independent issue found. |
| [Cases/InvestigationSourceKind.cs](../../../src/WildBunch.Domain/Cases/InvestigationSourceKind.cs) | Closed enum; no independent issue found. |
| [Cases/OutlawGang.cs](../../../src/WildBunch.Domain/Cases/OutlawGang.cs) | Single typed gang identifier; no independent issue found. |
| [Cases/SaloonPersonOfInterestConfrontation.cs](../../../src/WildBunch.Domain/Cases/SaloonPersonOfInterestConfrontation.cs) | Result factories map explicit outcomes; no independent issue found. |
| [Cases/SaloonPersonOfInterestDescriptor.cs](../../../src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs) | Warrant/profile/trait fallback chain is explicit; no independent issue found. |
| [Cases/SheriffTurnInResult.cs](../../../src/WildBunch.Domain/Cases/SheriffTurnInResult.cs) | Result factories preserve outcome and settlement data; no independent issue found. |
| [Cases/SuspectProfile.cs](../../../src/WildBunch.Domain/Cases/SuspectProfile.cs) | Immutable normalized collections; no independent issue found. |
| [Cases/SuspectTraitTags.cs](../../../src/WildBunch.Domain/Cases/SuspectTraitTags.cs) | Stable tags; no independent issue found. |
| [Cases/SuspectTurfAssignment.cs](../../../src/WildBunch.Domain/Cases/SuspectTurfAssignment.cs) | Typed assignment record; no independent issue found. |
| [Cases/UnrelatedCriminalLedger.cs](../../../src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs) | See DN-10 and replay root-cause clarification. |
| [Cases/WantedPosterResolver.cs](../../../src/WildBunch.Domain/Cases/WantedPosterResolver.cs) | Filters known, unreleased-culprit, and retired warrants; safe modulo; no independent issue found. |
| [Cases/WantedSuspectConfrontation.cs](../../../src/WildBunch.Domain/Cases/WantedSuspectConfrontation.cs) | Outcome/result mapping is explicit; no independent issue found. |
| [Economy/StorePurchaseResult.cs](../../../src/WildBunch.Domain/Economy/StorePurchaseResult.cs) | Keep behavioral success/failure result. |
| [Economy/TownStoreCatalogModels.cs](../../../src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs) | Keep prosperity/vendor offers; handler validates availability, no confirmed catalog exploit. |
| [Economy/Wallet.cs](../../../src/WildBunch.Domain/Economy/Wallet.cs) | Keep cash behavior; permissive construction DN-15. |
| [Events/CaseFileGenerated.cs](../../../src/WildBunch.Domain/Events/CaseFileGenerated.cs) | Keep case fact; read-time occurrence getter DN-13. |
| [Events/DevDifficultyForced.cs](../../../src/WildBunch.Domain/Events/DevDifficultyForced.cs) | No independent issue found; typed dev fact. |
| [Events/DevEntropyChanged.cs](../../../src/WildBunch.Domain/Events/DevEntropyChanged.cs) | No independent issue found; typed dev fact. |
| [Events/DevLayoutSaltsForced.cs](../../../src/WildBunch.Domain/Events/DevLayoutSaltsForced.cs) | Keep typed preparation; missing persisted codec PS-02. |
| [Events/DevSaloonOverrideCleared.cs](../../../src/WildBunch.Domain/Events/DevSaloonOverrideCleared.cs) | No independent issue found. |
| [Events/DevSaloonOverrideConsumed.cs](../../../src/WildBunch.Domain/Events/DevSaloonOverrideConsumed.cs) | No independent issue found; consumption has explicit event. |
| [Events/DevSaloonOverrideForced.cs](../../../src/WildBunch.Domain/Events/DevSaloonOverrideForced.cs) | No independent issue found; typed override data. |
| [Events/DevSaltSourceCleared.cs](../../../src/WildBunch.Domain/Events/DevSaltSourceCleared.cs) | Correct missing runtime salt fact, DN-01; historical entropy was never captured. |
| [Events/DevSaltSourceForced.cs](../../../src/WildBunch.Domain/Events/DevSaltSourceForced.cs) | No independent issue found; typed salt source. |
| [Events/DevTravelOverrideCleared.cs](../../../src/WildBunch.Domain/Events/DevTravelOverrideCleared.cs) | No independent issue found. |
| [Events/DevTravelOverrideConsumed.cs](../../../src/WildBunch.Domain/Events/DevTravelOverrideConsumed.cs) | No independent issue found; consumption has explicit event. |
| [Events/DevTravelOverrideForced.cs](../../../src/WildBunch.Domain/Events/DevTravelOverrideForced.cs) | No independent issue found; typed override data. |
| [Events/GameStarted.cs](../../../src/WildBunch.Domain/Events/GameStarted.cs) | Carries absolute starting player/configuration state; no independent issue found. |
| [Events/IDomainEvent.cs](../../../src/WildBunch.Domain/Events/IDomainEvent.cs) | Clear typed marker and envelope boundary; no independent issue found. |
| [Events/InvestigationPerformed.cs](../../../src/WildBunch.Domain/Events/InvestigationPerformed.cs) | Public clue/warrant IDs only; no hidden culprit payload. Apply uses current town rather than event TownId; see unresolved note below. |
| [Events/JourneyArrivalAcknowledged.cs](../../../src/WildBunch.Domain/Events/JourneyArrivalAcknowledged.cs) | Carries journey snapshot and diary message; no independent issue found. |
| [Events/JourneyCompleted.cs](../../../src/WildBunch.Domain/Events/JourneyCompleted.cs) | Absolute destination/journey state; no independent issue found. |
| [Events/JourneyEncounterResolved.cs](../../../src/WildBunch.Domain/Events/JourneyEncounterResolved.cs) | Explicit absolute/additive fields and snapshots; no independent issue found in scoped review. |
| [Events/JourneyStarted.cs](../../../src/WildBunch.Domain/Events/JourneyStarted.cs) | Absolute journey and heat; no independent issue found. |
| [Events/PlayerSetupCompleted.cs](../../../src/WildBunch.Domain/Events/PlayerSetupCompleted.cs) | Captures setup name/difficulty/entropy/seed; no independent issue found. |
| [Events/PlaythroughArchived.cs](../../../src/WildBunch.Domain/Events/PlaythroughArchived.cs) | Captures archival metadata; Apply’s terminal status behavior matches intent. |
| [Events/PrologueViewed.cs](../../../src/WildBunch.Domain/Events/PrologueViewed.cs) | Phase fact plus revealed identifier; identifier is not aggregate state, and current query deterministically resolves it. No defect established. |
| [Events/SaloonPersonOfInterestConfronted.cs](../../../src/WildBunch.Domain/Events/SaloonPersonOfInterestConfronted.cs) | Public outcome data; no independent issue found. |
| [Events/SaloonPersonOfInterestSpotted.cs](../../../src/WildBunch.Domain/Events/SaloonPersonOfInterestSpotted.cs) | Public POI data and explicit log policy; no independent issue found. |
| [Events/SheriffTurnInSettled.cs](../../../src/WildBunch.Domain/Events/SheriffTurnInSettled.cs) | Settlement data; no independent issue found. |
| [Events/StartingTownSelected.cs](../../../src/WildBunch.Domain/Events/StartingTownSelected.cs) | Keep selected-town fact; occurrence DN-13, malformed-history policy DN-16. |
| [Events/StoreItemPurchased.cs](../../../src/WildBunch.Domain/Events/StoreItemPurchased.cs) | Carries purchase totals and resulting wallet; no independent issue found. |
| [Events/TownActionContextEntered.cs](../../../src/WildBunch.Domain/Events/TownActionContextEntered.cs) | Absolute context/clock/heat state; no independent issue found. |
| [Events/TrailEventApplied.cs](../../../src/WildBunch.Domain/Events/TrailEventApplied.cs) | Carries absolute journey/wallet/heat; dead/reserved `HeatIncrease` is documented. No independent issue found. |
| [Events/TravelDayAdvanced.cs](../../../src/WildBunch.Domain/Events/TravelDayAdvanced.cs) | Absolute day/journey plus health delta; no independent issue found. |
| [Events/UnrelatedCriminalTurnInSettled.cs](../../../src/WildBunch.Domain/Events/UnrelatedCriminalTurnInSettled.cs) | Typed warrant settlement; replay effects are later overwritten by D1. |
| [Events/WantedSuspectConfronted.cs](../../../src/WildBunch.Domain/Events/WantedSuspectConfronted.cs) | Public outcome and no culprit truth; no independent issue found. |
| [Events/WorldGenerated.cs](../../../src/WildBunch.Domain/Events/WorldGenerated.cs) | Keep world/configuration fact; DN-07 apply completeness, DN-11 mapping, DN-13 getter. |
| [Game/ActionContextTracker.cs](../../../src/WildBunch.Domain/Game/ActionContextTracker.cs) | Keep child-owned context and event decision. |
| [Game/BeatNarration.cs](../../../src/WildBunch.Domain/Game/BeatNarration.cs) | Keep authored beat/context values pending AP-19 correction. |
| [Game/BountyLoop.cs](../../../src/WildBunch.Domain/Game/BountyLoop.cs) | See DN-02, DN-15, and DN-15. Command methods return events; no direct event emission found. |
| [Game/BountyLoopContexts.cs](../../../src/WildBunch.Domain/Game/BountyLoopContexts.cs) | Narrow immutable context records; no independent issue found. |
| [Game/CitizenCast.cs](../../../src/WildBunch.Domain/Game/CitizenCast.cs) | See DN-15; `ResolveRevealName` and `ResolveRevealNarration` have no production source callers and are documented convenience helpers. |
| [Game/DevSaloonOverride.cs](../../../src/WildBunch.Domain/Game/DevSaloonOverride.cs) | Override factories are clear; validation is at force command as documented. No independent issue found. |
| [Game/DevTravelOverride.cs](../../../src/WildBunch.Domain/Game/DevTravelOverride.cs) | Keep next-day preparation; validation/default debt AP-04. |
| [Game/GameClock.cs](../../../src/WildBunch.Domain/Game/GameClock.cs) | Keep clock behavior; exposed mutation DN-09 and pre-event advance DN-06. |
| [Game/GameLogEntry.cs](../../../src/WildBunch.Domain/Game/GameLogEntry.cs) | Keep consumed projection contract pending transfer; not dead because of legacy wording. |
| [Game/GameLogEntryKind.cs](../../../src/WildBunch.Domain/Game/GameLogEntryKind.cs) | Keep projected log identifiers. |
| [Game/GameSession.cs](../../../src/WildBunch.Domain/Game/GameSession.cs) | Correct DN-01 through DN-07; public authority seams DN-09 and dispatcher drift DN-14. |
| [Game/GameSessionEventReplay.cs](../../../src/WildBunch.Domain/Game/GameSessionEventReplay.cs) | Correct ordered ledger DN-03, salt fallback DN-01, dead local DN-14; history policy DN-16. |
| [Game/GameSessionId.cs](../../../src/WildBunch.Domain/Game/GameSessionId.cs) | Keep typed identity/new-session factory; not replay-time gameplay RNG. |
| [Game/GameStatus.cs](../../../src/WildBunch.Domain/Game/GameStatus.cs) | Keep statuses; Completed/Failed membership does not establish implemented transitions. |
| [Game/InvestigationLoop.cs](../../../src/WildBunch.Domain/Game/InvestigationLoop.cs) | Decision logic returns typed events and does not mutate CaseFile; see DN-08 for missing release-progress decision. |
| [Game/JourneyLoop.cs](../../../src/WildBunch.Domain/Game/JourneyLoop.cs) | Correct DN-04/DN-05/DN-06; legacy repair DN-16, stale task comments DN-14. |
| [Game/JourneyLoopContexts.cs](../../../src/WildBunch.Domain/Game/JourneyLoopContexts.cs) | Keep narrow inputs/outcome; references do not enforce immutability, DN-09. |
| [Game/Player.cs](../../../src/WildBunch.Domain/Game/Player.cs) | Keep player behavior; exposed mutation DN-09. |
| [Game/PursuitState.cs](../../../src/WildBunch.Domain/Game/PursuitState.cs) | Keep town-pressure state; mutation seam DN-09 and old parity comments DN-14. |
| [Game/SaltSource.cs](../../../src/WildBunch.Domain/Game/SaltSource.cs) | Keep explicit runtime/fixed decision; sampling outside Apply, DN-01. |
| [Game/StartFlowPhase.cs](../../../src/WildBunch.Domain/Game/StartFlowPhase.cs) | Keep start vocabulary; event-backed prep debt DN-07. |
| [Game/StoreLoop.cs](../../../src/WildBunch.Domain/Game/StoreLoop.cs) | Keep decision child; duplicate checks prevent normal nonstackable purchase bypass. |
| [Game/TimeOfDay.cs](../../../src/WildBunch.Domain/Game/TimeOfDay.cs) | Keep four-slot names. |
| [Game/TownActionContext.cs](../../../src/WildBunch.Domain/Game/TownActionContext.cs) | Keep town-context vocabulary and event ownership. |
| [Game/TownAggregate.cs](../../../src/WildBunch.Domain/Game/TownAggregate.cs) | Thin town-definition/visit-state wrapper; public mutation surface noted in DN-09. |
| [Game/TownSourceVisitState.cs](../../../src/WildBunch.Domain/Game/TownSourceVisitState.cs) | See DN-09 and DN-14; no other independent issue found. |
| [Game/TownVisitState.cs](../../../src/WildBunch.Domain/Game/TownVisitState.cs) | Visit/source state owner; public mutation surface noted in DN-09. |
| [Game/TravelDayOutcome.cs](../../../src/WildBunch.Domain/Game/TravelDayOutcome.cs) | Keep travel outcome vocabulary. |
| [Game/WantedSuspectPresenceLedger.cs](../../../src/WildBunch.Domain/Game/WantedSuspectPresenceLedger.cs) | Missing state defaults to Unavailable, which is material to DN-02; duplicate entries are normalized by replacement. |
| [IAggregateRoot.cs](../../../src/WildBunch.Domain/IAggregateRoot.cs) | Keep selected marker; marker cannot enforce mutation authority. |
| [Inventory/CanteenState.cs](../../../src/WildBunch.Domain/Inventory/CanteenState.cs) | Keep charge/capacity invariants and immutable operations. |
| [Inventory/HorseTravelState.cs](../../../src/WildBunch.Domain/Inventory/HorseTravelState.cs) | Keep horse condition/profile checks; consumer assessment for legacy AdvanceTravelDay. |
| [Inventory/Inventory.cs](../../../src/WildBunch.Domain/Inventory/Inventory.cs) | Keep concrete rules; misleading/dead APIs DN-14, public mutation DN-09. |
| [Inventory/InventoryCapabilities.cs](../../../src/WildBunch.Domain/Inventory/InventoryCapabilities.cs) | Keep named capability result. |
| [Inventory/InventoryCapabilityResolver.cs](../../../src/WildBunch.Domain/Inventory/InventoryCapabilityResolver.cs) | Keep horse/saddle, charged-canteen and loaded-weapon rules. |
| [Inventory/InventoryItem.cs](../../../src/WildBunch.Domain/Inventory/InventoryItem.cs) | Keep typed item rules; construction defaults do not prove stored-field completeness. |
| [Inventory/ItemKind.cs](../../../src/WildBunch.Domain/Inventory/ItemKind.cs) | Keep concrete item vocabulary. |
| [Journal/JournalResolver.cs](../../../src/WildBunch.Domain/Journal/JournalResolver.cs) | Keep consumer behavior pending read-contract owner consolidation; setup-null concerns AP-07. |
| [Journal/JournalSnapshot.cs](../../../src/WildBunch.Domain/Journal/JournalSnapshot.cs) | Keep consumed read contract pending owner decision; final mapping owns audience safety. |
| [Properties/AssemblyInfo.cs](../../../src/WildBunch.Domain/Properties/AssemblyInfo.cs) | Keep named adapter/test access; not aggregate enforcement. |
| [Travel/GameDifficulty.cs](../../../src/WildBunch.Domain/Travel/GameDifficulty.cs) | Keep difficulty input; invalid enum fallback relates to AP-04. |
| [Travel/GameEntropy.cs](../../../src/WildBunch.Domain/Travel/GameEntropy.cs) | Keep entropy input. |
| [Travel/JourneyEncounterModels.cs](../../../src/WildBunch.Domain/Travel/JourneyEncounterModels.cs) | Keep encounter/profile/hidden state; legacy repair DN-16. |
| [Travel/JourneyEncounterResolutionEngine.cs](../../../src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs) | Keep pure decisions; theft DN-04, NPC/heat semantics DN-16. |
| [Travel/JourneyTrailEventModels.cs](../../../src/WildBunch.Domain/Travel/JourneyTrailEventModels.cs) | Keep effect plans; legal and truthful selected effects DN-06/DN-16. |
| [Travel/JourneyUpkeepRules.cs](../../../src/WildBunch.Domain/Travel/JourneyUpkeepRules.cs) | Keep terrain/water/horse upkeep; horse-loss progression DN-05. |
| [Travel/TrailBeatSlotMapper.cs](../../../src/WildBunch.Domain/Travel/TrailBeatSlotMapper.cs) | Keep slot mapping. |
| [Travel/TrailBeatSlotType.cs](../../../src/WildBunch.Domain/Travel/TrailBeatSlotType.cs) | Keep slot vocabulary. |
| [Travel/TrailEventCatalog.cs](../../../src/WildBunch.Domain/Travel/TrailEventCatalog.cs) | Keep authored effect identifiers. |
| [Travel/TravelDayGenerationContext.cs](../../../src/WildBunch.Domain/Travel/TravelDayGenerationContext.cs) | Keep explicit deterministic inputs and history. |
| [Travel/TravelDayPlanFactory.cs](../../../src/WildBunch.Domain/Travel/TravelDayPlanFactory.cs) | Keep forced preparation; validation/default debt AP-04. |
| [Travel/TravelDayPlanGenerator.Context.cs](../../../src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs) | Correct dust legality DN-06, duplicate branch DN-14; NPC question DN-16. |
| [Travel/TravelDiaryDayFactory.cs](../../../src/WildBunch.Domain/Travel/TravelDiaryDayFactory.cs) | Keep day-state composition with explicit derived-state authority. |
| [Travel/TravelDiaryModels.cs](../../../src/WildBunch.Domain/Travel/TravelDiaryModels.cs) | Keep typed diary/baseline values; heat implications DN-16. |
| [Travel/TravelJourney.cs](../../../src/WildBunch.Domain/Travel/TravelJourney.cs) | Correct pacing DN-05/food effects DN-06; exposed mutation DN-09. |
| [Travel/TravelModels.cs](../../../src/WildBunch.Domain/Travel/TravelModels.cs) | Keep behavioral step/resolution/arrival results. |
| [Travel/TravelResolver.cs](../../../src/WildBunch.Domain/Travel/TravelResolver.cs) | Keep preview/profile calculations; shortage rule unresolved DN-16. |
| [Travel/TravelRouteModels.cs](../../../src/WildBunch.Domain/Travel/TravelRouteModels.cs) | Keep route/preview/snapshot values; distance/delay legality DN-05. |
| [Travel/TravelRulesProfile.cs](../../../src/WildBunch.Domain/Travel/TravelRulesProfile.cs) | Keep pressure knobs; retired heat fields DN-16. |
| [Travel/TravelWarningFilter.cs](../../../src/WildBunch.Domain/Travel/TravelWarningFilter.cs) | Keep warning filtering. |
| [WantedPosters/ReadWantedPostersResult.cs](../../../src/WildBunch.Domain/WantedPosters/ReadWantedPostersResult.cs) | Keep explicit success/message/session-change result. |
| [WildBunch.Domain.csproj](../../../src/WildBunch.Domain/WildBunch.Domain.csproj) | Keep framework-independent net10.0 project. |
| [World/BuildingKind.cs](../../../src/WildBunch.Domain/World/BuildingKind.cs) | Keep building navigation vocabulary. |
| [World/BuildingLayoutPalette.cs](../../../src/WildBunch.Domain/World/BuildingLayoutPalette.cs) | Keep encoded palette; reserved values are declared capacity. |
| [World/BuildingPlacement.cs](../../../src/WildBunch.Domain/World/BuildingPlacement.cs) | Keep layout value; permissive bounds/dimensions DN-15. |
| [World/BuildingView.cs](../../../src/WildBunch.Domain/World/BuildingView.cs) | Keep viewing-angle vocabulary. |
| [World/LayoutSalts.cs](../../../src/WildBunch.Domain/World/LayoutSalts.cs) | Keep split inputs; seed/entropy determinism comment omits runtime salt and needs qualification. |
| [World/PathSegment.cs](../../../src/WildBunch.Domain/World/PathSegment.cs) | Keep path value; constructor bypasses factory bounds, DN-15. |
| [World/TownLayout.cs](../../../src/WildBunch.Domain/World/TownLayout.cs) | Keep layout value; mutable grid/shallow collection contract DN-09. |
| [World/TownSourceModels.cs](../../../src/WildBunch.Domain/World/TownSourceModels.cs) | Keep source capability rules; catalog preservation DN-11, refresh distinction DN-14. |
| [World/WorldModels.cs](../../../src/WildBunch.Domain/World/WorldModels.cs) | Keep world graph; custom catalogs DN-11 and permissive input contracts DN-15. |
| [World/WorldSnapshot.cs](../../../src/WildBunch.Domain/World/WorldSnapshot.cs) | Correct custom-catalog loss if supported, DN-11; typed domain snapshot is legitimate. |
