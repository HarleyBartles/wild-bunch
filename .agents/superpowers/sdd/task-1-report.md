# Task 1 Report: Extend the BUNCH-102 setup read model for map coordinates

## What I implemented

### 1. `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` (new)
A static, deterministic coordinate table for all 8 seeded towns plus a map projection of the canonical world's trail edges with ride-day distances.

- `SeedMapTown` record: `Id`, `Name`, `Services`, `X`, `Y`.
- `SeedMapTrailEdge` record: `Id`, `FromTownId`, `ToTownId`, `RideDayDistance`.
- `SeedWorldMapLayout.GetMapTowns()` — builds the canonical world via `SeedWorldCatalog.CreateWorld(SeedWorldVariant.Canonical)` (accessible because the layout lives in the same assembly) and joins each town to its static coordinate. Returns all 8 towns.
- `SeedWorldMapLayout.GetMapTrails()` — returns all 9 canonical trails with their `RideDayDistance` sourced from the world (not duplicated as literals).
- Coordinates are modest integers in the 0–800 range, hand-placed so the trail graph is readable. No procedural map art.

Coordinates:
| Town      | X   | Y   |
|-----------|-----|-----|
| pinecross | 150 | 500 |
| redmesa   | 450 | 400 |
| holloway  | 300 | 650 |
| sagewell  | 600 | 550 |
| dryfork   | 700 | 300 |
| emberfall | 800 | 500 |
| hardpan   | 100 | 300 |
| openpass  | 80  | 700 |

### 2. `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs` (new)
A companion map DTO (does not touch the existing `StartingTownDto`, preserving the `GET /api/games/starting-towns` contract):

- `StartingTownMapDto(Towns, Trails)`
- `StartingTownMapTownDto(Id, Name, Services, X, Y, Selectable)`
- `StartingTownMapTrailDto(Id, FromTownId, ToTownId, RideDayDistance)`

### 3. `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs` + `GetStartingTownMapHandler.cs` (new)
- `GetStartingTownMapQuery` — parameterless query record.
- `GetStartingTownMapHandler.HandleAsync` — reuses `StartingTownCatalog.GetStartingTownCandidates()` to compute the candidate id set, then projects `SeedWorldMapLayout.GetMapTowns()` into `StartingTownMapTownDto` with a `Selectable` flag set from the candidate set. Trail edges come from `SeedWorldMapLayout.GetMapTrails()`. No eligibility logic is duplicated.

### 4. `tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs` (new)
8 tests covering:
- All 8 seeded towns are returned.
- `Selectable` towns match `StartingTownCatalog.GetStartingTownCandidates()` (set equality, order-insensitive).
- The 4 selectable towns are exactly emberfall, pinecross, redmesa, sagewell.
- Selectable town ids match `GetStartingTownsHandler` output (proves the candidate source is shared, not a second algorithm).
- Coordinates are deterministic across calls.
- Trail edges carry the correct ride-day distances from `SeedWorldCatalog` (all 9 edges asserted).
- Trail edges only connect rendered towns (no dangling edge endpoints).
- All 9 seeded trails are present.

## The DTO shape I chose and why

I chose to include **all 8 seeded towns** in the map DTO (with a `Selectable` boolean flag) plus **all 9 trail edges**, rather than only the 4 candidate towns.

Rationale:
- The task brief requires the layout to cover all 8 towns so trail edges between non-candidate towns still render.
- If the DTO only listed the 4 candidate towns, the 5 trail edges touching non-candidate towns (e.g. `trail-pine-hollow`, `trail-red-dry`, `trail-pine-hardpan`, `trail-pine-openpass`, `trail-hollow-sage`) would dangle — referencing town ids with no corresponding marker. That is not an honest representation for a frontend to render.
- Including all 8 towns with a `Selectable` flag gives full spatial context (the player sees the whole world graph) while keeping the candidate source shared: `Selectable` is derived solely from `StartingTownCatalog.GetStartingTownCandidates()`. The frontend renders all towns as markers but only marks candidates as clickable.
- This is the smallest honest representation that (a) covers all 8 towns, (b) keeps every trail edge connected to a rendered marker, and (c) reuses the BUNCH-102 eligibility source without duplicating it.

The existing `StartingTownDto` is left untouched, so the existing `GET /api/games/starting-towns` contract is not broken. The map read model is a separate companion projection.

## What I tested and test results

### Build
```
dotnet build WildBunch.sln
...
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Focused tests (TDD RED → GREEN)
First run (RED) — wrote the test file before the implementation existed; after implementation, one test failed because the map DTO preserves catalog order while `GetStartingTownCandidates()` returns name-ordered towns. Fixed the test to compare as order-insensitive sets (the map DTO's town order is intentionally the catalog order, not the candidate order):

```
dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GetStartingTownMapHandlerTests|FullyQualifiedName~GetStartingTownsHandlerTests"
...
Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 67 ms
```

### Full Application.Tests project (no regressions)
```
dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --no-build
...
Passed!  - Failed:     0, Passed:   170, Skipped:     0, Total:   170, Duration: 271 ms
```

## TDD evidence
- Wrote `GetStartingTownMapHandlerTests.cs` first (RED: the handler/DTO/layout types did not exist, so the test project did not compile).
- Implemented `SeedWorldMapLayout`, `StartingTownMapDto`, `GetStartingTownMapQuery`, `GetStartingTownMapHandler`.
- Build succeeded; ran tests — 1 failed on ordering semantics (`SelectableTownsMatchStartingTownCandidates` compared name-ordered candidates against catalog-ordered selectable towns).
- Corrected the test to compare order-insensitively (the ordering difference is intentional and correct: the map DTO keeps catalog order; the candidate list is name-ordered).
- Re-ran — GREEN: 11/11 passed, 170/170 full project passed.

## Files changed
- `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` (new)
- `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs` (new)
- `src/WildBunch.Application/Games/Queries/GetStartingTownMapQuery.cs` (new)
- `src/WildBunch.Application/Games/Queries/GetStartingTownMapHandler.cs` (new)
- `tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs` (new)

## Self-review findings
- No comments added; follows existing code style (records, no doc comments on DTOs/handlers, `ArgumentNullException.ThrowIfNull` on query).
- `StartingTownDto` and the existing `GET /api/games/starting-towns` contract are untouched.
- Map truth stays in `WildBunch.GameContent/NewGame/` next to `SeedWorldCatalog.cs`; the frontend will consume read data only. No map truth moved into the web project.
- Eligibility logic is not duplicated: `GetStartingTownMapHandler` calls `StartingTownCatalog.GetStartingTownCandidates()` for the candidate set, same as `GetStartingTownsHandler`.
- Ride-day distances are sourced from the canonical `World` built by `SeedWorldCatalog.CreateWorld`, not redeclared as literals in the layout — so they cannot drift from the catalog.
- No DB tables added; no runtime session state normalized. Read-only query handler.
- `SeedWorldMapLayout` is `public` (like `StartingTownCatalog`) so the Application handler can call it; `SeedWorldCatalog` remains `internal`.
- No ADR log read this turn, so no ADR freshness responsibility triggered.

## Issues or concerns
- None. The endpoint wiring (controller route for `GetStartingTownMapQuery`) is intentionally out of scope for this task (Task 1 is the read model + layout; a later task wires the web/controller seam).
