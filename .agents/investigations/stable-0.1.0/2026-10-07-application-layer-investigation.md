# Application layer investigation

**Status:** Static source audit for the interactive stable `0.1.0` investigation. Findings and proposed dispositions do not authorize application remediation. No application code, service or test behavior was changed or exercised.

**Scope:** Every authored file under `src/WildBunch.Application`, including the project file. Generated `bin/` and `obj/` contents are build output rather than authored architecture. Supporting API, Domain and Persistence paths were followed to assess authority, callers and replay. The [stable baseline tracker](2026-10-06-stable-0.1.0-investigation.md) owns agreed remediation scope.

**Test remediation:** [The source-to-test follow-up](2026-10-07-application-test-followup.md) maps each AP finding to existing tests and the behavioral proof needed after correction, including obsolete expectations, useful assertions to retain and genuine coverage gaps.

**Dated disposition, 2026-10-08:** The town-layout override, its prep/start commands and the session RNG lock/clear commands described in this source snapshot were retired from the 0.1.0 candidate. Findings and file inventory rows about those paths are historical evidence, not current implementation recommendations; see the dated outcomes in the [Application test follow-up](2026-10-07-application-test-followup.md) and the [developer control feature record](../../../docs/features.md#dev-001-developer-salt-controls).

## Folder ownership

| Current owner | Assessment | Proposed disposition |
|---|---|---|
| `Abstractions` | Four repository/UoW ports required by application use cases; Persistence implements them. The journal port is a read store, not an independent child mutation repository. | Keep. No generic interface framework or child repository expansion is justified. |
| `Games/Commands` and `Games/Queries` | Intent contracts and orchestration handlers are correctly in Application. Small records and separate handlers are ordinary CQRS, not inherently sprawl. | Keep the broad split. Clarify developer-only prep/start ownership before relocation; common lifecycle capability can legitimately serve both audiences. |
| `Games/Execution` | Aggregate load, setup guard, optimistic retry and persistence sequencing are application concerns. Both player and developer handlers use this owner. | Keep the mechanism; a shared session execution location would better advertise both audiences if the audience distinction is made strict. Avoid duplicating the pipeline. |
| `Games/Exceptions` | Use-case and persistence-port failures are correctly owned by Application. Several are used across player and developer operations. | Keep Application ownership. Align any future shared-session grouping with Execution rather than making developer copies. Correct false comments independently of file moves. |
| `Games/Mapping` | DTO translation and authored player presentation belong here; rule resolution belongs to Domain. Event-derived helpers overlap the root projection owner. | Keep focused mappers/renderers. Assess projection helpers and repeated formatting at their actual behavior owners, not by filename suffix alone. |
| `Games/Models` | Application output contracts and read-model contracts are legitimate. `GameDtos.cs` mixes many independently changing feature contracts. | Group that large DTO bundle by coherent session, inventory, world and travel ownership if decomposition improves maintenance. Do not create a separate folder for every record. |
| `Dev/Commands`, `Dev/Queries`, `Dev/Mapping` | Developer use cases and explicitly hidden diagnostics are legitimate Application responsibilities. Hidden state in developer diagnostics is not itself a player leak. | Keep; fix actual lifecycle, parsing and reachability defects before cosmetic subfolder proliferation. |
| `Dev/Models` | Diagnostic/result DTOs belong in Application, but five HTTP request types and a dual-purpose input/output DTO blur transport ownership. | Move HTTP inputs to API; keep application commands/results here. Separate town-layout mutation input from its versioned query response. |
| `Projections` | Event-derived application read state belongs in this layer. Shared root placement is workable, but full audit has a developer audience and player projections must remain safe. | Preserve explicit audience boundaries and fix derivation defects. Folder moves alone cannot implement authorization or replay integrity. |
| Project file | References only Domain and GameContent; no API, EF or Persistence dependency was found. | Keep inward dependency direction. |

There is no evidence that every file must move to a new vertical-slice tree. The coarse `Games`/`Dev` organization is serviceable; the concrete problems are mixed ownership, overlapping output derivation and behavior that contradicts its comments/contracts. Feature colocation is a possible later layout choice, not an accepted requirement from this audit.

## Findings

### AP-01: Developer preparation is not replay-complete

**Classification:** Confirmed static replay gap, related to the existing ADR and test findings. `Dev/Commands/SetTownLayoutSaltsHandler.cs:31-50` emits `DevLayoutSaltsForced` on a `Prepped` session. `Games/Commands/PrepGameSessionHandler.cs:31-38` creates the snapshot-owned genesis; `Domain/Game/GameSessionEventReplay.cs:29-54` requires a setup/start event, and `Persistence/GameSessions/SessionRebuilder.cs:28` requires `WorldGenerated`. An injected-salts stream has neither. Starting from prep adds a world event but does not supply the missing setup/start genesis. Missing/stale snapshot recovery cannot reconstruct these intermediate states.

**Proposed correction:** Design the complete prepped lifecycle and event genesis so its legal states replay independently. The user has established event authority as the intended rule; the existing snapshot exception describes current debt and must not silently become a new permanent decision. Prove replay before and after injection and start, without feeding the replay already-mutated state. This is developer workflow debt, not proof that a normal player setup stream has the same failure.

### AP-02: Start retries use stale generation inputs

**Classification:** Confirmed static concurrency hazard. `Games/Commands/StartGameSessionHandler.cs:47-70` loads setup and resolves world/case/salt data before `ExecuteWithRetryAsync`; lines 73-77 reuse captured data on every fresh aggregate load. Concurrent preparation changes can therefore be followed by a retry that records a world generated from old inputs. Its placeholder player-name commentary also conflicts with the nullable-unknown setup rule.

**Proposed correction:** Resolve attempt-dependent state inside the retry delegate, while preserving explicitly fixed command intent across attempts. Reconcile the player's actual identity with the setup lifecycle; do not normalize placeholder names into durable truth. A conflict test must reload a distinct changed aggregate and establish the generated result, not merely count retries against the same mutated object.

### AP-03: Domain legality partly resides in handlers

**Classification:** Ownership drift with different confidence by case. `Dev/Commands/SetTownLayoutSaltsHandler.cs:33-39` alone enforces `Prepped`; `GameSession.SetDevLayoutSalts` has no status guard. Move that invariant to the aggregate with the established failure contract. The endpoint currently advertises a client failure but does not translate this exception; verify the response after fixing composition.

`Games/Commands/PurchaseStoreItemHandler.cs:42-79` resolves a canonical domain catalog and checks requested location/availability before passing a trusted offer to `GameSession.Purchase`. Domain `StoreLoop` already owns quantity, funds and inventory legality. The narrower unresolved question is whether offer provenance and location must also be guaranteed by the external aggregate command boundary. No public counterfeit-price exploit is established. Decide between a complete domain intent operation and an explicit trusted-offer contract; do not move legitimate catalog orchestration merely because it is in a handler.

### AP-04: Developer travel input is silently reinterpreted

**Classification:** Confirmed static input/default drift. `Dev/Commands/ForceTravelOverrideHandler.cs:25-37` parses a category but any supplied foe field forces `ForFoe`, even for a request for `Quiet`. Omitted foe fields introduce local gameplay defaults, including a `5m` bribe instead of the difficulty policy. Numeric text accepted by `Enum.Parse` can also create undefined categories because the domain construction path does not validate them.

**Proposed correction:** Reject contradictory category/profile inputs, validate defined categories at the domain boundary, and use domain-owned defaults or require explicit complete profiles. Preserve lawful branch preparation followed by normal gameplay consumption. Cover expected rejection and the partial-profile difficulty behavior.

### AP-05: Implemented handlers are disconnected from HTTP composition

**Classification:** Confirmed static reachability gap, supporting the API audit. `Api/DependencyInjection.cs` does not register the three town-layout handlers or prep/start handlers requested by mapped developer endpoints. RNG lock/clear handlers are registered, but their routes and methods are commented out with a TODO claiming implementation is missing.

**Proposed correction:** Decide which developer capabilities belong in the baseline, then wire and exercise them or retire dormant surfaces with truthful history. Handler unit tests do not establish endpoint reachability. Do not restore developer routes in production while implementing preprod composition.

### AP-06: Random generator has a false session contract

**Classification:** Confirmed static contract mismatch and an unresolved generation choice. `Dev/Commands/GenerateRandomTownLayoutSaltsCommand.cs:6` carries a game ID that the handler ignores. The session-scoped endpoint advertises not-found behavior that this implementation cannot provide. It also returns the same salt for four independent controls.

**Proposed correction:** Choose a stateless generator contract or validate the session and applicable phase. Decide whether correlated salts are intended before changing generation. Random candidate generation without mutation is a utility use case; calling it a command is not by itself a CQRS violation requiring it to become a cached query.

### AP-07: Queries assume world-backed setup

**Classification:** Confirmed static precondition gap, currently developer-prep dependent. `Games/Queries/GetStartingTownMapHandler.cs:22-42` and `GetTownStoreOffersHandler.cs:30-39` dereference the world, while `StartPrepped` has no world. Their endpoints translate not-found errors rather than this phase failure.

**Proposed correction:** Define prepped-state rejection without preventing world-backed player setup from displaying its starting map. Aggregate-backed queries are not automatically invalid CQRS; assess audience, cost and semantics rather than mandate a new read store for every query.

### AP-08: Start idempotency ignores conflicting intent

**Classification:** Confirmed static behavior requiring a contract decision. `Games/Commands/CompleteGameStartHandler.cs:46-50` returns an already-started session regardless of requested town; `GameSession.SelectStartingTown` likewise accepts a later different selection as a no-op.

**Proposed correction:** Distinguish an identical retry from a conflicting town request, or explicitly document first-selection-wins behavior if accepted. Use a defined failure/conflict response rather than silently reporting the conflicting choice as applied.

### AP-09: Setup archival is global and its comment overclaims

**Classification:** Existing single-player shape, public-playability feature gap and misleading comment. `Games/Commands/CompletePlayerSetupHandler.cs:12-15` says all non-archived sessions are archived; lines 47-55 select only `Active`. The repository query is global, with no player ownership. Atomic per-request commits do not alone serialize simultaneous read-and-create requests.

**Proposed correction:** Correct the current scope claim and carry player-owned active-playthrough selection plus concurrent creation enforcement into the agreed accounts/isolation feature. Do not describe missing accounts as an accidental slop regression or implement a global uniqueness constraint that preserves the wrong multi-user rule.

### AP-10: Architecture comments describe nonexistent methods

**Classification:** Confirmed documentation slop inside source. `Games/Execution/GameSessionCommandHandler.cs:13-15` refers to an `ExecuteAsync` subclass method that does not exist and says subclasses load the session although the base pipeline does. `Games/Exceptions/SetupPhaseException.cs:7-11` claims `GameSession.ThrowIfSetupPhase` owns the guard; no such method exists. The actual Application guard reads the domain's `IsSetupPhase` property.

**Proposed correction:** Make comments describe the real extension point and current authority boundary. If the desired invariant placement differs from implementation, record and resolve that discrepancy rather than rewriting comments to bless it. Keep the useful common pipeline and repository load helper.

### AP-11: Developer diagnostics and version metadata drift

**Classification:** Static diagnostic/maintenance drift. `Dev/Mapping/SaloonDevContextMapper.cs:137-145` explains released culprit eligibility and then describes a non-culprit-only candidate pool. Town-layout query/generator code and DTO/domain defaults repeat `1.0.0` rather than reading a single resolver authority.

**Proposed correction:** Keep diagnostic prose consistent with the existing domain eligibility policy and take resolver metadata from its actual owner. A hardcoded initial version is not independently a gameplay defect; the problem is multiple authorities that can disagree.

### AP-12: Setup projections cannot represent unknown state

**Classification:** Confirmed static projection-contract gap. `Projections/HudProjector.cs:21-28` initializes zero health, an empty player name and a default town; `DiaryProjector.cs:19-24` also invents a default location. Their contracts require non-null town IDs. Player setup responses attach these projections before `GameStarted`, and projection endpoints accept existing setup sessions. The aggregate explicitly preserves a null current town until start. `Active` status alone does not mean a game has started, so the defect is the absent phase/unknown-state representation rather than that enum alone.

**Proposed correction:** Represent setup explicitly, with nullable unknown fields or absent phase-inapplicable projections. Do not manufacture a town, health or identity before an event establishes them. Prove the real player setup response and projection routes, including a legitimate world-backed setup session.

### AP-13: Public classification lacks an explicit knowledge boundary

**Classification:** Confirmed derivation, unresolved player-knowledge policy. `Games/Mapping/WantedPosterMapper.cs:74-77` labels a warrant target gang-affiliated based on `WarrantTerms.GangAffiliations`, metadata copied from generation profiles alongside internal target kind/pressure routing. The browser renders this label. The fact that a warrant is known does not establish whether every affiliation on its internal model is a public fact; the reviewed source does not encode that distinction.

**Proposed correction:** Establish whether gang affiliation is deliberately public warrant information. If it is, express that public fact in the warrant contract; otherwise omit or derive the label from authored public information. Do not report this as a proven culprit-identity leak or remove intentional public lore without deciding the knowledge policy.

### AP-14: Audit occurrence times are generated during reading

**Classification:** Confirmed static truth/determinism defect. `Projections/FullAuditProjector.cs:23-33` assigns `DateTime.UtcNow` to each historical event, and `Dev/Queries/GetSessionAuditHandler.cs:27-34` returns it as `OccurredAtUtc`. Re-reading identical history changes its apparent occurrence times. The comment calls this a pure event projection.

**Proposed correction:** Obtain occurrence metadata through an application audit read port implemented from persisted envelopes, keeping envelope/storage details outside Domain, or explicitly label a generated projection time. Domain event timestamp getters do not recover stored occurrence time. Preserve developer-only access to audit information.

### AP-15: Overlapping player histories have divergent semantics

**Classification:** Confirmed static duplication with an unresolved intended output distinction. `DiaryProjector` uses generic purchase copy, includes sheriff settlement and omits additional travel messages; `JournalLogProjector` uses specific purchase copy, omits settlement and includes additional travel messages with event-order handling. Both are active player-facing histories. `DiaryProjector` also says every purchase happened at the general store regardless of the actual vendor.

**Proposed correction:** Define distinct purposes if both histories are intentional, or derive overlapping behavior from one curated owner. Preserve legitimate audience differences. Correct unsupported purchase narration and prove day/turn ordering and additional-message handling rather than forcing every event into every feed.

**Dated disposition, 2026-10-10, row 08:** The baseline's player journal decision settles AP-15 as one semantic player history. The `.57` slice retires `DiaryProjector`, `DiaryProjection` and `/projections/diary`, leaving `JournalLogProjector` and `/journal` as the player-history owner. It transfers the missing sheriff settlement message at its event day/turn and unique saloon citizen-fine/rejected outcomes, retains `RecordLog` selection and avoids the duplicate saloon summary when detailed wanted/settlement events already represent the outcome. HUD, full audit and the detailed `TravelDiaryDay` projection remain separate audience/state outputs. The source, API, and web no longer carry the overlapping player Diary contract; meaningful coverage moves to journal behavior and the persisted `/journal` read test. No compatibility layer is required for local disposable playtests, while the existing migration chain remains intact.

### AP-16: Reference case projection is incomplete and unused in production

**Classification:** Confirmed static reference-only debt. `Projections/CaseFileViewProjector.cs:27-35,66-90` uses supplied seed known clues/warrants, keeps all when there are no reveal events, and filters them once a reveal appears. It never handles generated case events, and accusation stays null. Production source has no caller, although tests exercise it.

**Proposed correction:** Establish whether this reference is still required. Retire it if obsolete, preserving material removal history, or implement coherent initial-known and reveal semantics from authoritative generated state. Do not wire incomplete reference code into production merely to justify its existence.

### AP-17: Case identity is matched by display names

**Classification:** Confirmed mechanism, conditional behavioral risk. `Games/Mapping/CaseBoardMapper.cs:19-21,46-95` and `JournalMapper.cs:35-45` associate captured settlements with warrants using normalized target names; captured evidence is then hidden from the board. Settlements have stable suspect IDs, but warrants do not supply the association needed here. Duplicate/changed display names can misclassify a record. Current generated name uniqueness and the intended captured-evidence presentation require further domain assessment before declaring a live duplicate-name bug.

**Proposed correction:** Give the case model an explicit stable public identity association if these records must join reliably, and specify what captured evidence remains visible. Keep read derivation in Application where appropriate; do not invent a second write authority inside a mapper.

### AP-18: Compatibility and helper surface needs consumer-led cleanup

**Classification:** Concrete response inconsistency and dormant helper debt. `Games/Mapping/GameTurnResultFactory.cs:9-33` has overlapping optional and three-argument overloads; three-argument calls omit top-level travel diary while the nested current session contains it. `GameDtos.cs:30-42` has a JSON-ignored saloon compatibility property with only a test consumer; web types still declare the obsolete JSON field. `BeatNarrationRenderer` is a pass-through with no production caller found. `GameSessionLogProjection` is a one-method wrapper with a real caller.

**Proposed correction:** Settle one consistent turn-response contract and retire unsupported aliases/facades after verifying consumers. Consolidate useful wrappers only if it improves ownership. No lost nested diary or production failure is established merely by the redundant overload or wrapper.

### AP-19: Fallback narration asserts unsupported causes

**Classification:** Confirmed static conditional presentation defect. `Games/Mapping/TravelDiaryTextRenderer.cs:33-36,172-311` invents fallback entries when authoritative entries are empty, including hunger as the cause of a dead horse based only on end state and health-loss narration for a fight even when the recorded health delta is zero. One encounter rendering helper also accepts unused flavor-tracking arguments.

**Proposed correction:** Prefer authoritative authored entries; constrain fallback wording to facts that the diary state establishes. Simplify unused parameters with the renderer's actual consumers. Do not remove all fallback presentation or relocate rendering into gameplay mutation merely because it contains authored copy.

### AP-20: Shared rendering DTO is owned by the developer slice

**Classification:** Confirmed ownership mismatch, not a demonstrated hidden-truth leak. `Games/Models/TownLayoutDto.cs:1,21-22` and `Games/Mapping/TownLayoutMapper.cs:34-41` use `Dev.Models.TownLayoutSaltsDto` in the ordinary world response. The browser consumes the salts to render deterministic scenery, so these fields are not simply unused developer detail.

**Proposed correction:** Put the shared layout/rendering output contract under its actual world/layout owner, and give developer mutation its own HTTP input. Preserve deterministic browser rendering. The existing diagnostic result type, public render metadata and mutation body need explicit ownership rather than an automatic move of all salt data behind developer routes.

The travel-day projector's silent missing-baseline return also warrants fail-closed assessment for malformed histories; no valid live history losing a day was demonstrated here. The projection interface comment's fixed four-type taxonomy omits the existing travel-day projection and should defer to live implementations rather than preserve a stale inventory.

## File dispositions

The following tables preserve the exhaustive assessment. `Keep` means justified placement, not a claim that the implementation is defect-free. `Investigate` marks a specific behavior or ownership question. Moves and consolidation remain proposed until agreed.

| File | Disposition | Assessment |
|---|---|---|
| `src/WildBunch.Application/Abstractions/IGameJournalReadRepository.cs` | Keep | Journal read-store port; not a child mutation repository. |
| `src/WildBunch.Application/Abstractions/IGameSessionReadRepository.cs` | Keep | Session read-store port. |
| `src/WildBunch.Application/Abstractions/IGameSessionRepository.cs` | Keep | Aggregate/event stream port; global status lookup must evolve with player ownership, AP-09. |
| `src/WildBunch.Application/Abstractions/IGameSessionUnitOfWork.cs` | Keep | Minimal application commit port. |
| `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceCommand.cs` | Keep | Application command intent. |
| `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceHandler.cs` | Keep | Aggregate command orchestration; currently no HTTP route. |
| `src/WildBunch.Application/Dev/Commands/ClearSaloonOverrideCommand.cs` | Keep | Application command intent. |
| `src/WildBunch.Application/Dev/Commands/ClearSaloonOverrideHandler.cs` | Keep | Aggregate command orchestration. |
| `src/WildBunch.Application/Dev/Commands/ClearTravelOverrideCommand.cs` | Keep | Application command intent. |
| `src/WildBunch.Application/Dev/Commands/ClearTravelOverrideHandler.cs` | Keep | Aggregate command orchestration. |
| `src/WildBunch.Application/Dev/Commands/ForceDevDifficultyCommand.cs` | Keep | Application command intent using a domain value. |
| `src/WildBunch.Application/Dev/Commands/ForceDevDifficultyHandler.cs` | Keep | Aggregate command orchestration. |
| `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceCommand.cs` | Keep | Application command intent. |
| `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceHandler.cs` | Keep | Aggregate orchestration with one generated salt retained across retries; currently no HTTP route. |
| `src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideCommand.cs` | Keep | Application command intent; request interpretation needs correction. |
| `src/WildBunch.Application/Dev/Commands/ForceSaloonOverrideHandler.cs` | Keep | Translates command to domain override; candidate legality is checked in Domain. |
| `src/WildBunch.Application/Dev/Commands/ForceTravelOverrideCommand.cs` | Keep | Application command intent; request interpretation needs correction. |
| `src/WildBunch.Application/Dev/Commands/ForceTravelOverrideHandler.cs` | Keep | Aggregate orchestration; coercion and enum validation need correction. |
| `src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsCommand.cs` | Investigate | Carries a session ID that handler ignores; decide whether generation is session scoped. |
| `src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsHandler.cs` | Investigate | Stateless salt generation sits under Commands but creates no mutation, uses no session ID, and returns one salt for four controls. |
| `src/WildBunch.Application/Dev/Commands/SetDevEntropyCommand.cs` | Keep | Application command intent. |
| `src/WildBunch.Application/Dev/Commands/SetDevEntropyHandler.cs` | Keep | Aggregate command orchestration. |
| `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsCommand.cs` | Keep | Application command intent for setup. |
| `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsHandler.cs` | Keep | Application orchestration, though it currently owns a domain lifecycle invariant. |
| `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs` | Keep | Dev-only aggregate-to-DTO mapping; one narrative statement is inaccurate. |
| `src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs` | Keep | Dev-only aggregate-to-DTO mapping. |
| `src/WildBunch.Application/Dev/Mapping/TravelDevContextMapper.cs` | Keep | Dev-only aggregate-to-DTO mapping. |
| `src/WildBunch.Application/Dev/Models/ForceDevDifficultyRequestDto.cs` | Move | HTTP request body belongs in API transport, while command stays in Application. |
| `src/WildBunch.Application/Dev/Models/ForceSaloonOverrideRequestDto.cs` | Move | HTTP request body belongs in API transport. |
| `src/WildBunch.Application/Dev/Models/ForceTravelOverrideRequestDto.cs` | Move | HTTP request body belongs in API transport. |
| `src/WildBunch.Application/Dev/Models/LockRngRequestDto.cs` | Move | HTTP request body belongs in API transport; corresponding route is disabled. |
| `src/WildBunch.Application/Dev/Models/SaloonDevContextDto.cs` | Keep | Dev-only application output contract, including explicitly hidden truth. |
| `src/WildBunch.Application/Dev/Models/SessionAuditDto.cs` | Keep | Dev-only application audit output contract. |
| `src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs` | Keep | Dev-only application output contract. |
| `src/WildBunch.Application/Dev/Models/SetDevEntropyRequestDto.cs` | Move | `JsonRequired` is a transport serialization concern in Application. |
| `src/WildBunch.Application/Dev/Models/TownLayoutSaltsDto.cs` | Investigate | Used both as dev query result and API mutation request, mixing output version with input salts. |
| `src/WildBunch.Application/Dev/Models/TravelDevContextDto.cs` | Keep | Dev-only application output contract. |
| `src/WildBunch.Application/Dev/Queries/GetSaloonDevContextHandler.cs` | Keep | Developer-only query mapping. |
| `src/WildBunch.Application/Dev/Queries/GetSaloonDevContextQuery.cs` | Keep | Application query intent. |
| `src/WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs` | Keep | Developer-only audit projection from event stream. |
| `src/WildBunch.Application/Dev/Queries/GetSessionAuditQuery.cs` | Keep | Application query intent. |
| `src/WildBunch.Application/Dev/Queries/GetSessionDevContextHandler.cs` | Keep | Developer-only query mapping. |
| `src/WildBunch.Application/Dev/Queries/GetSessionDevContextQuery.cs` | Keep | Application query intent. |
| `src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsHandler.cs` | Keep | Developer-only query, with duplicated version literal. |
| `src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsQuery.cs` | Keep | Application query intent. |
| `src/WildBunch.Application/Dev/Queries/GetTravelDevContextHandler.cs` | Keep | Developer-only query mapping. |
| `src/WildBunch.Application/Dev/Queries/GetTravelDevContextQuery.cs` | Keep | Application query intent. |
| `src/WildBunch.Application/Games/Commands/AcknowledgeJourneyArrivalCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/AcknowledgeJourneyArrivalHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/AdvanceTravelDayCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/AdvanceTravelDayHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ArchivePlaythroughCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ArchivePlaythroughHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/CheckSheriffRecordsCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/CheckSheriffRecordsHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/CompleteGameStartCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/CompleteGameStartHandler.cs` | investigate | Concrete finding above; retain folder ownership while correcting behavior. |
| `src/WildBunch.Application/Games/Commands/CompletePlayerSetupCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs` | investigate | Concrete finding above; retain folder ownership while correcting behavior. |
| `src/WildBunch.Application/Games/Commands/ConfrontSaloonPersonOfInterestCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ConfrontSaloonPersonOfInterestHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ConfrontSaloonWantedSuspectCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ConfrontSaloonWantedSuspectHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ConfrontWantedSuspectCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ConfrontWantedSuspectHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/FollowTelegraphLeadsCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/FollowTelegraphLeadsHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/GatherLocalGossipCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/GatherLocalGossipHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/InspectNoticeBoardCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/InspectNoticeBoardHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/LookAroundSaloonCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/LookAroundSaloonHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/PrepGameSessionCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/PrepGameSessionHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/PurchaseStoreItemCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs` | investigate | Concrete finding above; retain folder ownership while correcting behavior. |
| `src/WildBunch.Application/Games/Commands/ReadWantedPostersCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ReadWantedPostersHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ResolveJourneyEncounterCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ResolveJourneyEncounterHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/StartGameSessionCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs` | investigate | Concrete finding above; retain folder ownership while correcting behavior. |
| `src/WildBunch.Application/Games/Commands/TravelToTownCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/TravelToTownHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/TurnInToSheriffCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/TurnInToSheriffHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ViewPrologueCommand.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Commands/ViewPrologueHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Exceptions/ConcurrencyException.cs` | Keep | Persistence-port conflict contract used by retry orchestration. |
| `src/WildBunch.Application/Games/Exceptions/GameSessionNotFoundException.cs` | Keep | Use-case failure contract shared across audiences. |
| `src/WildBunch.Application/Games/Exceptions/SetupPhaseException.cs` | Investigate | Appropriate application failure type; false invariant-owner comment, AP-10. |
| `src/WildBunch.Application/Games/Exceptions/TownNotFoundException.cs` | Keep | Application lookup failure contract. |
| `src/WildBunch.Application/Games/Execution/GameSessionCommandHandler.cs` | Investigate | Valid common orchestration owner; stale extension-point comments, AP-10; retain meaningful retry proof. |
| `src/WildBunch.Application/Games/Execution/GameSessionRepositoryExtensions.cs` | Keep | Small shared required-session loading helper. |
| `src/WildBunch.Application/Games/Mapping/AvailableActionMapper.cs` | Keep | Thin domain-to-DTO translation. |
| `src/WildBunch.Application/Games/Mapping/BeatLabelRenderer.cs` | Keep | Player label formatting consumed by case/journal mapping. |
| `src/WildBunch.Application/Games/Mapping/BeatNarrationRenderer.cs` | Investigate | Unused pass-through façade; verify external/tests before retirement. |
| `src/WildBunch.Application/Games/Mapping/CaseBoardMapper.cs` | Investigate | Player case-board derivation belongs here, but identity/capture matching needs stable authority. |
| `src/WildBunch.Application/Games/Mapping/CaseReadMapper.cs` | Keep | Safe clue formatting and trail status wording; check status audience if case policy changes. |
| `src/WildBunch.Application/Games/Mapping/GameSessionLogProjection.cs` | Consolidate | One-method wrapper around `JournalLogProjector` with a production caller; move call to an owned journal read helper if simplifying. |
| `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` | Keep | Central session response mapping, with duplicate case/journal view concerns noted. |
| `src/WildBunch.Application/Games/Mapping/GameTurnResultFactory.cs` | Consolidate | Resolve overload response-shape ambiguity while preserving actual callers. |
| `src/WildBunch.Application/Games/Mapping/InventoryMapper.cs` | Keep | Maps owned inventory capability results, no gameplay legality computed here. |
| `src/WildBunch.Application/Games/Mapping/JournalMapper.cs` | Investigate | Captured-warrant filtering by display name and duplicated case-board construction. |
| `src/WildBunch.Application/Games/Mapping/StoreCatalogMapper.cs` | Keep | Straight catalog projection. |
| `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs` | Keep | Domain layout-to-DTO translation; player payload includes developer layout salts, see note below. |
| `src/WildBunch.Application/Games/Mapping/TrailBeatSlotProjection.cs` | Keep | Presentation categorization of authoritative day state. |
| `src/WildBunch.Application/Games/Mapping/TravelDiaryMapper.cs` | Keep | Focused travel-day DTO mapping. |
| `src/WildBunch.Application/Games/Mapping/TravelDiaryTextRenderer.cs` | Investigate | Large authored presentation renderer with unsupported fallback assertions. |
| `src/WildBunch.Application/Games/Mapping/TravelMapper.cs` | Keep | Focused travel DTO mapping; default rules arguments should be checked at each caller. |
| `src/WildBunch.Application/Games/Mapping/WantedPosterMapper.cs` | Investigate | Public classification currently inferred from internal gang metadata. |
| `src/WildBunch.Application/Games/Models/ArchivePlaythroughResultDto.cs` | Keep | Lifecycle response shape. |
| `src/WildBunch.Application/Games/Models/AvailableActionDto.cs` | Keep | Small action contract. |
| `src/WildBunch.Application/Games/Models/BuildingPlacementDto.cs` | Keep | Town layout child contract. |
| `src/WildBunch.Application/Games/Models/CaseReadDtos.cs` | Keep | Coherent case-board read contracts, despite size. |
| `src/WildBunch.Application/Games/Models/GameDtos.cs` | Consolidate | 312-line mixed session, inventory, world, case, travel, diary, and result bucket. Split by consumer feature boundary, not one file per record. |
| `src/WildBunch.Application/Games/Models/GameSessionReadModel.cs` | Keep | Read-store handoff type; domain objects remain behind Application mapping. |
| `src/WildBunch.Application/Games/Models/InvestigationActionResultDto.cs` | Keep | Investigation result. |
| `src/WildBunch.Application/Games/Models/JournalDto.cs` | Keep | Coherent journal response family. |
| `src/WildBunch.Application/Games/Models/PathSegmentDto.cs` | Keep | Town layout child contract. |
| `src/WildBunch.Application/Games/Models/PrologueDto.cs` | Keep | Explicit player-safe prologue contract. |
| `src/WildBunch.Application/Games/Models/SaloonPersonOfInterestConfrontationResultDto.cs` | Keep | Specific outcome response. |
| `src/WildBunch.Application/Games/Models/SheriffTurnInResultDto.cs` | Keep | Specific outcome response. |
| `src/WildBunch.Application/Games/Models/StartingTownDto.cs` | Keep | Setup choice shape. |
| `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs` | Keep | Setup map response family. |
| `src/WildBunch.Application/Games/Models/StoreDtos.cs` | Keep | Coherent store response family. |
| `src/WildBunch.Application/Games/Models/TownLayoutDto.cs` | Investigate | Player DTO imports `Dev.Models` salts, mixing dev setup detail into normal world response. |
| `src/WildBunch.Application/Games/Models/WantedPosterDtos.cs` | Keep | Coherent wanted-poster response family. |
| `src/WildBunch.Application/Games/Models/WantedSuspectConfrontationResultDto.cs` | Keep | Specific outcome response. |
| `src/WildBunch.Application/Games/Queries/GetAvailableActionsHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetAvailableActionsQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetGameSessionHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetGameSessionQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetJournalHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetJournalQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetPrologueHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetPrologueQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetStartingTownMapHandler.cs` | investigate | Concrete finding above; retain folder ownership while correcting behavior. |
| `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetStartingTownsHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetStartingTownsQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/GetTownStoreOffersHandler.cs` | investigate | Concrete finding above; retain folder ownership while correcting behavior. |
| `src/WildBunch.Application/Games/Queries/GetTownStoreOffersQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/PreviewTravelHandler.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Games/Queries/PreviewTravelQuery.cs` | keep | Cohesive command/query contract or orchestration in its current folder. |
| `src/WildBunch.Application/Projections/CaseFileViewProjection.cs` | Investigate | Domain-object projection type has no production consumer. |
| `src/WildBunch.Application/Projections/CaseFileViewProjector.cs` | Investigate | Reference-only, incomplete case reconstruction. |
| `src/WildBunch.Application/Projections/DiaryProjection.cs` | Consolidate | Retired with the `.57` single-journal slice; no player consumer remains. |
| `src/WildBunch.Application/Projections/DiaryProjector.cs` | Consolidate | Retired with the `.57` single-journal slice; its unique player facts and useful tests move to the journal owner. |
| `src/WildBunch.Application/Projections/FullAuditProjection.cs` | Keep | Developer-only audit result type. |
| `src/WildBunch.Application/Projections/FullAuditProjector.cs` | Investigate | Fabricated occurrence timestamps on live dev audit route. |
| `src/WildBunch.Application/Projections/HudProjection.cs` | Investigate | Setup fields cannot represent absent player/town. |
| `src/WildBunch.Application/Projections/HudProjector.cs` | Investigate | Setup defaults exposed through player API. |
| `src/WildBunch.Application/Projections/IDomainEventProjector.cs` | Keep | Minimal projection boundary, though comment's fixed four-type taxonomy is stale because travel-day projection exists. |
| `src/WildBunch.Application/Projections/JournalLogProjector.cs` | Keep | Live event-derived journal log used by persistence read store and session mapper. |
| `src/WildBunch.Application/Projections/TravelDiaryDayProjection.cs` | Keep | Replayable travel-day result. |
| `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs` | Keep | Used by persistence rebuild; `CreateAndStoreDiaryDay` silently returning when baseline is absent (`:217-219`) merits a fail-closed review for malformed streams. |
| `src/WildBunch.Application/WildBunch.Application.csproj` | Keep | Inward Domain/GameContent references; no adapter dependency. |
