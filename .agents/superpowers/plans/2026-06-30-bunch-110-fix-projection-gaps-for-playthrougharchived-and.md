# BUNCH-110: Fix Projection Gaps for PlaythroughArchived and UnrelatedCriminalTurnInSettled — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close projection gaps for two domain events so BUNCH-91's replay-hardening completeness check can assert that every event type is handled by the relevant read-model projectors.

**Architecture:** Add explicit switch-arm handling for `PlaythroughArchived` in `HudProjector`, `DiaryProjector`, and `CaseFileViewProjector`, and add `UnrelatedCriminalTurnInSettled` handling to `HudProjector`. All three projectors are pure functions over the typed event stream; the changes are additive and do not mutate aggregate state. The HUD already tracks `GameStatus` and `WalletCash`, so it naturally reflects archive status and bounty income. The diary emits a human-readable entry on archive. The case file view has no relevant state change for an archive but must still list the event type as handled in its switch.

**Tech Stack:** C# 14 / .NET, xUnit, `WildBunch.Application.Tests`.

## Global Constraints

- Projectors are pure functions over `IReadOnlyList<IDomainEvent>`; no side effects or aggregate mutation.
- Add only the missing switch arms; do not refactor unrelated event handling.
- Wallet changes from bounties are always additive (`+=`) because preceding events have already set the absolute wallet state.
- HUD status is derived from the last applicable event; `PlaythroughArchived` overwrites status with `GameStatus.Archived`.
- Diary entries use the current `day`/`turn` values already tracked by the projector; `PlaythroughArchived` carries its own `Day`/`Turn` but the projector uses the context-driven values.
- Validation: `dotnet test` must pass for the Application test project.

---

## File Structure

| File | Responsibility | Action |
| --- | --- | --- |
| `src/WildBunch.Application/Projections/HudProjector.cs` | HUD projection | Modify — add `PlaythroughArchived` and `UnrelatedCriminalTurnInSettled` arms |
| `src/WildBunch.Application/Projections/DiaryProjector.cs` | Diary projection | Modify — add `PlaythroughArchived` arm |
| `src/WildBunch.Application/Projections/CaseFileViewProjector.cs` | Case file view projection | Modify — add `PlaythroughArchived` arm (no-op) |
| `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs` | Existing projector tests | Modify — add tests proving the new arms |

---

## Task 1: Handle `PlaythroughArchived` in `HudProjector`

`PlaythroughArchived` signals the session was archived. The HUD projection's `Status` field should reflect the archived state so downstream UI can render end-of-playthrough summaries.

**Files:**
- Modify: `src/WildBunch.Application/Projections/HudProjector.cs`
- Test: `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`

**Interfaces:**
- Consumes: `PlaythroughArchived` event with `StatusBeforeArchive` and `LastTownName`/`LastTownId`.
- Produces: `HudProjection.Status` becomes `GameStatus.Archived` when the event stream contains `PlaythroughArchived`. `CurrentTownId` and `CurrentTownName` are updated from the event's `LastTownId`/`LastTownName` if present.

- [ ] **Step 1: Add the `PlaythroughArchived` arm to the HUD switch**

In `src/WildBunch.Application/Projections/HudProjector.cs`, add the following `case` inside the `foreach` switch:

```csharp
                case PlaythroughArchived pa:
                    status = GameStatus.Archived;
                    if (pa.LastTownId is { } lastTownId)
                    {
                        currentTownId = lastTownId;
                        currentTownName = pa.LastTownName ?? currentTownName;
                    }
                    break;
```

Place it after the `JourneyCompleted` case or near the end of the switch so that archive status wins over any prior status.

- [ ] **Step 2: Add a failing HUD archive test**

In `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, add a new test:

```csharp
    [Fact]
    public void HudProjector_PlaythroughArchived_SetsStatusToArchived()
    {
        var projector = new HudProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 25m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            },
            new PlaythroughArchived
            {
                ArchivedAtUtc = DateTime.UtcNow,
                ArchiveReason = "Completed",
                PlayerName = "Ranger Vale",
                LastTownId = new TownId("pinecross"),
                LastTownName = "Pinecross",
                Day = 1,
                Turn = "Morning",
                StatusBeforeArchive = GameStatus.Completed
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(GameStatus.Archived, hud.Status);
        Assert.Equal(new TownId("pinecross"), hud.CurrentTownId);
        Assert.Equal("Pinecross", hud.CurrentTownName);
    }
