# GameSession Decomposition Audit

> Capstone audit for BUNCH-121. Source snapshot: `99970d8` (origin/main, 2026-07-01).
> This is a committed artifact, not a Linear comment. Update it when the
> decomposition posture changes materially.

## Posture

`GameSession` remains the live-play aggregate root, command entry point,
event-production boundary, apply-dispatch owner, and persistence boundary
(ADR-0002, ADR-0020, ADR-0028). It may orchestrate cross-component behavior.
It should not directly accumulate all game rules. New cohesive gameplay loops
should become internal child domain components when they own state plus rules,
have a clear event family or state family, and can receive narrow context
records.

## Child components

| Child component | File | Lines | Owns | Issue |
| --- | --- | --- | --- | --- |
| `BountyLoop` | `src/WildBunch.Domain/Game/BountyLoop.cs` | 910 | Wanted-suspect presence ledger, unrelated-criminal parity ledger, saloon POI confrontation decision logic, dev saloon override state | BUNCH-112 |
| `JourneyLoop` | `src/WildBunch.Domain/Game/JourneyLoop.cs` | 1390 | Travel/journey state, travel-diary days, completed journey history, journey command decision logic, dev travel override state | BUNCH-119 |
| `InvestigationLoop` | `src/WildBunch.Domain/Game/InvestigationLoop.cs` | 337 | Stateless investigation source resolution and clue/warrant surfacing decision logic | BUNCH-120 |
| `StoreLoop` | `src/WildBunch.Domain/Game/StoreLoop.cs` | 105 | Stateless store purchase decision logic | BUNCH-120 |
| `ActionContextTracker` | `src/WildBunch.Domain/Game/ActionContextTracker.cs` | 113 | Town action-context state and turn-advancement tracking | BUNCH-120 |

All five child components conform to the lawful child boundary:
- `internal sealed`, living under `src/WildBunch.Domain/Game/`.
- Receive narrow context records (e.g. `SaloonConfrontationContext`, `StartJourneyContext`, `InvestigationContext`, `StorePurchaseContext`, `ActionContextEnterInputs`), not the parent aggregate.
- Return results plus events-to-produce (e.g. `SaloonConfrontationOutcome`, `JourneyLoopResult<T>`, `InvestigationOutcome`, `StorePurchaseOutcome`, `TownActionContextEntered?`). They do NOT produce events directly.
- Do NOT reference `GameSession`.
- Do NOT call `EnterActionContext` (they are not the action-context owner).
- Do NOT mutate owners they do not own (CaseFile, TownVisitState, Player, Clock, PursuitState).
- Do NOT own infrastructure or persistence.
- Owned state is restored during snapshot rehydration via `Restore*` helpers on `GameSession` that delegate to the child (`RestoreBountyLoopState`, `RestorePendingDevTravelOverride`, `RestoreActionContextState`).

## Remaining GameSession public/internal methods by responsibility

### 1. Session lifecycle

| Member | Line range | Notes |
| --- | --- | --- |
| `GameSession(...)` constructor | 42–89 | Acceptable orchestration — owns session-level construction and child-component wiring. |
| `StartSetup` (static) | 816–874 | Acceptable — session lifecycle entry point. |
| `CompleteGameStart` | 876–912 | Acceptable — session lifecycle transition. |
| `StartNew(string, ...)` (static) | 914 | Acceptable — delegates to the overload. |
| `StartNew(string, world, caseFile, ...)` (static) | 917–992 | Acceptable — session lifecycle entry point. |
| `ViewPrologue` | 994–1020 | Acceptable — session lifecycle transition. |
| `ArchivePlaythrough` | 1031–1053 | Acceptable — session lifecycle terminal transition. |
| `Status` | 133 | Read-only state. |
| `StartFlowPhase` | 140 | Read-only state. |
| `RehydrateFromEvents` (static, partial) | `GameSessionEventReplay.cs:30–100` | Acceptable — event-sourcing rehydration factory. |
| `ApplyCommittedEvents` (partial) | `GameSessionEventReplay.cs:205–212` | Acceptable — event-sourcing replay helper. |

### 2. Event-sourcing infrastructure

