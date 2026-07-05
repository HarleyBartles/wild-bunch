# Collapse SeedWorldBuilder into SeedWorldFactory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Collapse the split-brained `SeedWorldBuilder`/`SeedWorldCatalog` pair into a single `SeedWorldFactory` and move the canonical-shape predicate onto `SeedWorld.IsCanonical`, deleting the dead wrapper.

**Architecture:** Pure rename/move refactor — zero behavior change. `SeedWorldCatalog` is renamed to `SeedWorldFactory` (file + class + all references). `SeedWorldBuilder.IsCanonicalSeedWorld` becomes a computed `IsCanonical` property on the `SeedWorld` record. `SeedWorldBuilder.cs` is deleted. Tests, AGENTS.md, INDEX.md, and ADR-0012 references are updated.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, PowerShell (for index mesh regeneration)

## Global Constraints

- **Zero behavior change.** This is a pure rename/move refactor. No logic changes, no new fields, no API surface changes beyond the rename and the property move.
- **No compatibility shims.** Per `src/WildBunch.GameContent/AGENTS.md`: do not add compatibility shims for old names. Rename all call sites in one pass.
- **`SeedWorldFactory` is `internal static`** (same as `SeedWorldCatalog` today). It is visible to `WildBunch.GameContent.Tests` via `InternalsVisibleTo`.
- **`SeedWorld.IsCanonical` is a computed instance property** on the `public sealed record SeedWorld`. It reads only existing record fields. No new constructor parameters.
- **INDEX.md files are regenerated via `python scripts/generate_index_mesh.py`** — do not hand-edit them.
- **Historical/design spec references are out of scope.** `.agents/superpowers/specs/2026-07-03-geometry-first-procedural-map-generation-design.md` and `.agents/superpowers/sdd/historical/*` describe past state (what was replaced) and are not updated by this refactor. They are historical artifacts.
- **Validation:** `dotnet build` and full `dotnet test` must pass, matching the issue DoD. Integration tests require the shared PostgreSQL dev cluster via `.\scripts\postgres-dev.ps1 ensure` (see Task 6). This refactor touches no integration/persistence surface, but the full suite is run to confirm zero behavior change end-to-end.

## File Structure

| File | Action | Responsibility |
| --- | --- | --- |
| `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` | Rename to `SeedWorldFactory.cs`; rename class `SeedWorldCatalog` → `SeedWorldFactory` | Name pool, palettes, `DeriveTownNames`, `CreateWorld`, `CreateCanonicalWorld` |
| `src/WildBunch.GameContent/NewGame/SeedWorld.cs` | Modify: add `IsCanonical` computed property; update `GetSelectedTownIds` to call `SeedWorldFactory` | Seed-owned world record + canonical shape query |
| `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs` | Delete | (removed — wrapper is dead ceremony) |
| `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs` | Modify line 65: `SeedWorldBuilder.IsCanonicalSeedWorld(seedWorld)` → `seedWorld.IsCanonical` | Pipeline orchestration |
| `src/WildBunch.GameContent/NewGame/StartingTownCatalog.cs` | Modify line 14: `SeedWorldCatalog.CreateCanonicalWorld()` → `SeedWorldFactory.CreateCanonicalWorld()` | Starting town candidate source |
| `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` | Modify line 22: `SeedWorldCatalog.CreateCanonicalWorld()` → `SeedWorldFactory.CreateCanonicalWorld()` | Start-screen map layout |
| `src/WildBunch.GameContent/NewGame/MapGenerator.cs` | Modify lines 26, 58, 60, 90: `SeedWorldCatalog.*` → `SeedWorldFactory.*` | Geometry-first map generation |
| `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs` | Rename to `SeedWorldFactoryTests.cs`; rename class; update `SeedWorldBuilder.CreateCanonicalWorld()` → `SeedWorldFactory.CreateCanonicalWorld()` (6 call sites) | Canonical-world unit tests |
| `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs` | Modify lines 209, 210, 216, 217: `SeedWorldCatalog.NamePool` → `SeedWorldFactory.NamePool` | Seed resolver tests |
| `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` | Modify line 169: `SeedWorldCatalog.NamePool` → `SeedWorldFactory.NamePool` | Seeded new-game factory tests |
| `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs` | Modify lines 229, 237, 239: `SeedWorldCatalog.DeriveTownNames`/`NamePool` → `SeedWorldFactory.*` | Map generator tests |
| `src/WildBunch.GameContent/AGENTS.md` | Modify line 53: `SeedWorldCatalog.cs` → `SeedWorldFactory.cs`; `SeedWorldBuilderTests` → `SeedWorldFactoryTests` | Project guidance |
| `docs/adr/ADR-0012-gamecontent-in-code-now-db-backed-content-later.md` | Modify line 98: `SeedWorldBuilder.cs` → `SeedWorldFactory.cs` | ADR freshness (per AGENTS.md root rule) |
| `src/WildBunch.GameContent/NewGame/INDEX.md` | Regenerate via script | Index mesh |
| `tests/WildBunch.GameContent.Tests/INDEX.md` | Regenerate via script | Index mesh |
| `.agents/superpowers/plans/INDEX.md` | Regenerate via script | Index mesh |

