# BUNCH-148 Background Building Fill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fill eligible empty town plots with prosperity-scaled background houses and shops, support below-spur plots for background buildings only, and emit spur-path-cross tiles when paired background buildings face each other across a spur.

**Architecture:** Keep the current town hub scene as the render entrypoint, but move the background-placement logic into a small helper module so the scene stays readable. The helper will scan eligible plots, apply foreground occupancy first, derive a deterministic background budget from prosperity, and return background sprite placements plus any spur cross tiles. `TownHubScene` then renders the returned placements with the existing ground/path/road underlays and the existing foreground buildings.

**Tech Stack:** TypeScript, Phaser 3, Vite, Vitest

## Global Constraints

- Background buildings are decorative only and must not receive click handlers.
- Use the existing prosperity ladder only: `Boomtown`, `Prosperous`, `Poor`, `Destitute`.
- Use the existing five-view turnaround contract for all town buildings: `front`, `profile`, `rear`, `front-oblique`, `rear-oblique`.
- Use the existing support families only: `background-house` and `background-shop`.
- Treat `spur-path-cross` as a required art preflight dependency for Task 3; if the generated tile is not present when implementation starts, stop and block rather than inventing a fallback.
- Preserve the current path-underlay rules already used for main-road and spur-adjacent buildings.
- Support below-spur placement in addition to the current above-spur placement for background buildings only.
- Background and foreground buildings are a semantic distinction, not a renderer layer distinction; they may not overlap because they occupy different tiles.
- Keep the feature deterministic for a given layout and seed-derived town state.
- Keep scope limited to the town-hub renderer and its helper/tests unless a file boundary must change to keep the scene small.

---

### Task 1: Add Background Sprite URL Helper

**Files:**
- Modify: `src/WildBunch.Web/src/components/town-hub/sprite-loader.ts`
- Modify: `src/WildBunch.Web/src/components/town-hub/sprite-loader.test.ts`

**Interfaces:**
- Consumes: `BuildingView` numeric values, `TownProsperity`, and the existing named-building sprite URL helper patterns.
- Produces:
  - `getBackgroundSpriteKey(family: "background-house" | "background-shop", view: number, prosperity: TownProsperity): string`
  - `getBackgroundSpriteUrl(family: "background-house" | "background-shop", view: number, prosperity: TownProsperity): string`

- [ ] **Step 1: Write the failing tests for background house/shop sprite URLs**

```ts
import { describe, expect, it } from "vitest";
import { TownProsperity } from "../../api/types";
import { getBackgroundSpriteKey, getBackgroundSpriteUrl } from "./sprite-loader";

describe("getBackgroundSpriteUrl", () => {
  it("returns the prosperous background house front-oblique URL", () => {
    expect(getBackgroundSpriteKey("background-house", 3, TownProsperity.Prosperous))
      .toBe("background-house-prosperous-front-oblique");
    expect(getBackgroundSpriteUrl("background-house", 3, TownProsperity.Prosperous))
      .toBe("/assets/town-hub-buildings/prosperous/background-house/front-oblique.png");
  });

  it("returns the boomtown background shop rear-oblique URL", () => {
    expect(getBackgroundSpriteUrl("background-shop", 4, TownProsperity.Boomtown))
      .toBe("/assets/town-hub-buildings/boomtown/background-shop/rear-oblique.png");
  });

  it("keeps the same 5-view contract for all background families", () => {
    const views = [0, 1, 2, 3, 4];
    const names = ["front", "profile", "rear", "front-oblique", "rear-oblique"];

    for (let index = 0; index < views.length; index++) {
      expect(getBackgroundSpriteUrl("background-house", views[index], TownProsperity.Poor))
        .toBe(`/assets/town-hub-buildings/poor/background-house/${names[index]}.png`);
      expect(getBackgroundSpriteUrl("background-shop", views[index], TownProsperity.Destitute))
        .toBe(`/assets/town-hub-buildings/destitute/background-shop/${names[index]}.png`);
    }
  });
});
```