| Member | Line range | Notes |
| --- | --- | --- |
| `ProduceEvent<T>` | 324–328 | Acceptable — canonical event-sourcing produce step (Apply + record). |
| `ApplyProducedEvent` | 335–420 | Acceptable — command-path event dispatcher; mirrors the replay dispatcher. |
| `Apply(...)` overloads | 428–1172 | Acceptable — the single mutation path (ADR-0028). Each `Apply` delegates owned-state mutation to the relevant child component and applies cross-owner mutations (Clock, PursuitState, Player, CaseFile) directly. |
| `SetCommittedEvents` | 232–237 | Acceptable — repository load-path helper. |
| `MarkEventsCommitted` | 251–255 | Acceptable — post-commit transfer helper. |
| `UncommittedEvents` | 199 | Read-only state. |
| `CommittedEvents` | 205 | Read-only state. |
| `AllEvents` | 212–226 | Read-only state (projection source). |
| `Version` | 242 | Read-only state. |

### 3. Cross-component orchestration

| Member | Line range | Notes |
| --- | --- | --- |
| `EnterActionContext` | 284–295 | Acceptable — delegates the decision to `ActionContextTracker`, produces the event, and owns the cross-owner Apply (Clock.Set, PursuitState.SetHeat). |
| `CanConfrontWantedSuspectInCurrentContext` | 312–317 | Acceptable — delegates to `ActionContextTracker`, reads `CurrentTownVisit` for the active POI id. |
| `RefreshTownVisit` | 1382–1398 | Acceptable — session-level town-change orchestration. |
| `ResetActionContextForTownChange` | 1400 | Acceptable — delegates to `ActionContextTracker.Reset()`. |
| `RefillCanteenAfterArrival` | 1402–1412 | Acceptable — session-level post-arrival orchestration. |
| `CreateTravelDayGenerationContext` | 1430–1494 | Acceptable — builds the narrow context record for `JourneyLoop` from session-level state (Player capabilities, travel rules, salt, entropy, clock, pursuit). |
| `CreateFoodPressureBand` (static) | 1496–1519 | Future extraction candidate — pure helper, could move to `JourneyLoop` or a travel-pressure helper. Not extracted now (scope discipline). |
| `CreateCanteenPressureBand` (static) | 1521–1558 | Future extraction candidate — same rationale. |
| `CreateHorseFeedPressureBand` (static) | 1560–1592 | Future extraction candidate — same rationale. |
| `CreateHorseConditionBand` (static) | 1594–1617 | Future extraction candidate — same rationale. |
| `CreateWalletBand` (static) | 1619–1642 | Future extraction candidate — same rationale. |
| `SyncPlayerFromJourneySnapshot` | 587–617 | Acceptable — converges command and replay paths for player state from the journey snapshot. |
| `IsJourneyModal` | 2448–2449 | Acceptable — session-level guard. |
| `IsArchived` | 2451 | Acceptable — session-level guard. |

### 4. Dev overrides

| Member | Line range | Notes |
| --- | --- | --- |
| `ForceDevTravelOverride` | 1247–1260 | Acceptable — delegates to `JourneyLoop`, produces the event. |
| `ClearDevTravelOverride` | 1262–1275 | Acceptable — delegates to `JourneyLoop`. |
| `ForceDevSaloonOverride` | 1277–1301 | Acceptable — delegates to `BountyLoop`. |
| `ClearDevSaloonOverride` | 1303–1320 | Acceptable — delegates to `BountyLoop`. |
| `ForceDevSaltSource` | 1322–1337 | Acceptable — session-level dev override. |
| `ClearDevSaltSource` | 1339–1348 | Acceptable — session-level dev override. |
| `ForceDevDifficulty` | 1350–1367 | Acceptable — session-level dev override. |
| `SetDevEntropy` | 1369–1380 | Acceptable — session-level dev override. |
| `PendingDevTravelOverride` | 172 | Read-only delegate to `JourneyLoop`. |
| `PendingDevSaloonOverride` | 178 | Read-only delegate to `BountyLoop`. |
| `RestoreBountyLoopState` | 100–112 | Acceptable — rehydration helper delegating to `BountyLoop`. |
| `RestorePendingDevTravelOverride` | 118–121 | Acceptable — rehydration helper delegating to `JourneyLoop`. |
| `RestoreActionContextState` | 128–131 | Acceptable — rehydration helper delegating to `ActionContextTracker`. |

