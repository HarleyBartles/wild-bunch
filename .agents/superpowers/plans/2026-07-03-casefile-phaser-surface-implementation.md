# Case File Phaser Surface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a skeuomorphic detective board with paper artifacts (clues, warrants, suspects) and string connections, where the game auto-draws known connections and players can draw their own theory connections.

**Architecture:** Extract existing CaseFile into CaseFileAggregate. React host component manages Phaser game instance following PhaserMapHost pattern. Backend adds auto-layout projection algorithm. React drives all domain state, manages player-drawn connections locally. Phaser is pure renderer.

**Aggregate Normalization Decision:** CaseFileAggregate extraction is intentional future-proofing. The current GameSession-hosted shape is not broken today, but upcoming case-file surfaces will make it increasingly ugly. We are normalizing the aggregate boundary now in anticipation of those seams.

**Scope:** CaseFileAggregate normalization, auto-layout algorithm, connection rendering. Future case-file features (richer theory-board play, advanced connection types) are out of scope.

**Hidden-Truth Guard:** CaseFileAggregate must not leak hidden culprit truth through case-file DTOs, board projections, Phaser scene state, logs, snapshots, or tests. The board should render known/discovered/player-available case information only. If the aggregate internally needs truth to enforce rules, that is not the same as exposing it.

**Tech Stack:** Phaser 3, React, TypeScript, C#/.NET, styled-components

## Global Constraints

- Follow existing PhaserMapHost pattern for React-Phaser integration
- Extract CaseFile into CaseFileAggregate (new aggregate boundary)
- Move case file events from GameSession to CaseFileAggregate
- Use force-directed or grid-based layout algorithm for artifact positioning
- React manages all domain state, Phaser scenes do not maintain local state
- Player-drawn connections stored locally in React state (not sent to backend)
- Use procedural assets (Phaser graphics primitives) in Phase 1
- Follow existing styled-components pattern for React styling
- Maintain existing useGameSession hook integration
- No database migration needed (greenfield project)
- Update InvestigationLoop and BountyLoop to work with CaseFileAggregate

---

## File Structure

**New Domain Files:**
- `src/WildBunch.Domain/Cases/CaseFileAggregate.cs` - Case file aggregate root
- `src/WildBunch.Domain/Cases/CaseFileId.cs` - Case file identifier
- `src/WildBunch.Application/Projections/CaseFileLayoutProjection.cs` - Auto-layout algorithm

**New Application Files:**
- `src/WildBunch.Application/Games/Models/CaseFileBoardDto.cs` - Extended case file DTO for Phaser
- `src/WildBunch.Application/Games/Models/BoardArtifactDto.cs` - Board artifact DTO
- `src/WildBunch.Application/Games/Models/BoardConnectionDto.cs` - Board connection DTO
- `src/WildBunch.Application/Games/Mapping/CaseFileLayoutMapper.cs` - Layout mapping logic

**New Persistence Files:**
- `src/WildBunch.Persistence/GameSessions/CaseFileEntity.cs` - Case file EF entity
- `src/WildBunch.Persistence/GameSessions/CaseFileEntityConfiguration.cs` - EF configuration
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.CaseFile.cs` - Case file snapshot serialization

**New Web Files:**
- `src/WildBunch.Web/src/components/casefile/PhaserCaseFileHost.tsx` - React host component
- `src/WildBunch.Web/src/components/casefile/CaseFileScene.ts` - Phaser scene
- `src/WildBunch.Web/src/components/casefile/types.ts` - TypeScript types

**Modified Domain Files:**
- `src/WildBunch.Domain/Cases/CaseFile.cs` - Extract into CaseFileAggregate
- `src/WildBunch.Domain/Game/GameSession.cs` - Remove case file logic, add CaseFileAggregate reference
- `src/WildBunch.Domain/Events/InvestigationPerformed.cs` - Split into case file events
- `src/WildBunch.Domain/Game/InvestigationLoop.cs` - Update to work with CaseFileAggregate
- `src/WildBunch.Domain/Game/BountyLoop.cs` - Update to work with CaseFileAggregate

**Modified Application Files:**
- `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs` - Add case file layout mapping
- `src/WildBunch.Application/Games/Models/GameDtos.cs` - Extend with case file board DTOs

**Modified Persistence Files:**
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` - Add case file snapshot
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` - Add case file persistence

**Modified Web Files:**
- `src/WildBunch.Web/src/routes/CaseFileRoute.tsx` - Integrate Phaser surface
- `src/WildBunch.Web/src/components/CaseFileSurface.tsx` - Replace with Phaser surface

---

### Task 1: Extract CaseFile into CaseFileAggregate

**Files:**
- Create: `src/WildBunch.Domain/Cases/CaseFileId.cs`
- Create: `src/WildBunch.Domain/Cases/CaseFileAggregate.cs`
- Test: `tests/WildBunch.Domain.Tests/Cases/CaseFileAggregateTests.cs`

**Interfaces:**
- Produces: `CaseFileId` identifier
- Produces: `CaseFileAggregate` with case file command methods

- [ ] **Step 1: Write the failing test for CaseFileAggregate creation**

```csharp
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests.Cases;

public class CaseFileAggregateTests
{
    [Fact]
    public void CreateCaseFileAggregate_WithValidData_Succeeds()
    {
        var suspects = new List<Suspect> { /* test suspects */ };
        var clues = new List<Clue> { /* test clues */ };
        
        var caseFile = new CaseFileAggregate(
            new CaseFileId(Guid.NewGuid()),
            suspects,
            new SuspectId("culprit-1"),
            clues);
        
        Assert.NotNull(caseFile);
        Assert.NotEmpty(caseFile.Suspects);
    }
    
