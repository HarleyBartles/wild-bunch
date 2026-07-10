# Dev Layout Salts Integration Design

**Date:** 2026-07-10
**Issue:** BUNCH-147
**Status:** Design

## Overview

Integrate dev layout salts into the game setup pipeline so that dev-set salts are used in world generation and persist with the playthrough. The dev overlay should be able to set salts before game start, and those salts should flow through to MapGenerator. The UI orchestrates a multi-phase setup flow to maintain clean dependency inversion and respect the dev-only API contract.

## Background

Currently, the dev overlay can set layout salts via `SetTownLayoutSaltsCommand`, which stores them in `GameSession.DevLayoutSalts`. However, `MapGenerator` receives `devLayoutSalts: null` hardcoded, so the dev salts don't affect actual layout generation. The dev panel can view salts, but they don't flow into world generation.

## Goals

1. Dev salts set in the dev overlay are used in world generation
2. Dev salts persist with the playthrough (saved in TownLayout)
3. Dev overlay can view the salts that were actually used
4. Dev-only API contract is respected (normal players cannot inject dev salts)
5. Clean dependency inversion: UI orchestrates phases, backend executes phases

## Architecture

### Multi-Phase Game Session Setup

The game setup is split into three phases orchestrated by the UI:

**Phase 1: Prep Game Session**
- UI calls `POST /api/sessions/prep` with seed, difficulty, entropy
- Backend creates minimal GameSession with seed/difficulty/entropy, no world yet
- Backend returns `gameSessionId`

**Phase 2: Inject Dev Salts (Optional)**
- If dev panel has salts set, UI calls `POST /api/dev/sessions/{id}/town-layout/set-salts` (dev-only API)
- Backend sets `GameSession.DevLayoutSalts` via `DevLayoutSaltsForced` event
- If no dev salts set, this phase is skipped

**Phase 3: Start Game Session**
- UI calls `POST /api/sessions/{id}/start`
- Backend loads GameSession, runs game setup pipeline, generates world
- Backend checks `GameSession.DevLayoutSalts` and passes to MapGenerator
- Backend returns the fully initialized GameSession with world

### Backend Flow

1. **Prep Phase:**
   - Create GameSession with seed, difficulty, entropy
   - Store in repository
   - Return session ID

2. **Inject Phase (dev-only):**
   - Load GameSession by ID
   - Apply `DevLayoutSaltsForced` event
   - Save to repository

3. **Start Phase:**
   - Load GameSession by ID
   - Call `GameSetupResolver.Resolve()` with GameSession
   - GameSetupResolver checks `GameSession.DevLayoutSalts`
   - Pass dev salts through `ResolvedGameSetup` → `MapGenerator`
   - MapGenerator uses dev salts if present, otherwise derives normal salts via `LayoutSaltDeriver`
   - Layout salts are saved in `TownLayout.LayoutSalts`
   - Return fully initialized GameSession

## Components

### New APIs

**Prep Game Session API**
- Endpoint: `POST /api/sessions/prep`
- Request: `{ seed: string, difficulty: GameDifficulty, entropy: GameEntropy }`
- Response: `{ gameSessionId: string }`
- Access: Public (normal players use this)

**Start Game Session API**
- Endpoint: `POST /api/sessions/{id}/start`
- Response: `GameSessionDto` (fully initialized with world)
- Access: Public (normal players use this)

