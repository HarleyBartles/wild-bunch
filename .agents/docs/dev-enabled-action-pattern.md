# Dev-Enabled Action Pattern

## Overview

The Dev-Enabled Action Pattern is a three-phase flow for actions that can be influenced by dev controls. Rather than requiring dev panels to always submit a dev action alongside a play action, this pattern uses dependency inversion to separate the phases:

1. **Prep Phase** (public API) - Creates minimal state
2. **Inject Phase** (dev-only API, optional) - Sets dev overrides
3. **Act Phase** (public API) - Consumes prepped state and applies dev overrides if present

## Pattern Structure

```
Phase 1: Prep (public API)
  - Creates minimal aggregate state
  - Returns ID for next phases
  - Example: POST /api/games/prep → GameSession.Prepped

Phase 2: Inject (dev-only API, optional)
  - Sets dev overrides on the prepped state
  - Skipped if no dev options set
  - Protected by DevRoleGuard
  - Example: POST /api/dev/sessions/{id}/town-layout/set-salts → DevLayoutSaltsForced

Phase 3: Act (public API)
  - Loads prepped state
  - Applies dev overrides if present
  - Produces final events
  - Example: POST /api/games/{id}/start → GameStarted
```

## Architecture Compliance

The pattern is consistent with Wild Bunch architecture:

- **DDD/CQRS/Event Sourcing**: Each phase is a command → handler → aggregate root → events
- **Dev-only API contract**: Dev injection endpoints are separate, guarded by DevRoleGuard
- **Dependency inversion**: UI orchestrates the phases; backend just executes each phase
- **Normal play API**: Doesn't accept dev parameters - it just consumes whatever was set

## Examples

### Game Session (BUNCH-147)

**Prep Phase:**
```csharp
POST /api/games/prep
{
  "seedCode": "abc123",
  "gameDifficulty": "Standard",
  "gameEntropy": "Classic"
}
→ PrepGameSessionResult { gameSessionId: "guid" }
```

**Inject Phase (optional):**
```csharp
POST /api/dev/sessions/{id}/town-layout/set-salts
{
  "buildingsSalt": "buildings",
  "roadsSalt": "roads",
  "dirtSalt": "dirt",
  "propsSalt": "props"
}
→ 204 No Content
```

**Act Phase:**
```csharp
POST /api/games/{id}/start
→ GameSessionDto (with world generated using dev salts if set)
```

### Encounter (Future Example)

**Prep Phase:**
```csharp
POST /api/travel/encounters/prep
→ PrepEncounterResult { encounterId: "guid" }
```

**Inject Phase (optional):**
```csharp
POST /api/dev/travel/encounters/{id}/force-encounter
{
  "forcedEncounterType": "BanditAmbush"
}
→ 204 No Content
```

**Act Phase:**
```csharp
POST /api/travel/encounters/{id}/resolve
→ EncounterDto (with forced encounter if set)
```

## When to Use This Pattern

Use this pattern when:
- An action can be influenced by dev controls
- You want to keep the normal play API clean (no dev parameters)
- You want the backend to decide if dev options were set, not the API call
- You want to apply dependency inversion between UI and backend

## When NOT to Use This Pattern

Don't use this pattern when:
- The action is always dev-only (use a single dev endpoint)
- The action has no dev controls (use a single public endpoint)
- The dev control is a one-time toggle that doesn't need state persistence

## Implementation Guidelines

1. **Prep Phase**:
   - Create minimal aggregate state with just the core parameters
   - Return an ID for the next phases
   - Use a status enum value (e.g., `GameStatus.Prepped`)

2. **Inject Phase**:
   - Use dev-only endpoints (under `/api/dev/`)
   - Protect with DevRoleGuard
   - Apply dev events to the prepped state
   - Make this phase optional - if skipped, the act phase uses default behavior

3. **Act Phase**:
   - Load the prepped state
   - Check for dev overrides
   - Apply dev overrides if present
   - Produce final events
   - Return the final DTO

4. **Backend Logic**:
   - The act phase handler should check if dev overrides were set
   - Use conditional logic: `if (devOverrides is not null) { ... } else { ... }`
   - Don't require the UI to tell the backend whether dev overrides are present

5. **Frontend Orchestration**:
   - UI calls prep phase
   - UI checks if dev options are set in the dev overlay
   - If set, UI calls inject phase
   - UI calls act phase
   - The backend decides whether to use dev overrides

## Related Patterns

- **Dev-Only API Pattern**: For actions that are always dev-only (no public endpoint)
- **Normal Play API Pattern**: For actions with no dev controls (single public endpoint)
- **Three-Phase Setup Pattern**: This pattern (prep → inject → act)

## References

- BUNCH-147: Town Hub Deterministic Layout Resolver
- ADR-0028: Event-Sourced Command Flows
- `.agents/docs/architecture-guardrails.md`
- `.agents/docs/dev-overlay-doctrine.md`
