# Domain Model

## Ownership

- `GameSession` is the live-play aggregate root, command entry point, event-production boundary, and apply-dispatch owner.
- `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, and `ActionContextTracker` are internal child owners under `GameSession`.
- A child component receives narrow context, returns an outcome or events-to-produce, and does not reference or mutate another owner.
- Cross-component coordination, session clock, player, world, case file, and persistence seams remain aggregate-level concerns.

## Gameplay invariants

- Keep wallet and inventory concrete.
- Keep horse and saddle separate. Mounted travel requires a living, non-lame horse and a saddle.
- Keep hidden culprit truth internal and expose only player-known investigation state.
- Preserve clue, journal, warrant, and wanted-suspect flows unless the task changes them.

## Travel checks

- Use the active `JourneyLoop`, `TravelJourney`, diary, completed-history, dev-override, and encounter-resolution paths.
- Advance travel one trail day at a time.
- Preserve origin, destination, route, progress, travel mode, player and horse condition, resources, and pending encounter state when relevant.
- Pause when player choice is required.
- Produce typed events and apply them; do not mutate travel state directly.

Inspect `src/WildBunch.Domain/Game/GameSession.cs`, the relevant child component, its events, and covering tests before retaining a current-state claim.
