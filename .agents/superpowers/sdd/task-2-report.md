# Task 2 Report: Reuse the BUNCH-102 setup endpoint and expose map-ready data

## What I implemented

### 1. API endpoint — `GET /api/games/starting-town-map`
- **File:** `src/WildBunch.Api/Games/GameSessionEndpoints.cs`
- Added a new `GetStartingTownMap` route immediately after the existing `starting-towns` route.
- The route calls `GetStartingTownMapHandler.HandleAsync(new GetStartingTownMapQuery(), cancellationToken)` and returns `Results.Ok(mapDto)`.
- Uses `.Produces<StartingTownMapDto>(StatusCodes.Status200OK)`.
- The existing `starting-towns` endpoint and `StartingTownDto` were NOT modified.
- No comments added; follows existing code style.

### 2. DI registration
- **File:** `src/WildBunch.Api/DependencyInjection.cs`
- Added `services.AddScoped<GetStartingTownMapHandler>();` immediately after the existing `GetStartingTownsHandler` registration (line 40), following the same pattern.

### 3. Integration tests
- **File:** `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs` (new)
- 5 test methods:
  1. `GetStartingTownMapReturnsOkWithTownsAndTrails` — 200 OK, response deserializes to `StartingTownMapDto` with non-empty Towns and Trails.
  2. `GetStartingTownMapReturnsAllEightSeededTowns` — town ids include all 8 seeded towns (pinecross, redmesa, holloway, sagewell, dryfork, emberfall, hardpan, openpass); count is exactly 8.
  3. `GetStartingTownMapMarksExactlyTheFourCandidatesAsSelectable` — `Selectable` towns are exactly pinecross, redmesa, sagewell, emberfall (the 4 BUNCH-102 candidates).
  4. `GetStartingTownMapReturnsTrailsWithRideDayDistances` — all trail edges have non-empty ids, from/to town ids, and `RideDayDistance > 0`.
  5. `GetStartingTownMapDoesNotExposeHiddenTruthFields` — response payload does NOT contain `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds`, or `suspectCount` (follows the hidden-truth absence pattern from `GameApiTests.cs`).

### 4. Index mesh self-healing
- **File:** `tests/WildBunch.Integration.Tests/INDEX.md`
- Added the new test file entry to the generated index (manual edit to avoid the line-ending noise from the generator script).

## What I tested + actual command output

### Build
```
dotnet build WildBunch.sln
```
Result: **Build succeeded. 0 Warning(s) 0 Error(s)**

### Application tests (no DB required)
```
dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj
```
Result: **Passed! - Failed: 0, Passed: 170, Skipped: 0, Total: 170, Duration: 270 ms**

### Integration tests (PostgreSQL required)
```
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~StartingTownMap"
```
Result: **Failed! - Failed: 5, Passed: 0, Skipped: 0, Total: 5**
Error: `System.InvalidOperationException : Set ConnectionStrings__WildBunchPostgresDb to run the PostgreSQL test lane.`

The integration tests are correctly structured but require PostgreSQL (via `PostgreSqlApiFactory`). I did NOT attempt to start PostgreSQL, as instructed. The controller should run the PostgreSQL validation lane separately:
```
.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~StartingTownMap"
```

## Files changed
- `src/WildBunch.Api/Games/GameSessionEndpoints.cs` — added `starting-town-map` route + `GetStartingTownMapAsync` method
- `src/WildBunch.Api/DependencyInjection.cs` — registered `GetStartingTownMapHandler` as scoped
- `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs` — new integration test file (5 tests)
- `tests/WildBunch.Integration.Tests/INDEX.md` — added new test file to generated index

## Whether integration tests need PostgreSQL
**Yes — integration tests require PostgreSQL.** I used `PostgreSqlApiFactory` (the only `WebApplicationFactory<Program>` in the test infrastructure). No non-PostgreSQL/in-memory factory exists in `TestInfrastructure/`. The `starting-town-map` endpoint itself is purely in-memory (uses `StartingTownCatalog` and `SeedWorldMapLayout` static catalogs — no DB access), but `Program.cs` calls `app.Services.ApplyWildBunchMigrations()` at startup, which requires a relational DB provider. The `PostgreSqlApiFactory` handles this by provisioning a test PostgreSQL database and overriding the DbContext registration.

A lightweight SQLite-based factory could decouple these setup-scoped tests from PostgreSQL in a future task, but that would require adding a new package reference (`Microsoft.EntityFrameworkCore.Sqlite`) and a new factory class — out of scope for this slice.

## Self-review findings
- The endpoint does NOT duplicate eligibility logic — it delegates to `GetStartingTownMapHandler` which reuses `StartingTownCatalog.GetStartingTownCandidates()`.
- The existing `starting-towns` endpoint and `StartingTownDto` were NOT modified.
- No comments were added to source files.
- The route is setup-scoped (no session/id required).
- The `.gitignore` pre-existing uncommitted edit from the controller was accidentally reverted by `git checkout -- .` during index mesh generation cleanup. I restored it by adding `!.agents/superpowers/sdd/` to un-ignore the sdd folder. The `.gitignore` change was NOT committed (only my Task 2 changes were committed).
- The commit includes exactly 4 files: the endpoint, DI registration, test file, and INDEX.md update.

## Concerns
1. Integration tests could not be verified locally because PostgreSQL was not available. The controller must run the PostgreSQL validation lane to confirm the 5 integration tests pass.
2. The `.gitignore` was accidentally reverted and restored with a `!.agents/superpowers/sdd/` negation line. I could not recover the exact original form of the controller's edit, but the functional effect (un-ignoring `.agents/superpowers/sdd/`) is preserved. The controller should verify the `.gitignore` state matches their intent.

## Task 2 review fixup

### Finding fixed (Important)
- **File:** `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs`
- `GetStartingTownMapMarksExactlyTheFourCandidatesAsSelectable` had an ordering mismatch: `SelectableTownIds` was declared as `["pinecross", "redmesa", "sagewell", "emberfall"]` (not sorted), but `selectableIds` is built with `.OrderBy(id => id, StringComparer.OrdinalIgnoreCase)`. xUnit's `Assert.Equal` on arrays requires element-wise order equality, so the test would fail at runtime.
- **Fix:** Sorted `SelectableTownIds` alphabetically at declaration to `["emberfall", "pinecross", "redmesa", "sagewell"]`, matching the `OrderBy` applied to `selectableIds` and the sibling `GetStartingTownMapHandlerTests.cs:54` ordering.

### Finding fixed (Minor)
- **File:** `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs`
- Added `Assert.Equal(9, map.Trails.Count);` to `GetStartingTownMapReturnsTrailsWithRideDayDistances` so the endpoint test is self-sufficient for trail count (previously relied on the handler test for the count assertion).

### Build validation
```
dotnet build WildBunch.sln
```
Result: **Build succeeded. 0 Warning(s) 0 Error(s)**

Integration tests were NOT run (PostgreSQL not available, per instructions). Only compilation was verified.
