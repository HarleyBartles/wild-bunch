### Task 2: Reuse the BUNCH-102 setup endpoint and expose map-ready data

**Files:**
- Modify: `src/WildBunch.Application/Games/Queries/GetStartingTownsHandler.cs` or the BUNCH-102 setup-town query surface
- Modify: `src/WildBunch.Api/Games/GameSessionEndpoints.cs` or the BUNCH-102 setup endpoint surface
- Create or modify: `tests/WildBunch.Application.Tests/Games/Queries/GetStartingTownsHandlerTests.cs`
- Create or modify: `tests/WildBunch.Api.Tests/Games/GameSessionEndpointsTests.cs` — **PLAN PATH REPAIR:** no `WildBunch.Api.Tests` project exists in this repo. Put endpoint tests in `tests/WildBunch.Integration.Tests/` (which already has `GameApiTests.cs` and `PostgreSqlApiFactory` / `WebApplicationFactory` setup). Do NOT invent a new test project.

**Interfaces:**
- Consumes: the BUNCH-102 setup-town candidate source and the map coordinate extension.
- Produces: a setup-scoped read endpoint that returns the existing candidate towns plus optional map-ready coordinates and trail edges.

- [ ] **Step 1: Verify and reuse the BUNCH-102 setup-town endpoint rather than creating a second eligibility algorithm.**

If the existing endpoint can carry coordinates and edges, extend it; otherwise add a clearly named companion map endpoint that still uses the same candidate source.

- [ ] **Step 2: Add or extend the API route without duplicating eligibility.**

Use a setup-scoped map route rather than forcing the caller to create a session first.

- [ ] **Step 3: Add tests that prove the map data is deterministic, backend-sourced, and shares the candidate list with BUNCH-102.**

The tests should assert town ids, coordinates, trail distances, and candidate eligibility without depending on frontend state.

## Global Constraints (binding for this task)

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- Keep the backend/application/domain route authoritative for towns, routes, distances, start eligibility, and game creation.
- Do not normalize runtime session state into new tables for this slice.
- The allowed-town list must come from the same eligibility logic BUNCH-102 already owns (`StartingTownCatalog.GetStartingTownCandidates()`).
- Use a setup-scoped map route (no session required) rather than forcing the caller to create a session first.
- Do not add comments unless asked. Follow existing code style (the existing endpoint files have no comments).

## Task 1 output (already landed on this branch — consume it)

Task 1 added (commit `65cc149`):
- `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` — `GetMapTowns()` returns all 8 towns with X/Y coords; `GetMapTrails()` returns all 9 trail edges with `RideDayDistance`.
- `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs` — `StartingTownMapDto(Towns, Trails)`, `StartingTownMapTownDto(Id, Name, Services, X, Y, Selectable)`, `StartingTownMapTrailDto(Id, FromTownId, ToTownId, RideDayDistance)`.
- `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs` + `GetStartingTownMapHandler.cs` — parameterless query; handler reuses `StartingTownCatalog.GetStartingTownCandidates()` for the `Selectable` flag, projects all 8 towns + 9 trail edges.

This task wires the API endpoint for `GetStartingTownMapQuery` and adds endpoint/integration tests.