### 5. Bounty/saloon/sheriff orchestration

| Member | Line range | Notes |
| --- | --- | --- |
| `LookAroundSaloon` | 1798–1838 | Acceptable — builds `SaloonLookAroundContext`, delegates to `BountyLoop`, produces events. |
| `ConfrontSaloonPersonOfInterest` | 1841–1924 | Acceptable — builds `SaloonConfrontationContext`, delegates to `BountyLoop`, orchestrates the settlement request between pre/post events. |
| `ConfrontSaloonWantedSuspect` | 1926–1970 | Acceptable — delegates to `ConfrontSaloonPersonOfInterest` + compatibility result mapping. |
| `ResolveWantedSuspectConfrontation` | 1972–1999 | Acceptable — builds `WantedSuspectConfrontationContext`, delegates to `BountyLoop`. |
| `AssessSheriffTurnIn` | 2001–2021 | Acceptable — builds context for the assessment. |
| `SettleSheriffTurnIn` | 2023–2083 | Acceptable — builds context, delegates to `BountyLoop`, produces events, adjusts wallet. |
| `SettleUnrelatedCriminalTurnIn` | 2085–2150 | Acceptable — builds context, delegates to `BountyLoop`, produces events, adjusts wallet. |
| `ReadWantedPosters` | 1767–1796 | Acceptable — builds `InvestigationContext`, delegates to `InvestigationLoop`. |
| `GetWantedSuspectPresenceState` | 1174–1175 | Read-only delegate to `BountyLoop`. |
| `TryGetWantedSuspectPresenceState` | 1177–1178 | Read-only delegate to `BountyLoop`. |
| `SetWantedSuspectPresenceState` | 1180–1181 | Read-only delegate to `BountyLoop`. |
| `BuildUnrelatedCriminalLedger` (static) | 1702–1732 | Acceptable — session-construction helper that reads CaseFile to build the initial ledger for `BountyLoop`. |
| `IsEligibleSaloonPersonOfInterestCandidate` | 2434–2435 | Read-only delegate to `BountyLoop` (passes `CaseFile.TrueCulpritId` and `KillerReleaseState`). |
| `GetSaloonPoiIneligibilityReason` | 2442–2443 | Read-only delegate to `BountyLoop`. |

### 6. Investigation orchestration

| Member | Line range | Notes |
| --- | --- | --- |
| `FollowTelegraphLeads` | 2152–2188 | Acceptable — builds `InvestigationContext`, delegates to `InvestigationLoop`. |
| `GatherLocalGossip` | 2190–2221 | Acceptable — same pattern. |
| `InspectNoticeBoard` | 2223–2253 | Acceptable — same pattern. |
| `CheckSheriffRecords` | 2255–2285 | Acceptable — same pattern. |

### 7. Store orchestration

| Member | Line range | Notes |
| --- | --- | --- |
| `Purchase` | 1734–1765 | Acceptable — builds `StorePurchaseContext`, delegates to `StoreLoop`, produces the event. |

### 8. Journey orchestration

| Member | Line range | Notes |
| --- | --- | --- |
| `StartJourney` | 1183–1199 | Acceptable — builds `StartJourneyContext`, delegates to `JourneyLoop`. |
| `AdvanceJourneyDay` | 1201–1245 | Acceptable — advances clock, builds `AdvanceJourneyDayContext`, delegates to `JourneyLoop`, produces events, runs post-arrival refill. |
| `AcknowledgeJourneyArrival` | 1414–1428 | Acceptable — builds `AcknowledgeJourneyArrivalContext`, delegates to `JourneyLoop`, refreshes town visit. |
| `ResolveJourneyEncounter(string)` | 1644 | Acceptable — delegates to the overload. |
| `ResolveJourneyEncounter(string, int?, decimal?)` | 1647–1648 | Acceptable — delegates to the internal overload. |
| `ResolveJourneyEncounter` (internal) | 1650–1700 | Acceptable — builds context, delegates to `JourneyLoop`, produces events. |
| `AppendTravelDiaryDay` | 2287–2291 | Read-only delegate to `JourneyLoop`. |
| `UpdateLatestTravelDiaryDay` | 2293–2297 | Read-only delegate to `JourneyLoop`. |
| `ReplaceTravelDiaryDays` | 2356–2361 | Read-only delegate to `JourneyLoop` (rehydration). |

