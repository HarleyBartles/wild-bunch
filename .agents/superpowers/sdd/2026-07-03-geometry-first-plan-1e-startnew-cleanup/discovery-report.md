# Plan 1e — StartNew Cleanup: Discovery Report

**Date:** 2026-07-03
**Task:** 1 — Discovery pass (enumerate remaining `StartNew` call sites and test failures)

## Summary

| Metric | Value |
|--------|-------|
| Files with `GameSession.StartNew` calls | 65 |
| Total `StartNew` call sites | 119 |
| Domain.Tests result | 525 passed, 0 failed |
| Application.Tests result | 204 passed, 0 failed |
| Integration.Tests result | SKIPPED (Docker/Testcontainers not available) |
| Files needing INLINE migration | 4 |
| Files needing assertion updates | 4 (major) + 2 (minor comment-only) |
| Files needing FACTORY_DELEGATE migration | 59 |

## Test Run Results

### Domain.Tests
```
Passed!  - Failed: 0, Passed: 525, Skipped: 0, Total: 525, Duration: 294 ms
```
No failures. All `StartNew` call sites currently pass because the legacy single-event
`StartNew` factory is still present in `GameSession.cs` (lines 950-1024). Failures will
appear only AFTER `StartNew` is removed or AFTER a call site is migrated to the canonical
4-event flow (StartSetup -> ViewPrologue -> SelectStartingTown -> CompleteGameStart).

### Application.Tests
```
Passed!  - Failed: 0, Passed: 204, Skipped: 0, Total: 204, Duration: 325 ms
```
No failures.

### Integration.Tests
SKIPPED — `docker` is not available on this machine. Integration tests use
Testcontainers/PostgreSQL and cannot run. These tests will be verified in the final
phase when Docker is available. The 4 Integration test files below are assessed by
static inspection only.

## Migration Strategy Key

- **FACTORY_DELEGATE** — Replace `GameSession.StartNew(...)` with a call to
  `TestSessionFactory.StartGameCanonical(...)` (or an existing `TestSessionFactory.Create*`
  helper that already wraps the canonical flow). The test only needs a fully-started
  session; it does not assert on start-flow event count, version, or event index.
  No assertion changes required.

- **INLINE** — Inline the 4-step canonical flow
  (`StartSetup` -> `ViewPrologue` -> `SelectStartingTown` -> `CompleteGameStart`)
  directly in the test/helper. Required when the test captures the `GameStarted` event
  for replay-stream construction, asserts on start-flow event count/types/indices, or
  asserts an absolute `Version` value that depends on the number of start events.
  Assertion updates required.

## Assertion-Update Key

- **YES** — Assertions reference start-flow event count (`UncommittedEvents.Single()`,
  `UncommittedEvents.Count`), event index (`UncommittedEvents[0]`), absolute `Version`,
  or stored-event stream composition. These break when the single `GameStarted` event
  becomes 4 canonical events.
- **MINOR** — A comment references `StartNew`/`GameStarted` but the assertions
  themselves use `OfType<T>()` filtering or version deltas that remain valid.
  Comment update only; no logic change.
- **NO** — No start-flow-sensitive assertions.

---

## Enumerated Migration Checklist

### Domain.Tests (24 files, 60 call sites)

