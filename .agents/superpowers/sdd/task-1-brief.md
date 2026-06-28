### Task 1: Extend the BUNCH-102 setup read model for map coordinates

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/StartingTownDto.cs` or the existing setup-town DTO surface from BUNCH-102
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs`
- Create: `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`
- Modify: `src/WildBunch.Domain/World/WorldModels.cs` only if the smallest honest representation needs a domain-level coordinate value object instead of a GameContent-local layout table

**Interfaces:**
- Consumes: the BUNCH-102 setup-town candidate source and the seeded world/trail truth.
- Produces: a deterministic map layout extension with town coordinates and route edges that reuses the same eligibility/candidate source as BUNCH-102.

- [ ] **Step 1: Add a deterministic coordinate layout for the seeded towns.**

The layout should stay static and modest. Use coordinates that make the trail graph readable; do not generate procedural map art.

- [ ] **Step 2: Extend the setup-town read model or add a companion map projection, but keep the candidate source shared.**

The map view may add x/y coordinates and trail-edge labels, but the allowed-town list must come from the same eligibility logic BUNCH-102 already owns.

- [ ] **Step 3: Keep the map source next to the existing seeded world catalog and setup read model.**

Do not move map truth into the web project. The frontend should consume read data only.

## Global Constraints (binding for this task)

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- BUNCH-102 has landed on `main`. BUNCH-75 composes with that plan and reuses its starting-town selection, request, and confirmation seams.
- The Phaser layer is presentation/input only. It may emit `townSelected` intent, but it must not calculate legal moves, start eligibility, or route truth.
- Keep the backend/application/domain route authoritative for towns, trails, distances, selected starting town validity, and game creation.
- Do not normalize runtime session state into new tables for this slice.
- Do not move map truth into the web project. The frontend should consume read data only.
- Prefer a modest static coordinate model if that is the smallest honest proof; do not overbuild procedural map generation just to prove the Phaser seam.
- Keep any new map source next to the existing seeded world catalog (`src/WildBunch.GameContent/NewGame/`).
- The allowed-town list must come from the same eligibility logic BUNCH-102 already owns (`StartingTownCatalog.GetStartingTownCandidates()` — towns with Supplies or NoticeBoard services in the canonical variant).
