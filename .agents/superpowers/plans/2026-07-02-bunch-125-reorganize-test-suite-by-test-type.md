# BUNCH-125 Reorganize Test Suite by Test Type — Preflight Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Linear issue:** [BUNCH-125](https://linear.app/harleys-workspace/issue/BUNCH-125/reorganize-test-suite-by-test-type)
**Route state:** `preflight_needed` → this plan is the preflight artifact. After approval and merge, route state becomes `approved_plan_execution_ready`.
**Branch (plan-only PR):** `harleydbartles/bunch-125-reorganize-test-suite-by-test-type`
**Worktree:** `C:\WORK\repo-workspace\wild-bunch\.worktrees\bunch-125`
**Base commit:** `9194c93` (origin/main tip as of plan authoring)

## Goal

Reorganize the Wild Bunch test suite so that test files are separated by test type into subdirectories that mirror the source-layer structure, and create the missing `.agents/docs/test-patterns.md` guidance document that the issue references. The reorganization must preserve every test, every namespace convention, and every cross-reference; `dotnet build` and `dotnet test` must pass unchanged after the moves.

## Architecture posture (binding)

- **Source of truth:** Current repo state on `main` is the source of truth. The test suite is the executable specification; reorganization must not change test behavior, test names, assertion content, or test count.
- **Scope discipline (AGENTS.md):** Do only the requested slice. No opportunistic refactors of test bodies, no renaming of test methods, no merging or splitting of test classes. Only file moves, namespace updates to match the new folder, `using` updates in cross-referencing files, and the one rename explicitly named in the issue (`CorsPolicyTests` → `CorsConfigurationTests`).
- **Namespace convention:** The repo uses folder-matching namespaces. Files moved into a new subdirectory must update their `namespace` declaration to match the new path (e.g., `WildBunch.Application.Tests.Mappers`). Files that reference moved types via `using` must update their `using` lines.
- **Index mesh:** `python scripts/generate_index_mesh.py` must be run after every file move/add/rename so the committed INDEX.md files match the generator output. The `--check` flag is the CI gate.
- **Testing posture (AGENTS.md):** New or updated real application behavior should normally include test coverage in the same slice. This issue is a reorganization of existing coverage, not new behavior, so no new tests are required.
- **Repo skills:** `testing` (xUnit/.NET 10 patterns), `wild-bunch-dotnet-architecture` (layer boundaries), `clean-architecture` (test-layer separation) are the controlling surfaces.

## Tech Stack

C# / .NET 10, xUnit v2, `WebApplicationFactory` for integration tests, Testcontainers for PostgreSQL-backed integration tests. No new dependencies. No `.csproj` changes — the test projects already use the SDK default that compiles all `.cs` files under the project root recursively.

## Current test suite snapshot (audit baseline)

Source inspection at `9194c93` shows 5 test projects with 137 `.cs` files (excluding `.csproj` and `INDEX.md`):

| Project | Root files | Subdirectories (existing) | Test count by type (approx) |
| --- | --- | --- | --- |
| `WildBunch.Api.Tests` | 1 (`CorsPolicyTests.cs`) | none | 1 configuration |
| `WildBunch.Application.Tests` | 36 root files | `Dev/` (12 handler), `Execution/` (1 handler), `Projections/` (4 projection), `TestDoubles/` (2 helpers) | 24 handler, 6 mapper, 3 renderer, 2 projection, 1 structural/guardrail |
| `WildBunch.Domain.Tests` | 67 root files | `Events/` (3 event sourcing) | ~50 unit/domain, ~10 characterization, 3 event sourcing, 2 structural/guardrail, 2 projection-equivalence, 1 mapper |
| `WildBunch.GameContent.Tests` | 12 root files | none | 12 unit/content |
| `WildBunch.Integration.Tests` | 22 root files | `Acceptance/` (3), `Dev/` (5), `TestInfrastructure/` (8) | 14 integration/endpoint, 3 acceptance, 5 dev-endpoint, 2 persistence, 1 migration, 1 serializer |

### Identified mixed-concern and misplaced files

The audit found these specific files that are misplaced relative to their test type:

1. **Projection tests in `Application.Tests` root** (should be in `Projections/`):
   - `GameSessionDtoProjectionFieldsTests.cs` — tests `GameSessionMapper.ToDto` projection fields (projection/mapper boundary)
   - `ReadStoreLoaderJournalProjectionGuardrailTests.cs` — structural guardrail for projection read path
   - `QueryHandlersAreReadOnlyTests.cs` — structural guardrail proving query handlers do not persist (references `GameSessionLogProjection`)

2. **Mapper tests in `Application.Tests` root** (should be in `Mappers/`):
   - `CaseBoardMapperTests.cs`
   - `ClueTimeAnchorBeatLabelTests.cs` (tests `CaseReadMapper.ToDto`)
   - `JournalMapperTests.cs`
   - `SaloonPersonOfInterestDescriptorParityTests.cs` (tests mapper parity)
   - `TrailBeatSlotDtoTests.cs` (tests `TravelDiaryMapper.ToDto`)
   - `TravelDiaryMapperTests.cs`
   - `WantedPosterMapperTests.cs`