| # | File | Call sites | Strategy | Assertions need updating? |
|---|------|-----------|----------|---------------------------|
| 1 | `WildBunch.Domain.Tests/ClockTurnCorrectionTests.cs` | 1 (L148) | INLINE | YES — L151 `UncommittedEvents.Single()` expects 1 event; L97 replay stream built from single `gameStarted` |
| 2 | `WildBunch.Domain.Tests/BountySaloonEventSourcingTests.cs` | 1 (L141) | INLINE | YES — L144 `UncommittedEvents.Single()` expects 1 event; L93-94 replay stream built from single `gameStarted` |
| 3 | `WildBunch.Domain.Tests/GameSessionArchiveTests.cs` | 1 (L210) | FACTORY_DELEGATE | MINOR — L34 comment "StartNew emits GameStarted"; `OfType<PlaythroughArchived>().Single()` and version-delta (L47) remain valid |
| 4 | `WildBunch.Domain.Tests/TravelRulesProfileTests.cs` | 1 (L75) | FACTORY_DELEGATE | NO |
| 5 | `WildBunch.Domain.Tests/TravelResolverTests.cs` | 17 (L397,416,454,457,1012,1025,1050,1084,1105,1131,1155,1179,1203,1227,1251,1287,1322) | FACTORY_DELEGATE | NO |
| 6 | `WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs` | 6 (L457,501,539,567,595,623) | FACTORY_DELEGATE | NO |
| 7 | `WildBunch.Domain.Tests/GameSessionResolverWiringTests.cs` | 4 (L131,188,244,304) | FACTORY_DELEGATE | NO |
| 8 | `WildBunch.Domain.Tests/GameSessionPurchaseTests.cs` | 1 (L175) | FACTORY_DELEGATE | NO |
| 9 | `WildBunch.Domain.Tests/GameSessionJourneyHistoryTests.cs` | 1 (L87) | FACTORY_DELEGATE | NO |
| 10 | `WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs` | 7 (L347,446,504,570,644,710,762) | FACTORY_DELEGATE | NO |
| 11 | `WildBunch.Domain.Tests/GameSessionBountyLoopCoordinatorTests.cs` | 1 (L73) | FACTORY_DELEGATE | NO |
| 12 | `WildBunch.Domain.Tests/PurchaseBeatCostTests.cs` | 1 (L75) | FACTORY_DELEGATE | NO |
| 13 | `WildBunch.Domain.Tests/GameSessionWantedSuspectPresenceTests.cs` | 1 (L82) | FACTORY_DELEGATE | NO |
| 14 | `WildBunch.Domain.Tests/GameSessionSaloonWantedSuspectLoopTests.cs` | 2 (L171,196) | FACTORY_DELEGATE | NO |
| 15 | `WildBunch.Domain.Tests/GameSessionUnrelatedCriminalLedgerWiringTests.cs` | 2 (L171,233) | FACTORY_DELEGATE | NO |
| 16 | `WildBunch.Domain.Tests/GameSessionWantedSuspectConfrontationTests.cs` | 1 (L313) | FACTORY_DELEGATE | NO |
| 17 | `WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs` | 1 (L247) | FACTORY_DELEGATE | NO — L168 uses `OfType<TownActionContextEntered>().Single()` (filter; canonical flow does not emit this type) |
| 18 | `WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` | 6 (L428,475,522,541,573,605) | FACTORY_DELEGATE | NO |
| 19 | `WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs` | 1 (L216) | FACTORY_DELEGATE | NO |
| 20 | `WildBunch.Domain.Tests/TownActionAvailabilityTests.cs` | 1 (L48) | FACTORY_DELEGATE | NO |
| 21 | `WildBunch.Domain.Tests/JournalResolverTests.cs` | 1 (L112) | FACTORY_DELEGATE | NO |
| 22 | `WildBunch.Domain.Tests/BountySettlementPolicyTests.cs` | 1 (L114) | FACTORY_DELEGATE | NO |
| 23 | `WildBunch.Domain.Tests/BeatModelEconomyTests.cs` | 1 (L138) | FACTORY_DELEGATE | NO |
| 24 | `WildBunch.Domain.Tests/ActionAvailabilityResolverTests.cs` | 2 (L157,188) | FACTORY_DELEGATE | NO |

### Application.Tests (33 files, 37 call sites)