```

- [ ] **Step 3: Run the new test to confirm it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~HudProjector_PlaythroughArchived"`

Expected: FAIL — `PlaythroughArchived` is not matched in the switch, so `Status` remains `Active`.

- [ ] **Step 4: Verify the test passes after the switch arm is added**

Run the same command.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/HudProjector.cs tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs
git commit -m "BUNCH-110: Handle PlaythroughArchived in HudProjector

Archive the HUD projection's status when PlaythroughArchived is seen.
The event carries the last known town, so the projector also updates
current town state to reflect the archived position."
```

---

## Task 2: Handle `UnrelatedCriminalTurnInSettled` in `HudProjector`

`UnrelatedCriminalTurnInSettled` is the bounty-only counterpart to `SheriffTurnInSettled`. It carries a `BountyAmount` and should increase the HUD wallet the same way.

**Files:**
- Modify: `src/WildBunch.Application/Projections/HudProjector.cs`
- Test: `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`

**Interfaces:**
- Consumes: `UnrelatedCriminalTurnInSettled` with `BountyAmount`.
- Produces: `HudProjection.WalletCash` increases by `BountyAmount`.

- [ ] **Step 1: Add the `UnrelatedCriminalTurnInSettled` arm to the HUD switch**

In `src/WildBunch.Application/Projections/HudProjector.cs`, add the following case:

```csharp
                case UnrelatedCriminalTurnInSettled ut:
                    walletCash += ut.BountyAmount;
                    break;
```

Place it next to the existing `SheriffTurnInSettled` case.

- [ ] **Step 2: Add a failing HUD unrelated-bounty test**

In `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, add a new test:

```csharp
    [Fact]
    public void HudProjector_UnrelatedCriminalTurnInSettled_IncreasesWalletCash()
    {
        var projector = new HudProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 25m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            },
            new UnrelatedCriminalTurnInSettled
            {
                WarrantId = new WarrantId("warrant-1"),
                TargetName = "Bloody Bob",
                Disposition = WarrantDisposition.DeadOrAlive,
                IsAlive = true,
                BountyAmount = 50m,
                Message = "Turned in Bloody Bob for $50.",
                Day = 1,
                Turn = 1
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(75m, hud.WalletCash);
    }
```

- [ ] **Step 3: Run the new test to confirm it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~HudProjector_UnrelatedCriminalTurnInSettled"`

Expected: FAIL — `WalletCash` remains `25m` because the event is not handled.

- [ ] **Step 4: Verify the test passes after the switch arm is added**

Run the same command.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/HudProjector.cs tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs
git commit -m "BUNCH-110: Handle UnrelatedCriminalTurnInSettled in HudProjector

UnrelatedCriminalTurnInSettled carries a bounty amount that should be
reflected in the HUD wallet, just like SheriffTurnInSettled."
```

---

## Task 3: Handle `PlaythroughArchived` in `DiaryProjector`

The diary should record the archive so replayed playthroughs have a readable end-of-session entry.

**Files:**
- Modify: `src/WildBunch.Application/Projections/DiaryProjector.cs`
- Test: `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`

**Interfaces:**
- Consumes: `PlaythroughArchived` with `ArchiveReason`, `LastTownName`, `Day`, `Turn`.
- Produces: A `DiaryEntry` summarizing the archive.

- [ ] **Step 1: Add the `PlaythroughArchived` arm to the diary switch**

In `src/WildBunch.Application/Projections/DiaryProjector.cs`, add the following case:

```csharp
                case PlaythroughArchived pa:
                    var location = string.IsNullOrEmpty(pa.LastTownName) ? "an unknown location" : pa.LastTownName;
                    entries.Add(new DiaryEntry(day, turn, $"Playthrough archived at {location}: {pa.ArchiveReason}."));
                    break;
```

Place it after the `JourneyArrivalAcknowledged` case or near the end of the switch.

- [ ] **Step 2: Add a failing diary archive test**

In `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, add a new test:

```csharp
    [Fact]
    public void DiaryProjector_PlaythroughArchived_AddsArchiveEntry()
    {
        var projector = new DiaryProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 25m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            },
            new PlaythroughArchived
            {
                ArchivedAtUtc = DateTime.UtcNow,
                ArchiveReason = "Completed",
                PlayerName = "Ranger Vale",
                LastTownId = new TownId("pinecross"),
                LastTownName = "Pinecross",
                Day = 1,
                Turn = "Morning",
                StatusBeforeArchive = GameStatus.Completed
            }
        };

        var diary = projector.Project(events);

        Assert.Equal(2, diary.Entries.Count);
        Assert.Contains("archived", diary.Entries[1].Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Completed", diary.Entries[1].Summary);
    }
```

- [ ] **Step 3: Run the new test to confirm it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~DiaryProjector_PlaythroughArchived"`

Expected: FAIL — only one diary entry exists.

- [ ] **Step 4: Verify the test passes after the switch arm is added**

Run the same command.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/DiaryProjector.cs tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs
git commit -m "BUNCH-110: Handle PlaythroughArchived in DiaryProjector

Record a readable diary entry when a playthrough is archived, using the
current day/turn and the archive reason."
```

---

## Task 4: Handle `PlaythroughArchived` in `CaseFileViewProjector`

The case file view has no dedicated archive state, but BUNCH-91's completeness check requires the event to be explicitly listed in the projector's switch as handled.

**Files:**
- Modify: `src/WildBunch.Application/Projections/CaseFileViewProjector.cs`

**Interfaces:**
- Consumes: `PlaythroughArchived`.
- Produces: No change to `CaseFileViewProjection` state.

- [ ] **Step 1: Add the `PlaythroughArchived` arm to the case file switch**

In `src/WildBunch.Application/Projections/CaseFileViewProjector.cs`, add the following case:

```csharp
                case PlaythroughArchived:
                    // No case file view state change on archive.
                    break;
```

Place it after the `SheriffTurnInSettled` case or near the end of the switch.

- [ ] **Step 2: Compile to verify the event type is reachable**

Run: `dotnet build src/WildBunch.Application`

Expected: PASS with no warnings or errors.

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Application/Projections/CaseFileViewProjector.cs
git commit -m "BUNCH-110: Handle PlaythroughArchived in CaseFileViewProjector

The case file view has no state to update on archive, but the event must
be explicitly listed as handled so projection completeness checks pass."
```

---

## Task 5: Full validation and projection completeness

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs` (if needed)
- Run: `dotnet test`

**Interfaces:**
- Consumes: All three projectors and all relevant event types.
- Produces: A passing test suite with all new event arms covered.

- [ ] **Step 1: Run the full application test suite**

Run: `dotnet test tests/WildBunch.Application.Tests`

Expected: All tests pass, including the three new ones.

- [ ] **Step 2: Run the full repo test suite**

Run: `dotnet test`

Expected: All tests pass.

- [ ] **Step 3: Commit any final index mesh updates**

If `dotnet test` created a `TestResults/` directory, do not commit it. If the plan file itself changed, it was already committed. Regenerate the index mesh if the plan file added new entries under `.agents/superpowers/plans/`:

```bash
python scripts/generate_index_mesh.py
```

If the index mesh changes, commit the updated `INDEX.md` files.

---

## Self-Review

**1. Spec coverage:**
- Add `PlaythroughArchived` to `HudProjector` → Task 1.
- Add `PlaythroughArchived` to `DiaryProjector` → Task 3.
- Add `PlaythroughArchived` to `CaseFileViewProjector` → Task 4.
- Add `UnrelatedCriminalTurnInSettled` to `HudProjector` → Task 2.
- Validation via `dotnet test` → Task 5.

**2. Placeholder scan:** No TBD/TODO/"implement later" placeholders. Every step includes exact file paths, code, and expected test output.

**3. Type consistency:** All event property names (`ArchivedAtUtc`, `ArchiveReason`, `LastTownId`, `LastTownName`, `Day`, `Turn`, `StatusBeforeArchive`, `BountyAmount`, `WarrantId`, `TargetName`, `Disposition`, `IsAlive`, `Message`) match the current domain event records.

## Execution Handoff

**Plan complete and saved to `.agents/superpowers/plans/2026-06-30-bunch-110-fix-projection-gaps-for-playthrougharchived-and.md`.**

Two execution options:

1. **Subagent-Driven (recommended)** — Dispatch a fresh subagent per task and review between tasks.
2. **Inline Execution** — Execute all tasks in this session using `executing-plans`.

Which approach?