3. **Renderer tests in `Application.Tests` root** (should be in `Renderers/`):
   - `BeatLabelRendererTests.cs`
   - `BeatNarrationRendererTests.cs`
   - `TravelDiaryTextRendererTests.cs`

4. **Handler tests in `Application.Tests` root** (should be in `Handlers/`):
   - All 24 `*HandlerTests.cs` files in the root (excluding `Dev/` and `Execution/` which are already organized)

5. **Configuration test in `Api.Tests` root** (rename + stay at root or move to `Configuration/`):
   - `CorsPolicyTests.cs` → `CorsConfigurationTests.cs` (issue explicitly requests this rename)

6. **Domain.Tests projection-equivalence and mapper tests in root** (consider `Projections/` and `Mappers/`):
   - `JournalLogProjectorEquivalenceTests.cs` — projection equivalence (cross-layer, tests `JournalLogProjector` from Application)
   - `JournalResolverTests.cs` — projection/resolver
   - `TrailBeatSlotMappingTests.cs` — mapper
   - `TravelDiarySnapshotShapeTests.cs` — projection/mapper shape

7. **Domain.Tests characterization tests in root** (consider `Characterization/`):
   - `TravelStateMachineCharacterizationTests.cs`
   - `TravelDiaryCharacterizationTests.cs`
   - `TravelEncounterResolutionCharacterizationTests.cs`
   - `TravelResourceTrackingCharacterizationTests.cs`

8. **Domain.Tests structural/guardrail tests in root** (consider `Guardrails/`):
   - `HeatSemanticGuardrailTests.cs`
   - `ReadStoreLoaderJournalProjectionGuardrailTests.cs` (in Application.Tests)

9. **Domain.Tests event-sourcing tests in root** (consider `EventSourcing/` alongside existing `Events/`):
   - `BountySaloonEventSourcingTests.cs`
   - `InvestigationEventSourcingTests.cs`
   - `ClockTurnCorrectionTests.cs` (event-sourcing apply)
   - `TravelEventApplyTests.cs`
   - `TravelReplayEqualityTests.cs`

### Cross-reference impact

- `using WildBunch.Application.Tests.TestDoubles;` appears in 36 files. The `TestDoubles/` folder is NOT moving, so these usings are stable.
- `using WildBunch.Integration.Tests.TestInfrastructure;` appears in 30 files. The `TestInfrastructure/` folder is NOT moving, so these usings are stable.
- No `using WildBunch.Domain.Tests.*` references exist outside `Domain.Tests` itself.
- No `using WildBunch.Application.Tests;` (root namespace) references exist — all cross-references go through `.TestDoubles` which is stable.
- Files moved between subdirectories within the same project will need their `namespace` declaration updated. Other files in the same project that reference them via `using` will need those `using` lines updated. The executing worker must run `dotnet build` after each task group to catch missing references.

## File Structure

This plan touches only test files and the index mesh. No production source files are created or modified.

**Create:**
- `.agents/docs/test-patterns.md` — the missing test patterns guidance document referenced by the issue.
- New subdirectories under `tests/WildBunch.Application.Tests/`: `Handlers/`, `Mappers/`, `Renderers/`.
- New subdirectories under `tests/WildBunch.Domain.Tests/`: `Characterization/`, `Guardrails/`, `Mappers/`, `Projections/`, `EventSourcing/`.
- New subdirectory under `tests/WildBunch.Api.Tests/`: `Configuration/` (only if the rename target warrants a folder; see Task 1).

**Move (file content unchanged except `namespace` declaration):**
- 24 handler test files from `Application.Tests/` root to `Application.Tests/Handlers/`.
- 7 mapper test files from `Application.Tests/` root to `Application.Tests/Mappers/`.
- 3 renderer test files from `Application.Tests/` root to `Application.Tests/Renderers/`.
- 3 projection/guardrail test files from `Application.Tests/` root to `Application.Tests/Projections/` (or `Guardrails/` for the guardrail — see Task 4).
- Domain.Tests characterization, guardrail, event-sourcing, projection-equivalence, and mapper files to their respective new subdirectories.

**Rename:**
- `tests/WildBunch.Api.Tests/CorsPolicyTests.cs` → `tests/WildBunch.Api.Tests/Configuration/CorsConfigurationTests.cs` (issue explicitly requests this rename; class name `CorsPolicyTests` → `CorsConfigurationTests`).

**Regenerate:**
- All `INDEX.md` files under `tests/` via `python scripts/generate_index_mesh.py`.

## Tasks

### Task 1: Create the missing `.agents/docs/test-patterns.md` guidance document