---

## Task 1: Rename `SeedWorldCatalog` → `SeedWorldFactory` (file + class)

This is the foundational rename. All subsequent tasks depend on the new name existing. We do the file rename via `git mv` to preserve history, then update the class declaration and the XML doc comment that refers to "catalog".

**Files:**
- Rename: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` → `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs` (class declaration + doc comment)

**Interfaces:**
- Consumes: nothing new
- Produces: `internal static class SeedWorldFactory` with identical members (`NamePool`, `DeriveTownNames`, `CreateWorld`, `CreateCanonicalWorld`) — same signatures, same namespace `WildBunch.GameContent.NewGame`

- [ ] **Step 1: Rename the file via git mv**

Run from the worktree root:
```bash
git mv src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs
```

- [ ] **Step 2: Rename the class declaration and update the doc comment**

In `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs`, replace the class declaration and its preceding doc comment.

Find (around line 108-117):
```csharp
/// <summary>
/// The slot-based world catalog. Town names are flavor — derived from the
/// seed, not encoded. The catalog provides a name pool (40 entries, twice
/// the max town count of 20) and a slot-based trail topology covering
/// slots 0-19. Services and prosperity are palette-indexed. The seed
/// encodes only: town count, variant, services palette, prosperity palette,
/// accusation index, culprit index, and cash bonus. Bandwidth scales with
/// max selection (20), not catalog size.
/// </summary>
internal static class SeedWorldCatalog
```

Replace with:
```csharp
/// <summary>
/// The slot-based world factory. Town names are flavor — derived from the
/// seed, not encoded. The factory provides a name pool (40 entries, twice
/// the max town count of 20) and a slot-based trail topology covering
/// slots 0-19. Services and prosperity are palette-indexed. The seed
/// encodes only: town count, variant, services palette, prosperity palette,
/// accusation index, culprit index, and cash bonus. Bandwidth scales with
/// max selection (20), not factory size.
/// </summary>
internal static class SeedWorldFactory
```

- [ ] **Step 3: Verify the build compiles (expect reference errors — they will be fixed in Tasks 2-4)**

Run: `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj`
Expected: FAIL with CS0103 "The name 'SeedWorldCatalog' does not exist in the current context" errors at the call sites listed in Tasks 2-4. This confirms the rename took effect and the remaining work is reference updates.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs
git commit -m "refactor: rename SeedWorldCatalog file to SeedWorldFactory"
```

---

## Task 2: Move `IsCanonicalSeedWorld` to `SeedWorld.IsCanonical` and delete `SeedWorldBuilder.cs`

This task moves the canonical-shape predicate onto the `SeedWorld` record as a computed property, updates the `SeedWorld.GetSelectedTownIds` call to use the new factory name, and deletes the now-dead `SeedWorldBuilder.cs`.

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorld.cs`
- Delete: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`

**Interfaces:**
- Consumes: `SeedWorldFactory.DeriveTownNames` (from Task 1)
- Produces: `SeedWorld.IsCanonical` (computed `bool` property on the `SeedWorld` record)

- [ ] **Step 1: Add the `IsCanonical` property to `SeedWorld`**

In `src/WildBunch.GameContent/NewGame/SeedWorld.cs`, add the computed property inside the record body, after `SeedCodeText` and before `GetSelectedTownIds`.

Find:
```csharp
    public string SeedCodeText => SeedCode.ToString("D");

    /// <summary>
    /// Derives the selected town IDs for this seed world by running the
```

