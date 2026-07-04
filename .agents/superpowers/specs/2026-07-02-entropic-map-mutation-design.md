# Entropic Map Mutation Design

## Goals

1. **Interesting maps** - Variety and replayability through entropy-based variation
2. **Seeded maps** - Deterministic from seed (town count, layout palette), no seed bit expansion
3. **Entropic variation** - Maps change from base seed in interesting ways when entropy increases
4. **Entropy scaling** - Higher entropy = more drastic changes
5. **Playability** - All towns must remain reachable
6. **Outlier feature** - Sometimes have dead-end towns with one long trail as a play feature

## Current State Analysis

**Seed Encoding (29 bits used, 99 reserved):**
- World variant: 2 bits (3 variants: Canonical, Frontier, Rail)
- Town count: 4 bits (offset-encoded 5-20, wrapped to 5-10)
- Accusation index: 4 bits
- Default culprit index: 4 bits
- Cash bonus: 4 bits
- Prosperity palette: 3 bits (8 patterns)
- Services palette: 3 bits (8 patterns)
- Map layout palette: 3 bits (8 layouts: HubAndSpoke, DoubleLine, XShaped, Tree, Star, Cluster, Mesh, Grid)
- Outlier slot type: 2 bits (0=no outlier, 1=simple outlier, 2-3=reserved)

**Current Problem:**
- Layout-specific trail removal is complex and tightly coupled to outlier creation
- Budget accounting between outlier removal and layout removal is convoluted
- Outlier selection has multiple fallback strategies that add complexity
- The 4 current layouts may not be optimal for entropic variation

## Clean Slate Approach

### Option 1: Layout + Hidden Outlier Slot Encoding

**Concept:** Encode base layout + hidden outlier slot activation in seed bits. Outlier creation is independent of trail removal - it's a pre-positioned slot that gets activated by entropy.

**Bit Allocation:**
- Keep current 3 bits for base layout (8 layouts)
- Add 2 bits for "outlier slot type" (from 99 reserved bits)
- Total: 5 bits (minimal expansion with future-proofing)
- Values: 0=no outlier, 1=simple outlier, 2-3=reserved for future expansion

**Layout Redesign (8 layouts, no crossing trails):**
- **HubAndSpoke:** Central hub (slot 0) with outer ring towns connected via spokes
- **DoubleLine:** Two parallel lines of towns, connected at endpoints (no crossing trails)
- **X-shaped:** Four arms meeting at central town (slot 0), each arm is a line of towns
- **Tree:** Hierarchical structure - main trunk splits into branches at towns
- **Star:** Central hub (slot 0) with dead-end spokes (natural outlier positions)
- **Cluster:** Multiple mini-hubs (2-3 towns each) connected via inter-cluster trails
- **Mesh:** Fully connected network - every town connected to every other town
- **Grid:** 2D grid structure (3x3 max) - towns at grid intersections, trails along grid lines

**Hidden Outlier Slot:**
- Each layout has 1-2 pre-designated "outlier slots" at specific positions
- Example: Star layout has slot 8 as a potential outlier 6 days from the hub
- If "has outlier slot" is true AND entropy level is high enough, activate the slot
- The outlier slot uses a town name from the pool (deterministically selected)
- Outlier's single trail is pre-designed as 6 days to a strategic hub

**Entropy Activation:**
- Boring: Never activate outlier slot
- Classic: Activate if "has outlier slot" is true
- Adventurous: Always activate if available
- Wild: Always activate, layouts with multiple outlier slots can activate 2

**Trail Removal (Independent of Outliers):**
- Simple trail removal based on entropy level (no budget complexity)
- Remove trails up to entropy allowance, always verify connectivity
- Layouts are designed with redundancy to support removal
- Outlier's single trail is never removed (protected during removal phase)

**Benefits:**
- No trail removal complexity for outliers
- Outlier creation is simple activation, not complex removal
- Layouts can be designed without crossing trails constraint
- Clean separation: layout variety and outlier creation are independent
- Minimal bit expansion

### Option 2: Entropy-Driven Layout Mutation
*(Removed - Option 1 preferred)*

