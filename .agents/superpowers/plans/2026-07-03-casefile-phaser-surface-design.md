# Case File Phaser Surface Design

## Overview
Skeuomorphic detective board with paper artifacts (clues, warrants, suspects) and string connections. Game auto-draws known connections between related artifacts, and players can draw their own theory strings. The surface uses Phaser for rendering while React manages state and backend integration.

## Architecture

### React Host Component
**File**: `src/WildBunch.Web/src/components/casefile/PhaserCaseFileHost.tsx`

Follows the existing `PhaserMapHost` pattern:
- Manages Phaser game instance lifecycle (creation, updates, destruction)
- Receives case file data from backend via DTOs
- Passes case file data to Phaser scene via constructor
- Handles interaction callbacks (artifact clicks, string drawing, tool selection)
- Manages player-drawn connection state locally (not sent to backend)
- Integrates with existing `useGameSession` hook for state management

### Phaser Scene
**File**: `src/WildBunch.Web/src/components/casefile/CaseFileScene.ts`

Extends `Phaser.Scene`:
- Renders skeuomorphic detective board with paper artifacts
- Manages artifact positioning and rendering
- Handles auto-drawn game-known connections
- Handles player-drawn theory connections
- Manages tool palette (move, connect, delete)
- Visual feedback for selected artifacts and connections

### Aggregate Architecture
**New Aggregate**: `CaseFileAggregate`

**File**: `src/WildBunch.Domain/Cases/CaseFileAggregate.cs`

Extract existing `CaseFile` domain object into a proper aggregate:

```csharp
public sealed class CaseFileAggregate
{
    public CaseFileId Id { get; }
    public GameSessionId GameSessionId { get; }
    
    // Case state
    public CaseState State { get; }
    public string OpeningLead { get; }
    public string CaseSummary { get; }
    
    // Suspects
    public IReadOnlyList<Suspect> Suspects { get; }
    public IReadOnlyList<SuspectId> DiscoveredSuspectIds { get; }
    public SuspectId? Accusation { get; }
    public SuspectId TrueCulpritId { get; }
    
    // Clues
    public IReadOnlyList<Clue> KnownClues { get; }
    public IReadOnlyList<Clue> PublicClues { get; }
    
    // Warrants
    public IReadOnlyList<Warrant> KnownWarrants { get; }
    public IReadOnlyList<Warrant> PublicWarrants { get; }
    
    // Confrontations and settlements
    public IReadOnlyList<WantedSuspectConfrontationState> WantedSuspectConfrontations { get; }
    public IReadOnlyList<SheriffTurnInSettlementState> SheriffTurnInSettlements { get; }
    
    // Turf assignments
    public IReadOnlyList<SuspectTurfAssignment> SuspectTurfAssignments { get; }
    
    // Killer release progress
    public int KillerReleaseThreshold { get; }
    public int KillerReleaseProgress { get; }
    
    // Command methods
    public CaseFileResult AddClue(AddClueContext context);
    public CaseFileResult AddWarrant(AddWarrantContext context);
    public CaseFileResult DiscoverSuspect(DiscoverSuspectContext context);
    public CaseFileResult RecordConfrontation(ConfrontationContext context);
    public CaseFileResult RecordSheriffTurnIn(SheriffTurnInContext context);
    public CaseFileResult UpdateAccusation(AccusationContext context);
}
```

### Data Source
**Backend Extension**: Extend existing case file DTOs for Phaser rendering:

```csharp
// Extended DTO for Phaser rendering
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
    GameKnown,    // Auto-drawn by game
    PlayerTheory  // Drawn by player
}
```

## Scene Layout

### Visual Style
- **Perspective**: Direct view of detective board (2D)
- **Aesthetic**: Skeuomorphic paper/ink style with corkboard texture
- **Background**: Corkboard or wooden board texture
- **Artifacts**: Paper note textures with different styles (clues = yellow notes, warrants = official documents, suspects = cards)
- **Connections**: String lines with pins at artifact edges
- **Tools**: Tool palette with move, connect, delete tools

### Artifact Rendering
- **Clues**: Yellow sticky note style, pinned to board
- **Warrants**: Official document style with seal, pinned to board
- **Suspects**: Card style with suspect name and status, pinned to board
- **Player Notes**: White note style, pinned to board (future)
- **Selected Artifact**: Highlighted with glow/border
- **Artifact Content**: Click to expand/show details

### Connection Rendering
- **Game-Known Connections**: Dark string, solid line, small pins
- **Player Theory Connections**: Light string, dashed line, larger pins
- **Connection Labels**: Small text labels on connections (optional)
- **Pin Rendering**: Small circle sprites at artifact edges
- **String Physics**: Slight curve for visual interest (catenary curve)

### Tool Palette
- **Move Tool**: Drag artifacts to reposition
- **Connect Tool**: Click two artifacts to draw connection
- **Delete Tool**: Click connection to remove (player connections only)
- **Tool Selection**: Visual feedback for active tool