- [ ] **Step 2: Run the focused sprite-loader test to confirm the new helper is missing**

Run: `npm.cmd run test -- --run src/components/town-hub/sprite-loader.test.ts`
Expected: FAIL with `getBackgroundSpriteUrl` missing.

- [ ] **Step 3: Implement the helper by extracting the prosperity, family, and view mapping logic**

```ts
type BackgroundBuildingFamily = "background-house" | "background-shop";

export function getBackgroundSpriteUrl(
  family: BackgroundBuildingFamily,
  view: number,
  prosperity: TownProsperity,
): string {
  // Reuse the same prosperity directory contract and the same 5-view naming scheme.
}

export function getBackgroundSpriteKey(
  family: BackgroundBuildingFamily,
  view: number,
  prosperity: TownProsperity,
): string {
  // Stable Phaser load key: `${family}-${prosperityDir}-${viewFileName}`
}
```

- [ ] **Step 4: Run the focused sprite-loader test again**

Run: `npm.cmd run test -- --run src/components/town-hub/sprite-loader.test.ts`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/sprite-loader.ts src/WildBunch.Web/src/components/town-hub/sprite-loader.test.ts
git commit -m "feat: add background town building sprite urls"
```

### Task 2: Add a Background Placement Planner Helper

**Files:**
- Create: `src/WildBunch.Web/src/components/town-hub/background-building-planner.ts`
- Create: `src/WildBunch.Web/src/components/town-hub/background-building-planner.test.ts`

**Interfaces:**
- Consumes: `TownLayoutDto`, `BuildingPlacementDto`, `BuildingView`, the tile grid, and the foreground building list from the current town layout.
- Produces:
  - `collectForegroundOccupiedSlots(layout: TownLayoutDto): Set<string>`
  - `collectEligibleBackgroundSlots(layout: TownLayoutDto): BackgroundSlot[]`
  - `planBackgroundBuildings(layout: TownLayoutDto, occupiedSlots: Set<string>): PlannedBackgroundBuilding[]`
  - `planSpurCrossTiles(layout: TownLayoutDto, backgroundPlacements: PlannedBackgroundBuilding[]): SpurCrossTile[]`

- [ ] **Step 1: Write the failing planner tests for slot collection, prosperity counts, and spur pairing**

```ts
import { describe, expect, it } from "vitest";
import { BuildingKind, BuildingView, TownProsperity } from "../../api/types";
import { collectEligibleBackgroundSlots, planBackgroundBuildings, planSpurCrossTiles } from "./background-building-planner";

describe("background building planner", () => {
  it("includes below-spur plots in the eligible slot list", () => {
    // Layout with a spur and empty plots on both sides of the spur row.
    // Expect the helper to return both above-spur and below-spur candidates.
  });

  it("caps destitute coverage at one or two background buildings", () => {
    // A destitute town with multiple eligible slots should return 1 or 2 placements only.
  });

  it("pushes boomtown coverage to near full occupancy while leaving one or two slots empty", () => {
    // A boomtown should leave one or two eligible plots empty.
  });

  it("produces a spur cross tile only when paired background buildings face each other across the same spur", () => {
    // One above + one below the same spur row should return a cross tile.
  });
});
```

- [ ] **Step 2: Run the planner test file to confirm the helper module is missing**

Run: `npm.cmd run test -- --run src/components/town-hub/background-building-planner.test.ts`
Expected: FAIL with missing module / missing exports.

- [ ] **Step 3: Implement the planner helper and its local types**

```ts
export interface BackgroundSlot {
  row: number;
  col: number;
  side: "east" | "west";
  attachesTo: "road" | "spur-above" | "spur-below";
}

export interface PlannedBackgroundBuilding {
  row: number;
  col: number;
  family: "background-house" | "background-shop";
  view: BuildingView;
  flipX: boolean;
  flipY: boolean;
}

export interface SpurCrossTile {
  row: number;
  col: number;
  flipX: boolean;
  flipY: boolean;
}

