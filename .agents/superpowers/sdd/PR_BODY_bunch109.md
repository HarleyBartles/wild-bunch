## Summary

- **Start flow phase tracking**: New `StartFlowPhase` enum (`NotStarted → SetupComplete → PrologueViewed → GameStarted`) persisted via `PlayerSetupCompleted` and `PrologueViewed` domain events. The game start flow is now resumable after page refresh — the backend phase is the source of truth, not frontend local state.
- **Three-step API replaces direct-create**: `POST /api/games` (direct create-and-start) is **removed entirely**. The three-step flow is the only path: `POST /api/games/setup`, `POST /api/games/{id}/prologue-viewed`, `POST /api/games/{id}/start`. `StartNewGameHandler`, `StartNewGameCommand`, `StartGameRequest`, `createGame()`, `StartGamePanel`, and `CampRoute` are deleted.
- **ViewPrologue contract**: `GameSession.ViewPrologue(...)` throws `InvalidOperationException` if the phase is `NotStarted` or `GameStarted`, and is idempotent if already `PrologueViewed`. `ViewPrologueCommand` no longer carries `RevealedSuspectIdentifier` — the handler resolves the true culprit descriptor from session state.
- **Entropy-salted trail distances**: `TrailDistanceSalter` applies a deterministic per-trail swing (Boring ±0, Classic ±1, Adventurous ±2, Wild ±3, floor at 1) to baseline trail distances. Applied in `GameSetupResolver.Resolve` between world building and starting town resolution.
- **Session-scoped map endpoint**: `GET /api/games/{id}/starting-town-map` loads from the session's salted world, returns 404 for missing sessions.
- **Integration test fixture drift fixes**: Tests broken by BUNCH-93/94/107 map layout refactoring are repaired. All integration tests now use the `ThreeStepGameSetup.CreateStartedGameAsync()` helper.

Linear issue: [BUNCH-109](https://linear.app/harleys-workspace/issue/BUNCH-109/start-flow-phase-tracking-and-entropy-salted-trail-distances)

#### Test plan

- [x] `TrailDistanceSalterTests` — 8 unit tests (Boring no-op, Classic/Adventurous/Wild bounds, floor at 1, reproducibility, salt sensitivity, topology preservation)
- [x] `StartFlowEventSourcingTests` — event replay + throwing contract tests (`ViewPrologue_WhenGameStarted_Throws`, `ViewPrologue_WhenAlreadyPrologueViewed_IsIdempotent`)
- [x] `StartingTownMapEndpointTests` — session-scoped endpoint + 404 test
- [x] `CompletePlayerSetupHandlerTests` — replaces old `StartNewGameHandlerTests`
- [x] `CompletePlayerSetupOneActivePlaythroughTests` — replaces old `StartNewGameOneActivePlaythroughTests`
- [x] `GameSetupResolverTests` — deterministic setups with FixedSaltSourceFactory
- [x] `TravelEntropyVarianceTests` — Runtime-salt tests use CreateSessionWithRuntimeSalt helper
- [x] `GetStartingTownMapHandlerTests` — rewritten with InMemoryGameSessionRepository
- [x] All integration tests migrated to `ThreeStepGameSetup.CreateStartedGameAsync()` helper
- [x] Frontend tests updated — `createGame` references removed, `StartGamePanel.test.tsx` deleted
- [x] All 962 .NET tests pass (139 GameContent + 182 Application + 475 Domain + 166 Integration)
- [x] All 174 frontend tests pass (21 test files)
- [x] `dotnet build` — 0 errors
- [x] `tsc --noEmit` — 0 errors

Generated with [Devin](https://devin.ai)
