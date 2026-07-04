# Geometry-First Map Generation - Plan 1f: Clean Handoff (Doc Freshness + Test Hygiene + Plan 2 Refresh)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Clear the deck for Plan 2 by fixing every issue flagged in the Plan 1e senior "clean handoff" review — stale ADRs and architecture docs referencing the deleted `StartNew`/`StartNewGameHandler`, a 4th duplicated canonical-flow copy, misleading `TravelTestFactory` method names, the invisible `GameDifficulty.Easy` default mismatch, a durable doc for the `TownStates` parity gap — and then refreshing Plan 2 so it is execution-ready against the current codebase.

**Architecture:** Seven tasks. Tasks 1-3 are documentation freshness (ADRs, decomposition audit, tracked-items doc). Tasks 4-5 are test hygiene (consolidate the 4th canonical-flow copy, rename misleading methods, add the difficulty-default comment). Task 6 refreshes Plan 2 against the current codebase state. Task 7 is the final verification gate. Tasks 1-6 are independently testable and committable; Task 7 is verification-only.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, Markdown docs

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plans 1a-1e must be complete — `GameSession.StartNew` and `SeededNewGameFactory.Create` are deleted, all test files use the canonical start flow, the round-trip proof test passes.
- The PR for Plans 0-1e (PR #149) is the base; this plan branches from its head.

## Starting state

After Plan 1e:
- `GameSession.StartNew` is deleted. `SeededNewGameFactory.Create` is deleted. All 65 test files use the canonical start flow (`StartSetup` → `ViewPrologue` → `SelectStartingTown` → `CompleteGameStart`).
- 869 tests pass (526 Domain + 204 Application + 139 GameContent), 1 skipped (the `TownStates` parity-gap documentation test).
- `CompletePlayerSetupHandler.cs` is the current name of the file formerly known as `StartNewGameHandler.cs`.
- `MapGenerator.Generate` exists at `src/WildBunch.GameContent/NewGame/MapGenerator.cs` but is NOT yet wired into `GameSetupResolver` — `GameSetupResolver.cs:55` still calls `SeedWorldBuilder.CreateWorld(...)`.
- Plan 2 (`2026-07-03-geometry-first-plan-2-integration.md`) is marked stale and needs refreshing.

## Senior review findings addressed by this plan

These are the exact findings from the Plan 1e senior "clean handoff" review:

**Biting (will mislead the next engineer):**
1. **Stale `StartNew` references in durable docs** — `ADR-0002:99`, `ADR-0028:15`, `ADR-0034` (11 references), and `.agents/docs/game-session-decomposition-audit.md:46-47` all reference `StartNewGameHandler` or `StartNew` at line numbers that no longer exist.
2. **4th canonical-flow copy** — `SeededNewGameFactoryTests.cs:370` has a private `StartGameCanonical` that duplicates `CanonicalStartFlow.StartGame` in the same project (`tests/WildBunch.GameContent.Tests/CanonicalStartFlow.cs:17`).
3. **Misleading `TravelTestFactory` method names** — `CreateEasyShortJourneyWithGameStarted` and `CreateSixDayQuietJourneyWithGameStarted` return the full 6-event setup stream, not just `GameStarted`.

**Invisible (tracked but not in the codebase):**
4. **`TownStates` parity gap** — documented with a skipped test (`GameSessionEventSourcingTests.cs:484`) but no durable doc says which plan owns the fix or why it matters.
5. **`GameDifficulty.Easy` vs `Standard` default mismatch** — `TestSessionFactory.StartGameCanonical` (line 37) defaults to `Easy` while `CanonicalStartFlow.StartGame` defaults to `Standard`. No comment explains this.

**Plan 2 staleness:**
6. **Plan 2 is marked stale** — it was written before Plans 1b-1e and references integration points that have changed (event-sourced world generation, canonical start flow).

## Global Constraints

- **No production behavior changes.** Tasks 1-5 are documentation, comments, and test-factory consolidation only. No `src/` production code changes except the `TestSessionFactory` comment (which is in `tests/`).
- **ADR freshness doctrine.** Per the repo's own doctrine (`.agents/docs/workflow-policy.md`), reading a stale ADR creates a responsibility to update it. This plan fulfills that responsibility.
- **Greenfield repo.** No backward compatibility, no migration shims. Current correctness wins.
- **Commit per task.** Each task ends with a commit. Commit messages use the `docs:` / `refactor:` / `test:` prefix as appropriate.
- **Validation gate.** After every code-touching task, run `dotnet build` and the affected test project. After the final task, run the full solution build + Domain/Application/GameContent test suites.

---

## Task 1: ADR freshness pass — replace StartNewGameHandler and StartNew references

**Goal:** Update ADR-0002, ADR-0028, and ADR-0034 to replace all `StartNewGameHandler` references with `CompletePlayerSetupHandler` and remove `StartNew` references. These are the docs the next engineer will read to understand the architecture.

**Files:**
- Modify: `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`
- Modify: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`
- Modify: `docs/adr/ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md`

**Context — what changed:**
- `StartNewGameHandler.cs` was renamed to `CompletePlayerSetupHandler.cs` during the canonical start-flow migration (Plans 1d-1e).
- `GameSession.StartNew` (both overloads) was deleted in Plan 1e. The canonical flow is now `StartSetup` → `ViewPrologue` → `SelectStartingTown` → `CompleteGameStart`.
- The one-active-playthrough invariant enforcement moved from `StartNewGameHandler` into `CompletePlayerSetupHandler`.

**Important — historical changelog entries MUST be updated.** ADR dated changelog entries (e.g., "2026-06-27 - live (BUNCH-102): ...") reference `StartNewGameHandler` as the file that enforces the invariant. Even though these are historical entries, the file `StartNewGameHandler.cs` no longer exists — a reader following the reference will hit a dead path. Update ALL references including dated changelog entries. The historical fact is "the invariant was introduced in BUNCH-102"; the file name is a pointer, not a historical fact.

**Verified reference inventory (as of Plan 1e head):**
- ADR-0002: exactly 1 reference — line 99 (file path in a list)
- ADR-0028: exactly 2 references — line 15 (changelog prose) and line 169 (step description)
- ADR-0034: exactly 11 references — lines 9, 49, 67, 104, 105, 110, 115, 123, 131, 143, 147

- [x] **Step 1: Fix ADR-0002** — `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`

This file has exactly 1 reference (verified by grep). Line 99:
```
- `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`
```
Replace with:
```
- `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`
```

No other `StartNew` or `StartNewGameHandler` references exist in this file. Do not modify any other content.

- [x] **Step 2: Fix ADR-0028** — `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`

This file has exactly 2 references (verified by grep):

**Reference 1 — line 15** (BUNCH-102 changelog entry, in prose):
```
The one-active-playthrough invariant is enforced in `StartNewGameHandler`:
```
Replace `StartNewGameHandler` with `CompletePlayerSetupHandler`:
```
The one-active-playthrough invariant is enforced in `CompletePlayerSetupHandler`:
```

**Reference 2 — line 169** (step description):
```
- Step 2: event-sourced `GameSession` (`Apply` methods, `RehydrateFromEvents`, refactored `StartNew` and `Purchase`).
```
Replace `refactored `StartNew` and `Purchase`` with `refactored `StartSetup`/`CompleteGameStart` (canonical start flow) and `Purchase``:
```
- Step 2: event-sourced `GameSession` (`Apply` methods, `RehydrateFromEvents`, refactored `StartSetup`/`CompleteGameStart` (canonical start flow) and `Purchase`).
```

No other `StartNew` references exist in this file.

- [x] **Step 3: Fix ADR-0034** — `docs/adr/ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md`

This file has exactly 11 references to `StartNewGameHandler` (verified by grep). Replace ALL of them with `CompletePlayerSetupHandler` using a global replace. The references are at lines 9, 49, 67, 104, 105, 110, 115, 123, 131, 143, 147.

After the global replace, also fix line 131's file path — it currently reads:
```
- `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`
```
The global replace will have changed `StartNewGameHandler` to `CompletePlayerSetupHandler` in this path too, producing:
```
- `src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs`
```
Verify this is correct — the file exists at that path.

- [x] **Step 4: Verify no stale references remain**

Run:
```bash
rg -n "StartNewGameHandler|GameSession\.StartNew" docs/adr/
```
Expected: zero matches. If any remain, fix them before committing.

- [x] **Step 5: Commit**

```bash
git add docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md docs/adr/ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md
git commit -m "docs: replace StartNewGameHandler with CompletePlayerSetupHandler in ADR-0002, ADR-0028, ADR-0034"
```

---

## Task 2: Fix game-session-decomposition-audit.md — remove deleted StartNew entries, update line numbers

**Goal:** The decomposition audit is required reading (referenced from ADR-0002 and `.agents/docs/architecture-guardrails.md`). Its line-number table is now wrong because `StartNew` was deleted and line numbers shifted.

**Files:**
- Modify: `.agents/docs/game-session-decomposition-audit.md`

**Context — current state of `GameSession.cs` line numbers (verified at Plan 1e head, commit `60fbf78`):**
- `StartSetup` (static) — line 816, ends at line 883
- `SelectStartingTown` — line 889, ends at line 908
- `CompleteGameStart` — line 915, ends at line 948
- `ViewPrologue` — line 954, ends at line 980
- `ArchivePlaythrough` — line 991, ends at line 1052

The audit currently lists (lines 44-49):
```
| `StartSetup` (static) | 816–874 | Acceptable — session lifecycle entry point. |
| `CompleteGameStart` | 876–912 | Acceptable — session lifecycle transition. |
| `StartNew(string, ...)` (static) | 914 | Acceptable — delegates to the overload. |
| `StartNew(string, world, caseFile, ...)` (static) | 917–992 | Acceptable — session lifecycle entry point. |
| `ViewPrologue` | 994–1020 | Acceptable — session lifecycle transition. |
| `ArchivePlaythrough` | 1031–1053 | Acceptable — session lifecycle terminal transition. |
```

- [x] **Step 1: Delete the two StartNew rows, add SelectStartingTown, and update line numbers**

Replace the six-row block (audit lines 44-49) with the corrected five-row block below. The line numbers below were verified at commit `60fbf78` (Plan 1e head). If the plan is being executed from a different commit, re-verify by running:
```bash
rg -n "public static GameSession StartSetup|public void SelectStartingTown|public void CompleteGameStart|public void ViewPrologue|public void ArchivePlaythrough" src/WildBunch.Domain/Game/GameSession.cs
```

If the grep output matches the numbers below, use them as-is. If not, use the grep output and adjust the end-line by finding the closing brace of each method.

Write the corrected table rows (replacing the six-row block):
```
| `StartSetup` (static) | 816–883 | Acceptable — session lifecycle entry point. |
| `SelectStartingTown` | 889–908 | Acceptable — session lifecycle transition. |
| `CompleteGameStart` | 915–948 | Acceptable — session lifecycle transition. |
| `ViewPrologue` | 954–980 | Acceptable — session lifecycle transition. |
| `ArchivePlaythrough` | 991–1052 | Acceptable — session lifecycle terminal transition. |
```

---

## Task 3: Create tracked-items doc for TownStates parity gap

**Goal:** The `TownStates` parity gap is documented with a skipped test but no durable doc says which plan owns the fix or why it matters. Create a tracked-items doc so the next engineer can find the context without reading session artifacts.

**Files:**
- Create: `.agents/docs/tracked-items.md`

**Context -- the parity gap:**
When the starting town differs from `world.Towns.First()`, `StartSetup` creates a phantom `TownVisitState` entry for the first town (visit number 1) before `Apply(GameStarted)` updates `_currentTown` to the actual starting town. On rehydration, the phantom entry does not appear because `Apply(GameStarted)` sets `_currentTown` directly without creating a `TownVisitState` for the placeholder. Result: live session has 2 `TownStates` entries (phantom first town + actual starting town), rehydrated session has 1 (actual starting town only).

The skipped test `NonFirstStartingTown_TownStates_Parity_Between_Live_And_Rehydrated` at `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs:484` documents this with its `Skip` reason and inline comment.

**Decision for this plan:** Plan 1f does NOT fix the parity gap. It documents it as accepted-for-now and assigns ownership to a future plan. The fix likely involves not creating a `TownVisitState` entry for the placeholder town in `StartSetup`, or clearing it when `Apply(GameStarted)` updates `_currentTown`. That is a production behavior change and belongs in a dedicated plan, not a cleanup plan.

- [x] **Step 1: Create the tracked-items doc**

Create `.agents/docs/tracked-items.md` with this content:

```markdown
# Tracked Items

Open items that are documented in the codebase (skipped tests, comments) but not yet assigned to a plan or fixed.

## 1. TownStates parity gap (non-first-town starts)

**Status:** Accepted-for-now, assigned to a future plan (not Plan 1f, not Plan 2).

**Symptom:** When the starting town differs from `world.Towns.First()`, the live session has a phantom `TownVisitState` entry for the first town that does not appear after rehydration from events.

**Location:** Skipped test at `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs:484` -- `NonFirstStartingTown_TownStates_Parity_Between_Live_And_Rehydrated`.

**Root cause:** `StartSetup` creates a `TownVisitState` entry for the placeholder town (the first town in the world) before `Apply(GameStarted)` updates `_currentTown` to the actual starting town. On rehydration, `Apply(GameStarted)` sets `_currentTown` directly without creating the phantom entry.

**Fix direction:** Either (a) do not create a `TownVisitState` entry for the placeholder town in `StartSetup`, or (b) clear the phantom entry when `Apply(GameStarted)` updates `_currentTown`. This is a production behavior change and requires its own plan with tests.

**Why it matters:** Event-sourcing correctness. The live session and the rehydrated session must have identical state. A phantom `TownStates` entry means the live session's town-visit history disagrees with its event stream, which could cause projection drift or incorrect visit-number assertions.

**History:** Discovered during Plan 1e Task 4 (Domain.Tests migration). The `Apply(GameStarted)` `_currentTown` fix in Plan 1e improved the situation (the rehydrated session now correctly sets `_currentTown`) but did not address the phantom `TownStates` entry.
```

- [x] **Step 2: Verify the doc is well-formed**

```bash
rg -n "TownStates" .agents/docs/tracked-items.md
```
Expected: multiple matches confirming the doc content.

- [x] **Step 3: Commit**

```bash
git add .agents/docs/tracked-items.md
git commit -m "docs: add tracked-items doc for TownStates parity gap"
```

---

## Task 4: Consolidate 4th canonical-flow copy + add GameDifficulty.Easy default comment

**Goal:** Eliminate the duplicated `StartGameCanonical` in `SeededNewGameFactoryTests.cs` by replacing its calls with `CanonicalStartFlow.StartGame` (already in the same project). Also add a comment to `TestSessionFactory.StartGameCanonical` documenting the `GameDifficulty.Easy` default and the mismatch with other helpers.

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`

**Context -- the two methods have identical bodies but different signatures:**

`CanonicalStartFlow.StartGame` (`tests/WildBunch.GameContent.Tests/CanonicalStartFlow.cs:17`):
```csharp
public static GameSession StartGame(
    SeededNewGameFactory factory,
    string playerName,
    GameDifficulty gameDifficulty,         // NO default -- required
    string? setupSeedCode,                 // NO default -- required
    GameEntropy gameEntropy,               // NO default -- required
    string? startingTownId = null)         // default null
```

Private `StartGameCanonical` (`tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs:370`):
```csharp
private static GameSession StartGameCanonical(
    SeededNewGameFactory factory,
    string playerName,
    GameDifficulty gameDifficulty = GameDifficulty.Standard,  // has default
    string? setupSeedCode = null,                              // has default
    GameEntropy gameEntropy = GameEntropy.Classic,             // has default
    string? startingTownId = null)                             // default null
```

**Critical:** Both methods default to `GameDifficulty.Standard` -- NOT `Easy`. The `Easy` default is in `TestSessionFactory.StartGameCanonical` (Domain.Tests, a different file/project). Do not confuse the two. The `GameDifficulty.Easy` comment in Step 4 is for `TestSessionFactory` only; the consolidation in Steps 1-3 does NOT need to pass `Easy` anywhere because both methods use `Standard`.

**The signature gap:** `CanonicalStartFlow.StartGame` has no defaults for `gameDifficulty`, `setupSeedCode`, or `gameEntropy`. The private copy has defaults for all three. There are 22 call sites that must be updated to pass all three required args explicitly.

**Interfaces:**
- Consumes: `CanonicalStartFlow.StartGame(SeededNewGameFactory, string, GameDifficulty, string?, GameEntropy, string?)` -- the shared helper
- Produces: a single canonical-flow helper per test project, with documented defaults

- [x] **Step 1: Replace all 22 call sites by pattern**

There are exactly 22 call sites of the private `StartGameCanonical` in `SeededNewGameFactoryTests.cs` (verified by grep at Plan 1e head). They fall into 4 patterns. Replace each pattern as shown:

**Pattern A -- bare 2-arg call (7 sites):** `StartGameCanonical(factory, "Ranger Vale")`
- Lines: 17, 135, 145, 156, 220, 221, 248
- These relied on ALL three defaults: `Standard`, `null`, `Classic`
- Replace with:
```csharp
CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic)
```

**Pattern B -- 4-arg positional call (1 site):** `StartGameCanonical(factory, "Ranger Vale", GameDifficulty.Standard, seedCode)`
- Line: 167
- This passed difficulty + seedCode but relied on the `Classic` default for entropy
- Replace with:
```csharp
CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, seedCode, GameEntropy.Classic)
```

**Pattern C -- 5-arg positional call (9 sites):** `StartGameCanonical(factory, "Ranger Vale", GameDifficulty.XXX, seedCode, GameEntropy.YYY)`
- Lines: 185, 186, 187, 206, 207, 313, 314, 315, 316
- These already pass all 5 required args positionally -- they only need the method name changed
- Replace `StartGameCanonical(` with `CanonicalStartFlow.StartGame(` at each of these lines. No additional args needed.

**Pattern D -- named-parameter calls skipping middle args (5 sites):**
- Line 234: `StartGameCanonical(factory, "Ranger Vale", setupSeedCode: boringSeed, gameEntropy: GameEntropy.Boring)`
- Line 235: same as 234
- Line 254: `StartGameCanonical(factory, "Ranger Vale", startingTownId: overriddenTown.Id.Value)`
- Line 265: `StartGameCanonical(factory, "Ranger Vale", startingTownId: null)`
- Line 283: `StartGameCanonical(factory, "Ranger Vale", setupSeedCode: boringSeed, gameEntropy: GameEntropy.Boring)`

These used named params to skip `gameDifficulty` (defaulting to `Standard`) and/or `setupSeedCode` (defaulting to `null`). `CanonicalStartFlow.StartGame` has no defaults for these, so they must be restructured to positional args:

- Lines 234, 235, 283 (skip gameDifficulty, pass setupSeedCode + gameEntropy):
```csharp
CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, boringSeed, GameEntropy.Boring)
```

- Line 254 (skip gameDifficulty/setupSeedCode/gameEntropy, pass startingTownId):
```csharp
CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic, startingTownId: overriddenTown.Id.Value)
```

- Line 265 (skip gameDifficulty/setupSeedCode/gameEntropy, pass startingTownId: null):
```csharp
CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic, startingTownId: null)
```

**Summary of all 22 replacements:** Replace `StartGameCanonical(` -> `CanonicalStartFlow.StartGame(` everywhere, then add the missing required args (`GameDifficulty.Standard`, `null`, `GameEntropy.Classic`) at the 13 call sites that relied on defaults (Patterns A, B, D). The 9 call sites in Pattern C already pass all args and only need the rename.

- [x] **Step 2: Delete the private StartGameCanonical method**

Delete the private `StartGameCanonical` method (lines 364-395, including the XML doc comment starting at line 364) from `SeededNewGameFactoryTests.cs`.

- [x] **Step 3: Build and run the GameContent.Tests suite**

```bash
dotnet build tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj
dotnet test tests/WildBunch.GameContent.Tests/ --filter "SeededNewGameFactoryTests"
```
Expected: PASS -- all `SeededNewGameFactoryTests` pass with `CanonicalStartFlow.StartGame`.

- [x] **Step 4: Add the GameDifficulty.Easy default comment to TestSessionFactory**

In `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`, the `StartGameCanonical` method is at line 30. It has an existing XML doc comment (the `<summary>` block above it). Replace the existing `<summary>` block with:

```csharp
    /// <summary>
    /// Starts a game through the canonical flow (StartSetup -> ViewPrologue -> SelectStartingTown -> CompleteGameStart).
    /// Defaults to GameDifficulty.Easy for historical reasons (the original CreateDefault used Easy).
    /// Callers that need production-default behavior must pass GameDifficulty.Standard explicitly.
    /// Note: CanonicalStartFlow.StartGame in GameContent.Tests and Integration.Tests defaults to Standard.
    /// </summary>
```

This is a comment-only change -- no behavior change.

- [x] **Step 5: Build and run Domain.Tests**

```bash
dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj
dotnet test tests/WildBunch.Domain.Tests/ --filter "TestSessionFactory"
```
Expected: PASS (comment-only change, no behavior change).

- [x] **Step 6: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs tests/WildBunch.Domain.Tests/TestSessionFactory.cs
git commit -m "refactor: consolidate 4th canonical-flow copy, document GameDifficulty.Easy default"
```

---

## Task 5: Rename misleading TravelTestFactory method names

**Goal:** `CreateEasyShortJourneyWithGameStarted` and `CreateSixDayQuietJourneyWithGameStarted` return the full 6-event setup stream, not just `GameStarted`. Rename them to match the actual return semantics.

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`
- Modify: `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs`

**Context:**
- `TravelTestFactory.cs:44` -- `CreateEasyShortJourneyWithGameStarted()` returns `(GameSession session, TravelPreview preview, IReadOnlyList<IDomainEvent> setupEvents)`
- `TravelTestFactory.cs:56` -- `CreateSixDayQuietJourneyWithGameStarted()` returns the same tuple shape
- The variables were already renamed from `gameStarted` to `setupEvents` in Plan 1e Task 2, but the method names were not.
- Callers: `JournalLogProjectorEquivalenceTests.cs` (lines 21, 72), `TravelReplayEqualityTests.cs` (lines 20, 43, 65)

- [x] **Step 1: Rename the two methods in TravelTestFactory.cs**

In `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`:
- Line 44: `CreateEasyShortJourneyWithGameStarted` -> `CreateEasyShortJourneyWithSetupEvents`
- Line 56: `CreateSixDayQuietJourneyWithGameStarted` -> `CreateSixDayQuietJourneyWithSetupEvents`

- [x] **Step 2: Update all callers**

Run to find all call sites:
```bash
rg -n "CreateEasyShortJourneyWithGameStarted|CreateSixDayQuietJourneyWithGameStarted" tests/
```

Update each call site to use the new method name. The known callers are:
- `tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs` -- lines 21, 72
- `tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs` -- lines 20, 43, 65

- [x] **Step 3: Verify no stale names remain**

```bash
rg -n "WithGameStarted" tests/
```
Expected: zero matches.

- [x] **Step 4: Build and run the affected tests**

```bash
dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj
dotnet test tests/WildBunch.Domain.Tests/ --filter "JournalLogProjectorEquivalenceTests|TravelReplayEqualityTests"
```
Expected: PASS -- all tests pass with the renamed methods.

- [x] **Step 5: Commit**

```bash
git add tests/WildBunch.Domain.Tests/TravelTestFactory.cs tests/WildBunch.Domain.Tests/JournalLogProjectorEquivalenceTests.cs tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs
git commit -m "refactor: rename TravelTestFactory methods from WithGameStarted to WithSetupEvents"
```

---

## Task 6: Refresh Plan 2 for execution against current codebase

**Goal:** Plan 2 (`2026-07-03-geometry-first-plan-2-integration.md`) is marked stale. It was written before Plans 1b-1e and needs updating to reflect the current codebase state: event-sourced world generation, canonical start flow, and the fact that `MapLayoutPalette` is already deleted. This task removes the stale banner, updates prerequisites, and verifies that Plan 2's three tasks are still accurate against the current code.

**Files:**
- Modify: `.agents/superpowers/plans/2026-07-03-geometry-first-plan-2-integration.md`

**Context -- what changed since Plan 2 was written:**
- Plans 1b-1e are complete. World generation is event-sourced (`WorldGenerated` event). The canonical start flow (`StartSetup` -> `ViewPrologue` -> `SelectStartingTown` -> `CompleteGameStart`) replaces `StartNew`.
- `MapLayoutPalette` enum is already deleted (Plan 0). `ClusterCount` and `GraphDensity` are in `SeedWorld`.
- `SeedWorldBuilder.CreateWorld` (line 23) is still a stub with a linear trail chain. `MapGenerator.Generate` exists but is not yet wired into `GameSetupResolver`.
- `GameSetupResolver.cs:55` calls `SeedWorldBuilder.CreateWorld(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource)`.
- No tests currently assert `Assert.Empty` on trails (the Plan 2 claim about `Assert.Empty` trail assertions is stale -- verify at execution time).

- [x] **Step 1: Remove the stale banner**

Delete lines 3-7 of Plan 2 (the `STALE - DO NOT EXECUTE` block and the "Execute Plan 1b first" note).

- [x] **Step 2: Update the prerequisites section**

Replace the existing prerequisites (lines 17-20) with:
```markdown
## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plans 1a-1e must be complete -- `MapGenerator.Generate` exists, `StartNew` is deleted, canonical start flow is in place, `CaseFileSnapshot` carries all 14 fields.
- Plan 1f (Clean Handoff) must be complete -- ADRs and decomposition audit are fresh, tracked-items doc exists.
```

- [x] **Step 3: Verify Task 1 accuracy (wire MapGenerator)**

Read `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs:55` and confirm it still calls `SeedWorldBuilder.CreateWorld(...)`. Read `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs` and confirm `CreateWorld` is still a stub (line 23) and `CreateCanonicalWorld` (line 15) still exists.

If the call site or method names have changed, update Plan 2 Task 1's "Changes" section with the actual current names. The core action remains: replace `SeedWorldBuilder.CreateWorld(...)` with `MapGenerator.Generate(...)` in `GameSetupResolver`, then delete the stub `CreateWorld` method. If the names are unchanged, no edit to Task 1 is needed.

- [x] **Step 4: Verify Task 2 accuracy (rewrite geometry tests)**

Search for any tests that currently assert on trails being empty:
```bash
rg -n "Assert\.Empty\(.*[Tt]rails" tests/
```

If any are found, update Plan 2 Task 2's file list with the actual locations. If none are found (the current state as of Plan 1e head), add a note to Plan 2 Task 2: "Note: As of Plan 1e head, no tests assert `Assert.Empty` on trails. The `Assert.Empty` to `Assert.NotEmpty` changes listed below may not be needed. Verify at execution time and skip any that do not apply."

Also verify the test file names in Plan 2 Task 2 still exist:
```bash
ls tests/WildBunch.GameContent.Tests/GeometryPipelineTests.cs
ls tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs
ls tests/WildBunch.Application.Tests/GetWorldMapHandlerTests.cs
ls tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs
```

If any file does not exist, search for a renamed equivalent:
```bash
rg -l "GetStartingTownMap|GetWorldMap|StartingTownMapEndpoint" tests/
```
If a renamed file is found, update Plan 2 Task 2's file list. If no equivalent is found, remove the file from the list and add a note: "File `<name>` does not exist and no renamed equivalent was found. This test may have been deleted or consolidated."

- [x] **Step 5: Verify Task 3 accuracy (final cleanup)**

Check whether `MapLayoutPalette` references still exist (they should not -- Plan 0 deleted them):
```bash
rg -n "MapLayoutPalette" src/ tests/
```
Expected: zero matches. If zero, update Plan 2 Task 3 to note that `MapLayoutPalette` cleanup is already done and the step is a confirmation only. If any matches are found, leave the step as-is.

Check whether `SeedWorldBuilder.ComputeStableHash` overloads are still present:
```bash
rg -n "ComputeStableHash" src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs
```
Update Plan 2 Task 3 with the actual finding. If the overloads have been removed, note that the step is a confirmation only.

- [x] **Step 6: Update the Definition of Done**

Update Plan 2's Definition of Done to reflect that `MapLayoutPalette` is already deleted (mark as confirmed-done) and add a prerequisite confirmation item:
```markdown
## Definition of Done

- [x] Prerequisites confirmed: Plans 0-1f complete, `MapGenerator.Generate` exists, `SeedWorldBuilder.CreateWorld` stub exists
- [x] `GameSetupResolver` calls `MapGenerator.Generate` instead of `SeedWorldBuilder.CreateWorld`
- [x] `SeedWorldBuilder.CreateWorld` stub is deleted; kept members remain
- [x] Geometry/trail tests assert real pipeline behavior (not placeholder)
- [x] `MapLayoutPalette` references confirmed absent (already deleted in Plan 0)
- [x] Full solution builds with zero related warnings
- [x] Full test suite passes (Domain + Application + GameContent + Integration if Docker available)
```

- [x] **Step 7: Commit**

```bash
git add .agents/superpowers/plans/2026-07-03-geometry-first-plan-2-integration.md
git commit -m "docs: refresh Plan 2 for execution against post-1e codebase"
```

---

## Task 7: Final verification -- full build + test suite

**Goal:** Confirm that all Tasks 1-6 produced a clean build and passing tests. No new code changes in this task -- it is a verification gate only.

- [x] **Step 1: Build the full solution**

```bash
dotnet build
```
Expected: PASS -- 0 errors, 0 warnings.

- [x] **Step 2: Run Domain.Tests**

```bash
dotnet test tests/WildBunch.Domain.Tests/
```
Expected: PASS -- 526 passed, 0 failed, 1 skipped (the `TownStates` parity-gap test).

- [x] **Step 3: Run Application.Tests**

```bash
dotnet test tests/WildBunch.Application.Tests/
```
Expected: PASS -- 204 passed, 0 failed.

- [x] **Step 4: Run GameContent.Tests**

```bash
dotnet test tests/WildBunch.GameContent.Tests/
```
Expected: PASS -- 139 passed, 0 failed.

- [x] **Step 5: Verify no stale StartNew references remain anywhere**

```bash
rg -n "StartNewGameHandler|GameSession\.StartNew" docs/ .agents/docs/ src/ tests/
```
Expected: zero matches in `docs/`, `.agents/docs/`, `src/`. Test files may still reference `StartGameCanonical` (the helper method) -- that is expected and correct.

- [x] **Step 6: Verify no stale WithGameStarted method names remain**

```bash
rg -n "WithGameStarted" tests/
```
Expected: zero matches.

- [x] **Step 7: Verify the tracked-items doc exists**

```bash
ls .agents/docs/tracked-items.md
```
Expected: file exists.

- [x] **Step 8: Verify Plan 2 is no longer marked stale**

```bash
rg -n "STALE" .agents/superpowers/plans/2026-07-03-geometry-first-plan-2-integration.md
```
Expected: zero matches.

- [x] **Step 9: No commit needed (verification-only task)**

If any verification step fails, fix the issue in the relevant task and re-run. Do not commit a fix as part of Task 7 -- go back to the task that introduced the problem.

---

## Definition of Done

- [x] ADR-0002, ADR-0028, ADR-0034 have zero `StartNewGameHandler` or `StartNew` references
- [x] `game-session-decomposition-audit.md` has zero `StartNew` references and correct line numbers
- [x] `.agents/docs/tracked-items.md` exists and documents the `TownStates` parity gap with ownership assignment
- [x] `SeededNewGameFactoryTests.cs` has no private `StartGameCanonical` -- uses `CanonicalStartFlow.StartGame` instead
- [x] `TestSessionFactory.StartGameCanonical` has a comment documenting the `GameDifficulty.Easy` default
- [x] `TravelTestFactory` methods are renamed from `*WithGameStarted` to `*WithSetupEvents`
- [x] Plan 2 is refreshed: stale banner removed, prerequisites updated, tasks verified against current code
- [x] Full solution builds with 0 errors, 0 warnings
- [x] Domain.Tests (526+1skip), Application.Tests (204), GameContent.Tests (139) all pass
- [x] No `StartNewGameHandler` or `GameSession.StartNew` references in `docs/`, `.agents/docs/`, `src/`
- [x] No `WithGameStarted` method names in `tests/`