### Option 3: Layout Families with Entropy Variants
*(Removed - Option 1 preferred)*

## Recommendation

**Option 1 (Layout + Hidden Outlier Slot Encoding)** provides the best balance:

- **Minimal seed bit expansion** (4 bits vs current 3 bits)
- **Clean separation of concerns** (layout variety vs outlier creation)
- **Simple outlier activation** (no trail removal complexity)
- **Layout flexibility** (can redesign layouts without touching outlier logic)
- **No crossing trails constraint** (all trails meet at towns only)
- **Deterministic** (same seed + same entropy = same result)

**Refined Layout Palette (8 layouts, 3 bits, no crossing trails):**
1. HubAndSpoke - Central hub with outer ring towns connected via spokes
2. DoubleLine - Two parallel lines of towns, connected at endpoints
3. X-shaped - Four arms meeting at central town, each arm is a line of towns
4. Tree - Hierarchical structure with main trunk and branches (covers Y-shaped)
5. Star - Central hub with many dead-end spokes
6. Cluster - Multiple mini-hubs (2-3 towns each) connected together
7. Mesh - Fully connected network with lots of redundancy
8. Grid - 2D grid structure (3x3 max) with trails along grid lines

**Layout Design Constraints:**
- All layouts must have built-in redundancy to support trail removal while maintaining connectivity
- All layouts must have natural dead-end positions for outlier slots
- Trails should only meet at towns - no crossing trails between towns
- Layouts should support 5-10 town count range (current seed encoding)

**Implementation Path:**
1. Expand seed encoding by 1 bit for "has outlier slot"
2. Redesign layouts to remove Ring/LinearChain, add X-shaped, Tree, Star, Cluster, Mesh, Grid
3. Implement hidden outlier slot activation based on entropy level
4. Implement simple trail removal based on entropy level (no budget complexity)
5. Remove all complex budget accounting and outlier selection logic
6. Test connectivity after each trail removal step

## Implementation Details

**Bit Allocation:**
- Current: 24 bits used, 104 reserved
- New: 25 bits used (add 1 bit for HasOutlierSlot at position 27)
- MapLayoutPalette: 3 bits (8 layouts)
- HasOutlierSlot: 1 bit (from reserved bits)

**Outlier Activation Rules:**
- Boring: Never activate outlier slot
- Classic: Activate if HasOutlierSlot is true
- Adventurous: Always activate if available
- Wild: Always activate, layouts with multiple outlier slots can activate 2

**Trail Distance Rules:**
- Normal trails: 2-5 days (clamp any 6-day trails to 5)
- Outlier trail: Exactly 6 days (guaranteed to be the longest)
- 7+ days: Not used in this game

**Outlier Connection Selection:**
- Deterministic hash-based selection from all towns
- Uses seed + entropy + salt for reproducibility
- No layout-specific rules (universal approach)

**Outlier Name Pool:**
- For now: Use regular town name pool (next name after base towns)
- Future: Separate curated list of 10-20 "remote-sounding" names
- Future: Seam for themed outlier names (mining town, outlaw town, etc.)

**Trail Removal:**
- Random selection using seed/salt for determinism
- Always verify connectivity after each removal
- Maintain playability over all else
- No budget complexity - simple count-based removal

**Outlier Town Count:**
- Seed encodes base town count (e.g., 8)
- If outlier slot activated: total becomes base + 1 (e.g., 9)
- Outlier uses next available slot index

**Layout-Specific Removal Patterns:**
- HubAndSpoke: Remove spokes while keeping hub connected
- DoubleLine: Remove trails along lines, keep connectivity through endpoints
- XShaped: Remove entire arms or partial arms, keep connectivity through center
- Tree: Remove leaf branches, keep core trunk intact
- Star: Remove spokes freely (natural outlier positions)
- Cluster: Remove inter-cluster connections, keep intra-cluster connectivity
- Mesh: Remove many trails while maintaining full connectivity
- Grid: Remove trails in grid patterns, keep connectivity through grid paths

## Next Steps

1. Get user approval on refined approach
2. Create implementation plan (completed)
3. Execute implementation plan