**Files:**
- Create: `.agents/docs/test-patterns.md`
- Modify: `.agents/docs/INDEX.md` (regenerated by the index-mesh script)

**Interfaces:**
- Consumes: the test-type taxonomy from the issue, the existing test suite structure, and the `testing` repo skill.
- Produces: a durable guidance document that future workers cite when adding tests.

- [ ] **Step 1: Write the test-patterns document**

Create `.agents/docs/test-patterns.md` with this structure:

```markdown
# Test Patterns

> Guidance for organizing and naming tests in the Wild Bunch repo.
> Source snapshot: `9194c93` (origin/main, 2026-07-02).

## Test types

| Type | What it proves | Where it lives | Naming convention |
| --- | --- | --- | --- |
| Unit | A single domain/application class behaves correctly in isolation | `{Project}.Tests/` root or `{Concern}/` subdirectory | `{ClassUnderTest}Tests.cs` |
| Structural / Guardrail | Source inspection proves an invariant about the codebase shape (no deleted table, no leaked secret, read-only handler) | `{Project}.Tests/Guardrails/` | `{Invariant}GuardrailTests.cs` or `{Invariant}StructuralTests.cs` |
| Projection | A projector produces the expected read-model from events | `{Project}.Tests/Projections/` | `{Projector}Tests.cs` or `{Projection}Tests.cs` |
| Mapper | A mapper converts domain state to DTO shape correctly | `{Project}.Tests/Mappers/` | `{Mapper}Tests.cs` |
| Renderer | A renderer produces text/label output from domain state | `{Project}.Tests/Renderers/` | `{Renderer}Tests.cs` |
| Handler | A command/query handler orchestrates the aggregate and returns the right result | `{Project}.Tests/Handlers/` | `{Handler}Tests.cs` |
| Factory orchestration | A factory wires dependencies and produces a valid aggregate/session | `{Project}.Tests/` root or `Factories/` | `{Factory}Tests.cs` |
| Configuration | A configuration/policy registration produces the expected service collection | `{Project}.Tests/Configuration/` | `{Config}ConfigurationTests.cs` |
| Characterization | Pins exact current behavior before a migration; values are captured from deterministic scenarios | `{Project}.Tests/Characterization/` | `{Subject}CharacterizationTests.cs` |
| Event sourcing | Proves event apply/replay produces the same state as the command path | `{Project}.Tests/EventSourcing/` or `Events/` | `{Subject}EventSourcingTests.cs` |
| Acceptance | End-to-end scenario through the API proving a user-facing flow works | `Integration.Tests/Acceptance/` | `{Flow}AcceptanceTests.cs` |
| Integration | Full HTTP pipeline via `WebApplicationFactory` | `Integration.Tests/` root or `{Concern}/` | `{Endpoint}Tests.cs` |

## When to add which test type

1. **New behavior in a domain class** → unit test in `Domain.Tests/` root or the matching concern subdirectory.
2. **New projector** → projection test in `Application.Tests/Projections/`.
3. **New mapper** → mapper test in `Application.Tests/Mappers/`.
4. **New renderer** → renderer test in `Application.Tests/Renderers/`.
5. **New command/query handler** → handler test in `Application.Tests/Handlers/` (or `Dev/` for dev handlers).
6. **New API endpoint** → integration test in `Integration.Tests/` root.
7. **New user-facing flow** → acceptance test in `Integration.Tests/Acceptance/`.
8. **New configuration/policy** → configuration test in `Api.Tests/Configuration/`.
9. **Migration that changes behavior** → characterization tests first, then update them after migration.
10. **Source-shape invariant** → structural/guardrail test in `{Project}.Tests/Guardrails/`.

## Test organization rules

- One test type per file. Do not mix unit, projection, and handler tests in the same file.
- Folder structure mirrors the source layer: `Application.Tests/Handlers/` mirrors `Application/Games/Commands/` and `Application/Games/Queries/`.
- Namespace matches folder path: `WildBunch.Application.Tests.Handlers`, `WildBunch.Application.Tests.Mappers`, etc.
- Test doubles (fakes, stubs, in-memory repos) live in `TestDoubles/` and are NOT test files.
- Test infrastructure (fixtures, harnesses, builders) lives in `TestInfrastructure/` and is NOT test files.
- Characterization tests are temporary by nature — they become regular unit tests after the migration they pin is complete. Keep them in `Characterization/` only while the migration is in progress; move them to the matching concern folder when the migration lands.

## Naming conventions

- Test class: `{ClassUnderTest}Tests.cs` (e.g., `GameSessionMapperTests`, `CorsConfigurationTests`).
- Test method: `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `CreateOrder_WithValidItems_ReturnsSuccessResult`).
- Configuration tests: `{Config}ConfigurationTests` (not `{Config}PolicyTests`).
- Guardrail tests: `{Invariant}GuardrailTests` (not `{Invariant}Tests`).
- Characterization tests: `{Subject}CharacterizationTests` (not `{Subject}Tests`).
```

Fill every section with the content above. No placeholders.

- [ ] **Step 2: Regenerate the index mesh**

Run: `python scripts/generate_index_mesh.py`
Expected: the generator walks the live tree and updates `.agents/docs/INDEX.md` to include the new `test-patterns.md` entry. Inspect the diff to confirm the new file appears and no unrelated INDEX.md files changed.

- [ ] **Step 3: Commit the test-patterns document**

```bash
git add .agents/docs/test-patterns.md .agents/docs/INDEX.md
git commit -m "BUNCH-125: add test-patterns guidance document"
```

### Task 2: Move Application.Tests handler files to `Handlers/`

**Files:**
- Move: 24 `*HandlerTests.cs` files from `tests/WildBunch.Application.Tests/` root to `tests/WildBunch.Application.Tests/Handlers/`.
- Modify: each moved file's `namespace` declaration from `WildBunch.Application.Tests;` to `WildBunch.Application.Tests.Handlers;`.
- Modify: any files that reference these moved types via `using` (audit: none expected — no cross-references to handler test classes exist outside the test files themselves).

**Interfaces:**
- Consumes: the audit baseline.
- Produces: all handler tests in `Handlers/` with matching namespaces.

The 24 handler files to move:
1. `AdvanceTravelDayHandlerTests.cs`
2. `ArchivePlaythroughHandlerTests.cs`
3. `CheckSheriffRecordsHandlerTests.cs`
4. `CompletePlayerSetupHandlerTests.cs`
5. `CompletePlayerSetupOneActivePlaythroughTests.cs`
6. `ConfrontSaloonWantedSuspectHandlerTests.cs`
7. `ConfrontWantedSuspectHandlerTests.cs`
8. `GetAvailableActionsHandlerTests.cs`
9. `GetGameSessionHandlerTests.cs`
10. `GetJournalHandlerTests.cs`
11. `GetStartingTownMapHandlerTests.cs`
12. `GetStartingTownsHandlerTests.cs`
13. `GetTownStoreOffersHandlerTests.cs`
14. `GetWorldMapHandlerTests.cs`
15. `InspectNoticeBoardHandlerTests.cs`
16. `InvestigationSourceHandlerTests.cs`
17. `PreviewTravelHandlerTests.cs`
18. `PrologueHandlerTests.cs`
19. `PurchaseStoreItemHandlerTests.cs`
20. `ReadWantedPostersHandlerTests.cs`
21. `ResolveJourneyEncounterHandlerTests.cs`
22. `TravelToTownHandlerTests.cs`
23. `TurnInToSheriffHandlerTests.cs`
24. `SaloonPersonOfInterestDescriptorParityTests.cs` (tests handler-to-mapper parity; primary concern is handler orchestration)

**Note:** `CompletePlayerSetupOneActivePlaythroughTests.cs` does not end in `HandlerTests` but tests the `CompletePlayerSetup` handler flow. It belongs with the handler tests.

- [ ] **Step 1: Create the `Handlers/` directory**

```bash
mkdir tests/WildBunch.Application.Tests/Handlers
```

- [ ] **Step 2: Move each handler test file and update its namespace**

For each file listed above:
```bash
git mv tests/WildBunch.Application.Tests/{filename} tests/WildBunch.Application.Tests/Handlers/{filename}
```
Then update the `namespace` declaration in the moved file from `namespace WildBunch.Application.Tests;` to `namespace WildBunch.Application.Tests.Handlers;`.

- [ ] **Step 3: Build and run the Application.Tests suite**

Run: `dotnet build tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
Expected: success, 0 errors. If any `using` references break, fix them.

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --no-build`
Expected: all tests pass, same count as before the move.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`
Inspect the diff: `tests/WildBunch.Application.Tests/INDEX.md` should show the 24 files moved from the Files section to a new `Handlers/` directory entry.