### 9. Read-only state projections on the root

| Member | Line range | Notes |
| --- | --- | --- |
| `Journey` | 152 | Delegate to `JourneyLoop`. |
| `TravelDiaryDays` | 180 | Delegate to `JourneyLoop`. |
| `CompletedJourneyHistory` | 182 | Delegate to `JourneyLoop`. |
| `WantedSuspectPresenceEntries` | 184 | Delegate to `BountyLoop`. |
| `UnrelatedCriminalLedger` | 193 | Delegate to `BountyLoop`. |
| `CurrentActionContext` | 263 | Delegate to `ActionContextTracker`. |
| `CurrentActionContextTownId` | 271 | Delegate to `ActionContextTracker`. |
| `Player` | 142 | Session-level state. |
| `World` | 144 | Session-level state. |
| `CaseFile` | 146 | Session-level state. |
| `PursuitState` | 148 | Session-level state. |
| `Clock` | 150 | Session-level state. |
| `GameDifficulty` | 154 | Session-level state. |
| `GameEntropy` | 156 | Session-level state. |
| `SaltSource` | 158 | Session-level state. |
| `SeedCode` | 160 | Session-level state. |
| `CurrentTown` | 162 | Session-level state. |
| `CurrentTownVisit` | 164 | Session-level state. |
| `TravelRules` | 166 | Session-level state. |
| `Id` | 91 | Session-level state. |

### 10. Warrant/suspect pure helpers

| Member | Line range | Notes |
| --- | --- | --- |
| `MatchesKnownWarrant` (static) | 2299–2310 | **Duplicate** of `BountyLoop.MatchesKnownWarrant` (line 630). Still used by `TryGetKnownWarrantForSuspect` (line 2321). Future extraction candidate — collapse to a shared helper or delegate to `BountyLoop`. Not extracted now (scope discipline; would touch call sites). |
| `TryGetKnownWarrantForSuspect` | 2312–2323 | Acceptable — used by `AssessSheriffTurnIn`/`SettleSheriffTurnIn`. Uses the duplicate `MatchesKnownWarrant`. |
| `DescribeWarrantDisposition` (static) | 2325–2331 | **Duplicate** of `BountyLoop.DescribeWarrantDisposition` (line 643). Still used by `SettleSheriffTurnIn` (lines 2129–2130). Future extraction candidate. |
| `DescribeConfrontationNarration` (static) | 2333–2354 | **Duplicate** of `BountyLoop.DescribeConfrontationNarration` (line 651). Not directly called from `GameSession` in the current snapshot — verify before any removal. Future extraction candidate. |
| `CollectSuspectFeatureDescriptions` | 2363–2370 | Acceptable — reads `CaseFile.Suspects` to build feature descriptions for the saloon look-around context. Stays on `GameSession` because it reads state `BountyLoop` should not own directly. |
| `TryGetEligibleSaloonSuspectCandidate` | 2411–2426 | **Dead code** — defined but never called in production or tests. Removed in BUNCH-121 small safe cleanup. |
| `ResolveSaloonPersonOfInterestCompatibilityResult` (static) | 2445–2446 | Acceptable — used by `ConfrontSaloonWantedSuspect` (line 1968) to map the saloon result to a wanted-suspect result. |
| `SpendFirearmAmmo` | 2453–2484 | Acceptable — mutates `Player` inventory; stays on `GameSession` because it owns the Player mutation boundary. |
| `RetiredWarrantIds` | 2406–2409 | Acceptable — read-only helper that reads `BountyLoop.UnrelatedCriminalLedger` to build the retired/taken-in set for `InvestigationContext`. |
| `StartingHealthFor` (static) | 1055–1062 | Acceptable — session-lifecycle helper. |

## Acceptable orchestration

The methods in buckets 1, 3, 4, and the orchestration entry points in buckets 5–8 legitimately belong on `GameSession` because they:

