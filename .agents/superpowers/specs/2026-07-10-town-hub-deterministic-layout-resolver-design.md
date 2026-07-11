# Town Hub Deterministic Layout Resolver and Salt Controls - Design

## Overview

Build a versioned deterministic town-hub layout resolver that turns `seed + layout salts + resolver version` into a stable town layout. The invariant: same town revisited on the same playthrough must look identical every time. Tiles do not shift. Sprites do not shift.

## Architecture

Layout resolution flows from seed + entropy → derived salts → versioned resolver → resolved layout → frontend rendering. The frontend is a pure renderer of the resolved layout, with no layout decision logic.

## Tech Stack

- C#/.NET backend with existing seed system
- Phaser/TypeScript frontend
- Existing dev overlay panel system

## Design Decisions

### 1. Resolver Versioning

**Approach:** Add `ResolverVersion` field to `TownLayout` domain model as a semantic version string (e.g., "1.0.0").

**Implementation:**
- `TownLayout` record gains: `string ResolverVersion`
- `TownLayoutDto` gains: `string resolverVersion`
- `TownLayoutGenerator.GenerateLayout()` signature adds: `string resolverVersion = "1.0.0"`
- Version is passed through the generation pipeline and stored in the layout
- Migration: When resolver version changes, detect at layout consumption time and apply migrations if needed

**Rationale:** Makes layouts self-describing, supports dev overlay inspection, aligns with seed system pattern, enables testability of resolver contract.

### 2. Split Salt Structure

**Approach:** Create a `LayoutSalts` record with 4 named string fields for the layout concerns.

**Implementation:**
- New domain record: `LayoutSalts(string BuildingsSalt, string RoadsSalt, string DirtSalt, string PropsSalt)`
- Replaces single `SaltSource` in layout generation context
- `TownLayoutGenerator.GenerateLayout()` signature changes from `SaltSource? saltSource` to `LayoutSalts? layoutSalts`
- Salts are derived from seed + entropy policy, not stored directly
- Dev overlay can serialize/deserialize this record easily for copy/freeze operations

**Rationale:** Bounded scope for 4 fixed concerns, simple dev overlay integration, type safety, testability, YAGNI (no dictionary complexity needed), aligns with existing record patterns.

### 3. Dev Overlay Panel Ownership

**Approach:** Create a new "Town Layout" dev panel that owns layout salts, resolver version, and layout inspection.

**Implementation:**
- New dev panel: `TownLayoutDevPanel` with ID `"town-layout"`
- Panel owns: layout salts bundle, resolver version, current layout inspection
- Panel visible when: current surface is town hub (TownHubScene)
- Default panel selection: Town hub surface → Town Layout dev panel
- Panel follows dev-overlay doctrine for panel ownership (deep controls for the owned noun)
- Related panels: Session Audit (always available), Session dev (for entropy policy interaction)

**Rationale:** Town layout is a distinct domain node with its own concerns, surface-specific ownership follows doctrine pattern, scope and complexity warrant dedicated panel, future growth room, aligns with surface → panel mapping.

### 4. Dev Overlay UI Shape

**Approach:** Setup-time salt control with "Copy Bundle" and "Set Salts" buttons for controlling world generation.

**Implementation:**
- Compact mode: Single block showing:
  - Resolver version (e.g., "1.0.0")
  - Buildings salt (hex string, editable text field)
  - Roads salt (hex string, editable text field)
  - Dirt salt (hex string, editable text field)
  - Props salt (hex string, editable text field)
- Buttons:
  - "Copy Bundle" - copies all salts + version as JSON to clipboard for saving
  - "Set Salts" - applies the entered salt values to the entropy policy for the next world generation
  - "Generate Random" - fills all 4 salt fields with new random values for exploration
- Expanded mode: Shows full salt values and additional layout inspection (tile grid preview, building list) after generation
- Salt values are opaque hex strings — editable text fields allow dev to paste saved values
- Salts are set at setup time before world generation, not changed mid-game

**Rationale:** Satisfies issue requirement for "inspect, copy, and freeze" (interpreted as set/save), salts are opaque so detailed inspection adds little value, compact display fits doctrine, setup-time salt control allows devs to reproduce specific world layouts by setting salts before generation.