```bash
git add tests/WildBunch.Application.Tests/Handlers/ tests/WildBunch.Application.Tests/INDEX.md
git add tests/WildBunch.Application.Tests/*.cs  # catch any remaining namespace edits
git commit -m "BUNCH-125: move Application.Tests handler tests to Handlers/"
```

### Task 3: Move Application.Tests mapper files to `Mappers/`

**Files:**
- Move: 7 mapper test files from `tests/WildBunch.Application.Tests/` root to `tests/WildBunch.Application.Tests/Mappers/`.
- Modify: each moved file's `namespace` declaration from `WildBunch.Application.Tests;` to `WildBunch.Application.Tests.Mappers;`.

The 7 mapper files:
1. `CaseBoardMapperTests.cs`
2. `ClueTimeAnchorBeatLabelTests.cs` (tests `CaseReadMapper.ToDto`)
3. `JournalMapperTests.cs`
4. `TrailBeatSlotDtoTests.cs` (tests `TravelDiaryMapper.ToDto`)
5. `TravelDiaryMapperTests.cs`
6. `WantedPosterMapperTests.cs`
7. `GameSessionDtoProjectionFieldsTests.cs` (tests `GameSessionMapper.ToDto` — mapper/projection boundary; primary concern is DTO mapping)

**Note:** `SaloonPersonOfInterestDescriptorParityTests.cs` was moved to `Handlers/` in Task 2 because its primary concern is handler-to-mapper parity in the handler flow. `GameSessionDtoProjectionFieldsTests.cs` is a mapper test (tests `GameSessionMapper.ToDto`), so it goes in `Mappers/`.