| # | File | Call sites | Strategy | Assertions need updating? |
|---|------|-----------|----------|---------------------------|
| 25 | `WildBunch.Application.Tests/InspectNoticeBoardHandlerTests.cs` | 1 (L125) | FACTORY_DELEGATE | NO |
| 26 | `WildBunch.Application.Tests/TurnInToSheriffHandlerTests.cs` | 1 (L101) | FACTORY_DELEGATE | NO |
| 27 | `WildBunch.Application.Tests/QueryHandlersAreReadOnlyTests.cs` | 1 (L56) | FACTORY_DELEGATE | NO |
| 28 | `WildBunch.Application.Tests/GetTownStoreOffersHandlerTests.cs` | 1 (L99) | FACTORY_DELEGATE | NO |
| 29 | `WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs` | 1 (L196) | FACTORY_DELEGATE | NO |
| 30 | `WildBunch.Application.Tests/TravelToTownHandlerTests.cs` | 1 (L120) | FACTORY_DELEGATE | NO |
| 31 | `WildBunch.Application.Tests/ResolveJourneyEncounterHandlerTests.cs` | 1 (L210) | FACTORY_DELEGATE | NO |
| 32 | `WildBunch.Application.Tests/PreviewTravelHandlerTests.cs` | 1 (L72) | FACTORY_DELEGATE | NO |
| 33 | `WildBunch.Application.Tests/ReadWantedPostersHandlerTests.cs` | 1 (L180) | FACTORY_DELEGATE | NO |
| 34 | `WildBunch.Application.Tests/InvestigationSourceHandlerTests.cs` | 1 (L162) | FACTORY_DELEGATE | NO |
| 35 | `WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs` | 2 (L155,174) | FACTORY_DELEGATE | NO |
| 36 | `WildBunch.Application.Tests/CheckSheriffRecordsHandlerTests.cs` | 1 (L125) | FACTORY_DELEGATE | NO |
| 37 | `WildBunch.Application.Tests/CaseBoardMapperTests.cs` | 1 (L364) | FACTORY_DELEGATE | NO |
| 38 | `WildBunch.Application.Tests/AdvanceTravelDayHandlerTests.cs` | 4 (L179,209,239,278) | FACTORY_DELEGATE | NO |
| 39 | `WildBunch.Application.Tests/ArchivePlaythroughHandlerTests.cs` | 1 (L71) | FACTORY_DELEGATE | NO |
| 40 | `WildBunch.Application.Tests/GetJournalHandlerTests.cs` | 1 (L188) | FACTORY_DELEGATE | NO |
| 41 | `WildBunch.Application.Tests/GetGameSessionHandlerTests.cs` | 1 (L194) | FACTORY_DELEGATE | NO |
| 42 | `WildBunch.Application.Tests/GetAvailableActionsHandlerTests.cs` | 1 (L81) | FACTORY_DELEGATE | NO |
| 43 | `WildBunch.Application.Tests/Execution/GameSessionCommandHandlerTests.cs` | 1 (L140) | FACTORY_DELEGATE | NO |
| 44 | `WildBunch.Application.Tests/CompletePlayerSetupOneActivePlaythroughTests.cs` | 1 (L175) | FACTORY_DELEGATE | NO |
| 45 | `WildBunch.Application.Tests/ConfrontWantedSuspectHandlerTests.cs` | 1 (L81) | FACTORY_DELEGATE | NO |
| 46 | `WildBunch.Application.Tests/ConfrontSaloonWantedSuspectHandlerTests.cs` | 1 (L87) | FACTORY_DELEGATE | NO |
| 47 | `WildBunch.Application.Tests/Dev/SetDevEntropyHandlerTests.cs` | 1 (L74) | FACTORY_DELEGATE | NO |
| 48 | `WildBunch.Application.Tests/Dev/GetTravelDevContextHandlerTests.cs` | 1 (L84) | FACTORY_DELEGATE | NO |
| 49 | `WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs` | 1 (L134) | FACTORY_DELEGATE | NO |
| 50 | `WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs` | 1 (L206) | FACTORY_DELEGATE | NO |
| 51 | `WildBunch.Application.Tests/Dev/ForceTravelOverrideHandlerTests.cs` | 1 (L88) | FACTORY_DELEGATE | NO |
| 52 | `WildBunch.Application.Tests/Dev/ForceSaloonOverrideHandlerTests.cs` | 1 (L168) | FACTORY_DELEGATE | NO |
| 53 | `WildBunch.Application.Tests/Dev/ForceDevSaltSourceHandlerTests.cs` | 1 (L139) | FACTORY_DELEGATE | NO |
| 54 | `WildBunch.Application.Tests/Dev/ForceDevDifficultyHandlerTests.cs` | 1 (L75) | FACTORY_DELEGATE | NO |
| 55 | `WildBunch.Application.Tests/Dev/ClearTravelOverrideHandlerTests.cs` | 1 (L73) | FACTORY_DELEGATE | NO |
| 56 | `WildBunch.Application.Tests/Dev/ClearSaloonOverrideHandlerTests.cs` | 1 (L84) | FACTORY_DELEGATE | NO |
| 57 | `WildBunch.Application.Tests/Dev/ClearDevSaltSourceHandlerTests.cs` | 1 (L72) | FACTORY_DELEGATE | NO |