Replace with:
```csharp
    public string SeedCodeText => SeedCode.ToString("D");

    /// <summary>
    /// Whether this seed world is the canonical shape (8 towns,
    /// Canonical variant, HubTelegraph services, UniformProsperous prosperity,
    /// single cluster, Sparse graph density, accusation index 1,
    /// default culprit index 3, zero cash bonus). Used by GameSetupResolver
    /// to select the canonical case file path.
    /// </summary>
    public bool IsCanonical =>
        WorldVariant == SeedWorldVariant.Canonical
            && TownCount == 8
            && ServicesPalette == ServicesPalette.HubTelegraph
            && ProsperityPalette == ProsperityPalette.UniformProsperous
            && ClusterCount == 1
            && GraphDensity == GraphDensity.Sparse
            && AccusationIndex == 1
            && DefaultCulpritIndex == 3
            && CashBonus == 0;

    /// <summary>
    /// Derives the selected town IDs for this seed world by running the
```

- [ ] **Step 2: Update `GetSelectedTownIds` to call `SeedWorldFactory`**

In the same file, replace the `SeedWorldCatalog.DeriveTownNames` call inside `GetSelectedTownIds`.

Find (around line 56):
```csharp
        => SeedWorldCatalog.DeriveTownNames(
```

Replace with:
```csharp
        => SeedWorldFactory.DeriveTownNames(
```

- [ ] **Step 3: Delete `SeedWorldBuilder.cs`**

Run from the worktree root:
```bash
git rm src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs
```

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorld.cs
git commit -m "refactor: move IsCanonicalSeedWorld to SeedWorld.IsCanonical, delete SeedWorldBuilder"
```

---

## Task 3: Update production call sites (`GameSetupResolver`, `StartingTownCatalog`, `SeedWorldMapLayout`, `MapGenerator`)

Update the four production files that still reference the old names.

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs:65`
- Modify: `src/WildBunch.GameContent/NewGame/StartingTownCatalog.cs:14`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs:22`
- Modify: `src/WildBunch.GameContent/NewGame/MapGenerator.cs:26,58,60,90`

**Interfaces:**
- Consumes: `SeedWorld.IsCanonical` (from Task 2), `SeedWorldFactory.*` (from Task 1)
- Produces: all production references resolved

- [ ] **Step 1: Update `GameSetupResolver.cs`**

In `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`, replace the `IsCanonicalSeedWorld` call.

Find (line 65):
```csharp
        var isCanonical = SeedWorldBuilder.IsCanonicalSeedWorld(seedWorld);
```

Replace with:
```csharp
        var isCanonical = seedWorld.IsCanonical;
```

- [ ] **Step 2: Update `StartingTownCatalog.cs`**

In `src/WildBunch.GameContent/NewGame/StartingTownCatalog.cs`, replace the `CreateCanonicalWorld` call.

Find (line 14):
```csharp
        var world = SeedWorldCatalog.CreateCanonicalWorld();
```

Replace with:
```csharp
        var world = SeedWorldFactory.CreateCanonicalWorld();
```

- [ ] **Step 3: Update `SeedWorldMapLayout.cs`**

In `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`, replace the `CreateCanonicalWorld` call.

Find (line 22):
```csharp
        var world = SeedWorldCatalog.CreateCanonicalWorld();
```

Replace with:
```csharp
        var world = SeedWorldFactory.CreateCanonicalWorld();