### 5. Salt Storage and Derivation

**Approach:** Layout salts are derived per-town-per-playthrough from seed + entropy policy, not stored.

**Implementation:**
- Salts are not stored in SeedWorld (seed-owned) or GameSession (session-owned)
- New derivation function: `LayoutSalts DeriveLayoutSalts(SeedWorld seedWorld, EntropyPolicy entropyPolicy, TownId townId, int townSlotIndex)`
- Function uses seed + entropy to deterministically generate the 4 salt values
- Same seed + same entropy policy = same derived salts = same layout
- Dev overlay Freeze works by setting entropy policy to Fixed mode with current salt values
- Layout resolution happens on-demand when entering town hub, no storage overhead

**Rationale:** Aligns with existing seed/entropy separation, preserves seed semantics, enables playthrough variability, deterministic by construction, storage-efficient, fits existing SaltSource(Fixed, salt) pattern.

### 6. Data Flow and Architecture

**Generation path:**
1. `SeedWorld` (seed-owned world structure)
2. `EntropyPolicy` (entropy-owned salt mode)
3. `DeriveLayoutSalts(seedWorld, entropyPolicy, townId, townSlotIndex)` → `LayoutSalts`
4. `TownLayoutGenerator.GenerateLayout(services, prosperity, townId, townSlotIndex, source, layoutSalts, resolverVersion)` → `TownLayout`
5. `TownLayout` includes `ResolverVersion` field
6. `TownLayoutDto` maps domain for frontend
7. `TownHubScene` renders resolved layout directly (no re-decision)

**Frontend consumption:**
- Phaser receives `TownLayoutDto` with buildings, tile grid, paths, resolver version
- `TownHubScene` renders tile grid first, then buildings with sprites
- No layout logic in frontend — pure rendering of resolved layout

**Dev overlay flow:**
- Town Layout dev panel shows current `LayoutSalts` + `ResolverVersion`
- "Copy Bundle" serializes salts + version as JSON for saving
- "Set Salts" applies entered salt values to entropy policy before world generation
- "Generate Random" fills salt fields with new random values for exploration
- Setting salts at setup time produces reproducible world layouts

**Rationale:** Clear separation of concerns, deterministic by construction, frontend is pure renderer, dev overlay can inspect and control the generation inputs.

## Implementation Integration

### Derivation Function Placement and Logic

**Placement:**
- New file: `src/WildBunch.GameContent/NewGame/LayoutSaltDeriver.cs`
- Namespace: `WildBunch.GameContent.NewGame`
- Function: `public static LayoutSalts DeriveLayoutSalts(SeedWorld seedWorld, EntropyPolicy entropyPolicy, TownId townId, int townSlotIndex)`

**Derivation Algorithm:**
- Use the existing `GameSetupDeterministicSource` from the seed system as the RNG source
- Derive each salt by combining seed bytes + town-specific context + entropy policy:
  - Buildings salt: `Hash(seedBytes + townId.Value + "buildings" + entropyPolicy.SaltMode)`
  - Roads salt: `Hash(seedBytes + townId.Value + "roads" + entropyPolicy.SaltMode)`
  - Dirt salt: `Hash(seedBytes + townId.Value + "dirt" + entropyPolicy.SaltMode)`
  - Props salt: `Hash(seedBytes + townId.Value + "props" + entropyPolicy.SaltMode)`
- Hash function: Use SHA-256 and convert to hex string (32 characters)
- When entropy policy is Fixed mode, use the fixed salt value directly instead of deriving
- Determinism guarantee: Same seed + same entropy policy + same townId + same townSlotIndex = same 4 salt values

**Integration Point:**
- Called in the application layer when generating town layout for a playthrough
- Integration with existing town entry flow in the application layer (not in GameSession directly)
- The application layer orchestrates: SeedWorld → EntropyPolicy → LayoutSaltDeriver → TownLayoutGenerator

### Resolver Version Migration Strategy

**Migration Approach:**
- No automatic migration system for v1.0.0 — layouts are regenerated on-demand
- When resolver version changes from X to Y:
  - Update the default resolverVersion parameter in `TownLayoutGenerator.GenerateLayout()`
  - Update any hardcoded version strings in tests
  - If a layout with version X is encountered at runtime, regenerate it with version Y