- [ ] **Step 1: Create the `Mappers/` directory**

```bash
mkdir tests/WildBunch.Application.Tests/Mappers
```

- [ ] **Step 2: Move each mapper test file and update its namespace**

For each file listed above:
```bash
git mv tests/WildBunch.Application.Tests/{filename} tests/WildBunch.Application.Tests/Mappers/{filename}
```
Then update the `namespace` declaration from `namespace WildBunch.Application.Tests;` to `namespace WildBunch.Application.Tests.Mappers;`.

- [ ] **Step 3: Build and run the Application.Tests suite**

Run: `dotnet build tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`
Inspect the diff: `tests/WildBunch.Application.Tests/INDEX.md` should show the 7 files moved to a new `Mappers/` directory entry.

```bash
git add tests/WildBunch.Application.Tests/Mappers/ tests/WildBunch.Application.Tests/INDEX.md
git add tests/WildBunch.Application.Tests/*.cs
git commit -m "BUNCH-125: move Application.Tests mapper tests to Mappers/"
```

### Task 4: Move Application.Tests renderer files to `Renderers/`

**Files:**
- Move: 3 renderer test files from `tests/WildBunch.Application.Tests/` root to `tests/WildBunch.Application.Tests/Renderers/`.
- Modify: each moved file's `namespace` declaration from `WildBunch.Application.Tests;` to `WildBunch.Application.Tests.Renderers;`.

The 3 renderer files:
1. `BeatLabelRendererTests.cs`
2. `BeatNarrationRendererTests.cs`
3. `TravelDiaryTextRendererTests.cs`

- [ ] **Step 1: Create the `Renderers/` directory**

```bash
mkdir tests/WildBunch.Application.Tests/Renderers
```

- [ ] **Step 2: Move each renderer test file and update its namespace**

For each file:
```bash
git mv tests/WildBunch.Application.Tests/{filename} tests/WildBunch.Application.Tests/Renderers/{filename}
```
Then update the `namespace` declaration from `namespace WildBunch.Application.Tests;` to `namespace WildBunch.Application.Tests.Renderers;`.

- [ ] **Step 3: Build and run the Application.Tests suite**

Run: `dotnet build tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`
Inspect the diff: `tests/WildBunch.Application.Tests/INDEX.md` should show the 3 files moved to a new `Renderers/` directory entry.

```bash
git add tests/WildBunch.Application.Tests/Renderers/ tests/WildBunch.Application.Tests/INDEX.md
git add tests/WildBunch.Application.Tests/*.cs
git commit -m "BUNCH-125: move Application.Tests renderer tests to Renderers/"
```

### Task 5: Move Application.Tests projection/guardrail files to `Projections/` and `Guardrails/`

**Files:**
- Move: `ReadStoreLoaderJournalProjectionGuardrailTests.cs` from root to new `Guardrails/` subdirectory.
- Move: `QueryHandlersAreReadOnlyTests.cs` from root to `Guardrails/` (it is a structural guardrail proving query handlers do not persist).
- Modify: each moved file's `namespace` declaration.

**Note:** `GameSessionDtoProjectionFieldsTests.cs` was moved to `Mappers/` in Task 3 (it tests `GameSessionMapper.ToDto`). The existing `Projections/` directory already has the projector tests and does not need these guardrail files.

- [ ] **Step 1: Create the `Guardrails/` directory**

```bash
mkdir tests/WildBunch.Application.Tests/Guardrails
```

- [ ] **Step 2: Move each guardrail test file and update its namespace**

```bash
git mv tests/WildBunch.Application.Tests/ReadStoreLoaderJournalProjectionGuardrailTests.cs tests/WildBunch.Application.Tests/Guardrails/ReadStoreLoaderJournalProjectionGuardrailTests.cs
git mv tests/WildBunch.Application.Tests/QueryHandlersAreReadOnlyTests.cs tests/WildBunch.Application.Tests/Guardrails/QueryHandlersAreReadOnlyTests.cs
```
Update `namespace` declarations from `namespace WildBunch.Application.Tests;` to `namespace WildBunch.Application.Tests.Guardrails;`.

- [ ] **Step 3: Build and run the Application.Tests suite**

Run: `dotnet build tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`

```bash
git add tests/WildBunch.Application.Tests/Guardrails/ tests/WildBunch.Application.Tests/INDEX.md
git add tests/WildBunch.Application.Tests/*.cs
git commit -m "BUNCH-125: move Application.Tests guardrail tests to Guardrails/"
```