1. **Coordinate across child components** — e.g. `EnterActionContext` delegates the decision to `ActionContextTracker` but owns the cross-owner `Apply` (Clock.Set, PursuitState.SetHeat); `AdvanceJourneyDay` advances the clock, builds the context for `JourneyLoop`, produces events, and runs post-arrival refill; `ConfrontSaloonPersonOfInterest` orchestrates the settlement request between pre-settlement and post-settlement events.
2. **Own session-level concerns** — clock, pursuit, player, world, case file, status, start-flow phase. These are not owned by any child component.
3. **Produce events through `ProduceEvent`** — the canonical event-sourcing produce step (Apply + record). Child components return events-to-produce; `GameSession` is the only entity that calls `ProduceEvent`.
4. **Dispatch `Apply`** — the single mutation path (ADR-0028). Each `Apply` overload delegates owned-state mutation to the relevant child and applies cross-owner mutations directly.
5. **Own the persistence/rehydration boundary** — `SetCommittedEvents`, `MarkEventsCommitted`, `RestoreBountyLoopState`, `RestorePendingDevTravelOverride`, `RestoreActionContextState`, `RehydrateFromEvents`, `ApplyCommittedEvents`.

This is consistent with ADR-0002 ("`GameSession` owns the live play state, the command methods, and the invariant boundary for session mutation") and ADR-0020 ("the root coordinates persistence or transaction posture for that boundary, but it does not gain authority to mutate or override another aggregate's internal domain rules"). The child components own their internal legality; `GameSession` orchestrates and produces events but does not hand-code child-component rules.

## Future extraction candidates

| Candidate | Cohesive state+rules | Event/state family | Rationale for not extracting now |
| --- | --- | --- | --- |
| Pressure-band helpers (`CreateFoodPressureBand`, `CreateCanteenPressureBand`, `CreateHorseFeedPressureBand`, `CreateHorseConditionBand`, `CreateWalletBand`) | Stateless pure helpers that build `TravelPressureBand` / `HorseConditionBand` / `WalletBand` from player + travel-rules state. | Used to build `AdvanceJourneyDayContext` for `JourneyLoop`. | Scope discipline — BUNCH-121 is a capstone, not a new extraction track. These are pure helpers with no owned state; moving them to `JourneyLoop` or a travel-pressure helper is a reasonable future cleanup but not required for the audit. |
| Duplicate warrant helpers (`MatchesKnownWarrant`, `DescribeWarrantDisposition`, `DescribeConfrontationNarration`) | Stateless pure helpers duplicated on both `GameSession` and `BountyLoop`. | Warrant matching and confrontation narration. | Scope discipline — collapsing the duplicates would touch call sites on both `GameSession` and `BountyLoop` and requires verifying the `GameSession` copies are not called after the `BountyLoop` extraction. A future issue should consolidate these into a shared helper or delegate to `BountyLoop`. |

No other extraction candidates were identified. The remaining `GameSession` methods are acceptable orchestration, session-level state, event-sourcing infrastructure, or read-only delegates to child components.

## Decomposition trajectory

| Commit | Milestone | `GameSession.cs` lines | `GameSessionEventReplay.cs` lines | Child components |
| --- | --- | --- | --- | --- |
| pre-BUNCH-112 | Before any extraction | 3515 | n/a | none |
| `2136584` (BUNCH-112) | BountyLoop extracted | 3556 | n/a | BountyLoop (910) |
| `d783f1f` (BUNCH-119) | JourneyLoop extracted | 2461 | n/a | BountyLoop (910), JourneyLoop (1390) |
| `99970d8` (BUNCH-120) | InvestigationLoop + ActionContextTracker + StoreLoop extracted | 2186 | 200 | BountyLoop (910), JourneyLoop (1390), InvestigationLoop (337), StoreLoop (105), ActionContextTracker (113) |

Note: `GameSession.cs` line count temporarily increased at the BUNCH-112 milestone (3515 → 3556) because the extraction added delegation wiring and context-record construction before the bulk of the bounty-loop logic was moved out. The trajectory then shows the expected decline: 3556 → 2461 (BUNCH-119) → 2186 (BUNCH-120). The `GameSessionEventReplay.cs` partial (200 lines) was introduced during the BUNCH-77 event-sourcing campaign and is not counted in the `GameSession.cs` total.

Total child-component lines at `99970d8`: 910 + 1390 + 337 + 105 + 113 = 2855.
`GameSession.cs` + `GameSessionEventReplay.cs` at `99970d8`: 2186 + 200 = 2386.

## Lawful child boundary