**Inject Dev Salts API** (already exists, just needs to work on prep'd session)
- Endpoint: `POST /api/dev/sessions/{id}/town-layout/set-salts`
- Request: `TownLayoutSaltsDto`
- Response: 204 No Content
- Access: Dev-only (existing `DevRoleGuard`)

### Domain Changes

**Add GameStatus.Prepped**
- Add `Prepped = 4` to `GameStatus` enum
- This status indicates a session has been prepped but not yet started
- Distinguishes from `Active` which indicates a fully initialized session

**Add GameSession.StartPrepped() factory method**
- Add static factory method to `GameSession.cs`: `public static GameSession StartPrepped(string seedCode, GameDifficulty gameDifficulty, GameEntropy gameEntropy)`
- Creates minimal GameSession with only seed/difficulty/entropy, no world yet
- Sets Status to `GameStatus.Prepped`
- Stores seedCode, gameDifficulty, gameEntropy
- Returns session with ID for later start
- Pattern follows existing `GameSession.StartSetup()` factory method

**Add LayoutSalts to TownLayout**
- Add `LayoutSalts? LayoutSalts` field to `TownLayout` record (after `ResolverVersion`)
- Update `TownLayoutGenerator.GenerateLayout()` to accept `LayoutSalts? usedLayoutSalts` parameter
- Pass `usedLayoutSalts` to the TownLayout constructor at line 163
- This persists the salts that were actually used in layout generation
- Allows dev overlay to view the salts that were used

**Update TownLayoutDto**
- Add `LayoutSaltsDto? LayoutSalts` field to `TownLayoutDto` (after `ResolverVersion`)
- Update `TownLayoutMapper.ToDto()` to map `TownLayout.LayoutSalts` to `TownLayoutDto.LayoutSalts`

**Update ResolvedGameSetup**
- Add `LayoutSalts? DevLayoutSalts` field to `ResolvedGameSetup` record
- GameSetupResolver reads from GameSession and passes through

**Update GameSetupResolver**
- Add overload: `public ResolvedGameSetup Resolve(SeedWorld seedWorld, DifficultyEnvelope difficulty, EntropyPolicy entropy, LayoutSalts? devLayoutSalts, TownId? playerChosenStartingTownId = null)`
- Keep existing signature for backward compatibility
- Pass devLayoutSalts to MapGenerator
- This approach avoids coupling to GameSession while still supporting dev salts

**Update MapGenerator**
- Change signature from `public static World Generate(SeedWorld seedWorld, GameSetupDeterministicSource source, GameEntropy entropy, SaltSource? saltSource)` to include `LayoutSalts? devLayoutSalts` parameter
- Pass devLayoutSalts to `LayoutSaltDeriver.DeriveLayoutSalts()` call
- Remove hardcoded `devLayoutSalts: null` at line 120

### Frontend Changes

**Add API Functions**
- Add `prepGameSession(seed: string, difficulty: GameDifficulty, entropy: GameEntropy)` to `src/api/wildBunchApi.ts`
- Add `startGameSession(gameSessionId: string)` to `src/api/wildBunchApi.ts`
- Existing `setTownLayoutSalts(gameSessionId: string, salts: TownLayoutSalts)` already exists in `src/dev/devApi.ts`

**Update Game Setup Screen**
- Game setup form is in `src/components/StartGameOptionsForm.tsx`
- Add call to `prepGameSession()` before showing game options (when player navigates to setup screen)
- Store `gameSessionId` in component state
- If dev panel has salts set, call `setTownLayoutSalts()` before start
- Change existing `completeGameSetup()` call to `startGameSession(gameSessionId)` when player clicks start
- Note: Existing API is `/api/games/setup` which calls `CompletePlayerSetupHandler` - this should be kept for normal players

**Update Dev Panel**
- Dev panel is in `src/dev/panels/TownLayoutDevPanel.tsx`
- Update to work with prep'd session (set salts before game start)
- Update `getTownLayoutSalts()` to read from `TownLayout.LayoutSalts` after game started (currently reads from GameSession.DevLayoutSalts)
- Show the salts that were actually used (dev or derived)

## Data Flow

```
UI → POST /api/sessions/prep → GameSession (minimal) → sessionId
UI → POST /api/dev/sessions/{id}/town-layout/set-salts → GameSession.DevLayoutSalts set
UI → POST /api/sessions/{id}/start → GameSetupResolver → MapGenerator → World → TownLayout.LayoutSalts
```

## Error Handling

- Prep phase: Validation of seed/difficulty/entropy
- Inject phase: Dev access denied if not dev role, session not found
- Start phase: Session not found, invalid session state (already started)

## Testing

### Unit Tests
- Test GameSetupResolver with dev salts vs without
- Test MapGenerator with dev salts vs without
- Test LayoutSaltDeriver with dev salts override

### Integration Tests
- Test full flow: prep → inject → start → verify layout uses dev salts
- Test flow without dev salts: prep → start → verify layout uses derived salts
- Test dev-only guard on inject API

### Frontend Tests
- Test dev panel sets salts before game start
- Test normal flow without dev salts
- Test dev panel reads salts from TownLayout after game started

## Implementation Order

1. Add `GameStatus.Prepped` to `src/WildBunch.Domain/Game/GameStatus.cs`
2. Add `GameSession.StartPrepped()` factory method to `src/WildBunch.Domain/Game/GameSession.cs`
3. Add `LayoutSalts` field to `TownLayout` in `src/WildBunch.Domain/World/TownLayout.cs`
4. Update `TownLayoutGenerator.GenerateLayout()` in `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs` to accept and use `usedLayoutSalts` parameter
5. Add `LayoutSalts` field to `TownLayoutDto` in `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
6. Update `TownLayoutMapper.ToDto()` in `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs` to map LayoutSalts
7. Add `DevLayoutSalts` field to `ResolvedGameSetup` in `src/WildBunch.GameContent/NewGame/ResolvedGameSetup.cs`
8. Add overload to `GameSetupResolver.Resolve()` in `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs` to accept devLayoutSalts
9. Update `MapGenerator.Generate()` in `src/WildBunch.GameContent/NewGame/MapGenerator.cs` to accept devLayoutSalts parameter
10. Add `PrepGameSessionCommand` and `PrepGameSessionHandler` in `src/WildBunch.Application/Games/Commands/`
11. Add `StartGameSessionCommand` and `StartGameSessionHandler` in `src/WildBunch.Application/Games/Commands/`
12. Register new endpoints in `src/WildBunch.Api/GamesEndpoints.cs`
13. Add API functions to `src/WildBunch.Web/src/api/wildBunchApi.ts`
14. Update `src/WildBunch.Web/src/components/StartGameOptionsForm.tsx` to orchestrate three-phase flow
15. Update `src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx` to read from TownLayout
16. Add unit tests for new domain changes
17. Add integration tests for full flow
18. Add frontend tests

## Design Decisions

**Q1: Should `prepGameSession` also accept player-chosen starting town?**
**A:** No. The starting town is validated by `StartingTownPolicy` during the start phase after the world is generated. The prep phase only needs seed, difficulty, and entropy.

**Q2: Should the existing game creation API be deprecated?**
**A:** For this vertical slice, keep the existing API for normal players and add the new three-phase flow for dev workflow. The existing API can be deprecated in a future refactor once the three-phase pattern is proven.

**Q3: Should other dev injections use this multi-phase pattern?**
**A:** Out of scope for this design. The multi-phase pattern is intentionally extensible for future dev injections (difficulty, entropy, etc.), but this design only addresses layout salts.

## Existing API Reference

**Current Game Creation API:**
- Endpoint: `POST /api/games/setup`
- Handler: `CompletePlayerSetupHandler` in `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`
- Creates fully initialized GameSession with world in one call
- Calls `GameSession.StartSetup()` factory method
- Archives existing active sessions (one-active-playthrough invariant)
- Returns `GameSessionDto` with HUD and Diary projections
- This API should be kept for normal players and not modified in this implementation

**Current Dev Salts API:**
- Endpoint: `POST /api/dev/sessions/{id}/town-layout/set-salts`
- Handler: `SetTownLayoutSaltsHandler` in `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsHandler.cs`
- Sets `GameSession.DevLayoutSalts` via `DevLayoutSaltsForced` event
- This API already exists and should work on prep'd sessions (status check may be needed)
