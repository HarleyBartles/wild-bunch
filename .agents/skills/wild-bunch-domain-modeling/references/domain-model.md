# Domain Notes

- Prefer `GameSession` as the live-play Aggregate Root unless the source proves another root.
- External live-play commands mutate through `GameSession`.
- `GameSession` owns `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`,
  and `ActionContextTracker`; child components receive narrow context and return
  outcomes or events-to-produce.
- Owned aggregate/component files under the root may own cohesive state, behavior, invariants, and lifecycle transitions.
- Policy/coordinator/resolver extraction is not aggregate extraction unless a DDD aggregate/component owns responsibility.
- Keep wallet and inventory concrete.
- Preserve the project's internal hidden-culprit truth.
- Keep clue, journal, and wanted-poster flows stable unless the scope says otherwise.
- Keep horse and saddle separate.
- Mounted travel requires a living, non-lame horse and a saddle.
- Do not turn water into a generic stackable good by accident.
- Use `JourneyLoop`, `TravelJourney`, diary, completed-history, dev override, and
  encounter-resolution routes for current travel work.
- Treat travel as the active `JourneyLoop` / trail-day loop under `GameSession`,
  not a single instant leap.
- Do not add direct travel mutations outside the event-sourced aggregate route.
- Prefer journey state that can represent origin, destination, route profile, remaining days or distance, travel mode, player condition, horse condition, resources, and pending encounter state.
- Advance travel by trail day and pause when a player decision is required.