### Task 6: Rename `CorsPolicyTests` to `CorsConfigurationTests` and move to `Configuration/`

**Files:**
- Move + rename: `tests/WildBunch.Api.Tests/CorsPolicyTests.cs` → `tests/WildBunch.Api.Tests/Configuration/CorsConfigurationTests.cs`.
- Modify: the class name from `CorsPolicyTests` to `CorsConfigurationTests`.
- Modify: the `namespace` declaration from `namespace WildBunch.Api.Tests;` to `namespace WildBunch.Api.Tests.Configuration;`.

- [ ] **Step 1: Create the `Configuration/` directory and move + rename the file**

```bash
mkdir tests/WildBunch.Api.Tests/Configuration
git mv tests/WildBunch.Api.Tests/CorsPolicyTests.cs tests/WildBunch.Api.Tests/Configuration/CorsConfigurationTests.cs
```

- [ ] **Step 2: Update the class name and namespace**

In `CorsConfigurationTests.cs`:
- Change `namespace WildBunch.Api.Tests;` to `namespace WildBunch.Api.Tests.Configuration;`
- Change `public sealed class CorsPolicyTests` to `public sealed class CorsConfigurationTests`

- [ ] **Step 3: Build and run the Api.Tests suite**

Run: `dotnet build tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Api.Tests/WildBunch.Api.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`

```bash
git add tests/WildBunch.Api.Tests/Configuration/ tests/WildBunch.Api.Tests/INDEX.md
git commit -m "BUNCH-125: rename CorsPolicyTests to CorsConfigurationTests and move to Configuration/"
```

### Task 7: Move Domain.Tests characterization files to `Characterization/`

**Files:**
- Move: 4 characterization test files from `tests/WildBunch.Domain.Tests/` root to `tests/WildBunch.Domain.Tests/Characterization/`.
- Modify: each moved file's `namespace` declaration from `namespace WildBunch.Domain.Tests;` to `namespace WildBunch.Domain.Tests.Characterization;`.

The 4 characterization files:
1. `TravelStateMachineCharacterizationTests.cs`
2. `TravelDiaryCharacterizationTests.cs`
3. `TravelEncounterResolutionCharacterizationTests.cs`
4. `TravelResourceTrackingCharacterizationTests.cs`

- [ ] **Step 1: Create the `Characterization/` directory**

```bash
mkdir tests/WildBunch.Domain.Tests/Characterization
```

- [ ] **Step 2: Move each characterization test file and update its namespace**

For each file:
```bash
git mv tests/WildBunch.Domain.Tests/{filename} tests/WildBunch.Domain.Tests/Characterization/{filename}
```
Update `namespace` from `namespace WildBunch.Domain.Tests;` to `namespace WildBunch.Domain.Tests.Characterization;`.

- [ ] **Step 3: Build and run the Domain.Tests suite**

Run: `dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`

```bash
git add tests/WildBunch.Domain.Tests/Characterization/ tests/WildBunch.Domain.Tests/INDEX.md
git add tests/WildBunch.Domain.Tests/*.cs
git commit -m "BUNCH-125: move Domain.Tests characterization tests to Characterization/"
```

### Task 8: Move Domain.Tests guardrail files to `Guardrails/`

**Files:**
- Move: `HeatSemanticGuardrailTests.cs` from root to `tests/WildBunch.Domain.Tests/Guardrails/`.
- Modify: the `namespace` declaration.

- [ ] **Step 1: Create the `Guardrails/` directory**

```bash
mkdir tests/WildBunch.Domain.Tests/Guardrails
```

- [ ] **Step 2: Move the guardrail test file and update its namespace**

```bash
git mv tests/WildBunch.Domain.Tests/HeatSemanticGuardrailTests.cs tests/WildBunch.Domain.Tests/Guardrails/HeatSemanticGuardrailTests.cs
```
Update `namespace` from `namespace WildBunch.Domain.Tests;` to `namespace WildBunch.Domain.Tests.Guardrails;`.

- [ ] **Step 3: Build and run the Domain.Tests suite**

Run: `dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`

```bash
git add tests/WildBunch.Domain.Tests/Guardrails/ tests/WildBunch.Domain.Tests/INDEX.md
git add tests/WildBunch.Domain.Tests/*.cs
git commit -m "BUNCH-125: move Domain.Tests guardrail tests to Guardrails/"
```

### Task 9: Move Domain.Tests event-sourcing files to `EventSourcing/`

**Files:**
- Move: 5 event-sourcing test files from root to `tests/WildBunch.Domain.Tests/EventSourcing/`.
- Modify: each moved file's `namespace` declaration.
- Note: The existing `Events/` directory contains typed-domain-event tests (`TypedDomainEventTests`, `GameSessionEventSourcingTests`, `StartFlowEventSourcingTests`). These are already organized. This task moves the event-sourcing tests that are in the root and test apply/replay behavior.

