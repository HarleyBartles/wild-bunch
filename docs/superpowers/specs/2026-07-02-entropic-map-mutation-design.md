# Entropic Map Mutation Design

## Goals

1. **Interesting maps** - Variety and replayability through entropy-based variation
2. **Seeded maps** - Deterministic from seed (town count, layout palette), no seed bit expansion
3. **Entropic variation** - Maps change from base seed in interesting ways when entropy increases
4. **Entropy scaling** - Higher entropy = more drastic changes
5. **Playability** - All towns must remain reachable
6. **Outlier feature** - Sometimes have dead-end towns with one long trail as a play feature

## Current State Analysis

**Seed Encoding (24 bits used, 104 reserved):**
- World variant: 2 bits (3 variants: Canonical, Frontier, Rail)
- Town count: 4 bits (offset-encoded 5-20, wrapped to 5-10)
- Accusation index: 4 bits
- Default culprit index: 4 bits
- Cash bonus: 4 bits
- Prosperity palette: 3 bits (8 patterns)
- Services palette: 3 bits (8 patterns)
- Map layout palette: 3 bits (currently 4 layouts: HubAndSpoke, LinearChain, Ring, DoubleLine)

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
- Add 1 bit for "has outlier slot" (from 104 reserved bits)
- Total: 4 bits (minimal expansion)

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

## Next Steps

1. Get user approval on Option 1 approach
2. Design new layout palette with mutation strategies
3. Design new base layouts that support entropic variation
4. Create implementation plan