### Integration.Tests (8 files, 22 call sites) — assessed by static inspection (Docker unavailable)

| # | File | Call sites | Strategy | Assertions need updating? |
|---|------|-----------|----------|---------------------------|
| 58 | `WildBunch.Integration.Tests/EventStorePersistenceTests.cs` | 2 (L612,654) | INLINE | YES — L235 `storedEvents.Length==3`, L236 `storedEvents[0]=="GameStarted"`, L237-238 indices, L271 count; all assume single start event |
| 59 | `WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs` | 1 (L67) | INLINE | YES — L83 `Single()`, L84 `UncommittedEvents[0]==GameStarted`, L91 `Version==1`, L96 `Version==1`, L112 `Version==3`, L116 `events.Count==3`, L117-119 event types/indices |
| 60 | `WildBunch.Integration.Tests/UnrelatedCriminalLedgerPersistenceTests.cs` | 1 (L143) | FACTORY_DELEGATE | NO |
| 61 | `WildBunch.Integration.Tests/PostgreSqlPersistenceTests.cs` | 1 (L242) | FACTORY_DELEGATE | NO |
| 62 | `WildBunch.Integration.Tests/MigrationTests.cs` | 1 (L132) | FACTORY_DELEGATE | NO |
| 63 | `WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs` | 3 (L433,481,695) | FACTORY_DELEGATE | NO |
| 64 | `WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` | 9 (L551,613,653,683,721,745,771,823,861) | FACTORY_DELEGATE | MINOR — L70 comment references "GameStarted event"; `SeedCode` assertion (L71) remains valid via canonical flow |
| 65 | `WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs` | 2 (L226,245) | FACTORY_DELEGATE | NO |

---

## Files Needing INLINE Migration (4)

These are the high-risk files. They capture the `GameStarted` event or assert on
start-flow event composition. Each requires the 4-step canonical flow inlined AND
assertion updates.

### 1. `ClockTurnCorrectionTests.cs` (Domain)
- **Call site:** L148 (helper `CreateDefaultSessionWithUncommittedGameStarted`)
- **Breaking assertions:**
  - L151: `Assert.IsType<GameStarted>(session.UncommittedEvents.Single())` — expects 1 uncommitted event; canonical produces 4
  - L97: `new[] { gameStarted }.Concat(contextEvents)` — replay stream omits the 3 other start events (PrologueViewed, StartingTownSelected, PlayerSetupCompleted)
- **Fix:** Inline 4-step flow; capture all 4 start events (or capture the full uncommitted start-event list) for replay-stream construction.

### 2. `BountySaloonEventSourcingTests.cs` (Domain)
- **Call site:** L141 (helper `CreateConfrontableSaloonSessionWithUncommittedGameStarted`)
- **Breaking assertions:**
  - L144: `Assert.IsType<GameStarted>(session.UncommittedEvents.Single())` — expects 1 event; canonical produces 4
  - L93-94: replay stream built from single `gameStarted`
- **Fix:** Same as ClockTurnCorrectionTests — inline 4-step flow, capture full start-event list for replay.