## Data Flow

### Backend Data Generation
1. **Case File Events**: Case file state derived from event stream
2. **Auto-Layout**: Force-directed or grid-based layout algorithm positions artifacts
3. **Auto-Connections**: Domain relationships determine which artifacts to connect
4. **Consistency**: Same case file always produces same auto-layout

### React Integration
1. **Load Phase**: React fetches case file data via existing `/api/games/{sessionId}/journal` endpoint
2. **Render Phase**: React passes case file data to Phaser scene constructor
3. **Update Phase**: React re-renders Phaser scene when case file changes
4. **Interaction Phase**: Phaser scene calls React callbacks for artifact interactions
5. **Local State**: React manages player-drawn connections locally (not sent to backend)

### Backend Commands
1. **Artifact Click**: React shows artifact details in existing UI (no backend command)
2. **Player Connections**: Stored locally in React state (not sent to backend)
3. **Case File Changes**: Existing investigation commands update case file via `CaseFileAggregate`

## Domain Integration

### CaseFileAggregate Events
**Extract Existing Events**:
```csharp
// Move from GameSession to CaseFileAggregate
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

public sealed record SuspectConfrontationRecorded : IDomainEvent
{
    public required SuspectId SuspectId { get; init; }
    public required ConfrontationOutcome Outcome { get; init; }
}

public sealed record SheriffTurnInRecorded : IDomainEvent
{
    public required SuspectId SuspectId { get; init; }
    public required WarrantDisposition Disposition { get; init; }
    public required decimal BountyAmount { get; init; }
}
```

### Auto-Layout Algorithm
**File**: `src/WildBunch.Application/Projections/CaseFileLayoutProjection.cs`

```csharp
public static class CaseFileLayoutProjection
{
    public static CaseFileBoardDto ProjectLayout(CaseFileAggregate caseFile)
    {
        // Use force-directed layout algorithm
        // Clues and warrants are nodes
        // Domain relationships are edges
        // Suspects positioned based on turf assignments
        // Return positioned artifacts and auto-connections
    }
    
    private static IReadOnlyList<BoardConnectionDto> GenerateAutoConnections(
        CaseFileAggregate caseFile)
    {
        // Connect clues to related suspects
        // Connect warrants to suspects
        // Connect clues to other clues (temporal, spatial)
        // Return game-known connections
    }
}
```

### Player Connection State
**Local State Management**:
```typescript
// React component state
interface PlayerConnections {
  connections: Array<{
    id: string;
    fromArtifactId: string;
    toArtifactId: string;
    label?: string;
  }>;
}

// Stored in React state, not sent to backend
// Allows players to draw theories without validation
// Game doesn't reveal if connections are right/wrong
```

## Technical Decisions

### Auto-Layout Strategy
- **Force-Directed Layout**: Use physics-based layout algorithm for natural artifact positioning
- **Domain Relationships**: Use existing domain relationships to determine connections
- **Consistency**: Same case file always produces same auto-layout
- **Player Customization**: Players can drag artifacts to custom positions (stored locally)

### Asset Strategy (Phase 1)
- **Procedural Generation**: Use Phaser graphics primitives (rectangles, circles, text, lines)
- **Artifact Textures**: Simple colored rectangles with text rendering
- **Pins**: Small circle sprites at artifact edges
- **Strings**: Phaser graphics lines with slight curve
- **Background**: Solid color with simple texture pattern

### Asset Strategy (Phase 2 - Deferred)
- **Artist-Created Assets**: Detailed paper textures, corkboard texture, realistic pins
- **Artifact Styling**: Different paper textures for different artifact types
- **String Physics**: More realistic string rendering with tension
- **Environmental Details**: Shadows, lighting effects, board texture details

### State Management
- **React-Driven**: React manages all domain state, Phaser is pure renderer
- **Local Player State**: React manages player-drawn connections locally
- **No Backend Sync**: Player connections never sent to backend (theory tracking only)
- **Reactivity**: React `useEffect` triggers Phaser scene updates when case file changes

### Performance Considerations
- **Single Instance**: One Phaser game instance per case file surface
- **Cleanup**: Proper destruction of Phaser instance when closing case file
- **Memory**: Reuse artifact textures, limit connection count
- **Rendering**: Simple 2D rendering, minimal performance impact

## API Changes

### Extended Endpoints
```csharp
// Existing endpoint, extended with layout data
GET /api/games/{sessionId}/journal
Response: JournalDto (extended with CaseFileBoardDto)

// No new endpoints needed for case file commands
// Existing investigation commands work with CaseFileAggregate
```

### DTO Changes
- Extend `JournalDto` to include `CaseFileBoardDto`
- Add `BoardArtifactDto` to shared types
- Add `BoardConnectionDto` to shared types
- Add `ArtifactKind` enum to shared types
- Add `ConnectionKind` enum to shared types

## Migration Strategy

