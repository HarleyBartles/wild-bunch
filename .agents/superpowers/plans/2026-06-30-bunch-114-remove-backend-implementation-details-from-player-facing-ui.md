# BUNCH-114: Remove Backend Implementation Details from Player-Facing UI

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove backend implementation details (ids, counts, delta values, parenthetical numbers, capability flags, backend terminology) from five player-facing UI components so the play surfaces read like in-world game surfaces, not debug dashboards.

**Architecture:** Pure frontend presentation cleanup. No backend/API/domain changes. Each component already renders player-known state from DTOs — we remove the backend-leaking parts and clean up unused imports/styled components. Tests that assert on the removed elements are updated to assert the cleaner shape.

**Tech Stack:** React, TypeScript, styled-components, Vitest, React Testing Library.

## Global Constraints

- All styling via styled-components — no plain CSS classes (enforced by `src/tests/stylingEnforcement.test.ts`).
- Reference design tokens via `var(--token-name)`.
- Re-use shared primitives from `src/components/ui/sharedStyled.tsx` where already used.
- Player-facing UI must read as in-world game surfaces, not dashboards (per `.agents/unslop/play-surface-ui.md`).
- React renders backend/player-known state — no frontend-invented truth.
- No backend/API/domain changes in this slice.
- Test command: `cd src/WildBunch.Web && npm test`
- Typecheck/build command: `cd src/WildBunch.Web && npm run build`

## File Discrepancy Note

The Linear issue lists `TravelDiaryNotebook.tsx` as a target file, but the delta values (Wallet Δ, Food Δ, etc.), parenthetical numbers (Health 9 (0)), and redundant item counts (Horse feed 3 (0), Ammo 6 (0)) all live in **`TravelDiaryDayCard.tsx`** — the child component that `TravelDiaryNotebook.tsx` renders per diary day. `TravelDiaryNotebook.tsx` is a thin wrapper with no delta/parenthetical content. This plan modifies `TravelDiaryDayCard.tsx` (the actual target) and does not touch `TravelDiaryNotebook.tsx`.

---

## Task 1: Remove StatList section from StoreOffersPanel

**Files:**
- Modify: `src/WildBunch.Web/src/components/StoreOffersPanel.tsx` (lines 5-13, 114-133)
- Test: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx` (no assertions on removed elements — verify no breakage)

**Interfaces:**
- Consumes: `TownStoreOffersDto` from `../api/types` (unchanged)
- Produces: `StoreOffersPanel` with StatList section removed; offer cards remain unchanged

**What to remove:** The entire `<StatList>` block (lines 116-133) that shows Town, Town id, Catalog, and Source. Also remove the now-unused `StatList` import.

- [ ] **Step 1: Remove StatList import**

In `src/WildBunch.Web/src/components/StoreOffersPanel.tsx`, change the import block (lines 5-13) to remove `StatList`:

```tsx
import {
  StatusCard,
  Stack,
  ItemCard,
  Muted,
  Field,
  Button,
} from "./ui/sharedStyled";
```

- [ ] **Step 2: Remove StatList section from the component body**

In the `StoreOffersPanel` function, remove the entire `<StatList>...</StatList>` block (lines 116-133). The `storeOffers ?` block should now start directly with the `<OfferList>`:

```tsx
      {storeOffers ? (
        <>
          <OfferList>
            {storeOffers.offers.length > 0 ? (
              storeOffers.offers.map((offer) => (
                <StoreOfferCard
                  key={quantityKey(offer)}
                  offer={offer}
                  disabled={busy || loading}
                  onBuyOffer={onBuyOffer}
                />
              ))
            ) : (
              <Muted>No store offers are available in this town.</Muted>
            )}
          </OfferList>
        </>
      ) : null}
