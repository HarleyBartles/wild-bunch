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

**Approach:** Compact read-only display of salts + version with "Copy Bundle" and "Freeze" buttons.

**Implementation:**
- Compact mode: Single block showing:
  - Resolver version (e.g., "1.0.0")
  - Buildings salt (hex string, truncated if long)
  - Roads salt (hex string, truncated if long)
  - Dirt salt (hex string, truncated if long)
  - Props salt (hex string, truncated if long)
- Buttons:
  - "Copy Bundle" - copies all salts + version as JSON to clipboard
  - "Freeze" - locks current salts by setting entropy policy to Fixed mode with current salt values
- Expanded mode: Shows full salt values and additional layout inspection (tile grid preview, building list)
- Salt values are opaque hex strings — no need for expandable sections per salt

**Rationale:** Satisfies issue requirement for "inspect, copy, and freeze", salts are opaque so detailed inspection adds little value, compact display fits doctrine, Freeze button is primary testing tool for determinism verification.

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
- "Copy Bundle" serializes salts + version as JSON
- "Freeze" sets entropy policy to Fixed mode with current salt values
- Revisiting town with frozen salts produces identical layout

**Rationale:** Clear separation of concerns, deterministic by construction, frontend is pure renderer, dev overlay can inspect and control the generation inputs.

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
