# Code Review: BUNCH-147 Town Hub Deterministic Layout Resolver

## Overview
This PR implements a deterministic town hub layout resolver with dev controls, including a three-phase setup flow (prep → inject dev salts → start) and dev API endpoints for layout salt management.

## Architecture & Design

### Strengths
1. **Clean separation of concerns**: The implementation follows the existing dev-overlay pattern with events, handlers, and persistence separation.
2. **Three-phase flow is well-designed**: The prep → inject → start pattern provides a clean boundary for dev injections before world generation.
3. **Event-sourcing compliance**: DevLayoutSaltsForced event follows the established event-sourcing pattern with proper Apply method and event replay support.
4. **Domain modeling**: LayoutSalts is a clean value object with clear purpose - split salts for buildings, roads, dirt, and props.

### Concerns

#### 1. **Persistence Gap - CRITICAL**
**Issue**: `DevLayoutSalts` is not persisted in the `GameSessionSnapshot` in `GameSessionJsonSerializer.SessionSnapshot.cs`.

**Impact**: Dev layout salts will be lost on session rehydration from snapshot. The event replay will restore them if the event stream is replayed, but snapshot-only persistence will lose this state.

**Location**: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`

**Evidence**:
- `GameSessionSnapshot` record (lines 10-31) does not include `DevLayoutSalts` field
- `FromDomain` method (lines 33-57) does not capture `session.DevLayoutSalts`
- `ToDomain` method (lines 59-108) does not restore `DevLayoutSalts`

**Recommendation**: Add `LayoutSaltsSnapshot? DevLayoutSalts` to the snapshot record and implement the round-trip logic, similar to how `PendingDevTravelOverride` and `PendingDevSaloonOverride` are handled.

#### 2. **Event Naming Inconsistency**
**Issue**: The event property name `ForcedLayoutSalts` differs from the domain property name `DevLayoutSalts`.

**Location**: 
- `src/WildBunch.Domain/Events/DevLayoutSaltsForced.cs` (line 11): `LayoutSalts ForcedLayoutSalts`
- `src/WildBunch.Domain/Game/GameSession.cs` (line 797): `DevLayoutSalts = e.ForcedLayoutSalts`

**Impact**: Minor naming inconsistency that could confuse future developers. The event uses "Forced" prefix while the domain property uses "Dev" prefix.

**Recommendation**: Consider aligning naming conventions. Either:
- Change event to `LayoutSalts DevLayoutSalts` (matches domain property)
- Change domain property to `ForcedLayoutSalts` (matches event)
- Or keep as-is if the distinction is intentional (forced vs dev-controlled)

#### 3. **StartGameSessionHandler Creates New Session Instead of Transitioning**
**Issue**: `StartGameSessionHandler` creates a completely new session via `GameSession.StartSetup` instead of transitioning the prepped session to active state.

**Location**: `src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs` (lines 70-77)

**Evidence**:
```csharp
// Create the session in setup-complete phase
var newSession = GameSession.StartSetup(
    "Player",
    world,
    caseFile,
    preppedSession.GameDifficulty,
    preppedSession.GameEntropy,
    seedCodeText,
    saltSource);