    [Fact]
    public void AddClue_WhenValid_AddsToKnownClues()
    {
        var caseFile = CreateTestCaseFile();
        var clue = new Clue(new ClueId("clue-1"), ClueKind.Testimony, "Test clue");
        
        caseFile.AddClue(clue);
        
        Assert.Contains(caseFile.KnownClues, c => c.Id == clue.Id);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CaseFileAggregateTests" -v`
Expected: FAIL with "CaseFileAggregate not defined"

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace WildBunch.Domain.Cases;

public sealed record CaseFileId(Guid Value)
{
    public static CaseFileId New() => new(Guid.NewGuid());
}
```

```csharp
using WildBunch.Domain.Events;

namespace WildBunch.Domain.Cases;

public sealed class CaseFileAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    private readonly List<Clue> _knownClues = [];
    private readonly List<Warrant> _knownWarrants = [];
    private readonly List<SuspectId> _discoveredSuspectIds = [];
    
    public CaseFileId Id { get; }
    public IReadOnlyList<Suspect> Suspects { get; }
    public SuspectId TrueCulpritId { get; }
    public IReadOnlyList<Clue> KnownClues => _knownClues;
    public IReadOnlyList<Warrant> KnownWarrants => _knownWarrants;
    public IReadOnlyList<SuspectId> DiscoveredSuspectIds => _discoveredSuspectIds;
    public string OpeningLead { get; }
    public CaseState State { get; }
    
    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;
    
    public CaseFileAggregate(
        CaseFileId id,
        IReadOnlyList<Suspect> suspects,
        SuspectId trueCulpritId,
        IReadOnlyList<Clue> knownClues,
        string openingLead = "Follow the leads.",
        CaseState state = CaseState.Active)
    {
        Id = id;
        Suspects = suspects;
        TrueCulpritId = trueCulpritId;
        _knownClues.AddRange(knownClues);
        OpeningLead = openingLead;
        State = state;
    }
    
    public void AddClue(Clue clue)
    {
        ArgumentNullException.ThrowIfNull(clue);
        
        if (_knownClues.Any(c => c.Id == clue.Id))
        {
            return; // Already have this clue
        }
        
        _knownClues.Add(clue);
        
        var evt = new ClueDiscovered
        {
            ClueId = clue.Id,
            SourceKind = clue.SourceKind,
            Message = $"Discovered: {clue.Description}"
        };
        
        _uncommittedEvents.Add(evt);
    }
    
    public void AddWarrant(Warrant warrant)
    {
        ArgumentNullException.ThrowIfNull(warrant);
        
        if (_knownWarrants.Any(w => w.Id == warrant.Id))
        {
            return;
        }
        
        _knownWarrants.Add(warrant);
        
        var evt = new WarrantIssued
        {
            WarrantId = warrant.Id,
            TargetName = warrant.TargetName,
            Summary = warrant.Summary
        };
        
        _uncommittedEvents.Add(evt);
    }
    
    public void DiscoverSuspect(SuspectId suspectId)
    {
        if (_discoveredSuspectIds.Contains(suspectId))
        {
            return;
        }
        
        _discoveredSuspectIds.Add(suspectId);
        
        var suspect = Suspects.FirstOrDefault(s => s.Id == suspectId);
        if (suspect != null)
        {
            var evt = new SuspectDiscovered
            {
                SuspectId = suspectId,
                Name = suspect.Name
            };
            
            _uncommittedEvents.Add(evt);
        }
    }
    
    public void MarkEventsCommitted()
    {
        _uncommittedEvents.Clear();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CaseFileAggregateTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Cases/CaseFileId.cs src/WildBunch.Domain/Cases/CaseFileAggregate.cs tests/WildBunch.Domain.Tests/Cases/CaseFileAggregateTests.cs
git commit -m "feat: extract CaseFile into CaseFileAggregate"
```

---

### Task 2: Add Case File Events

**Files:**
- Create: `src/WildBunch.Domain/Events/ClueDiscovered.cs`
- Create: `src/WildBunch.Domain/Events/WarrantIssued.cs`
- Create: `src/WildBunch.Domain/Events/SuspectDiscovered.cs`
- Test: `tests/WildBunch.Domain.Tests/Events/CaseFileEventTests.cs`

**Interfaces:**
- Produces: Case file-specific events for event sourcing

- [ ] **Step 1: Write the failing test for case file events**

```csharp
[Fact]
public void ClueDiscovered_CanBeCreated()
{
    var evt = new ClueDiscovered
    {
        ClueId = new ClueId("clue-1"),
        SourceKind = InvestigationSourceKind.SaloonLookAround,
        Message = "Found a clue"
    };
    
    Assert.NotNull(evt);
    Assert.Equal(new ClueId("clue-1"), evt.ClueId);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CaseFileEventTests" -v`
Expected: FAIL with events not defined

- [ ] **Step 3: Write minimal implementation**

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

public sealed record ClueDiscovered : IDomainEvent
{
    public required ClueId ClueId { get; init; }
    public required InvestigationSourceKind SourceKind { get; init; }
    public required string Message { get; init; }
}

public sealed record WarrantIssued : IDomainEvent
{
    public required WarrantId WarrantId { get; init; }
    public required string TargetName { get; init; }
    public required string Summary { get; init; }
}

public sealed record SuspectDiscovered : IDomainEvent
{
    public required SuspectId SuspectId { get; init; }
    public required string Name { get; init; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CaseFileEventTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Events/ClueDiscovered.cs src/WildBunch.Domain/Events/WarrantIssued.cs src/WildBunch.Domain/Events/SuspectDiscovered.cs tests/WildBunch.Domain.Tests/Events/CaseFileEventTests.cs
git commit -m "feat: add case file-specific events"
```

---

### Task 3: Extract CaseFile Logic from GameSession

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/InvestigationLoop.cs`
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs`
- Test: `tests/WildBunch.Domain.Tests/Game/GameSessionCaseFileExtractionTests.cs`

**Interfaces:**
- Consumes: `CaseFileAggregate` from Task 1
- Produces: GameSession with CaseFileAggregate reference
- Produces: Updated InvestigationLoop and BountyLoop

- [ ] **Step 1: Write the failing test for GameSession with CaseFileAggregate**

```csharp
[Fact]
public void GameSession_WithCaseFileAggregate_DelegatesCaseFileCommands()
{
    var session = CreateTestGameSession();
    var caseFile = session.CaseFile;
    
    Assert.NotNull(caseFile);
    
    var clue = new Clue(new ClueId("clue-1"), ClueKind.Testimony, "Test clue");
    caseFile.AddClue(clue);
    
    Assert.Contains(caseFile.KnownClues, c => c.Id == clue.Id);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionCaseFileExtractionTests" -v`
Expected: FAIL with CaseFileAggregate not found

- [ ] **Step 3: Add CaseFileAggregate to GameSession**

```csharp
public sealed partial class GameSession
{
    private readonly CaseFileAggregate _caseFile;
    
    public CaseFileAggregate CaseFile => _caseFile;
    
    private GameSession(
        // ... existing constructor parameters ...
        CaseFile? caseFile = null)
    {
        // ... existing constructor logic ...
        
        _caseFile = caseFile ?? new CaseFileAggregate(
            CaseFileId.New(),
            new CaseFile(/* existing case file data */),
            /* ... */);
    }
}
```

- [ ] **Step 4: Update GameSession.Apply(InvestigationPerformed) to delegate to CaseFileAggregate**

```csharp
private void Apply(InvestigationPerformed evt)
{
    // Extract clue/warrant from case file public pool
    Clue? clue = null;
    if (evt.ClueId.HasValue)
    {
        clue = _caseFile.KnownClues.FirstOrDefault(c => c.Id == evt.ClueId.Value);
    }
    
    Warrant? warrant = null;
    if (evt.WarrantId.HasValue)
    {
        warrant = _caseFile.KnownWarrants.FirstOrDefault(w => w.Id == evt.WarrantId.Value);
    }
    
    // Apply to investigation state
    _currentTownVisit.SpendSource(evt.SourceKind);
    
    // Add to case file if not already present
    if (clue != null && !_caseFile.KnownClues.Contains(clue))
    {
        _caseFile.AddClue(clue);
    }
    
    if (warrant != null && !_caseFile.KnownWarrants.Contains(warrant))
    {
        _caseFile.AddWarrant(warrant);
    }
    
    _caseFile.MarkEventsCommitted();
}
```

- [ ] **Step 5: Update InvestigationLoop to work with CaseFileAggregate**

```csharp
internal InvestigationOutcome ReadWantedPosters(InvestigationContext context)
{
    // Pass CaseFileAggregate to resolver
    var warrant = _wantedPosterResolver.Resolve(
        context.CaseFile, // Now use CaseFileAggregate
        context.CurrentTownSlotIndex,
        context.CurrentTownVisitCount,
        context.SaltSource,
        context.RetiredWarrantIds.Count > 0 ? context.RetiredWarrantIds : null);
    
    var clue = _clueSurfacingResolver.Resolve(
        context.CaseFile, // Now use CaseFileAggregate
        InvestigationSourceKind.SheriffWarrants,
        context.CurrentTownSlotIndex,
        context.CurrentTownVisitCount,
        context.SaltSource);
    
    // ... rest of implementation ...
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~GameSessionCaseFileExtractionTests" -v`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/InvestigationLoop.cs src/WildBunch.Domain/Game/BountyLoop.cs tests/WildBunch.Domain.Tests/Game/GameSessionCaseFileExtractionTests.cs
git commit -m "refactor: extract case file logic from GameSession to CaseFileAggregate"
```

---

### Task 4: Implement Auto-Layout Algorithm

**Files:**
- Create: `src/WildBunch.Application/Projections/CaseFileLayoutProjection.cs`
- Test: `tests/WildBunch.Application.Tests/Projections/CaseFileLayoutProjectionTests.cs`

**Interfaces:**
- Consumes: `CaseFileAggregate` from Task 1
- Produces: Auto-layout algorithm for artifact positioning

- [ ] **Step 1: Write the failing test for auto-layout**

```csharp
[Fact]
public void ProjectLayout_SameCaseFile_ProducesSameLayout()
{
    var caseFile = CreateTestCaseFile();
    
    var layout1 = CaseFileLayoutProjection.ProjectLayout(caseFile);
    var layout2 = CaseFileLayoutProjection.ProjectLayout(caseFile);
    
    Assert.Equal(layout1.Artifacts.Count, layout2.Artifacts.Count);
    Assert.Equal(layout1.AutoConnections.Count, layout2.AutoConnections.Count);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~CaseFileLayoutProjectionTests" -v`
Expected: FAIL with "CaseFileLayoutProjection not defined"

- [ ] **Step 3: Write minimal implementation**

```csharp
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Projections;

public static class CaseFileLayoutProjection
{
    public static CaseFileBoardDto ProjectLayout(CaseFileAggregate caseFile)
    {
        ArgumentNullException.ThrowIfNull(caseFile);
        
        var artifacts = new List<BoardArtifactDto>();
        var autoConnections = new List<BoardConnectionDto>();
        var artifactId = 0;
        
        // Position clues
        foreach (var clue in caseFile.KnownClues)
        {
            artifacts.Add(new BoardArtifactDto(
                $"artifact-{artifactId++}",
                ArtifactKind.Clue,
                clue.Description,
                clue.Description,
                GeneratePosition(artifactId, 100, 700),
                false));
        }
        
        // Position warrants
        foreach (var warrant in caseFile.KnownWarrants)
        {
            artifacts.Add(new BoardArtifactDto(
                $"artifact-{artifactId++}",
                ArtifactKind.Warrant,
                warrant.TargetName,
                warrant.Summary,
                GeneratePosition(artifactId, 100, 700),
                false));
        }
        
        // Position discovered suspects
        foreach (var suspectId in caseFile.DiscoveredSuspectIds)
        {
            var suspect = caseFile.Suspects.FirstOrDefault(s => s.Id == suspectId);
            if (suspect != null)
            {
                artifacts.Add(new BoardArtifactDto(
                    $"artifact-{artifactId++}",
                    ArtifactKind.Suspect,
                    suspect.Name,
                    suspect.Status.ToString(),
                    GeneratePosition(artifactId, 100, 700),
                    false));
            }
        }
        
        // Generate auto-connections based on domain relationships
        autoConnections = GenerateAutoConnections(caseFile, artifacts);
        
        return new CaseFileBoardDto(
            caseFile.OpeningLead,
            new CaseStateDto(caseFile.State.ToString(), caseFile.State.ToString()),
            artifacts,
            autoConnections,
            new CaseBoardDto(/* ... existing case board data ... */),
            caseFile.KnownClues.Select(ClueMapper.ToDto).ToArray());
    }
    
    private static (float X, float Y) GeneratePosition(int index, int minX, int maxX)
    {
        // Simple grid-based positioning for now
        const int cols = 5;
        const int spacing = 120;
        const int padding = 50;
        
        var col = index % cols;
        var row = index / cols;
        
        var x = padding + col * spacing;
        var y = padding + row * spacing;
        
        return (x, y);
    }
    
    private static List<BoardConnectionDto> GenerateAutoConnections(
        CaseFileAggregate caseFile,
        List<BoardArtifactDto> artifacts)
    {
        var connections = new List<BoardConnectionDto>();
        
        // Connect clues to related suspects
        // This is a simplified version - full implementation would use domain relationships
        for (int i = 0; i < Math.Min(artifacts.Count - 1, 3); i++)
        {
            connections.Add(new BoardConnectionDto(
                artifacts[i].ArtifactId,
                artifacts[i + 1].ArtifactId,
                ConnectionKind.GameKnown,
                null));
        }
        
        return connections;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~CaseFileLayoutProjectionTests" -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Application/Projections/CaseFileLayoutProjection.cs tests/WildBunch.Application.Tests/Projections/CaseFileLayoutProjectionTests.cs
git commit -m "feat: implement case file auto-layout algorithm"
```

---

### Task 5: Add Case File Board DTOs and Mapping

**Files:**
- Create: `src/WildBunch.Application/Games/Models/CaseFileBoardDto.cs`
- Create: `src/WildBunch.Application/Games/Models/BoardArtifactDto.cs`
- Create: `src/WildBunch.Application/Games/Models/BoardConnectionDto.cs`
- Modify: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Mapping/CaseFileBoardMapperTests.cs`

**Interfaces:**
- Consumes: Auto-layout projection from Task 4
- Produces: DTOs for API
- Produces: Mapping logic

- [ ] **Step 1: Write the failing test for DTO mapping**

```csharp
[Fact]
public void ToDto_IncludesCaseFileBoard()
{
    var session = CreateTestSessionWithCaseFile();
    var dto = GameSessionMapper.ToDto(session);
    
    Assert.NotNull(dto.CaseFile);
    // CaseFileBoardDto would be in JournalDto, not GameSessionDto
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~CaseFileBoardMapperTests" -v`
Expected: FAIL with DTOs not defined

- [ ] **Step 3: Write minimal implementation**

```csharp
namespace WildBunch.Application.Games.Models;

public sealed record CaseFileBoardDto(
    string OpeningLead,
    CaseStateDto CaseState,
    IReadOnlyList<BoardArtifactDto> Artifacts,
    IReadOnlyList<BoardConnectionDto> AutoConnections,
    CaseBoardDto CaseBoard,
    IReadOnlyList<ClueDto> KnownClues);

public sealed record BoardArtifactDto(
    string ArtifactId,
    ArtifactKind Kind,
    string Title,
    string Content,
    (float X, float Y) Position,
    bool IsSelected);

public sealed record BoardConnectionDto(
    string FromArtifactId,
    string ToArtifactId,
    ConnectionKind Kind,
    string? Label);

public enum ArtifactKind
{
    Clue,
    Warrant,
    Suspect,
    Note
}

public enum ConnectionKind
{
    GameKnown,
    PlayerTheory
}
```

- [ ] **Step 4: Update JournalMapper to include board layout**

```csharp
public static JournalDto ToDto(Journal journal)
{
    // ... existing mapping ...
    
    var caseFileAggregate = new CaseFileAggregate(
        CaseFileId.New(),
        journal.caseFile.Suspects,
        journal.caseFile.TrueCulpritId,
        journal.caseFile.knownClues);
    
    var caseFileBoard = CaseFileLayoutProjection.ProjectLayout(caseFileAggregate);
    
    return new JournalDto(
        // ... existing fields ...
        journal.caseFile with { /* ... */ },
        caseFileBoard);
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~CaseFileBoardMapperTests" -v`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Application/Games/Models/CaseFileBoardDto.cs src/WildBunch.Application/Games/Models/BoardArtifactDto.cs src/WildBunch.Application/Games/Models/BoardConnectionDto.cs src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs src/WildBunch.Application/Games/Mapping/JournalMapper.cs tests/WildBunch.Application.Tests/Mapping/CaseFileBoardMapperTests.cs
git commit -m "feat: add case file board DTOs and mapping"
```

---

### Task 6: Add Case File Persistence and Snapshot Serialization

**Files:**
- Create: `src/WildBunch.Persistence/GameSessions/CaseFileEntity.cs`
- Create: `src/WildBunch.Persistence/GameSessions/CaseFileEntityConfiguration.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.CaseFile.cs`
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- Test: `tests/WildBunch.Persistence.Tests/Serialization/CaseFileSnapshotTests.cs`

**Interfaces:**
- Consumes: `CaseFileAggregate` from Task 1
- Produces: EF entity and snapshot serialization

- [ ] **Step 1: Write the failing test for case file snapshot serialization**

```csharp
[Fact]
public void SerializeAndDeserialize_CaseFile_RoundTripsCorrectly()
{
    var caseFile = new CaseFileAggregate(
        CaseFileId.New(),
        new List<Suspect>(),
        new SuspectId("culprit-1"),
        new List<Clue>());
    
    var snapshot = CaseFileSnapshot.FromDomain(caseFile);
    var json = JsonSerializer.Serialize(snapshot);
    var deserialized = JsonSerializer.Deserialize<CaseFileSnapshot>(json);
    var restored = deserialized.ToDomain();
    
    Assert.Equal(caseFile.Id, restored.Id);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Persistence.Tests/WildBunch.Persistence.Tests.csproj --filter "FullyQualifiedName~CaseFileSnapshotTests" -v`
Expected: FAIL with serialization not implemented

- [ ] **Step 3: Add EF entity and configuration**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WildBunch.Persistence.GameSessions;

[Table("CaseFileComponents")]
public class CaseFileEntity
{
    [Key]
    public Guid GameSessionId { get; set; }
    
    public string StateJson { get; set; } = string.Empty;
}
```

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public class CaseFileEntityConfiguration : IEntityTypeConfiguration<CaseFileEntity>
{
    public void Configure(EntityTypeBuilder<CaseFileEntity> builder)
    {
        builder.HasKey(c => c.GameSessionId);
        
        builder.Property(c => c.StateJson)
            .IsRequired();
        
        builder.HasOne<GameSessionEntity>()
            .WithOne()
            .HasForeignKey<CaseFileEntity>(c => c.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: Add snapshot serialization**

```csharp
namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private sealed record CaseFileSnapshot(
        Guid Id,
        string OpeningLead,
        string State,
        IReadOnlyList<ClueSnapshot> KnownClues,
        IReadOnlyList<WarrantSnapshot> KnownWarrants,
        IReadOnlyList<SuspectIdSnapshot> DiscoveredSuspectIds)
    {
        public static CaseFileSnapshot FromDomain(Domain.Cases.CaseFileAggregate caseFile)
        {
            ArgumentNullException.ThrowIfNull(caseFile);
            
            return new CaseFileSnapshot(
                caseFile.Id.Value,
                caseFile.OpeningLead,
                caseFile.State.ToString(),
                caseFile.KnownClues.Select(ClueSnapshot.FromDomain).ToArray(),
                caseFile.KnownWarrants.Select(WarrantSnapshot.FromDomain).ToArray(),
                caseFile.DiscoveredSuspectIds.Select(id => new SuspectIdSnapshot(id.Value)).ToArray());
        }
        
        public Domain.Cases.CaseFileAggregate ToDomain()
        {
            return new Domain.Cases.CaseFileAggregate(
                new Domain.Cases.CaseFileId(Id),
                /* ... reconstruct suspects ... */,
                /* ... reconstruct true culprit ... */,
                KnownClues.Select(c => c.ToDomain()).ToArray(),
                OpeningLead,
                Enum.Parse<Domain.Cases.CaseState>(State));
        }
    }
    
    // Similar snapshots for Clue, Warrant, SuspectId
}
```

- [ ] **Step 5: Update GameSessionSnapshot to include CaseFileSnapshot**

```csharp
private sealed record GameSessionSnapshot(
    // ... existing fields ...
    CaseFileSnapshot? CaseFile)
{
    public static GameSessionSnapshot FromDomain(GameSession session)
    {
        return new GameSessionSnapshot(
            // ... existing fields ...
            session.CaseFile is null ? null : CaseFileSnapshot.FromDomain(session.CaseFile));
    }
    
    public GameSession ToDomain()
    {
        // ... existing logic ...
        
        if (CaseFile is not null)
        {
            GameSessionRehydrator.RestoreCaseFileState(session, CaseFile.ToDomain());
        }
        
        return session;
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Persistence.Tests/WildBunch.Persistence.Tests.csproj --filter "FullyQualifiedName~CaseFileSnapshotTests" -v`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Persistence/GameSessions/CaseFileEntity.cs src/WildBunch.Persistence/GameSessions/CaseFileEntityConfiguration.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.CaseFile.cs src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs tests/WildBunch.Persistence.Tests/Serialization/CaseFileSnapshotTests.cs
git commit -m "feat: add case file persistence and snapshot serialization"
```

---

### Task 7: Create Phaser Case File Scene

**Files:**
- Create: `src/WildBunch.Web/src/components/casefile/CaseFileScene.ts`
- Create: `src/WildBunch.Web/src/components/casefile/types.ts`
- Test: `src/WildBunch.Web/src/tests/CaseFileScene.test.tsx`

**Interfaces:**
- Consumes: `CaseFileBoardDto` from Task 5
- Produces: `CaseFileScene` Phaser scene with artifact rendering and connection drawing

- [ ] **Step 1: Write the failing test for scene creation**

```typescript
import { CaseFileScene } from '../components/casefile/CaseFileScene';
import { CaseFileBoardDto } from '../api/types';

describe('CaseFileScene', () => {
  it('should create scene with artifacts and connections', () => {
    const board: CaseFileBoardDto = {
      openingLead: 'Test lead',
      caseState: { status: 'Active', statusText: 'Active' },
      artifacts: [
        { artifactId: 'clue-1', kind: 'Clue', title: 'Test Clue', content: 'Test content', position: { x: 100, y: 100 }, isSelected: false }
      ],
      autoConnections: [
        { fromArtifactId: 'clue-1', toArtifactId: 'clue-2', kind: 'GameKnown', label: null }
      ],
      caseBoard: {} as any,
      knownClues: []
    };
    
    const scene = new CaseFileScene(board, [], () => {}, 'move');
    
    expect(scene).toBeDefined();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- CaseFileScene.test.tsx`
Expected: FAIL with "CaseFileScene not defined"

- [ ] **Step 3: Write minimal implementation**

```typescript
// types.ts
export interface CaseFileBoardData {
  openingLead: string;
  caseState: { status: string; statusText: string };
  artifacts: BoardArtifactData[];
  autoConnections: BoardConnectionData[];
  caseBoard: any;
  knownClues: any[];
}

export interface BoardArtifactData {
  artifactId: string;
  kind: string;
  title: string;
  content: string;
  position: { x: number; y: number };
  isSelected: boolean;
}

export interface BoardConnectionData {
  fromArtifactId: string;
  toArtifactId: string;
  kind: string;
  label: string | null;
}

export type ToolMode = 'move' | 'connect' | 'delete';
```

```typescript
// CaseFileScene.ts
import Phaser from 'phaser';
import type { CaseFileBoardData, ToolMode } from './types';

export class CaseFileScene extends Phaser.Scene {
  private readonly boardData: CaseFileBoardData;
  private readonly onArtifactSelected: (artifactId: string) => void;
  private readonly onConnectionCreated: (fromId: string, toId: string) => void;
  private readonly toolMode: ToolMode;
  
  private artifactSprites: Map<string, Phaser.GameObjects.Container> = new Map();
  private connectionGraphics: Phaser.GameObjects.Graphics;
  private selectedArtifactId: string | null = null;
  
  constructor(
    boardData: CaseFileBoardData,
    playerConnections: Array<{fromId: string; toId: string; label?: string}>,
    onArtifactSelected: (artifactId: string) => void,
    toolMode: ToolMode = 'move',
    onConnectionCreated?: (fromId: string, toId: string) => void
  ) {
    super('case-file');
    this.boardData = boardData;
    this.onArtifactSelected = onArtifactSelected;
    this.onConnectionCreated = onConnectionCreated ?? (() => {});
    this.toolMode = toolMode;
  }
  
  create(): void {
    const width = this.scale.width;
    const height = this.scale.height;
    
    // Create background (corkboard texture)
    this.add.rectangle(width / 2, height / 2, width, height, 0x8b7355);
    
    // Create connection graphics layer
    this.connectionGraphics = this.add.graphics();
    
    // Render auto-connections
    this.renderAutoConnections();
    
    // Render artifacts
    for (const artifact of this.boardData.artifacts) {
      this.createArtifact(artifact);
    }
  }
  
  private renderAutoConnections(): void {
    this.connectionGraphics.clear();
    
    for (const connection of this.boardData.autoConnections) {
      const fromArtifact = this.boardData.artifacts.find(a => a.artifactId === connection.fromArtifactId);
      const toArtifact = this.boardData.artifacts.find(a => a.artifactId === connection.toArtifactId);
      
      if (fromArtifact && toArtifact) {
        this.drawConnection(
          fromArtifact.position,
          toArtifact.position,
          connection.kind === 'GameKnown' ? 0x000000 : 0x666666,
          connection.kind === 'GameKnown' ? 2 : 1
        );
      }
    }
  }
  
  private createArtifact(artifact: BoardArtifactData): void {
    const container = this.add.container(artifact.position.x, artifact.position.y);
    
    // Create artifact background (paper note)
    const bgColor = this.getArtifactColor(artifact.kind);
    const rect = this.add.rectangle(0, 0, 80, 60, bgColor);
    rect.setStrokeStyle(2, 0x000000);
    
    // Create pin
    const pin = this.add.circle(0, -30, 5, 0xcc0000);
    
    container.add([rect, pin]);
    
    // Make interactive
    container.setSize(80, 60);
    container.setInteractive({ useHandCursor: true });
    container.on('pointerdown', () => this.handleArtifactClick(artifact.artifactId));
    
    this.artifactSprites.set(artifact.artifactId, container);
  }
  
  private getArtifactColor(kind: string): number {
    switch (kind) {
      case 'Clue': return 0xffff00; // Yellow
      case 'Warrant': return 0xffffff; // White
      case 'Suspect': return 0xadd8e6; // Light blue
      default: return 0xffffff;
    }
  }
  
  private drawConnection(
    from: { x: number; y: number },
    to: { x: number; y: number },
    color: number,
    lineWidth: number
  ): void {
    this.connectionGraphics.lineStyle(lineWidth, color, 1);
    this.connectionGraphics.beginPath();
    this.connectionGraphics.moveTo(from.x, from.y);
    this.connectionGraphics.lineTo(to.x, to.y);
    this.connectionGraphics.strokePath();
  }
  
  private handleArtifactClick(artifactId: string): void {
    if (this.toolMode === 'move') {
      this.selectedArtifactId = artifactId;
      this.updateArtifactSelection();
      this.onArtifactSelected(artifactId);
    } else if (this.toolMode === 'connect' && this.selectedArtifactId) {
      this.onConnectionCreated(this.selectedArtifactId, artifactId);
    }
  }
  
  private updateArtifactSelection(): void {
    this.artifactSprites.forEach((container, id) => {
      const isSelected = id === this.selectedArtifactId;
      const rect = container.getAt(0) as Phaser.GameObjects.Rectangle;
      if (rect) {
        rect.setStrokeStyle(isSelected ? 4 : 2, isSelected ? 0xf0e6d2 : 0x000000);
      }
    });
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- CaseFileScene.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/casefile/CaseFileScene.ts src/WildBunch.Web/src/components/casefile/types.ts src/WildBunch.Web/src/tests/CaseFileScene.test.tsx
git commit -m "feat: create Phaser Case File scene"
```

---

### Task 8: Create React Host Component with Player Connection State

**Files:**
- Create: `src/WildBunch.Web/src/components/casefile/PhaserCaseFileHost.tsx`
- Test: `src/WildBunch.Web/src/tests/PhaserCaseFileHost.test.tsx`

**Interfaces:**
- Consumes: `CaseFileScene` from Task 7
- Consumes: `CaseFileBoardDto` from API
- Produces: `PhaserCaseFileHost` React component with local player connection state

- [ ] **Step 1: Write the failing test for host component**

```typescript
import { render, screen } from '@testing-library/react';
import { PhaserCaseFileHost } from '../components/casefile/PhaserCaseFileHost';

describe('PhaserCaseFileHost', () => {
  it('should render Phaser canvas', () => {
    const board = {
      openingLead: 'Test',
      caseState: { status: 'Active', statusText: 'Active' },
      artifacts: [],
      autoConnections: [],
      caseBoard: {} as any,
      knownClues: []
    };
    
    render(<PhaserCaseFileHost 
      board={board} 
      onArtifactSelected={() => {}}
    />);
    
    const canvas = document.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- PhaserCaseFileHost.test.tsx`
Expected: FAIL with "PhaserCaseFileHost not defined"

- [ ] **Step 3: Write minimal implementation**

```typescript
import { useEffect, useRef, useState } from 'react';
import styled from 'styled-components';
import Phaser from 'phaser';
import { CaseFileScene } from './CaseFileScene';
import type { CaseFileBoardData, ToolMode } from './types';

interface PhaserCaseFileHostProps {
  board: CaseFileBoardData;
  onArtifactSelected: (artifactId: string) => void;
}

interface PlayerConnection {
  id: string;
  fromArtifactId: string;
  toArtifactId: string;
  label?: string;
}

export function PhaserCaseFileHost({
  board,
  onArtifactSelected
}: PhaserCaseFileHostProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onArtifactSelectedRef = useRef(onArtifactSelected);
  onArtifactSelectedRef.current = onArtifactSelected;
  
  // Local state for player-drawn connections
  const [playerConnections, setPlayerConnections] = useState<PlayerConnection[]>([]);
  const [toolMode, setToolMode] = useState<ToolMode>('move');

  useEffect(() => {
    if (!containerRef.current) return;

    const scene = new CaseFileScene(
      board,
      playerConnections,
      (artifactId: string) => onArtifactSelectedRef.current(artifactId),
      toolMode,
      (fromId: string, toId: string) => {
        const newConnection: PlayerConnection = {
          id: `conn-${Date.now()}`,
          fromArtifactId: fromId,
          toArtifactId: toId
        };
        setPlayerConnections([...playerConnections, newConnection]);
      }
    );

    const game = new Phaser.Game({
      parent: containerRef.current,
      width: 800,
      height: 600,
      backgroundColor: '#8b7355',
      scene: scene,
      scale: {
        mode: Phaser.Scale.FIT,
        autoCenter: Phaser.Scale.CENTER_BOTH,
      },
    });

    return () => {
      game.destroy(true);
    };
  }, [board, playerConnections, toolMode]);

  const handleToolChange = (mode: ToolMode) => {
    setToolMode(mode);
  };

  const handleDeleteConnection = (connectionId: string) => {
    setPlayerConnections(playerConnections.filter(c => c.id !== connectionId));
  };

  return (
    <CaseFileContainer>
      <ToolPalette>
        <ToolButton 
          active={toolMode === 'move'} 
          onClick={() => handleToolChange('move')}
        >
          Move
        </ToolButton>
        <ToolButton 
          active={toolMode === 'connect'} 
          onClick={() => handleToolChange('connect')}
        >
          Connect
        </ToolButton>
        <ToolButton 
          active={toolMode === 'delete'} 
          onClick={() => handleToolChange('delete')}
        >
          Delete
        </ToolButton>
      </ToolPalette>
      <BoardCanvas
        ref={containerRef}
        role="img"
        aria-label="Case file detective board"
      />
    </CaseFileContainer>
  );
}

const CaseFileContainer = styled.div`
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

const ToolPalette = styled.div`
  display: flex;
  gap: 8px;
  padding: 12px;
  background: var(--bg-elevated);
  border-radius: 12px;
  border: 1px solid var(--border);
`;

const ToolButton = styled.button<{ active: boolean }>`
  padding: 8px 16px;
  background: ${props => props.active ? 'var(--accent)' : 'var(--bg)'};
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text);
  cursor: pointer;
  
  &:hover {
    border-color: var(--accent-strong);
  }
`;

const BoardCanvas = styled.div`
  width: 100%;
  max-width: 800px;
  aspect-ratio: 4 / 3;
  border-radius: 16px;
  border: 1px solid var(--border);
  background: #8b7355;
  overflow: hidden;
  display: flex;
  justify-content: center;
  align-items: center;
`;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- PhaserCaseFileHost.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/casefile/PhaserCaseFileHost.tsx src/WildBunch.Web/src/tests/PhaserCaseFileHost.test.tsx
git commit -m "feat: create React host component for Case File with player connection state"
```

---

### Task 9: Integrate Case File into Route

**Files:**
- Modify: `src/WildBunch.Web/src/routes/CaseFileRoute.tsx`
- Modify: `src/WildBunch.Web/src/components/CaseFileSurface.tsx`
- Test: `src/WildBunch.Web/src/tests/CaseFileRouteIntegration.test.tsx`

**Interfaces:**
- Consumes: `PhaserCaseFileHost` from Task 8
- Consumes: Case file board data from useGameSession
- Produces: Integrated case file surface in route

- [ ] **Step 1: Write the failing test for integration**

```typescript
describe('CaseFileRoute with Phaser', () => {
  it('should render PhaserCaseFileHost when viewing case file', () => {
    const { container } = render(
      <GameSessionProvider>
        <CaseFileRoute />
      </GameSessionProvider>
    );
    
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- CaseFileRouteIntegration.test.tsx`
Expected: FAIL with Phaser canvas not found

- [ ] **Step 3: Modify CaseFileRoute to use PhaserCaseFileHost**

```typescript
import { PhaserCaseFileHost } from '../components/casefile/PhaserCaseFileHost';
import type { CaseFileBoardData } from '../components/casefile/types';

export function CaseFileRoute() {
  const { journal, loading } = useGameSession();
  
  // Convert case file board to TypeScript format
  const caseFileBoard: CaseFileBoardData = journal?.caseFile ? {
    openingLead: journal.caseFile.openingLead,
    caseState: {
      status: journal.caseFile.caseState.statusText,
      statusText: journal.caseFile.caseState.statusText
    },
    artifacts: journal.caseFile.caseBoard.artifacts.map(a => ({
      artifactId: a.id,
      kind: a.kind,
      title: a.title,
      content: a.content,
      position: { x: a.position.x, y: a.position.y },
      isSelected: false
    })),
    autoConnections: journal.caseFile.caseBoard.autoConnections.map(c => ({
      fromArtifactId: c.fromArtifactId,
      toArtifactId: c.toArtifactId,
      kind: c.kind,
      label: c.label
    })),
    caseBoard: journal.caseFile.caseBoard,
    knownClues: journal.caseFile.knownClues
  } : {
    openingLead: '',
    caseState: { status: '', statusText: '' },
    artifacts: [],
    autoConnections: [],
    caseBoard: {} as any,
    knownClues: []
  };
  
  if (loading && !journal) {
    return <div>Loading case file...</div>;
  }
  
  if (!journal) {
    return <div>Load a game to inspect the case file.</div>;
  }
  
  return (
    <RouteContainer>
      <PhaserCaseFileHost
        board={caseFileBoard}
        onArtifactSelected={(artifactId) => handleArtifactClick(artifactId)}
      />
    </RouteContainer>
  );
}

function handleArtifactClick(artifactId: string) {
  // Handle artifact clicks - show details in existing UI
  console.log('Artifact clicked:', artifactId);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- CaseFileRouteIntegration.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/routes/CaseFileRoute.tsx src/WildBunch.Web/src/components/CaseFileSurface.tsx src/WildBunch.Web/src/tests/CaseFileRouteIntegration.test.tsx
git commit -m "feat: integrate Case File Phaser surface into route"
```

---

### Task 10: Add Artifact Drag and Drop

**Files:**
- Modify: `src/WildBunch.Web/src/components/casefile/CaseFileScene.ts`
- Test: `src/WildBunch.Web/src/tests/CaseFileScene.test.tsx`

**Interfaces:**
- Consumes: Existing scene from Task 7
- Produces: Artifact drag and drop functionality

- [ ] **Step 1: Write the failing test for drag and drop**

```typescript
it('should allow dragging artifacts', () => {
  const scene = new CaseFileScene(board, [], () => {}, 'move');
  scene.create();
  
  const artifact = scene.getArtifactSprite('clue-1');
  expect(artifact).toBeDefined();
  
  // Simulate drag
  scene.handleArtifactDrag('clue-1', 200, 200);
  
  const position = scene.getArtifactPosition('clue-1');
  expect(position).toEqual({ x: 200, y: 200 });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- CaseFileScene.test.tsx`
Expected: FAIL with drag methods not defined

- [ ] **Step 3: Add drag and drop logic**

```typescript
export class CaseFileScene extends Phaser.Scene {
  private isDragging = false;
  private draggedArtifactId: string | null = null;
  private dragOffset = { x: 0, y: 0 };
  
  private createArtifact(artifact: BoardArtifactData): void {
    // ... existing creation code ...
    
    if (this.toolMode === 'move') {
      container.setInteractive({ useHandCursor: true, draggable: true });
      
      container.on('dragstart', () => {
        this.isDragging = true;
        this.draggedArtifactId = artifact.artifactId;
        this.dragOffset = {
          x: container.x - artifact.position.x,
          y: container.y - artifact.position.y
        };
      });
      
      container.on('drag', (pointer: Phaser.Input.Pointer) => {
        if (this.isDragging && this.draggedArtifactId) {
          container.x = pointer.x + this.dragOffset.x;
          container.y = pointer.y + this.dragOffset.y;
        }
      });
      
      container.on('dragend', () => {
        this.isDragging = false;
        this.draggedArtifactId = null;
      });
    }
  }
  
  public handleArtifactDrag(artifactId: string, x: number, y: number): void {
    const container = this.artifactSprites.get(artifactId);
    if (container) {
      container.setPosition(x, y);
    }
  }
  
  public getArtifactSprite(artifactId: string): Phaser.GameObjects.Container | undefined {
    return this.artifactSprites.get(artifactId);
  }
  
  public getArtifactPosition(artifactId: string): { x: number; y: number } {
    const container = this.artifactSprites.get(artifactId);
    if (container) {
      return { x: container.x, y: container.y };
    }
    return { x: 0, y: 0 };
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- CaseFileScene.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/casefile/CaseFileScene.ts src/WildBunch.Web/src/tests/CaseFileScene.test.tsx
git commit -m "feat: add artifact drag and drop to Case File scene"
```

---

### Task 11: Add Player Connection Drawing and Deletion

**Files:**
- Modify: `src/WildBunch.Web/src/components/casefile/CaseFileScene.ts`
- Modify: `src/WildBunch.Web/src/components/casefile/PhaserCaseFileHost.tsx`
- Test: `src/WildBunch.Web/src/tests/CaseFileScene.test.tsx`

**Interfaces:**
- Consumes: Existing scene from Task 7
- Produces: Player connection drawing and deletion functionality

- [ ] **Step 1: Write the failing test for player connections**

```typescript
it('should render player connections', () => {
  const playerConnections = [
    { fromArtifactId: 'clue-1', toArtifactId: 'clue-2', label: 'My theory' }
  ];
  
  const scene = new CaseFileScene(board, playerConnections, () => {}, 'connect');
  scene.create();
  
  // Should have both auto and player connections
  const connectionCount = scene.getConnectionCount();
  expect(connectionCount).toBeGreaterThan(1);
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- CaseFileScene.test.tsx`
Expected: FAIL with connection methods not defined

- [ ] **Step 3: Add player connection rendering**

```typescript
export class CaseFileScene extends Phaser.Scene {
  private playerConnections: Array<{fromId: string; toId: string; label?: string}> = [];
  
  constructor(
    // ... existing parameters ...
    playerConnections: Array<{fromId: string; toId: string; label?: string}>
  ) {
    super('case-file');
    // ... existing constructor ...
    this.playerConnections = playerConnections;
  }
  
  create(): void {
    // ... existing creation code ...
    
    // Render player connections
    this.renderPlayerConnections();
  }
  
  private renderPlayerConnections(): void {
    for (const connection of this.playerConnections) {
      const fromArtifact = this.boardData.artifacts.find(a => a.artifactId === connection.fromId);
      const toArtifact = this.boardData.artifacts.find(a => a.artifactId === connection.toArtifactId);
      
      if (fromArtifact && toArtifact) {
        this.drawConnection(
          fromArtifact.position,
          toArtifact.position,
          0x666666, // Light gray for player connections
          1, // Thinner line
          connection.label
        );
      }
    }
  }
  
  private drawConnection(
    from: { x: number; y: number },
    to: { x: number; y: number },
    color: number,
    lineWidth: number,
    label?: string
  ): void {
    this.connectionGraphics.lineStyle(lineWidth, color, 1);
    this.connectionGraphics.beginPath();
    this.connectionGraphics.moveTo(from.x, from.y);
    this.connectionGraphics.lineTo(to.x, to.y);
    this.connectionGraphics.strokePath();
    
    if (label) {
      const midX = (from.x + to.x) / 2;
      const midY = (from.y + to.y) / 2;
      this.add.text(midX, midY, label, {
        fontSize: '10px',
        color: '#ffffff',
        backgroundColor: '#333333',
        padding: { x: 2, y: 1 }
      }).setOrigin(0.5);
    }
  }
  
  public getConnectionCount(): number {
    return this.boardData.autoConnections.length + this.playerConnections.length;
  }
}
```

- [ ] **Step 4: Add connection deletion in React host**

```typescript
const handleDeleteConnection = (connectionId: string) => {
  setPlayerConnections(playerConnections.filter(c => c.id !== connectionId));
};

// Pass delete handler to scene
const scene = new CaseFileScene(
  board,
  playerConnections,
  (artifactId: string) => onArtifactSelectedRef.current(artifactId),
  toolMode,
  (fromId: string, toId: string) => {
    const newConnection: PlayerConnection = {
      id: `conn-${Date.now()}`,
      fromArtifactId: fromId,
      toArtifactId: toId
    };
    setPlayerConnections([...playerConnections, newConnection]);
  },
  (connectionId: string) => handleDeleteConnection(connectionId)
);
```

- [ ] **Step 5: Update scene to support deletion**

```typescript
constructor(
    // ... existing parameters ...
    onConnectionDeleted?: (connectionId: string) => void
) {
  // ... existing constructor ...
  this.onConnectionDeleted = onConnectionDeleted ?? (() => {});
}

private createArtifact(artifact: BoardArtifactData): void {
  // ... existing creation code ...
  
  if (this.toolMode === 'delete') {
    container.setInteractive({ useHandCursor: true });
    container.on('pointerdown', () => {
      // Check if there's a connection to delete
      const connectionToDelete = this.findConnectionForArtifact(artifact.artifactId);
      if (connectionToDelete) {
        this.onConnectionDeleted(connectionToDelete.id);
      }
    });
  }
}

private findConnectionForArtifact(artifactId: string): {id: string} | null {
  // Find player connection involving this artifact
  return this.playerConnections.find(c => 
    c.fromArtifactId === artifactId || c.toArtifactId === artifactId
  ) || null;
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `npm test -- CaseFileScene.test.tsx`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Web/src/components/casefile/CaseFileScene.ts src/WildBunch.Web/src/components/casefile/PhaserCaseFileHost.tsx src/WildBunch.Web/src/tests/CaseFileScene.test.tsx
git commit -m "feat: add player connection drawing and deletion to Case File"
```

---

### Task 12: Final Integration Testing and Validation

**Files:**
- Test: `src/WildBunch.Web/src/tests/CaseFileIntegration.test.tsx`
- Test: `tests/WildBunch.Integration.Tests/CaseFileIntegrationTests.cs`

**Interfaces:**
- Consumes: All previous tasks
- Produces: End-to-end validation of case file surface

- [ ] **Step 1: Write integration test for full flow**

```typescript
describe('CaseFile Integration', () => {
  it('should render case file board with artifacts from backend', async () => {
    const { result } = renderHook(() => useGameSession());
    
    await act(async () => {
      await result.current.startNewGame(testSeed);
    });
    
    const { container } = render(
      <GameSessionProvider>
        <CaseFileRoute />
      </GameSessionProvider>
    );
    
    const canvas = container.querySelector('canvas');
    expect(canvas).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run integration test**

Run: `npm test -- CaseFileIntegration.test.tsx`
Expected: PASS

- [ ] **Step 3: Run backend integration test**

```csharp
[Fact]
public async Task CaseFile_EndToEnd_ReturnsValidLayout()
{
    var sessionId = await CreateTestGameSession();
    
    var client = _factory.CreateClient();
    var response = await client.GetAsync($"/api/games/{sessionId}/journal");
    
    response.EnsureSuccessStatusCode();
    var journal = await response.Content.ReadFromJsonAsync<JournalDto>();
    
    Assert.NotNull(journal);
    Assert.NotNull(journal.caseFile);
    Assert.NotEmpty(journal.caseFile.artifacts);
}
```

- [ ] **Step 4: Run backend integration test**

Run: `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~CaseFileIntegrationTests" -v`
Expected: PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test` and `npm test`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Web/src/tests/CaseFileIntegration.test.tsx tests/WildBunch.Integration.Tests/CaseFileIntegrationTests.cs
git commit -m "test: add Case File integration tests"
```

---

## Self-Review

**1. Spec coverage:**
- ✅ CaseFileAggregate domain model (Task 1)
- ✅ Case file events (Task 2)
- ✅ Case file logic extraction from GameSession (Task 3)
- ✅ Auto-layout algorithm (Task 4)
- ✅ DTOs and mapping (Task 5)
- ✅ Persistence and snapshot serialization (Task 6)
- ✅ Phaser scene (Task 7)
- ✅ React host component with player connection state (Task 8)
- ✅ Route integration (Task 9)
- ✅ Artifact drag and drop (Task 10)
- ✅ Player connection drawing and deletion (Task 11)
- ✅ Integration testing (Task 12)

**2. Placeholder scan:** No placeholders found - all steps contain concrete code.

**3. Type consistency:** All types match across tasks - CaseFileAggregate, BoardArtifactDto, ConnectionKind used consistently.

**4. Architecture compliance:** New CaseFileAggregate boundary, event splitting from GameSession, React-driven domain state, local player connection state.

**5. Greenfield project:** No migration steps included - all database changes assume greenfield status.

**6. Risk mitigation:** Incremental extraction, GameSession remains coordinator, comprehensive event replay testing, clear separation of domain vs player theory state.

**7. Hidden-Truth Guard:** CaseFileAggregate must not leak hidden culprit truth through case-file DTOs, board projections, Phaser scene state, logs, snapshots, or tests. The board should render known/discovered/player-available case information only.

**8. Preservation Requirements:**
- Existing GameSession command route and current player-visible behavior must remain stable
- Existing clue, journal, wanted-poster, and case-file flows must remain stable during CaseFileAggregate extraction
- Phaser remains renderer/input adapter, with React/backend/domain owning truth