- Migration is handled by regeneration, not data transformation
- Version comparison: Use semantic version parsing to detect breaking changes (major version bump)

**Migration Triggers:**
- Town hub entry: Always regenerate layout with current resolver version
- Dev overlay freeze: Store the resolver version alongside frozen salts
- Test assertions: Include resolver version in snapshot tests

### Dev Overlay API Endpoints

**New Dev Endpoints:**
- `GET /api/dev/town-layout/salts` - Returns current layout salts for the active town
  - Response: `{ resolverVersion: string, buildingsSalt: string, roadsSalt: string, dirtSalt: string, propsSalt: string }`
- `POST /api/dev/town-layout/set-salts` - Sets layout salts for the next world generation
  - Request: `{ buildingsSalt: string, roadsSalt: string, dirtSalt: string, propsSalt: string }`
  - Response: Success/failure confirmation
- `POST /api/dev/town-layout/generate-random` - Generates random salt values for exploration
  - Response: `{ buildingsSalt: string, roadsSalt: string, dirtSalt: string, propsSalt: string }`
- `GET /api/dev/town-layout/layout` - Returns the current resolved layout for inspection
  - Response: `TownLayoutDto` with resolver version

**Implementation:**
- New controller: `src/WildBunch.Web/Controllers/DevTownLayoutController.cs`
- Endpoints are dev-only (require dev mode or authenticated dev user)
- Endpoints use the existing dev command infrastructure (ForceDevSaltSourceCommand pattern)
- Set-salts endpoint updates the entropy policy to Fixed mode with the provided salt values before world generation
- Generate-random endpoint creates new random salt values and returns them for the dev to review before setting

### Integration with Existing Town Entry Flow

**Current Flow:**
- Town entry happens through application layer commands
- Town layout is generated via `TownLayoutGenerator` and mapped to DTO
- DTO is sent to frontend for rendering

**New Flow:**
- Application layer calls `LayoutSaltDeriver.DeriveLayoutSalts()` before calling `TownLayoutGenerator`
- Derivation uses the current `SeedWorld` and `EntropyPolicy` from the game session
- Generated `LayoutSalts` are passed to `TownLayoutGenerator.GenerateLayout()`
- Resolver version is passed as a parameter (default "1.0.0")
- Resulting `TownLayout` includes `ResolverVersion` field
- DTO mapping includes resolver version
- Frontend receives and renders the versioned layout

**No Changes Required:**
- GameSession aggregate structure (layout is not stored in session)
- SeedWorld structure (layout salts are derived, not stored)
- EntropyPolicy structure (uses existing SaltSource pattern for freeze)

### Entropy Policy Extension

**No Extension Required:**
- Existing `EntropyPolicy` with `SaltSourceMode` (Runtime/Fixed) is sufficient
- Layout salt derivation uses the existing entropy policy salt mode
- When SaltSourceMode is Fixed, the fixed salt value is used for all 4 layout salts
- When SaltSourceMode is Runtime, salts are derived from seed + town context
- Dev overlay set-salts works by setting the entropy policy to Fixed mode with the provided salt values before world generation

**Set-Salts Implementation:**
- Dev overlay "Set Salts" button calls the set-salts endpoint with the entered salt values
- Backend creates a new `SaltSource` with Fixed mode and a combined salt value
- The entropy policy is updated to use this fixed salt source
- Subsequent world generation uses the fixed salt values, producing reproducible layouts
- This happens at setup time before world generation, not mid-game

## Guardrails

- Keep change narrow to town-hub layout resolution and dev controls
- Do not broaden into unrelated world generation or UI polish
- Do not change approved asset custody or regenerate assets
- Assume prop sprites exist via prop-sprite asset ticket before this lands
- Preserve existing seed semantics: same seed means same world structure and services
- Preserve playthrough semantics: different salts may change town look but not functional identity

## Success Criteria

- A town revisited on the same playthrough resolves to the same layout every time
- The layout contract is deterministic from `seed + salts + resolver version`
- The UI consumes resolved layout rather than inferring tile or sprite placement ad hoc
- The dev overlay exposes the grouped layout salts in a compact, inspectable form
- Tests lock seed and salts and pass repeatably
