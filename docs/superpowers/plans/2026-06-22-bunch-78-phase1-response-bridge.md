# BUNCH-78 Phase 1: Migrated-Flow Response Bridge

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax.
>
> **Prerequisite:** Read `2026-06-22-bunch-78-overview.md` for preflight context and campaign structure.

**Goal:** Add safe HUD/diary projection output to start-new-game and purchase-store-item command responses. Additive, backward-compatible, no domain changes.

**Tech Stack:** C# 14 / .NET, xUnit, TypeScript

---

## File Structure

| File | Action |
|------|--------|
| `src/WildBunch.Application/Games/Models/GameDtos.cs` | Add optional `HudProjection?` and `DiaryProjection?` to `GameSessionDto` |
| `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs` | Inject projectors, compute projections after commit |
| `src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs` | Inject projectors, compute projections after commit |
| `src/WildBunch.Web/src/api/types.ts` | Add projection types and optional fields |
| `tests/WildBunch.Application.Tests/StartNewGameHandlerTests.cs` | Add projection assertions |
| `tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs` | Add projection assertions, update constructor calls |
| `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs` | New: guardrail against new AddLogEntry call sites |
| `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md` | Update §10/§12 |

---

## Task 1: Add projection fields to GameSessionDto

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs:10-24`

- [ ] **Step 1: Write the failing test**

Create `tests/WildBunch.Application.Tests/GameSessionDtoProjectionFieldsTests.cs`:

```csharp
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class GameSessionDtoProjectionFieldsTests
{
    [Fact]
    public void GameSessionDtoDefaultsHudAndDiaryProjectionsToNull()
    {
        var dto = ConstructMinimalGameSessionDto();
        Assert.Null(dto.HudProjection);
        Assert.Null(dto.DiaryProjection);
    }

    [Fact]
    public void GameSessionDtoAcceptsHudAndDiaryProjectionsViaWithExpression()
    {
        var dto = ConstructMinimalGameSessionDto();
        var hud = new HudProjection(Guid.Empty, GameStatus.Active, "Test", 100, 50m,
            new TownId("dustvale"), "Dustvale", Array.Empty<HudInventoryItem>());
        var diary = new DiaryProjection(Guid.Empty, 1, 0, new TownId("dustvale"), "Dustvale",
            new[] { new DiaryEntry(1, 0, "Arrived.") });

        var withProjections = dto with { HudProjection = hud, DiaryProjection = diary };

        Assert.Same(hud, withProjections.HudProjection);
        Assert.Same(diary, withProjections.DiaryProjection);
    }

    private static GameSessionDto ConstructMinimalGameSessionDto()
    {
        // Use StubNewGameFactory + GameSessionMapper.ToDto, same as existing handler tests.
        var factory = new TestDoubles.StubNewGameFactory();
        var session = factory.Create("Test", Domain.Travel.TravelDifficulty.Normal, null, Domain.Travel.AdventureRandomnessPolicy.Standard);
        return Games.Mapping.GameSessionMapper.ToDto(session);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "GameSessionDtoProjectionFieldsTests"`
Expected: FAIL — `GameSessionDto` does not have `HudProjection` or `DiaryProjection` members.

- [ ] **Step 3: Add projection fields to GameSessionDto**

In `src/WildBunch.Application/Games/Models/GameDtos.cs`, add `using WildBunch.Application.Projections;` and extend the record:

```csharp
public sealed record GameSessionDto(
    Guid Id,
    GameStatus Status,
    TravelDifficulty TravelDifficulty,
    AdventureRandomnessPolicy Entropy,
    PlayerDto Player,
    WorldDto World,
    CaseFileDto CaseFile,
    InventoryDto Inventory,
    GameClockDto Clock,
    PursuitStateDto PursuitState,
    TravelJourneyDto? Journey,
    TravelDiaryDto? TravelDiary,
    IReadOnlyList<GameLogEntryDto> LogEntries,
    ActiveSaloonPersonOfInterestDto? ActiveSaloonPersonOfInterest,
    HudProjection? HudProjection = null,
    DiaryProjection? DiaryProjection = null)
{
    // ... existing body unchanged ...
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "GameSessionDtoProjectionFieldsTests"`
Expected: PASS.

- [ ] **Step 5: Run full build**

Run: `dotnet build`
Expected: PASS — optional parameters are backward-compatible.

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Application/Games/Models/GameDtos.cs tests/WildBunch.Application.Tests/GameSessionDtoProjectionFieldsTests.cs
git commit -m "BUNCH-78: add optional HudProjection/DiaryProjection fields to GameSessionDto"
```

---

## Task 2: Populate projections in StartNewGameHandler

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`

- [ ] **Step 1: Write the failing test**

Add to `tests/WildBunch.Application.Tests/StartNewGameHandlerTests.cs`:

```csharp
[Fact]
public async Task StartNewGameReturnsDtoWithHudAndDiaryProjections()
{
    var factory = new StubNewGameFactory();
    var repository = new InMemoryGameSessionRepository();
    var handler = new StartNewGameHandler(factory, repository, repository,
        new Projections.HudProjector(), new Projections.DiaryProjector());

    var result = await handler.HandleAsync(new StartNewGameCommand("Ranger Vale"));

    Assert.NotNull(result.HudProjection);
    Assert.Equal("Ranger Vale", result.HudProjection!.PlayerName);
    Assert.Equal(GameStatus.Active, result.HudProjection.Status);
    Assert.NotNull(result.DiaryProjection);
    Assert.NotEmpty(result.DiaryProjection!.Entries);
    Assert.Equal(result.Id, result.HudProjection.SessionId);
    Assert.Equal(result.Id, result.DiaryProjection.SessionId);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "StartNewGameReturnsDtoWithHudAndDiaryProjections"`
Expected: FAIL — constructor doesn't accept projectors.

- [ ] **Step 3: Update StartNewGameHandler**

```csharp
using WildBunch.Application.Projections;
// ... existing usings ...

public sealed class StartNewGameHandler : GameSessionCommandHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public StartNewGameHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    public async Task<GameSessionDto> HandleAsync(StartNewGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dto = await ExecuteNewSessionAsync(async ct =>
        {
            var session = _newGameFactory.Create(command.PlayerName, command.TravelDifficulty, command.SetupSeedCode, command.Entropy);
            return (session, GameSessionMapper.ToDto(session));
        }, cancellationToken).ConfigureAwait(false);

        var events = await GameSessionRepository.GetEventStreamAsync(
            new GameSessionId(dto.Id), 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "StartNewGameReturnsDtoWithHudAndDiaryProjections"`
Expected: PASS.

- [ ] **Step 5: Run all StartNewGameHandler tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "StartNewGameHandlerTests"`
Expected: PASS — update existing test constructor calls to pass `new HudProjector(), new DiaryProjector()`.

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs tests/WildBunch.Application.Tests/StartNewGameHandlerTests.cs
git commit -m "BUNCH-78: populate HUD/diary projections in StartNewGameHandler response"
```

---

## Task 3: Populate projections in PurchaseStoreItemHandler

**Files:**
- Modify: `src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs`

- [ ] **Step 1: Write the failing test**

Add to `tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs`:

```csharp
[Fact]
public async Task PurchaseReturnsDtoWithHudAndDiaryProjections()
{
    var repository = new InMemoryGameSessionRepository();
    var session = CreateSession();
    session.MarkEventsCommitted();
    repository.Seed(session);
    var handler = new PurchaseStoreItemHandler(repository, repository, new TownStoreCatalogResolver(),
        new Projections.HudProjector(), new Projections.DiaryProjector());

    var result = await handler.HandleAsync(new PurchaseStoreItemCommand(
        session.Id.Value, "pinecross", StoreVendorType.GeneralStore, DomainItemKind.Food, 2));

    Assert.True(result.Success);
    Assert.NotNull(result.CurrentSession.HudProjection);
    Assert.Equal(21m, result.CurrentSession.HudProjection!.WalletCash);
    Assert.NotNull(result.CurrentSession.DiaryProjection);
    Assert.NotEmpty(result.CurrentSession.DiaryProjection!.Entries);
    Assert.Equal(session.Id.Value, result.CurrentSession.HudProjection.SessionId);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "PurchaseReturnsDtoWithHudAndDiaryProjections"`
Expected: FAIL — constructor doesn't accept projectors.

- [ ] **Step 3: Update PurchaseStoreItemHandler**

Add `HudProjector` and `DiaryProjector` to constructor. After `ExecuteWithRetryAsync` returns, load event stream and compute projections. The existing lambda body (current lines 35-77) stays unchanged — only constructor and post-execution projection computation are added.

```csharp
// Constructor: add HudProjector and DiaryProjector parameters
// After ExecuteWithRetryAsync:
var events = await GameSessionRepository.GetEventStreamAsync(sessionId, 0, cancellationToken);
var hud = _hudProjector.Project(events) with { SessionId = command.GameSessionId };
var diary = _diaryProjector.Project(events) with { SessionId = command.GameSessionId };

return result with
{
    CurrentSession = result.CurrentSession with { HudProjection = hud, DiaryProjection = diary }
};
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "PurchaseReturnsDtoWithHudAndDiaryProjections"`
Expected: PASS.

- [ ] **Step 5: Run all PurchaseStoreItemHandler tests**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "PurchaseStoreItemHandlerTests"`
Expected: PASS — update existing test constructor calls to pass the two new projectors.

- [ ] **Step 6: Commit**

```powershell
git add src/WildBunch.Application/Games/Commands/PurchaseStoreItemHandler.cs tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs
git commit -m "BUNCH-78: populate HUD/diary projections in PurchaseStoreItemHandler response"
```

---

## Task 4: Update frontend TypeScript types

**Files:**
- Modify: `src/WildBunch.Web/src/api/types.ts`

- [ ] **Step 1: Add projection type definitions**

Add after the existing `GameLogEntryDto` type:

```typescript
export interface HudProjection {
  sessionId: string;
  status: GameStatus;
  playerName: string;
  health: number;
  walletCash: number;
  currentTownId: string;
  currentTownName: string;
  inventoryItems: HudInventoryItem[];
}

export interface HudInventoryItem {
  itemKind: ItemKind;
  quantity: number;
}

export interface DiaryProjection {
  sessionId: string;
  day: number;
  turn: number;
  currentTownId: string;
  currentTownName: string;
  entries: DiaryEntry[];
}

export interface DiaryEntry {
  day: number;
  turn: number;
  summary: string;
}
```

- [ ] **Step 2: Add optional projection fields to GameSessionDto interface**

```typescript
  hudProjection?: HudProjection | null;
  diaryProjection?: DiaryProjection | null;
```

- [ ] **Step 3: Run frontend build**

```powershell
cd src/WildBunch.Web; npm run build
```
Expected: PASS.

- [ ] **Step 4: Commit**

```powershell
git add src/WildBunch.Web/src/api/types.ts
git commit -m "BUNCH-78: add HudProjection/DiaryProjection TypeScript types"
```

---

## Task 4b: Verify DI registration for HudProjector and DiaryProjector

**Files:**
- Verify: `src/WildBunch.Api/DependencyInjection.cs`

`HudProjector` and `DiaryProjector` are already registered as singletons at lines 58-59 of `DependencyInjection.cs`. `StartNewGameHandler` and `PurchaseStoreItemHandler` are registered as scoped at lines 34 and 39. After changing the handler constructors to accept the projectors, DI will inject the singletons automatically.

- [ ] **Step 1: Verify existing registrations**

Read `src/WildBunch.Api/DependencyInjection.cs` and confirm:
- `services.AddSingleton<HudProjector>();` exists (line 58)
- `services.AddSingleton<DiaryProjector>();` exists (line 59)
- `services.AddScoped<StartNewGameHandler>();` exists (line 34)
- `services.AddScoped<PurchaseStoreItemHandler>();` exists (line 39)

- [ ] **Step 2: If any registration is missing, add it**

If `HudProjector` or `DiaryProjector` is not registered (should not be the case based on preflight, but verify), add:
```csharp
services.AddSingleton<HudProjector>();
services.AddSingleton<DiaryProjector>();
```

- [ ] **Step 3: Prove DI resolution with build + integration test**

Run: `dotnet build`
Expected: PASS — DI container can resolve the new constructor parameters.

Run: `dotnet test tests/WildBunch.Integration.Tests --filter "GameApiTests"` (or equivalent API smoke test)
Expected: PASS — API endpoints that use `StartNewGameHandler` and `PurchaseStoreItemHandler` resolve correctly through DI.

- [ ] **Step 4: No commit needed if registrations already exist**

If no changes to `DependencyInjection.cs` were needed, skip the commit. If changes were made:
```powershell
git add src/WildBunch.Api/DependencyInjection.cs
git commit -m "BUNCH-78: verify/add DI registration for HudProjector and DiaryProjector"
```

---

## Task 5: Add guardrail test against new AddLogEntry call sites

**Files:**
- Create: `tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs`

- [ ] **Step 1: Write the guardrail test**

The test uses a robust repo-root discovery strategy: walk up from `AppContext.BaseDirectory` looking for the `AGENTS.md` sentinel file. This works from any test output path (`bin/Debug/net8.0/`, `bin/Release/...`, etc.) without hardcoding relative depths.

```csharp
using System.Text.RegularExpressions;

namespace WildBunch.Application.Tests;

public sealed class AddLogEntryGuardrailTests
{
    // Known count of AddLogEntry call sites in GameSession.cs as of BUNCH-78 preflight.
    // Do not increase this number without explicit architecture approval.
    // AddLogEntry is [Obsolete] projection-legacy per ADR-0028.
    private const int KnownLegacyAddLogEntryCallSiteCount = 18;

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Could not find repo root (AGENTS.md sentinel) starting from {AppContext.BaseDirectory}");
    }

    [Fact]
    public void GameSessionDoesNotAddNewAddLogEntryCallSites()
    {
        var repoRoot = FindRepoRoot();
        var gameSessionPath = Path.Combine(repoRoot, "src", "WildBunch.Domain", "Game", "GameSession.cs");
        Assert.True(File.Exists(gameSessionPath),
            $"Could not find GameSession.cs at {gameSessionPath}. " +
            $"Repo root resolved to {repoRoot}. " +
            $"Test output base directory was {AppContext.BaseDirectory}.");

        var source = File.ReadAllText(gameSessionPath);
        var matches = Regex.Matches(source, @"\bAddLogEntry\s*\(");

        Assert.True(matches.Count <= KnownLegacyAddLogEntryCallSiteCount,
            $"AddLogEntry call site count increased to {matches.Count} (expected at most {KnownLegacyAddLogEntryCallSiteCount}). " +
            "AddLogEntry is [Obsolete] projection-legacy per ADR-0028. " +
            "New domain code must use typed domain events instead. " +
            "If this increase is intentional and approved, update KnownLegacyAddLogEntryCallSiteCount.");
    }
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "AddLogEntryGuardrailTests"`
Expected: PASS — current count is 18.

- [ ] **Step 3: Commit**

```powershell
git add tests/WildBunch.Application.Tests/AddLogEntryGuardrailTests.cs
git commit -m "BUNCH-78: add guardrail test against new AddLogEntry call sites"
```

---

## Task 6: Update ADR-0028

**Files:**
- Modify: `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`

- [ ] **Step 1: Update §10** — mark response bridge as addressed. Replace the "A follow-up issue should..." sentence with:

> BUNCH-78 addressed the first half of this follow-up: migrated command responses (start-new-game, purchase-store-item) now include safe HUD/diary projection output inline via optional `HudProjection` and `DiaryProjection` fields on `GameSessionDto`. Legacy `LogEntries` remains for backward compatibility. Dropping `LogEntries` from command responses entirely is a future slice pending UI migration.

- [ ] **Step 2: Note LegacyLogProjector gap in §12** — add after the `LegacyLogProjector` sentence:

> Note: `LegacyLogProjector` is referenced in this ADR but not yet implemented in source as of BUNCH-78. It remains a future implementation item.

- [ ] **Step 3: Commit**

```powershell
git add docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md
git commit -m "BUNCH-78: update ADR-0028 §10/§12 to mark response bridge addressed and note LegacyLogProjector gap"
```