```

**Impact**: 
- The prepped session ID is discarded - a new session ID is created
- This breaks the continuity of the session - the prepped session is orphaned
- Event history is split between two sessions instead of one continuous stream
- Dev salts set on the prepped session are lost (they're not carried forward)

**Recommendation**: Either:
1. Add a `GameSession.StartFromPrepped` method that transitions the same session from Prepped to SetupComplete, preserving the session ID and event history
2. Or document that this is intentional and the prepped session is a throwaway template

#### 4. **Missing Validation in SetTownLayoutSaltsHandler**
**Issue**: No validation that the session is in a valid state to receive dev layout salts.

**Location**: `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsHandler.cs`

**Evidence**: The handler directly calls `session.SetDevLayoutSalts(layoutSalts)` without checking session status.

**Impact**: Dev salts could be set on an active session where they would have no effect (layout generation already complete). This could confuse developers.

**Recommendation**: Add status validation similar to other dev handlers - only allow setting dev salts on sessions in Prepped or SetupComplete status (before world generation is complete).

#### 5. **Integration Test Bypasses API Layer**
**Issue**: The integration test directly manipulates the aggregate (`prepped.SetDevLayoutSalts`) instead of using the API/handler layer.

**Location**: `tests/WildBunch.Application.Tests/Integration/DevEnabledActionPatternIntegrationTests.cs` (lines 42-47)

**Evidence**:
```csharp
// Phase 2: Inject dev salts (simulated via direct aggregate manipulation for test)
var devSalts = new LayoutSalts("dev-buildings", "dev-roads", "dev-dirt", "dev-props");
prepped.SetDevLayoutSalts(devSalts);
```

**Impact**: The test doesn't validate the actual three-phase flow through the API/handler layer. It only tests the handler logic, not the full integration.

**Recommendation**: Either:
1. Use the actual `SetTownLayoutSaltsHandler` to inject salts
2. Or document that this is intentional and add a separate API-level integration test

#### 6. **API Tests Only Test 404 Responses**
**Issue**: The API tests only verify that endpoints return 404 for non-existent sessions.

**Location**: `tests/WildBunch.Application.Tests/Api/TownLayoutDevEndpointsTests.cs`

**Evidence**: All three tests use a non-existent session ID and only assert 404 status.

**Impact**: These tests don't validate:
- Successful GET of layout salts for an existing session
- Successful SET of layout salts for an existing session
- Successful random generation of layout salts
- Dev role guard behavior (403 in production)
- Actual data round-trip through the API

**Recommendation**: Add tests that:
1. Create a session via the prep endpoint
2. Set layout salts via the dev endpoint
3. Retrieve layout salts via the dev endpoint
4. Verify the values match
5. Test dev role guard behavior

## Code Quality

### Strengths
1. **Good documentation**: XML comments are clear and reference BUNCH-147 appropriately.
2. **Consistent patterns**: Follows existing dev handler patterns (DevTravelOverride, DevSaloonOverride).
3. **Proper null handling**: Uses nullable types appropriately for optional dev state.
4. **Test coverage**: Unit tests exist for handlers and domain logic.

### Minor Issues

#### 7. **Seed Code Conversion Complexity**
**Issue**: The integration test has complex seed code conversion logic that's hard to read.

**Location**: `tests/WildBunch.Application.Tests/Integration/DevEnabledActionPatternIntegrationTests.cs` (lines 30, 71)

**Evidence**:
```csharp
var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode())).ToString();
```

**Recommendation**: Extract this to a helper method or use a simpler test seed.

#### 8. **Hardcoded "Player" Name**
**Issue**: `StartGameSessionHandler` uses hardcoded "Player" name when calling `INewGameFactory.ResolveWorld`.

**Location**: `src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs` (line 63)

**Impact**: This ignores the player name from the prepped session (which uses "Prepped" placeholder).

**Recommendation**: Either:
1. Carry forward the player name from the prepped session
2. Or document that the player name is intentionally reset during start

## Frontend Work

The PR includes frontend work in `src/WildBunch.Web/`:
- `TownLayoutDevPanel.tsx` - React component for dev layout salt management
- `devApi.ts` updates - API client functions
- `TownLayoutDevPanel.test.tsx` - Component tests

**Observation**: The frontend work was not part of the code review findings P1-P3 and appears to be complete but not validated in this review.

## Documentation

### Strengths
1. **ADR-0036**: Well-written ADR documenting the Dev-Enabled Action Pattern.
2. **AGENTS.md updates**: Properly indexed in the documentation mesh.
3. **Code comments**: Clear XML comments explaining the three-phase flow.

### Minor Issue

#### 9. **Implementation Plan Outdated**
**Issue**: The implementation plan mentions skipped frontend tasks (Tasks 5 and 6) but the frontend work appears to be implemented.

**Location**: `.agents/superpowers/plans/2026-07-10-dev-layout-salts-integration-implementation.md`

**Recommendation**: Update the plan to reflect the actual implementation status.

## Security

### Strengths
1. **Dev role guard**: All dev endpoints are protected by `DevRoleGuard`.
2. **No secrets**: Dev layout salts are not sensitive data.

### No Concerns

## Performance

### No Concerns
The implementation has minimal performance impact:
- Layout salts are simple string values
- No additional database queries
- Dev-only feature, not used in production

## Summary

### Critical Issues (Must Fix Before Merge)
1. **Persistence Gap**: `DevLayoutSalts` not persisted in `GameSessionSnapshot` - will lose state on snapshot rehydration
2. **Session Continuity**: `StartGameSessionHandler` creates new session instead of transitioning prepped session - breaks event history continuity

### High Priority Issues (Should Fix)
3. **Missing Validation**: `SetTownLayoutSaltsHandler` doesn't validate session status
4. **Weak API Tests**: API tests only validate 404 responses, not successful flows

### Medium Priority Issues (Nice to Have)
5. **Naming Inconsistency**: Event property name differs from domain property
6. **Integration Test Bypass**: Integration test doesn't use actual handler for salt injection
7. **Seed Code Complexity**: Test seed conversion logic is hard to read
8. **Hardcoded Player Name**: Ignores player name from prepped session
9. **Outdated Plan**: Implementation plan doesn't reflect actual frontend work

### Recommendation
**Do not merge** until critical issues #1 and #2 are resolved. The persistence gap is a data loss bug, and the session continuity issue breaks the event-sourcing guarantees.

After critical fixes, address high priority issues #3 and #4 to ensure proper validation and test coverage.