The 5 event-sourcing files to move:
1. `BountySaloonEventSourcingTests.cs`
2. `InvestigationEventSourcingTests.cs`
3. `ClockTurnCorrectionTests.cs` (tests event-apply turn correction)
4. `TravelEventApplyTests.cs`
5. `TravelReplayEqualityTests.cs`

- [ ] **Step 1: Create the `EventSourcing/` directory**

```bash
mkdir tests/WildBunch.Domain.Tests/EventSourcing
```

- [ ] **Step 2: Move each event-sourcing test file and update its namespace**

For each file:
```bash
git mv tests/WildBunch.Domain.Tests/{filename} tests/WildBunch.Domain.Tests/EventSourcing/{filename}
```
Update `namespace` from `namespace WildBunch.Domain.Tests;` to `namespace WildBunch.Domain.Tests.EventSourcing;`.

- [ ] **Step 3: Build and run the Domain.Tests suite**

Run: `dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`

```bash
git add tests/WildBunch.Domain.Tests/EventSourcing/ tests/WildBunch.Domain.Tests/INDEX.md
git add tests/WildBunch.Domain.Tests/*.cs
git commit -m "BUNCH-125: move Domain.Tests event-sourcing tests to EventSourcing/"
```

### Task 10: Move Domain.Tests projection-equivalence and mapper files to `Projections/` and `Mappers/`

**Files:**
- Move: 2 projection-equivalence test files to `tests/WildBunch.Domain.Tests/Projections/`.
- Move: 2 mapper test files to `tests/WildBunch.Domain.Tests/Mappers/`.
- Modify: each moved file's `namespace` declaration.

Projection-equivalence files:
1. `JournalLogProjectorEquivalenceTests.cs` (tests `JournalLogProjector` from Application — cross-layer projection equivalence)
2. `JournalResolverTests.cs` (tests journal projection/resolver)

Mapper files:
1. `TrailBeatSlotMappingTests.cs`
2. `TravelDiarySnapshotShapeTests.cs` (tests projection/mapper shape; primary concern is snapshot shape mapping)

- [ ] **Step 1: Create the `Projections/` and `Mappers/` directories**

```bash
mkdir tests/WildBunch.Domain.Tests/Projections
mkdir tests/WildBunch.Domain.Tests/Mappers
```

- [ ] **Step 2: Move each file and update its namespace**

```bash
git mv tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs tests/WildBunch.Domain.Tests/Projections/JournalLogProjectorEquivalenceTests.cs
git mv tests/WildBunch.Domain.Tests/JournalResolverTests.cs tests/WildBunch.Domain.Tests/Projections/JournalResolverTests.cs
git mv tests/WildBunch.Domain.Tests/TrailBeatSlotMappingTests.cs tests/WildBunch.Domain.Tests/Mappers/TrailBeatSlotMappingTests.cs
git mv tests/WildBunch.Domain.Tests/TravelDiarySnapshotShapeTests.cs tests/WildBunch.Domain.Tests/Mappers/TravelDiarySnapshotShapeTests.cs
```
Update `namespace` from `namespace WildBunch.Domain.Tests;` to the matching subdirectory namespace (`WildBunch.Domain.Tests.Projections;` or `WildBunch.Domain.Tests.Mappers;`).

- [ ] **Step 3: Build and run the Domain.Tests suite**

Run: `dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
Expected: success, 0 errors.

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-build`
Expected: all tests pass, same count as before.

- [ ] **Step 4: Regenerate the index mesh and commit**

Run: `python scripts/generate_index_mesh.py`

```bash
git add tests/WildBunch.Domain.Tests/Projections/ tests/WildBunch.Domain.Tests/Mappers/ tests/WildBunch.Domain.Tests/INDEX.md
git add tests/WildBunch.Domain.Tests/*.cs
git commit -m "BUNCH-125: move Domain.Tests projection and mapper tests to subdirectories/"
```

### Task 11: Full validation and route-state update

**Files:**
- Read: all moved files, all regenerated INDEX.md files.
- Modify: Linear issue BUNCH-125 (route-state block only — via the Linear connector, not a GitHub mutation).

- [ ] **Step 1: Run the full build**