```

- [ ] **Step 3: Run tests to verify no regressions**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS (no existing tests assert on Town id, Catalog, or Source labels)

- [ ] **Step 4: Run build to verify no TypeScript errors**

Run: `cd src/WildBunch.Web && npm run build`
Expected: PASS (no unused import errors)

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/StoreOffersPanel.tsx
git commit -m "refactor(web): remove StatList backend details from StoreOffersPanel

Remove Town, Town id, Catalog, and Source display that duplicated
player-known context. Players already know which town they're in.

BUNCH-114"
```

---

## Task 2: Remove Loadout/Capabilities counts, capability flags, and "No travel state" from InventoryPanel

**Files:**
- Modify: `src/WildBunch.Web/src/components/InventoryPanel.tsx` (lines 2-3, 11-26, 59-66, 74-81, 85-91)
- Test: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx` (no assertions on removed elements — verify no breakage)

**Interfaces:**
- Consumes: `InventoryDto`, `InventoryItemDto` from `../api/types` (unchanged)
- Produces: `InventoryPanel` with only Cash, Horse state, Canteen, and item list (no counts, no capability tags, no "No travel state")

**What to remove:**
1. "Loadout items" count (lines 59-62)
2. "Capabilities" count (lines 63-66)
3. "No travel state" fallback text (line 80) — replace with empty string fallback
4. Capabilities `TagRow` with all capability flags (lines 85-91)
5. Now-unused imports: `formatCapabilityLabel`, `InventoryCapabilitiesDto`, `TagRow`, `Tag` styled components

- [ ] **Step 1: Remove unused imports**

In `src/WildBunch.Web/src/components/InventoryPanel.tsx`, change line 2 to remove `InventoryCapabilitiesDto`:

```tsx
import type { InventoryDto, InventoryItemDto } from "../api/types";
```

Change line 3 to remove `formatCapabilityLabel`:

```tsx
import { formatCanteenState, formatHorseTravelState, formatItemKind } from "../ui/formatters";
```

- [ ] **Step 2: Remove TagRow and Tag styled components**

Remove the `TagRow` styled component (lines 11-16) and the `Tag` styled component (lines 18-26). These are no longer used after removing the capabilities flags.

- [ ] **Step 3: Remove Loadout items and Capabilities count from StatList**

In the `InventoryPanel` function, remove the "Loadout items" `<div>` (lines 59-62) and the "Capabilities" `<div>` (lines 63-66). The `StatList` should end after the "Canteen" entry:

```tsx
      <StatList>
        <div>
          <dt>Cash</dt>
          <dd>${inventory.wallet.cash.toFixed(2)}</dd>
        </div>
        <div>
          <dt>Horse state</dt>
          <dd>{formatHorseTravelState(inventory.horseState)}</dd>
        </div>
        <div>
          <dt>Canteen</dt>
          <dd>{formatCanteenState(inventory.canteenState)}</dd>
        </div>
      </StatList>
```

- [ ] **Step 4: Render item detail line only when horse/canteen detail exists**

Compute the item detail text and render the `<ItemDetailLine>` only when there is horse or canteen state to show. Items without either (e.g. plain food, ammo) render no detail line at all instead of the backend "No travel state" fallback. Replace the existing `<ItemDetailLine>` block (lines 74-81) with:

```tsx
            {item.horseState || item.canteenState ? (
              <ItemDetailLine>
                {[
                  item.horseState ? `Horse: ${formatHorseTravelState(item.horseState)}` : null,
                  item.canteenState ? `Canteen: ${formatCanteenState(item.canteenState)}` : null,
                ]
                  .filter(Boolean)
                  .join(" | ")}
              </ItemDetailLine>
            ) : null}