A lawful child component inside the GameSession boundary:
- is `internal sealed` and lives under `src/WildBunch.Domain/Game/`;
- receives narrow context records (not the parent aggregate);
- returns results plus events-to-produce (it does NOT produce events directly);
- does NOT reference `GameSession`;
- does NOT call `EnterActionContext` (it is not the action-context owner);
- does NOT mutate owners it does not own (CaseFile, TownVisitState, Player,
  Clock, PursuitState);
- does NOT own infrastructure or persistence;
- has its owned state restored during snapshot rehydration via a
  `Restore*` helper on `GameSession` that delegates to the child.

## Drift modes this audit must catch

The child-component pattern exists to prevent the drift modes named in
`.agents/unslop/backend-architecture.md`:

- **Aggregate bypass** — gameplay mutation routing around `GameSession` or
  the current aggregate route. The child-component pattern prevents this by
  keeping `GameSession` as the sole event-production and `Apply`-dispatch
  boundary; child components return events-to-produce and never mutate
  cross-owner state directly.
- **Event-sourcing drift** — direct mutation beside event application, or
  generic event envelopes in Domain. The child-component pattern prevents
  this by requiring child components to return typed events-to-produce
  rather than mutating state or producing events themselves; `GameSession`
  calls `ProduceEvent` (Apply + record) as the single mutation path.
- **Repository proliferation** — repositories for aggregate children. The
  child-component pattern prevents this by keeping child components as
  internal domain components with no independent persistence identity;
  `GameSession` owns the single repository route.
- **Generic-backend-noun drift** — replacing Wild Bunch domain language
  with generic abstractions. The child-component pattern prevents this by
  naming components after their cohesive gameplay loop (BountyLoop,
  JourneyLoop, InvestigationLoop, StoreLoop, ActionContextTracker), not
  generic nouns like `Supplies` or `Stats`.
- **Architecture-name compliance theater** — using DDD/CQRS/Event-Sourcing
  vocabulary while violating the selected architecture responsibilities.
  The child-component pattern prevents this by enforcing the lawful
  boundary rules above as a concrete reviewable assertion: a child
  component that references `GameSession`, produces events directly, calls
  `EnterActionContext`, mutates cross-owner state, or owns infrastructure
  is a review failure.

## Stale guidance cleanup

BUNCH-67/68/72-era references were classified as follows:

- **ADR-0002** (`docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`):
  no BUNCH-67/68/72 references in the ADR body. Updated in BUNCH-121 to cite
  the audit report and the concrete child-component pattern.
- **ADR-0028** (`docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`):
  6 references to BUNCH-67 at lines 27, 57, 124, 140, 161, 202. All are
  historical reasoning inside the ADR (class (a)). Updated in BUNCH-121 with
  a historical note marking BUNCH-67/68/72 as superseded by the concrete
  child-component extraction pattern. The event-sourcing posture is
  unchanged.
- **Plan files under `.agents/superpowers/plans/`** (BUNCH-77, BUNCH-72):
  historical execution records (class (c)). Left unchanged — they document
  what was planned at the time.

No class (b) references (operative agent law that is now misleading) were
found outside the ADRs already handled.

## Small safe cleanup

- **Removed:** `TryGetEligibleSaloonSuspectCandidate` (lines 2411–2426 in
  `GameSession.cs`) — defined but never called in production code or tests.
  Dead code. Removal is clearly safe: no caller, no behavior change, no
  public API/DTO/event-payload/snapshot-shape change.
- **Removed:** `_pendingDevTravelOverride` field (line 36 in
  `GameSession.cs`) — declared but never used. Leftover from the BUNCH-119
  JourneyLoop extraction (the override state moved to `JourneyLoop`).
  Removal is clearly safe: no reference after the extraction, no behavior
  change, no shape change. The compiler confirmed the field was unused
  (`warning CS0169: The field 'GameSession._pendingDevTravelOverride' is
  never used`); the warning is gone after removal.
- **Not removed (documented as future extraction candidates):** the
  duplicate warrant helpers (`MatchesKnownWarrant`,
  `DescribeWarrantDisposition`, `DescribeConfrontationNarration`) on
  `GameSession`. These are duplicates of `BountyLoop` helpers but are still
  called from `GameSession` code. Collapsing them would touch call sites
  and is not "clearly safe dead code removal" — it is a future extraction
  candidate tracked in the "Future extraction candidates" section above.
