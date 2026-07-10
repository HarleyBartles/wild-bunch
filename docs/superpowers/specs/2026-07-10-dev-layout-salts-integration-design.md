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

**Add LayoutSalts to TownLayout**
- Add `LayoutSalts? LayoutSalts` field to `TownLayout` record
- This persists the salts that were actually used in layout generation
- Allows dev overlay to view the salts that were used

**Update TownLayoutDto**
- Add `LayoutSaltsDto? LayoutSalts` field to `TownLayoutDto`
- Include in `TownLayoutMapper.ToDto()`

**Update ResolvedGameSetup**
- Add `LayoutSalts? DevLayoutSalts` field to `ResolvedGameSetup` record
- GameSetupResolver reads from GameSession and passes through

**Update GameSetupResolver**
- Accept `GameSession` parameter (or accept dev salts directly)
- Read `GameSession.DevLayoutSalts` if session provided
- Pass dev salts through to MapGenerator

**Update MapGenerator**
- Accept `LayoutSalts? devLayoutSalts` parameter
- Pass to `LayoutSaltDeriver.DeriveLayoutSalts()`
- Remove hardcoded `devLayoutSalts: null`

### Frontend Changes

**Game Setup Screen**
- Add call to `prepGameSession` before showing game options
- Store `gameSessionId` in state
- If dev panel has salts set, call `injectDevSalts` before start
- Call `startGameSession` when player clicks start

**Dev Panel**
- Update to work with prep'd session (set salts before game start)
- Read salts from `TownLayout.LayoutSalts` after game started
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

1. Add LayoutSalts to TownLayout and TownLayoutDto
2. Add LayoutSalts to ResolvedGameSetup
3. Update GameSetupResolver to accept GameSession and read DevLayoutSalts
4. Update MapGenerator to accept devLayoutSalts parameter
5. Add prepGameSession API
6. Add startGameSession API
7. Update existing setTownLayoutSalts to work on prep'd session
8. Frontend: update game setup screen to orchestrate three-phase flow
9. Frontend: update dev panel to read from TownLayout
10. Add integration tests
11. Add frontend tests

## Design Decisions

**Q1: Should `prepGameSession` also accept player-chosen starting town?**
**A:** No. The starting town is validated by `StartingTownPolicy` during the start phase after the world is generated. The prep phase only needs seed, difficulty, and entropy.

**Q2: Should the existing game creation API be deprecated?**
**A:** For this vertical slice, keep the existing API for normal players and add the new three-phase flow for dev workflow. The existing API can be deprecated in a future refactor once the three-phase pattern is proven.

**Q3: Should other dev injections use this multi-phase pattern?**
**A:** Out of scope for this design. The multi-phase pattern is intentionally extensible for future dev injections (difficulty, entropy, etc.), but this design only addresses layout salts.