### 3. `EventSourcingEndToEndTests.cs` (Integration)
- **Call site:** L67 (helper `CreateSession`)
- **Breaking assertions:**
  - L83: `Assert.Single(session.UncommittedEvents)` — becomes 4
  - L84: `Assert.IsType<GameStarted>(session.UncommittedEvents[0])` — first event becomes `GameSetupStarted`
  - L91: `Assert.Equal(1, session.Version)` — becomes 4
  - L96: `Assert.Equal(1, reloaded!.Version)` — becomes 4
  - L112: `Assert.Equal(3, reloaded.Version)` — becomes 6
  - L116: `Assert.Equal(3, events.Count)` — becomes 6
  - L117-119: event type/index assertions shift
- **Fix:** Inline 4-step flow; update all counts (1->4, 3->6), indices, and version expectations.

### 4. `EventStorePersistenceTests.cs` (Integration)
- **Call sites:** L612, L654 (helpers `CreateSession`, `CreateSessionWithWarrantedSaloonSuspect`)
- **Breaking assertions:**
  - L235: `Assert.Equal(3, storedEvents.Length)` — becomes 6 (4 start + 2 action)
  - L236: `Assert.Equal("GameStarted", storedEvents[0].EventType)` — becomes `"GameSetupStarted"` (or whichever is first)
  - L237-238: event type indices shift
  - L271: `Assert.Equal(2, loaded.UncommittedEvents.Count)` — this one is AFTER reload+commit so may remain valid; verify
- **Fix:** Inline 4-step flow in both helpers; update stored-event count, type, and index assertions.

## Files Needing Minor Comment-Only Updates (2)

| File | Line | Current comment | Notes |
|------|------|-----------------|-------|
| `GameSessionArchiveTests.cs` | L34 | `// StartNew emits GameStarted; archive appends PlaythroughArchived.` | Update to reference canonical 4-event start flow. Assertions use `OfType` filtering and version delta — both remain valid. |
| `EfGameSessionRepositoryTests.cs` | L70 | `// Seed code is restored from the GameStarted event via event replay` | Update to reference canonical flow. `SeedCode` assertion remains valid. |

## Notes for Subsequent Tasks

1. **No current test failures.** All 729 Domain + Application tests pass because the
   legacy `StartNew` factory still exists. Failures will emerge per-file as each call
   site is migrated to the canonical flow. The 4 INLINE files above will fail
   immediately upon migration; the 59 FACTORY_DELEGATE files should pass without
   assertion changes (verify per-file after migration).

2. **Integration tests unverified.** The 8 Integration files (22 call sites) were
   assessed by static inspection only. Docker/Testcontainers is not available. The
   2 INLINE Integration files (`EventSourcingEndToEndTests`, `EventStorePersistenceTests`)
   are the highest risk and must be run with Docker before Plan 1e closes.

3. **`TestSessionFactory.StartGameCanonical` signature** (TestSessionFactory.cs L30):
   `(string playerName, DomainWorld world, CaseFile caseFile, TownId startingTownId,
   Wallet? wallet = null, DomainInventory? inventory = null,
   GameDifficulty gameDifficulty = GameDifficulty.Easy, SaltSource? saltSource = null,
   GameEntropy gameEntropy = GameEntropy.Classic, string? seedCode = null)`.
   Note: default `gameDifficulty` is `Easy` in the factory vs `Standard` in legacy
   `StartNew` — verify each call site's difficulty expectation when migrating.

4. **`MarkEventsCommitted` pattern.** Most FACTORY_DELEGATE files call
   `MarkEventsCommitted()` after session creation, which clears start events from
   `UncommittedEvents`. This is why their action-event count assertions (e.g.
   `Assert.Equal(2, ...)`) remain valid after migration. Confirm each file follows
   this pattern during migration.

5. **Phase 2 (legacy cleanup) should run first.** The plan's Phase 2 defers
   (`RecaptureGameStartedForReplay` alias, stale `gameStarted` variable names, stale
   comment, `CreateBaselineCaseFileFor` data loss) before migrating the 65 call sites,
   so the migration uses clean naming.