```

- [ ] **Step 4: Update `MapGenerator.cs` (4 references)**

In `src/WildBunch.GameContent/NewGame/MapGenerator.cs`, replace all four `SeedWorldCatalog` references with `SeedWorldFactory`. Use a project-wide replace scoped to this file.

Find each occurrence of `SeedWorldCatalog` in `MapGenerator.cs` (lines 26, 58, 60, 90) and replace with `SeedWorldFactory`. The four lines become:

Line 26:
```csharp
        var townNames = SeedWorldFactory.DeriveTownNames(
```

Line 58:
```csharp
            var outlierPool = SeedWorldFactory.DeriveTownNames(
```

Line 60:
```csharp
                townCount: SeedWorldFactory.NamePool.Count,
```

Line 90:
```csharp
        return SeedWorldFactory.CreateWorld(
```

- [ ] **Step 5: Verify the production build compiles**

Run: `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj`
Expected: PASS (zero errors, zero warnings). All production references are now resolved.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/GameSetupResolver.cs src/WildBunch.GameContent/NewGame/StartingTownCatalog.cs src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs src/WildBunch.GameContent/NewGame/MapGenerator.cs
git commit -m "refactor: update production call sites to SeedWorldFactory and SeedWorld.IsCanonical"
```

---

## Task 4: Update tests (rename `SeedWorldBuilderTests` → `SeedWorldFactoryTests`, update all test references)

Update the four test files that reference the old names. The primary test file is renamed to match the new factory.

**Files:**
- Rename: `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs` → `tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs` (class name + 6 `SeedWorldBuilder.CreateCanonicalWorld()` calls)
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs:209,210,216,217`
- Modify: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs:169`
- Modify: `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs:229,237,239`

**Interfaces:**
- Consumes: `SeedWorldFactory.*` (from Task 1), `SeedWorld.IsCanonical` (from Task 2)
- Produces: all test references resolved; test file naming matches production naming

- [ ] **Step 1: Rename the test file via git mv**

Run from the worktree root:
```bash
git mv tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs
```

- [ ] **Step 2: Rename the test class and update all 6 `SeedWorldBuilder.CreateCanonicalWorld()` calls**

In `tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs`:

Find the class declaration (line 8):
```csharp
public sealed class SeedWorldBuilderTests
```

Replace with:
```csharp
public sealed class SeedWorldFactoryTests
```

Then replace every occurrence of `SeedWorldBuilder.CreateCanonicalWorld()` with `SeedWorldFactory.CreateCanonicalWorld()`. There are 6 occurrences (lines 13, 24, 54, 62, 72 in the original file). Use a file-scoped replace-all.

- [ ] **Step 3: Update `SeedWorldResolverTests.cs` (4 references)**

In `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs`, replace all 4 occurrences of `SeedWorldCatalog.NamePool` with `SeedWorldFactory.NamePool` (lines 209, 210, 216, 217). Use a file-scoped replace-all of `SeedWorldCatalog` → `SeedWorldFactory`.

- [ ] **Step 4: Update `SeededNewGameFactoryTests.cs` (1 reference)**

In `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`, replace the single occurrence of `SeedWorldCatalog.NamePool` with `SeedWorldFactory.NamePool` (line 169).

Find:
```csharp
        Assert.Contains(session.Player.CurrentTownId!.Value.Value, SeedWorldCatalog.NamePool.Select(n => n.Id));
```

Replace with:
```csharp
        Assert.Contains(session.Player.CurrentTownId!.Value.Value, SeedWorldFactory.NamePool.Select(n => n.Id));
```

- [ ] **Step 5: Update `MapGeneratorTests.cs` (3 references)**

In `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`, replace all 3 occurrences of `SeedWorldCatalog` with `SeedWorldFactory` (lines 229, 237, 239). Use a file-scoped replace-all of `SeedWorldCatalog` → `SeedWorldFactory`.

- [ ] **Step 6: Run the GameContent test suite**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj`
Expected: PASS — all tests green. This is a pure rename, so no test logic changes are needed; only the referenced type names changed.

- [ ] **Step 7: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs
git commit -m "test: rename SeedWorldBuilderTests to SeedWorldFactoryTests, update all test references"
```

---

## Task 5: Update documentation (`AGENTS.md`, `ADR-0012`) and regenerate INDEX.md mesh

Update the two documentation files that reference the old names, then regenerate the index mesh to pick up the file renames and deletions.

**Files:**
- Modify: `src/WildBunch.GameContent/AGENTS.md:53`
- Modify: `docs/adr/ADR-0012-gamecontent-in-code-now-db-backed-content-later.md:98`
- Regenerate: `src/WildBunch.GameContent/NewGame/INDEX.md`, `tests/WildBunch.GameContent.Tests/INDEX.md`, `.agents/superpowers/plans/INDEX.md` (via script)

**Interfaces:**
- Consumes: completed renames from Tasks 1-4
- Produces: documentation matches the new source layout; index mesh current

- [ ] **Step 1: Update `src/WildBunch.GameContent/AGENTS.md`**

In `src/WildBunch.GameContent/AGENTS.md`, update the "When to update this project" line.

Find (line 53):
```
- **New town or trail**: add to `SeedWorldCatalog.cs`, update `SeedWorldBuilderTests` snapshot assertions, update `SeededNewGameFactoryTests` count assertions.
```

Replace with:
```
- **New town or trail**: add to `SeedWorldFactory.cs`, update `SeedWorldFactoryTests` snapshot assertions, update `SeededNewGameFactoryTests` count assertions.
```

- [ ] **Step 2: Update `docs/adr/ADR-0012-gamecontent-in-code-now-db-backed-content-later.md`**

In `docs/adr/ADR-0012-gamecontent-in-code-now-db-backed-content-later.md`, update the "Related Stable Source Surfaces" entry.

Find (line 98):
```
- `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
```

Replace with:
```
- `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs`
```

- [ ] **Step 3: Regenerate the index mesh**

Run from the worktree root:
```bash
python scripts/generate_index_mesh.py
```

Expected: exits 0. This regenerates `INDEX.md` files in `src/WildBunch.GameContent/NewGame/` (now lists `SeedWorldFactory.cs`, no longer lists `SeedWorldBuilder.cs` or `SeedWorldCatalog.cs`), `tests/WildBunch.GameContent.Tests/` (now lists `SeedWorldFactoryTests.cs`, no longer lists `SeedWorldBuilderTests.cs`), and `.agents/superpowers/plans/` (now lists this plan).

- [ ] **Step 4: Verify the regenerated index files look correct**

Read `src/WildBunch.GameContent/NewGame/INDEX.md` and confirm:
- `SeedWorldFactory.cs` is listed
- `SeedWorldBuilder.cs` is NOT listed
- `SeedWorldCatalog.cs` is NOT listed

Read `tests/WildBunch.GameContent.Tests/INDEX.md` and confirm:
- `SeedWorldFactoryTests.cs` is listed
- `SeedWorldBuilderTests.cs` is NOT listed

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/AGENTS.md docs/adr/ADR-0012-gamecontent-in-code-now-db-backed-content-later.md src/WildBunch.GameContent/NewGame/INDEX.md tests/WildBunch.GameContent.Tests/INDEX.md .agents/superpowers/plans/INDEX.md
git commit -m "docs: update AGENTS.md and ADR-0012 references, regenerate index mesh"
```

---

## Task 6: Final validation

Run the full validation suite to confirm zero behavior change. This matches the issue's Definition of Done (`dotnet build` passes, `dotnet test` passes).

- [ ] **Step 1: Full build**

Run: `dotnet build`
Expected: PASS — zero errors, zero warnings across the entire solution.

- [ ] **Step 2: Full test suite**

Run the full `dotnet test` to match the issue DoD. Integration tests require the shared PostgreSQL dev cluster.

Start the cluster first (idempotent — no-op if already running):
```bash
.\scripts\postgres-dev.ps1 ensure
```

Set the connection string for the session:
```powershell
$env:ConnectionStrings__WildBunchPostgresDb = "Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres"
```

Then run the full suite:
```bash
dotnet test
```

Expected: PASS — all tests green across all test projects (unit, integration, game-content, API). This is a pure rename refactor with no behavior change, so no test should fail. If an integration test fails, investigate whether a stale reference was missed in Task 3 or Task 4 — do not narrow the suite to make it green.

- [ ] **Step 3: Confirm no stale references remain on live surfaces**

Scan only live source, test, and durable-doc surfaces for the old names. Historical plan records (`.agents/superpowers/plans/**`), historical session artifacts (`.agents/superpowers/sdd/historical/**`), and design specs (`.agents/superpowers/specs/**`) are out of scope — they are historical artifacts that describe past state and must not be rewritten as part of this refactor.

```bash
rg -n "SeedWorldBuilder|SeedWorldCatalog|IsCanonicalSeedWorld" src/ tests/ docs/ --glob '!**/INDEX.md'
```

Note: `INDEX.md` files are excluded from the text scan because they are generated by `scripts/generate_index_mesh.py` and were already verified visually in Task 5 Step 4. The scan covers hand-authored source, tests, ADRs, and AGENTS.md files.

Expected: zero matches. If any match appears, it is a stale reference that must be fixed before the refactor is complete.

- [ ] **Step 4: Commit final state if any stray fixes were needed**

If Step 3 found stale references, fix them and commit. If clean, no commit needed — the refactor is complete.
