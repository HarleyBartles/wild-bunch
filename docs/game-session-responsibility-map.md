# GameSession Responsibility Map

## Purpose

This document maps the current responsibility slices carried by `GameSession` on the live `main` branch. It is a source-backed reference for later extraction work and does not change gameplay behavior.

## Source Surfaces Inspected

- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Domain/Game/Player.cs`
- `src/WildBunch.Domain/Game/TownAggregate.cs`
- `src/WildBunch.Domain/Game/TownVisitState.cs`
- `src/WildBunch.Domain/Game/WantedSuspectPresenceLedger.cs`
- `src/WildBunch.Domain/Game/PursuitState.cs`
- `src/WildBunch.Domain/Game/GameClock.cs`
- `src/WildBunch.Domain/Travel/TravelJourney.cs`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.Domain/Economy/Wallet.cs`
- `src/WildBunch.Domain/Inventory/Inventory.cs`
- `tests/WildBunch.Domain.Tests/GameSessionAggregateRootTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionPurchaseTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionSaloonWantedSuspectLoopTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionJourneyHistoryTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs`

## Responsibility Regions In `GameSession`

### Player economy and inventory

`GameSession` currently owns wallet mutation, inventory mutation, purchase legality, and the coupling between the player state and the session log. It uses `Wallet`, `Inventory`, `Player`, and store offer data together when buying items or applying trail-event effects.

Current evidence:

- purchase gating lives in `GameSession.Purchase`;
- inventory legality is enforced through `Inventory`;
- wallet balance changes are applied through `Player.SetWallet`;
- trail events can affect both wallet and inventory during travel.

Classification:

- `Wallet`: value object
- `Inventory`: concrete player-state aggregate candidate
- `Player`: session-owned domain object / aggregate candidate
- purchase rules: process plus legality checks, not a new root

### Case and wanted identity

`GameSession` orchestrates case updates, wanted-poster reads, saloon investigation, confrontation, and sheriff turn-in flow while `CaseFile` owns the durable case-local state.

Current evidence:

- wanted posters reveal public warrants and public clues;
- saloon look-around can surface either a wanted suspect or a citizen person of interest;
- confrontation records outcomes back into `CaseFile`;
- sheriff turn-in assessment and settlement are separate methods;
- hidden culprit truth stays in `CaseFile`.

Classification:

- `CaseFile`: session-owned aggregate/subaggregate
- warrant matching and turn-in eligibility: domain policy work
- confrontation and settlement narration: process/orchestration work
- hidden culprit truth: internal case boundary, not a separate root

### Town visit and saloon state

`GameSession` owns the live town boundary through `TownAggregate` and `TownVisitState`. That boundary tracks visit-scoped town source legality, wanted-poster bookkeeping, and saloon person-of-interest state.

Current evidence:

- `TownAggregate` couples the static town definition with visit state;
- `TownVisitState` tracks the current town and per-town spent-source state;
- saloon selection and clearing happen through `CurrentTownVisit.CurrentTownState`;
- wanted-poster repeat checks are town-local.

Classification:

- `TownAggregate`: session-owned aggregate boundary
- `TownVisitState`: session-owned state object
- saloon person-of-interest tracking: temporary root-owned object inside the town boundary

### Journey and travel

`GameSession` still owns travel commands, route advancement, arrival handling, travel diary persistence, and travel resource snapshotting. `TravelJourney` is already a coherent session-owned subtree with its own state transitions.

Current evidence:

- travel starts through `StartJourney`;
- day advancement is one trail day at a time;
- `TravelJourney` tracks day progress, interruptions, and arrival readiness;
- `GameSession` archives completed journey snapshots and clears the active journey after acknowledgement;
- travel diary days are owned by the session but built from travel-specific state.

Classification:

- `TravelJourney`: session-owned aggregate subtree
- travel progression rules: process/policy work
- travel diary entries: root-owned session history

### Log, clock, and pursuit state

`GameSession` owns the session clock, log entries, and pursuit heat. These are live-session support concerns rather than separate command roots.

Classification:

- `GameClock`: session-owned state object
- `PursuitState`: session-owned state object
- `GameLogEntry`: session log record

## Candidate Aggregates And Boundaries

### Strong candidates already justified in source

- `CaseFile`: already a real session-owned aggregate/subaggregate boundary.
- `TravelJourney`: already a real session-owned aggregate subtree.
- `TownAggregate`: already a real session-owned aggregate boundary for town definition plus visit state.

### Likely aggregate candidates inside the root

- `Player`: cohesive session-owned state that spans health, wallet, inventory, and travel-adjacent state.
- `Inventory`: concrete player-state boundary that already carries horse and canteen state.
- `WantedSuspectPresenceLedger`: narrow internal ledger for suspect presence state.

These are useful internal boundaries, but none of them currently has evidence that it should become a separate command root. The current single-root posture remains supported by the live ADRs.

### Policies and processes

- `Purchase` legality and item fit checks
- warrant matching and turn-in assessment
- confrontation narration and result shaping
- travel day generation and upkeep
- trail-event application and horse-loss handling
- public clue reveal selection

These are better treated as policies or processes than as roots.

### Unresolved seams

- The bounty loop is still split across saloon spotting, confrontation, and sheriff settlement inside `GameSession`.
- Public narration and result shaping still live close to the orchestration methods.
- Player state is cohesive enough to justify a future child slice, but the current source does not justify a new aggregate root.

## Parent DoD Coverage Notes

- P1: covered by the region map above.
- P2: covered by the candidate classifications above.
- P4: supported by ADR-0002, ADR-0013, and ADR-0020 plus the current source.
- P5: application handlers remain orchestration only in the current architecture.
- P6: persistence remains snapshot-oriented and root-coordinated.
- P7: the cited tests cover the current wallet, inventory, saloon, bounty, travel, and wanted-poster seams.
- P8: the saloon -> confrontation -> sheriff slice is referred to as the bounty loop.

## Follow-On Slice Order

The source map is intended to tighten the later extraction work in this order:

1. BUNCH-69
2. BUNCH-70
3. BUNCH-71
4. BUNCH-72
5. BUNCH-73

## Bottom Line

`GameSession` is still the live-play aggregate root. The current code already contains several real session-owned subboundaries, but the source does not justify a new top-level root for this slice.