export function collectForegroundOccupiedSlots(layout: TownLayoutDto): Set<string> {
  // Return tile keys such as "3:4" for every foreground building tile and any
  // tile the existing underlay rules reserve around that building.
}
```

The implementation should:
- scan the 10x10 tile grid for these exact eligible plot shapes:
  - west main-road slot: tile `row,col=3` when `row,col=4` is a road tile and the slot is empty
  - east main-road slot: tile `row,col=6` when `row,col=5` is a road tile and the slot is empty
  - above-spur slot: tile `row,col` when `row+1,col` is the spur road tile and the slot is empty
  - below-spur slot: tile `row,col` when `row-1,col` is the spur road tile and the slot is empty
- exclude plots already claimed by foreground buildings, where foreground occupancy is the exact set of tile cells used by `layout.buildings`
- apply the prosperity budget exactly as documented in the spec, with this deterministic fallback for small slot counts:
  - Destitute: choose a deterministic count from `0..min(2, eligibleCount)`
  - Poor: choose a deterministic count from `ceil(eligibleCount * 0.20)..floor(eligibleCount * 0.40)`
  - Prosperous: choose a deterministic count from `ceil(eligibleCount * 0.60)..floor(eligibleCount * 0.80)`
  - Boomtown: choose a deterministic count from `max(0, eligibleCount - 2)..eligibleCount`
- choose slots in deterministic seeded order by sorting on a stable hash of the layout seed plus row, column, and attachment type, then taking the first `budget` slots
- choose a background family and view from the existing support-building turnaround contract
- apply mirroring as direct reuse of the existing building rules:
  - road-adjacent background buildings reuse the current main-road underlay rules exactly
  - above-spur background buildings reuse the current spur rules exactly and only select from `front` or `front-oblique` views
  - below-spur background buildings reuse the current spur rules with a vertical mirror and only select from `rear` or `rear-oblique` views
  - below-spur path underlays mirror the above-spur path direction rules while preserving the side-based horizontal mirror decision
  - when the mirrored below-spur case is used, the vertical mirror toggles `flipY` relative to the above-spur rule set while preserving the side-based `flipX` decision
- record a spur cross tile when the same spur has paired above/below background buildings

- [ ] **Step 4: Run the planner tests until they pass**

Run: `npm.cmd run test -- --run src/components/town-hub/background-building-planner.test.ts`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/background-building-planner.ts src/WildBunch.Web/src/components/town-hub/background-building-planner.test.ts
git commit -m "feat: plan background town building placements"
```

### Task 3: Integrate Background Placements into TownHubScene

**Files:**
- Modify: `src/WildBunch.Web/src/components/town-hub/TownHubScene.ts`
- Modify: `src/WildBunch.Web/src/tests/TownHubScene.tiles.test.ts`

**Interfaces:**
- Consumes: `collectForegroundOccupiedSlots`, `planBackgroundBuildings`, `planSpurCrossTiles`, `getBackgroundSpriteKey`, and `getBackgroundSpriteUrl`.
- Produces: cached background building placements, preload calls for their sprite keys, and rendered background houses/shops plus spur-path-cross tiles.

- [ ] **Step 1: Write the failing scene regression tests**

Add one test that asserts:
- background placements appear in eligible empty spaces beside roads and above/below spurs
- the same prosperity seed produces the same background count and slot choices
- background buildings do not register interaction handlers

Add one test that asserts:
- when two background buildings face each other across a spur, the spur-path-cross tile appears in the spur cell between them

Use the existing `TownHubScene.tiles.test.ts` style:

```ts
expect(imageCalls).toEqual(
  expect.arrayContaining([
    expect.objectContaining({ key: "background-house", flipX: false, flipY: false }),
    expect.objectContaining({ key: "background-shop", flipX: true, flipY: false }),
    expect.objectContaining({ key: "spur-path-cross", flipX: false, flipY: false }),
  ]),
);
```