### Phase 1: Data Model (Highest Complexity)
1. Extract `CaseFile` into `CaseFileAggregate`
2. Move case file events from GameSession to CaseFileAggregate
3. Update snapshot format to include case file as separate aggregate
4. Add case file repository and unit of work
5. **No Migration Needed**: Greenfield project, can drop and rebuild database

### Phase 2: Backend Refactoring (High Complexity)
1. Move case file logic from `GameSession` to `CaseFileAggregate`
2. Move investigation event production to CaseFileAggregate
3. Update `InvestigationLoop` to work with CaseFileAggregate
4. Update `BountyLoop` to work with CaseFileAggregate
5. Add case file layout projection
6. **No Migration Needed**: Greenfield project, can drop and rebuild database

### Phase 3: Frontend Integration (Medium Complexity)
1. Create `PhaserCaseFileHost` component
2. Create `CaseFileScene` Phaser scene
3. Integrate with existing case file route
4. Replace React case file panels with Phaser surface
5. Add local player connection state management
6. **No Migration Needed**: UI change, no data migration needed

### Phase 4: Layout Refinement (Low Complexity)
1. Refine auto-layout algorithm based on playtesting
2. Add player customization (drag artifacts, custom positions)
3. Add connection labeling
4. **No Migration Needed**: Algorithm improvements, no breaking changes

## Testing Strategy

### Domain Tests
- Test case file aggregate command methods
- Test case file event production and application
- Test auto-layout algorithm determinism
- Test domain relationship mapping to connections
- Test case file state transitions

### Integration Tests
- Test case file layout projection returns valid data
- Test case file aggregate persistence across session load/save
- Test React-Phaser data flow
- Test artifact interaction callbacks
- Test command handler integration with new aggregate

### Frontend Tests
- Test Phaser scene creation/destruction
- Test artifact click callbacks
- Test artifact drag and drop
- Test connection drawing (auto and player)
- Test tool palette interactions
- Test local player connection state management

### Phaser Tests
- Test scene rendering with different case file states
- Test auto-layout algorithm visual output
- Test interaction zones (artifact click detection)
- Test connection rendering (pins, strings, curves)
- Test cleanup/memory management
- Test player connection drawing and deletion

## Success Criteria

### Functional Requirements
- Players can view case file as detective board
- Game auto-draws connections between related artifacts
- Players can draw their own theory connections
- Players can drag artifacts to reposition
- Players can delete their own connections
- Game doesn't validate player connections (theory tracking)
- Auto-layout is deterministic and readable
- Integration with existing case file UI

### Non-Functional Requirements
- Performance: Scene renders in < 100ms
- Memory: No memory leaks on scene destruction
- Accessibility: Keyboard navigation support
- Responsiveness: Works on different screen sizes
- Maintainability: Clear separation of React/Phaser responsibilities

## Open Questions

1. **Auto-Layout Algorithm**: Force-directed vs grid-based vs hybrid?
2. **Connection Visibility**: Should player connections be visible to game (for analytics)?
3. **Artifact Limits**: Maximum number of artifacts before layout becomes unreadable?
4. **Player Customization**: Should player artifact positions persist across sessions?
5. **Connection Validation**: Should game provide subtle hints about connection quality?

## Dependencies

### Existing Code
- `PhaserMapHost` pattern for React-Phaser integration
- Existing `CaseFile` domain model (to be extracted)
- Existing investigation events (to be moved)
- `InvestigationLoop` for clue/warrant discovery
- `BountyLoop` for suspect confrontation
- `JournalDto` for case file data
- `useGameSession` hook for state management
- Existing case file route

### New Code Required
- `CaseFileAggregate` domain model (extracted from existing CaseFile)
- Case file-specific events (moved from GameSession)
- `CaseFileLayoutProjection` for auto-layout
- `PhaserCaseFileHost` React component
- `CaseFileScene` Phaser scene
- Case file aggregate repository
- Case file snapshot serialization
- Extended DTOs for layout data

## Risks and Mitigations

### Risk: Aggregate Extraction Complexity
**Mitigation**: Incremental extraction, keep GameSession as coordinator during transition, comprehensive event replay testing

### Risk: InvestigationLoop Coupling
**Mitigation**: Extract case file logic from InvestigationLoop, pass CaseFileAggregate as context, maintain clear boundaries

### Risk: Auto-Layout Algorithm Complexity
**Mitigation**: Start with simple grid-based layout, evolve to force-directed based on playtesting, test with maximum artifact count

### Risk: Player Connection State Management
**Mitigation**: Keep player connections purely local, clear separation from domain state, simple data structure

### Risk: Performance with Many Artifacts
**Mitigation**: Limit artifact count based on case complexity, use sprite pooling, test with maximum artifact count (50 artifacts)

### Risk: Asset Pipeline
**Mitigation**: Phase 1 procedural assets, defer artist assets until layout patterns are stable, reuse artifact textures

### Risk: UI Complexity
**Mitigation**: Start with simple tool palette, add advanced features based on playtesting, maintain clear interaction model
