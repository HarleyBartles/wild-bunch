# ADR-0036 Dev-Enabled Action Pattern

## Status

`live`

## Dated Status History

- 2026-07-10 - live: Dev-Enabled Action Pattern established for BUNCH-147 town hub layout salts integration. Three-phase flow (prep → inject → act) separates dev controls from normal play API. INewGameFactory overload accepts dev salts. PrepGameSessionHandler and StartGameSessionHandler implement the pattern with ExecuteWithRetryAsync compliance. Pattern documented in `.agents/docs/dev-enabled-action-pattern.md` and wired to AGENTS.md.

## Decision Type

architecture, process

## Related ADRs

- `depends on`: ADR-0028 (event-sourced command flows — ExecuteWithRetryAsync pattern)
- `depends on`: ADR-0030 (dev endpoint namespace — dev-only endpoints under /api/dev/)
- `related to`: ADR-0002 (GameSession as aggregate root — prepped state uses GameSession.StartPrepped factory)

## Context

Previous dev controls (travel encounter forcing, saloon forcing) used a pattern where the dev action is submitted alongside the play action. For example, the dev panel would set a forced encounter, then the player would call the travel action which consumed that forced encounter. This pattern couples the dev control to the play action in the UI layer.

For BUNCH-147 (town hub layout salts), we need a way for dev-set layout salts to flow into world generation without coupling the dev panel to the normal play API. The normal `/api/games/setup` endpoint should remain unchanged for normal players, but dev users need a way to inject salts before world generation.

Additionally, we want to establish a reusable pattern for future dev-enabled actions (e.g., encounter forcing, difficulty overrides) that:
- Keeps the normal play API clean (no dev parameters)
- Uses dependency inversion between UI and backend
- Respects the dev-only API contract (DevRoleGuard)
- Follows DDD/CQRS/Event Sourcing patterns

## Decision

1. **Three-phase flow.** Dev-enabled actions use a three-phase flow orchestrated by the UI:
   - **Prep Phase (public API):** Creates minimal aggregate state and returns an ID
   - **Inject Phase (dev-only API, optional):** Sets dev overrides on the prepped state
   - **Act Phase (public API):** Consumes prepped state, applies dev overrides if present, produces final events

2. **Prep phase creates minimal state.** The prep command creates a minimal aggregate with just the core parameters (seed, difficulty, entropy) and a status enum value (e.g., `GameStatus.Prepped`). No world, no case file, no player state. Returns an ID for the next phases.

3. **Inject phase is dev-only and optional.** Dev injection endpoints live under `/api/dev/` and are protected by DevRoleGuard. They apply dev events to the prepped state (e.g., `DevLayoutSaltsForced`). If the inject phase is skipped, the act phase uses default behavior.

4. **Act phase consumes prepped state.** The act phase handler loads the prepped state, checks for dev overrides, applies them if present, and produces final events. The backend decides whether to use dev overrides, not the API call.

5. **Domain logic in factories, not handlers.** Dev override logic (seed parsing, GameSetupResolver calls) lives in domain services or factories (e.g., INewGameFactory), not in command handlers. Handlers delegate to factories.

6. **ExecuteWithRetryAsync compliance.** Act phase handlers inherit from GameSessionCommandHandler and use the ExecuteWithRetryAsync pattern for concurrency retry, correlation ID generation, and event commit orchestration (per ADR-0028).

7. **Normal play API unchanged.** The existing player-facing endpoints (e.g., `/api/games/setup`) remain unchanged. The three-phase flow is a separate flow for dev-enabled actions.

## Options Considered and Rejected

- **Dev parameters in normal play API.** Rejected: this would pollute the normal play API with dev-only parameters and require the UI to tell the backend whether dev options are set. The backend should decide based on state.

- **Single dev-only endpoint.** Rejected: this would prevent normal players from using the action at all. The action should be available to both normal players (without dev controls) and dev users (with dev controls).

- **Dev action alongside play action in UI.** Rejected: this couples the dev panel to the play action and requires the UI to orchestrate the timing. The three-phase flow separates the concerns and lets the backend decide.

- **State machine in UI.** Rejected: this would move domain state management to the UI. The state lives in the aggregate (GameSession), and the UI just orchestrates the phases.

## Consequences

- Future dev-enabled actions (encounter forcing, difficulty overrides, entropy overrides) should use this pattern
- Dev controls are cleanly separated from normal play flow
- The backend decides whether to use dev overrides based on state, not API parameters
- The pattern is documented in `.agents/docs/dev-enabled-action-pattern.md` and wired to AGENTS.md for agent discovery
- Dev endpoints remain under `/api/dev/` with DevRoleGuard protection
- Handlers use ExecuteWithRetryAsync for consistency with ADR-0028