Run: `dotnet build WildBunch.sln`
Expected: success, 0 errors. Record the exact output tail.

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test WildBunch.sln --no-build`
Expected: all tests pass, same total count as before the reorganization. Record the exact output tail (passed/failed/total).

- [ ] **Step 3: Run the index-mesh CI check**

Run: `python scripts/generate_index_mesh.py --check`
Expected: success (the committed INDEX.md files match the generator output from the current tree). If it fails, run `python scripts/generate_index_mesh.py`, commit the regenerated files, and rerun the check.

- [ ] **Step 4: Confirm a clean worktree and branch head proof**

Run: `git status` (expected: clean), `git log --oneline -15` (record head SHA and commit list), `git rev-parse origin/main` (record remote head for the falsification check).

- [ ] **Step 5: Push the execution branch and open the execution PR**

This is the **execution** phase. The plan-only PR was the preflight artifact; after approval and merge, the executing worker started in a fresh worktree branched from the merged `main` tip.

Push the execution branch and open the execution PR:

```bash
git push -u origin harleydbartles/bunch-125-reorganize-test-suite-by-test-type
gh pr create --title "BUNCH-125: Reorganize test suite by test type" --body-file .agents/superpowers/output/pr-body-bunch-125-execution.md
```

The execution PR body should describe:
- the test-patterns.md guidance document deliverable;
- the before/after file list showing all moves and the one rename;
- the test count by type before/after (unchanged — reorganization only);
- the validation evidence (build, test, index-mesh check);
- a link to the merged plan PR for traceability.

Record the execution PR URL.

- [ ] **Step 6: Update Linear route state**

Via the Linear connector (read the `using-linear` mutate-save reference first), post a comment on BUNCH-125 with a route-state block recording:

- route-state: `execution_complete_pending_review`
- plan path: `.agents/superpowers/plans/2026-07-02-bunch-125-reorganize-test-suite-by-test-type.md`
- plan PR (merged): <plan PR URL>
- execution PR: <execution PR URL from Step 5>
- execution branch: `harleydbartles/bunch-125-reorganize-test-suite-by-test-type`
- execution head: <SHA from Step 4>
- base: <merged main tip the execution branch was cut from>
- validation: build + test + index-mesh check results
- deliverables: test-patterns.md, all test file moves, CorsPolicyTests rename
- next: PR review + merge → closeout

Do NOT close the Linear issue. Do NOT mutate GitHub issue state. Stop after the route-state update.

## Non-goals

- Do NOT rename test methods, merge test classes, or change test body content.
- Do NOT move the `Dev/`, `Execution/`, `TestDoubles/`, `Acceptance/`, or `TestInfrastructure/` directories — they are already organized.
- Do NOT move the existing `Events/` directory in Domain.Tests — it is already organized.
- Do NOT move the existing `Projections/` directory in Application.Tests — it is already organized.
- Do NOT split the `GameApi*Tests.cs` integration tests into subdirectories — the issue says "consider `Acceptance/` subdirectory in Integration.Tests if volume grows" (conditional; volume does not warrant it yet).
- Do NOT change any `.csproj` file — the SDK default compiles all `.cs` files recursively.
- Do NOT add new tests or remove existing tests.
- Do NOT modify production source files.

## Self-Review

**1. Spec coverage:**
- Move projection tests to `Projections/` → Task 5 (guardrails) + Task 10 (Domain.Tests projections). Application.Tests `Projections/` already exists.
- Move dev handler tests to `Dev/` → already done (existing `Dev/` directory).
- Consider `Mappers/` and `Renderers/` subdirectories → Tasks 3, 4, 10.
- Consider `Acceptance/` subdirectory in Integration.Tests → non-goal (volume does not warrant it).
- Rename `CorsPolicyTests` → `CorsConfigurationTests` → Task 6.
- Follow test patterns guidance in `.agents/docs/test-patterns.md` → Task 1 creates the document.
- `dotnet test` passes after reorganization → Task 11 Step 2.
- All tests discoverable and running → Task 11 Step 2.
- Test file names follow naming conventions → Task 6 rename; all other files already follow conventions.
- Test locations match test type → Tasks 2–10.
- Update test patterns document with any new patterns discovered → Task 1 creates the document; no new patterns emerged during planning.

**2. Validation coverage:**
- `dotnet build` after each task group → Tasks 2–10 Step 3.
- `dotnet test` after each task group → Tasks 2–10 Step 3.
- Full `dotnet build` + `dotnet test` → Task 11 Steps 1–2.
- Index-mesh CI check → Task 11 Step 3.

**3. Scope guard:**
- No production source files modified.
- No test bodies modified (only `namespace` declarations and the one class rename in Task 6).
- No `.csproj` files modified.
- No new tests added, no tests removed.

**4. Falsification checks:**
- Before/after test count must be identical (Task 11 Step 2 records the count).
- `git status` must be clean before the execution PR (Task 11 Step 4).
- `generate_index_mesh.py --check` must pass (Task 11 Step 3).

## Drift modes this reorganization must catch

- **Namespace drift:** A file moved to a subdirectory without updating its `namespace` declaration will fail to compile. The build after each task group catches this.
- **Cross-reference drift:** A file that references a moved type via `using` will fail to compile. The build after each task group catches this. The audit confirmed no cross-project references to test namespaces exist.
- **Index-mesh drift:** A file move without regenerating INDEX.md will fail the CI check. The regenerate step after each task group catches this.
- **Test-count drift:** A move that accidentally drops or duplicates a test will change the test count. The full test run in Task 11 catches this.
