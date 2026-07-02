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

### Option 1: Layout + Mutation Strategy Encoding

**Concept:** Encode both the base layout AND how it should mutate by entropy in the seed bits.

**Bit Allocation:**
- Keep current 3 bits for base layout (8 possible layouts)
- Add 2 bits for mutation strategy (4 strategies: Conservative, Moderate, Aggressive, Wild)
- Total: 5 bits (still fits within current allocation with room to spare)

**Mutation Strategies:**
- **Conservative:** Boring = no changes, Classic = minor trail removal, Adventurous = moderate removal, Wild = aggressive removal
- **Moderate:** Boring = no changes, Classic = moderate removal, Adventurous = aggressive removal, Wild = very aggressive removal
- **Aggressive:** Boring = minor changes, Classic = moderate removal, Adventurous = aggressive removal, Wild = maximum removal
- **Wild:** All entropy levels apply significant changes, scaling with entropy

**Outlier Integration:**
- Outlier creation is a natural consequence of trail removal, not a separate phase
- If trail removal happens to create a dead-end with a 6-day trail, it's marked as outlier
- No proactive outlier creation - outliers emerge from the mutation process
- Mutation strategies can be tuned to make outliers more/less likely

**Layout Redesign:**
- Replace current 4 layouts with layouts that support better entropic variation
- Consider layouts with more redundancy (more trails) to allow more removal options
- Consider layouts with natural dead-ends that can become outliers
- Examples: HubAndSpokeWithRing, Mesh, Tree, Star, etc.

### Option 2: Entropy-Driven Layout Mutation

**Concept:** Base layout is just a starting topology. Entropy drives mutation through a unified process.

**Unified Mutation Process:**
1. Start with base layout from seed
2. Apply entropy-based mutations in priority order:
   - Remove trails (entropy level determines count)
   - Add trails (at higher entropy to maintain connectivity)
   - Modify trail properties (terrain, water, distance)
   - Create outliers if natural opportunities arise
3. Always verify connectivity after each mutation
4. If connectivity breaks, rollback and try different mutation

**Entropy Scaling:**
- Boring: No mutations (base layout as-is)
- Classic: 1-2 trail removals, minor property changes
- Adventurous: 2-3 trail removals, moderate property changes, possible trail additions
- Wild: 3-4 trail removals, major property changes, trail additions, outlier creation encouraged

**Outlier Creation:**
- During trail removal, prefer removing trails to create dead-ends
- If a dead-end has a 6-day trail, mark it as outlier
- Outliers are a side effect of the mutation process, not a separate feature

### Option 3: Layout Families with Entropy Variants

**Concept:** Each layout has built-in entropy variants encoded in the catalog, not the seed.

**Layout Families:**
- HubAndSpoke: Has 4 variants (Boring, Classic, Adventurous, Wild) pre-defined in catalog
- Ring: Has 4 variants pre-defined
- Mesh: Has 4 variants pre-defined
- etc.

**Seed Encoding:**
- 3 bits for layout family (8 families)
- 0 bits for entropy variant (determined at runtime by entropy setting)

**Catalog Lookup:**
- Seed encodes: layout family
- Runtime determines: entropy variant within that family
- Catalog provides: 4 variants per family, each with appropriate trail patterns

**Outlier Integration:**
- Higher entropy variants within families are designed to include outliers
- Catalog designers control when/how outliers appear
- No runtime outlier creation logic - it's baked into the catalog

## Recommendation

**Option 1 (Layout + Mutation Strategy Encoding)** provides the best balance:

- **Minimal seed bit expansion** (5 bits vs current 3 bits)
- **Clear separation of concerns** (layout vs mutation strategy)
- **Tunable entropy response** (different strategies for different play experiences)
- **Natural outlier emergence** (outliers happen as a consequence of mutation)
- **Layout flexibility** (can redesign layouts without changing mutation logic)
- **Deterministic** (same seed + same entropy = same result)

**Implementation Path:**
1. Expand MapLayoutPalette from 3 bits to 5 bits (add MutationStrategy field)
2. Redesign layouts to support better entropic variation
3. Implement unified trail removal based on mutation strategy + entropy level
4. Add outlier detection as post-processing (mark dead-ends with 6-day trails)
5. Remove complex budget accounting and outlier selection logic
6. Test connectivity after each mutation step

## Next Steps

1. Get user approval on Option 1 approach
2. Design new layout palette with mutation strategies
3. Design new base layouts that support entropic variation
4. Create implementation plan