- [ ] **Step 2: Run the targeted scene tests to confirm the new expectations fail**

Run: `npm.cmd run test -- --run src/tests/TownHubScene.tiles.test.ts src/tests/TownHubScene.test.ts`
Expected: FAIL because the background planner and rendering calls do not exist yet.

- [ ] **Step 3: Wire the planner into the scene and keep the foreground building behavior intact**

```ts
private backgroundPlacements: PlannedBackgroundBuilding[] = [];
private spurCrossTiles: SpurCrossTile[] = [];

preload(): void {
  const occupiedSlots = collectForegroundOccupiedSlots(this.layout);
  this.backgroundPlacements = planBackgroundBuildings(this.layout, occupiedSlots);
  this.spurCrossTiles = planSpurCrossTiles(this.layout, this.backgroundPlacements);

  for (const placement of this.backgroundPlacements) {
    const spriteKey = getBackgroundSpriteKey(placement.family, placement.view, this.layout.prosperity);
    const spriteUrl = getBackgroundSpriteUrl(placement.family, placement.view, this.layout.prosperity);
    this.load.image(spriteKey, spriteUrl);
  }
}

private renderBackgroundBuildings(): void {
  for (const placement of this.backgroundPlacements) {
    const spriteKey = getBackgroundSpriteKey(placement.family, placement.view, this.layout.prosperity);
    this.add
      .image(placement.col * 80 + 40, placement.row * 50 + 25, spriteKey)
      .setDisplaySize(80, 50)
      .setFlipX(placement.flipX)
      .setFlipY(placement.flipY);
  }

  for (const crossTile of this.spurCrossTiles) {
    this.add
      .image(crossTile.col * 80 + 40, crossTile.row * 50 + 25, "spur-path-cross")
      .setDisplaySize(80, 50)
      .setFlipX(crossTile.flipX)
      .setFlipY(crossTile.flipY);
  }
}
```

The integration should:
- preserve the existing foreground building render path
- keep path-underlay logic unchanged except where below-spur placements need to use it
- keep background buildings non-interactive
- keep the new spur-path-cross tile path isolated to the paired-spur case
- load `spur-path-cross` only after the art preflight task has produced the tile; if the loader path is missing, the implementation must stop and report the missing dependency instead of guessing at a substitute

- [ ] **Step 4: Run the targeted scene tests until they pass**

Run: `npm.cmd run test -- --run src/tests/TownHubScene.tiles.test.ts src/tests/TownHubScene.test.ts src/components/town-hub/background-building-planner.test.ts src/components/town-hub/sprite-loader.test.ts`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/TownHubScene.ts src/WildBunch.Web/src/tests/TownHubScene.tiles.test.ts
git commit -m "feat: render background town buildings"
```

### Task 4: Final Validation and Closeout

**Files:**
- Modify if needed: `src/WildBunch.Web/src/tests/TownHubScene.tiles.test.ts`
- Modify if needed: `src/WildBunch.Web/src/components/town-hub/background-building-planner.test.ts`

**Interfaces:**
- Consumes: the completed background placement helpers and the scene integration.
- Produces: a green validation pass that covers sprite URLs, planner rules, and scene rendering.

- [ ] **Step 1: Run the focused tests from a clean worktree snapshot**

Run:
`npm.cmd run test -- --run src/components/town-hub/sprite-loader.test.ts src/components/town-hub/background-building-planner.test.ts src/tests/TownHubScene.tiles.test.ts src/tests/TownHubScene.test.ts`

Expected:
- all tests pass
- no regressions in the existing town-hub surface tests

- [ ] **Step 2: Run the production build**

Run: `npm.cmd run build`

Expected:
- `tsc --noEmit` passes
- Vite production build completes successfully

- [ ] **Step 3: Commit any final test-only corrections**

```bash
git add src/WildBunch.Web/src/tests/TownHubScene.tiles.test.ts src/WildBunch.Web/src/components/town-hub/background-building-planner.test.ts
git commit -m "test: lock background town building coverage"
```
