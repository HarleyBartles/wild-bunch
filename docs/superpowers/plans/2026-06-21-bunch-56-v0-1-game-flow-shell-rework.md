# BUNCH-56 v0.1 Game Flow Shell Rework

## Date
2026-06-21

## Scope
Redirect the v0.1 shell from route showcase to game flow. Map/spatial generation is out of scope (BUNCH-75 owns that).

## Backend Change (1)

### Remove auto-advance from TravelToTownHandler
- **File:** `src/WildBunch.Application/Games/Commands/TravelToTownHandler.cs`
- **Change:** Remove the `session.AdvanceJourneyDay()` call after `StartJourney()`. The handler should only start the journey and return the session state with journey status = Active, daysTravelled = 0.
- **Reason:** Player must explicitly advance trail days. Starting a journey should not auto-advance the first day.
- **Test impact:** Update any characterization tests that expect the first day to advance on travel start.

## Frontend Rework

### Phase 1: Game flow model + pre-session gate

1. **Derive game phase from session state**
   - `pre-session` — no session (session === null)
   - `in-town` — session exists, no journey, no pending arrival
   - `travel-prep` — player chose a destination, showing prep screen (local UI state)
   - `on-trail` — journey active (status Active or Interrupted)
   - `arrival` — journey completed, awaiting acknowledgement (status Completed)

2. **Pre-session gate**
   - When `pre-session`, only show StartGameSurface (moved from CampRoute)
   - Lock all other navigation — no town hub, no trail, no case file, no wanted
   - Dev tools route still accessible

### Phase 2: Town hub

3. **TownHubSurface** (replaces HuntRoute as the main post-start screen)
   - Shows: "You are in {town name}" + available places + trailhead option
   - Places derived from town services + investigation sources:
     - Store (if Supplies service) → StoreSurface (browse/buy/leave)
     - Sheriff Office (if NoticeBoard service) → SheriffSurface (wanted posters, check records)
     - Saloon (if saloon investigation source) → SaloonSurface (look around, gossip, confront)
   - Trailhead → TravelPrepSurface (choose destination)
   - Fits 14-inch laptop viewport without scrolling

4. **StoreSurface** (town place, not a top-level route)
   - Enter store → browse offers → buy → leave → back to town hub
   - Reuses StoreOffersPanel + InventoryPanel

5. **SheriffSurface** (town place)
   - Read wanted posters, check local records
   - Reuses investigation action handlers

6. **SaloonSurface** (town place)
   - Look around saloon, gather gossip, confront person of interest
   - Reuses saloon action handlers

### Phase 3: Travel prep + committed trail

7. **TravelPrepSurface** (trailhead decision)
   - Player picks destination from available trails
   - Shows preparation/confirmation screen:
     - Destination name
     - Ride-day language: "That's a {N}-day ride" (uses baselineRideDays from preview)
     - Visible possessions: inventory, horse, canteen, wallet
     - "Start journey" button + "Back to town" button
   - Does NOT show mechanical warnings (required food/water/feed, dehydration forecasts)
   - Calls `travel()` API (which now just starts journey, no auto-advance)

8. **TrailFlowSurface** (committed trail, replaces TrailRoute)
   - Locked: no store, saloon, sheriff, buying, cancel/turn-back
   - Shows: journey progress, travel diary, advance day button
   - Advance one trail day per click → calls `advanceTravelDay()`
   - Encounter resolution → calls `resolveTravelEncounter()`
   - Arrival acknowledgement → calls `acknowledgeTravelArrival()`
   - Case file + wanted available as reference overlays (close returns to trail)

### Phase 4: Global overlays

9. **CaseFileOverlay** (modal/drawer, available from anywhere)
   - Reuses CaseFileSurface
   - Lazy-loaded journal query
   - Close returns to current flow surface

10. **WantedPostersOverlay** (modal/drawer)
    - Reuses WantedPosterSurface
    - Available after reading wanted posters
    - Close returns to current flow surface

11. **ActivityLogOverlay** (modal/drawer, globally available)
    - Lazy-loaded/paged log entries
    - Does not consume permanent layout space

### Phase 5: Shell restructure

12. **AppShell rework**
    - Remove nav showcase (Camp/Hunt/Case/Wanted/Trail links)
    - HUD stays (sticky compact status bar)
    - Main content area renders flow surface based on game phase
    - Global overlay buttons in HUD or shell chrome: Case file, Wanted, Activity log
    - Dev tools link separated (muted, as before)

13. **Router simplification**
    - `/` → flow router (renders based on phase)
    - `/debug` → Dev tools (unchanged)
    - Remove `/hunt`, `/case`, `/wanted`, `/trail` as top-level routes
    - Town places are UI state within the town hub, not routes

### Phase 6: Layout + styling

14. **14-inch viewport constraint**
    - Main decision screen (town hub, trail flow) fits without vertical scrolling
    - Secondary detail in modals/drawers/collapsibles
    - HUD is compact single row

15. **CSS adjustments**
    - Town hub layout: place cards in a grid, compact
    - Trail flow: progress + actions in one viewport
    - Modal/drawer styles for overlays

## Acceptance Checks

- [ ] Before session, normal game routes are locked behind Start Game
- [ ] After start, player lands in a town hub
- [ ] Store and Sheriff Office are present as guaranteed town places
- [ ] Saloon is a town place with Look Around as the v0.1 action
- [ ] Choosing a travel destination shows a preparation/confirmation screen before commitment
- [ ] Player-facing travel prep uses ride-day language and visible possessions, not hidden mechanical warning tables
- [ ] Starting a journey does not auto-advance a trail day
- [ ] While Journey is active, town actions/store/saloon/sheriff are unavailable
- [ ] There is no cancel/turn-back action once travelling
- [ ] Arrival requires acknowledgement before normal town actions resume
- [ ] Activity log is globally available as a lazy-loaded modal/overlay
- [ ] Main v0.1 decision surfaces fit a 14-inch laptop browser viewport without required scrolling
- [ ] Map/spatial generation is not implemented in this slice; BUNCH-75 owns that

## Validation

- `dotnet build` clean
- `dotnet test` — update travel handler tests for no-auto-advance
- `npm run typecheck` clean
- `npm test` — update shell tests for flow-based routing
- `npm run build` succeeds
- Browser evidence: pre-session gate, town hub, travel prep, trail flow, arrival ack