```

- [ ] **Step 5: Remove capabilities TagRow**

Remove the entire `<TagRow>` block (lines 85-91) after the `ItemList`:

```tsx
      </ItemList>
    </StatusCard>
  );
}
```

- [ ] **Step 6: Run tests to verify no regressions**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS (no existing tests assert on Loadout items, Capabilities, or No travel state)

- [ ] **Step 7: Run build to verify no TypeScript errors**

Run: `cd src/WildBunch.Web && npm run build`
Expected: PASS (no unused import or variable errors)

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Web/src/components/InventoryPanel.tsx
git commit -m "refactor(web): remove backend counts and capability flags from InventoryPanel

Remove Loadout items count, Capabilities count, capability flag tags,
and 'No travel state' fallback. These were backend implementation
details that didn't help players make decisions.

BUNCH-114"
```

---

## Task 3: Remove Delay margin, Ride-day distance, and Canteen needed from TravelSummary

**Files:**
- Modify: `src/WildBunch.Web/src/components/travel/TravelSummary.tsx` (lines 51-64, 73-80)
- Test: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx` (no assertions on removed elements — verify no breakage)

**Interfaces:**
- Consumes: `GameSessionDto` from `../../api/types` (unchanged)
- Produces: `TravelSummary` without Delay margin, Ride-day distance, or Canteen needed rows

**What to remove:**
1. "Canteen needed" SummaryItem (lines 55-60) — redundant with "Water pressure"
2. "Delay margin" SummaryItem (lines 61-64) — backend implementation detail
3. "Ride-day distance" SummaryItem (lines 73-80) — constant per-route value, less useful than "Remaining distance"

- [ ] **Step 1: Remove Canteen needed and Delay margin SummaryItems**

In `src/WildBunch.Web/src/components/travel/TravelSummary.tsx`, remove the "Canteen needed" `<SummaryItem>` (lines 55-60) and the "Delay margin" `<SummaryItem>` (lines 61-64). After the "Water pressure" item, the grid should continue directly to "Terrain":

```tsx
        <SummaryItem>
          <dt>Water pressure</dt>
          <dd>{journey.waterSecure ? "Secure" : "Drying out"}</dd>
        </SummaryItem>
        <SummaryItem>
          <dt>Terrain</dt>
          <dd>{formatTrailTerrain(journey.routeProfile.terrain)}</dd>
        </SummaryItem>
```

- [ ] **Step 2: Remove Ride-day distance SummaryItem**

Remove the "Ride-day distance" `<SummaryItem>` (lines 73-80). The grid should end after "Risk":

```tsx
        <SummaryItem>
          <dt>Risk</dt>
          <dd>{formatRisk(journey.routeProfile.risk)}</dd>
        </SummaryItem>
      </SummaryGrid>
```

- [ ] **Step 3: Run tests to verify no regressions**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS (no existing tests assert on Delay margin, Ride-day distance, or Canteen needed)

- [ ] **Step 4: Run build to verify no TypeScript errors**

Run: `cd src/WildBunch.Web && npm run build`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/components/travel/TravelSummary.tsx
git commit -m "refactor(web): remove Delay margin, Ride-day distance, Canteen needed from TravelSummary

Remove backend implementation details from the trail ledger: Delay
margin (internal scheduling margin), Ride-day distance (constant
per-route value redundant with Remaining distance), and Canteen needed
(redundant with Water pressure).

BUNCH-114"
```

---

## Task 4: Remove delta values, parenthetical numbers, and redundant item counts from TravelDiaryDayCard

**Files:**
- Modify: `src/WildBunch.Web/src/components/travel/TravelDiaryDayCard.tsx` (lines 3-4, 58-86, 108-125, 228-235)
- Test: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx` (lines 259-261 — update assertions)

**Interfaces:**
- Consumes: `TravelDiaryDayDto` from `../../api/types` (unchanged)
- Produces: `TravelDiaryDayCard` with trail event title only (no delta meta), resolution prose only (no delta meta), and day meta with current values only (no parenthetical deltas, no Horse feed, no Ammo)

**What to remove:**
1. TrailNote `TrailNoteMeta` — all Δ spans (Wallet Δ, Food Δ, Canteen Δ, Delay Δ, Lawman heat Δ, Horse hunger/thirst/exhaustion Δ)
2. ResolutionNote `TrailNoteMeta` — all Δ spans (Health Δ, Wallet Δ, Ammo Δ, Lawman heat Δ, Horse exhaustion Δ)
3. DayMeta parenthetical deltas — the `(formatSignedNumber(...))` parts from all entries
4. DayMeta redundant item counts — Horse feed and Ammo entries removed entirely
5. Now-unused imports: `formatSignedNumber`, `TrailNoteMeta` styled component

- [ ] **Step 1: Remove formatSignedNumber import**

In `src/WildBunch.Web/src/components/travel/TravelDiaryDayCard.tsx`, change line 4 to remove `formatSignedNumber`:

```tsx
import { formatHorseTravelState, formatJourneyStatus, formatTravelMode } from "../../ui/formatters";
```

- [ ] **Step 2: Remove TrailNoteMeta from TrailNote**

In the `TravelDiaryDayCard` function, remove the entire `<TrailNoteMeta>` block inside the TrailNote (lines 61-70). The TrailNote keeps only the title:

```tsx
      {day.trailEvent ? (
        <TrailNote>
          <strong>{day.trailEvent.title}</strong>
        </TrailNote>
      ) : null}
```

- [ ] **Step 3: Remove TrailNoteMeta from ResolutionNote**

Remove the entire `<TrailNoteMeta>` block inside the ResolutionNote (lines 78-84). The ResolutionNote keeps the choice label and prose summary:

```tsx
      {day.encounterResolution ? (
        <ResolutionNote>
          <strong>{day.encounterResolution.choiceLabel}</strong>
          <p>{renderResolutionSummary(day.encounterResolution)}</p>
        </ResolutionNote>
      ) : null}
```

- [ ] **Step 4: Rewrite renderDayMeta without parentheticals and redundant item counts**

Replace the entire `renderDayMeta` function (lines 108-125) with a version that shows only current values, removes parenthetical deltas, and removes Horse feed and Ammo entries:

```tsx
function renderDayMeta(day: TravelDiaryDayDto) {
  const hasHorseState = day.horseStateAfter !== null;
  const pieces = [
    `Health ${day.currentHealth}`,
    `Wallet ${day.currentWallet.toFixed(2)}`,
    `Food ${day.currentFood}`,
    `Canteen ${day.currentCanteenCharges}`,
    `Lawman heat ${day.currentHeat}`,
  ];

  if (hasHorseState) {
    pieces.splice(3, 0, `Horse ${formatHorseTravelState(day.horseStateAfter)}`);
  }

  return pieces.join(" | ");
}
```

- [ ] **Step 5: Remove TrailNoteMeta styled component**

Remove the `TrailNoteMeta` styled component definition (around lines 228-235). It is no longer used after removing both TrailNoteMeta blocks.

- [ ] **Step 6: Update TravelPanel test assertions for DayMeta**

In `src/WildBunch.Web/src/tests/TravelPanel.test.tsx`, update the three DayMeta assertions (lines 259-261). The DayMeta no longer has parenthetical deltas or an Ammo entry:

Change:
```tsx
    expect(screen.getByText(/Health 9 \(0\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Wallet 14\.00 \(0\.00\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Ammo 0 \(0\)/i)).toBeInTheDocument();
```

To:
```tsx
    expect(screen.getByText(/Health 9 \| Wallet 14\.00/i)).toBeInTheDocument();
    expect(screen.queryByText(/Ammo 0/i)).not.toBeInTheDocument();
```

The first assertion verifies the DayMeta renders with current values (no parentheticals). The second assertion verifies the Ammo entry was removed.

- [ ] **Step 7: Run tests to verify all pass**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS — all TravelPanel tests pass with updated assertions. The "hides horse-only travel diary fields" test (line 293) still passes because it asserts absence of horse-related text, and the Horse feed entry is now also absent (which satisfies `queryByText(/horse feed/i).not.toBeInTheDocument()`).

- [ ] **Step 8: Run build to verify no TypeScript errors**

Run: `cd src/WildBunch.Web && npm run build`
Expected: PASS (no unused import errors)

- [ ] **Step 9: Commit**

```bash
git add src/WildBunch.Web/src/components/travel/TravelDiaryDayCard.tsx src/WildBunch.Web/src/tests/TravelPanel.test.tsx
git commit -m "refactor(web): remove delta values and parentheticals from TravelDiaryDayCard

Remove trail event delta meta (Wallet Δ, Food Δ, etc.), resolution
delta meta (Health Δ, Ammo Δ, etc.), parenthetical numbers from day
meta (Health 9 (0) → Health 9), and redundant item counts (Horse feed,
Ammo). Update TravelPanel test assertions to match cleaner shape.

BUNCH-114"
```

---

## Task 5: Remove "npc" type label, rename "Fight bullets" to "Bullets", rename "Bribe amount" to "Bribe" in JourneyDecision

**Files:**
- Modify: `src/WildBunch.Web/src/components/travel/JourneyDecision.tsx` (lines 42-44, 51, 72)
- Test: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx` (lines 371, 413 — update label assertions)

**Interfaces:**
- Consumes: `JourneyEncounterDto` from `../../api/types` (unchanged)
- Produces: `JourneyDecision` without encounter kind label, with "Bullets" and "Bribe" control labels

**What to change:**
1. Remove `<span>{encounter.kind}</span>` from DecisionHeading (line 43) — `encounter.kind` can contain backend values like "npc"; the `encounter.message` already provides player-facing context
2. Change `ControlLabel` text "Fight bullets" → "Bullets" (line 51)
3. Change `ControlLabel` text "Bribe amount" → "Bribe" (line 72)

- [ ] **Step 1: Remove encounter kind span from DecisionHeading**

In `src/WildBunch.Web/src/components/travel/JourneyDecision.tsx`, remove the `<span>{encounter.kind}</span>` from the DecisionHeading (line 43). The heading keeps only the "Trail decision" strong:

```tsx
      <DecisionHeading>
        <strong>Trail decision</strong>
      </DecisionHeading>
```

- [ ] **Step 2: Rename "Fight bullets" label to "Bullets"**

Change the `ControlLabel` text on line 51 from "Fight bullets" to "Bullets":

```tsx
              <ControlLabel htmlFor="journey-fight-bullets">Bullets</ControlLabel>
```

- [ ] **Step 3: Rename "Bribe amount" label to "Bribe"**

Change the `ControlLabel` text on line 72 from "Bribe amount" to "Bribe":

```tsx
              <ControlLabel htmlFor="journey-bribe-amount">Bribe</ControlLabel>
```

- [ ] **Step 4: Update TravelPanel test label assertions**

In `src/WildBunch.Web/src/tests/TravelPanel.test.tsx`, update the two `findByLabelText` calls:

Line 371 — change:
```tsx
    const fightBullets = await screen.findByLabelText(/fight bullets/i);
```
To:
```tsx
    const fightBullets = await screen.findByLabelText(/^bullets$/i);
```

Line 413 — change:
```tsx
    const bribeAmount = await screen.findByLabelText(/bribe amount/i);
```
To:
```tsx
    const bribeAmount = await screen.findByLabelText(/^bribe$/i);
```

- [ ] **Step 5: Run tests to verify all pass**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS — both encounter resolution tests pass with updated label matchers. The `findByLabelText(/^bullets$/i)` matches the `<label>` associated with the fight bullets input. The `findByLabelText(/^bribe$/i)` matches the `<label>` associated with the bribe amount input. Neither conflicts with the choice buttons named "Fight" or "Bribe" because `findByLabelText` matches form controls associated with `<label>` elements, not `<button>` text.

- [ ] **Step 6: Run build to verify no TypeScript errors**

Run: `cd src/WildBunch.Web && npm run build`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Web/src/components/travel/JourneyDecision.tsx src/WildBunch.Web/src/tests/TravelPanel.test.tsx
git commit -m "refactor(web): remove npc type label and shorten encounter control labels

Remove encounter.kind span (could show backend values like 'npc';
encounter.message already provides player context). Rename 'Fight
bullets' to 'Bullets' and 'Bribe amount' to 'Bribe'. Update test
label matchers.

BUNCH-114"
```

---

## Task 6: Full validation and PR preparation

**Files:**
- No source changes — validation only

- [ ] **Step 1: Run full test suite**

Run: `cd src/WildBunch.Web && npm test`
Expected: ALL PASS

- [ ] **Step 2: Run full build (typecheck + vite build)**

Run: `cd src/WildBunch.Web && npm run build`
Expected: PASS (no TypeScript errors, no unused imports)

- [ ] **Step 3: Run dotnet build and test (backend unaffected, but verify no breakage)**

Run: `dotnet build`
Expected: PASS

Run: `dotnet test`
Expected: PASS

- [ ] **Step 4: Browser/manual playtest of affected surfaces**

The Linear issue explicitly requires a manual playtest to verify the UI is cleaner and more player-friendly. Start the dev server and exercise each affected surface, capturing screenshot evidence under `.agents/superpowers/output/screenshots/` (git-ignored).

Run: `cd src/WildBunch.Web && npm run dev`

Playtest checklist (verify each surface no longer shows the removed backend details):

1. **Store offers** (visit a town store): confirm no Town/Town id/Catalog/Source StatList; offer cards still show name, price, kind, availability, buy control.
2. **Inventory** (open inventory panel): confirm no Loadout items count, no Capabilities count, no capability flag tags; items without horse/canteen state render no detail line (no "No travel state"); items with horse/canteen state still show that detail.
3. **Trail ledger** (start or resume a journey): confirm no Delay margin, no Ride-day distance, no Canteen needed; Route, Travel mode, Remaining days/distance, Horse, Water pressure, Terrain, Water feature, Risk still present.
4. **Travel diary** (advance at least one trail day with a trail event and/or encounter): confirm no delta meta (Wallet Δ, Food Δ, etc.) on trail events or resolutions; day meta shows current values only (no parentheticals); no Horse feed or Ammo entries in day meta.
5. **Encounter UI** (trigger a journey encounter with fight/bribe choices): confirm no encounter kind label; fight control labeled "Bullets"; bribe control labeled "Bribe".

Capture screenshots of each surface and save to `.agents/superpowers/output/screenshots/bunch-114/` (e.g. `store-offers.png`, `inventory.png`, `trail-ledger.png`, `travel-diary.png`, `encounter.png`). These are git-ignored and cited in the PR/return notes by filename.

If screenshot capture is unavailable in the execution environment, state that explicitly in the PR body and provide a written playtest checklist with pass/fail per surface instead.

- [ ] **Step 5: Verify worktree is clean**

Run: `git status`
Expected: clean working tree (all changes committed; screenshots are git-ignored and should not appear)

- [ ] **Step 6: Push branch and create PR**

```bash
git push -u origin harleydbartles/bunch-114-remove-backend-implementation-details-from-player-facing-ui
```

Create PR with `gh pr create`:
- Title: `BUNCH-114: Remove backend implementation details from player-facing UI`
- Body: Summary of all 5 component cleanups + test updates, with test/build evidence and playtest checklist results (citing screenshot filenames or explaining screenshot unavailability).

- [ ] **Step 7: Update Linear issue with route state**

Post a comment on BUNCH-114 with:
- Plan path: `.agents/superpowers/plans/2026-06-30-bunch-114-remove-backend-implementation-details-from-player-facing-ui.md`
- PR URL
- Status: plan approved and ready for execution, or execution complete (depending on whether this plan is preflight-only or executed inline)
