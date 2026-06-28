### Task 4: Prove React owns the final confirmation and game creation

**Files:**
- Inspect: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Inspect: `src/WildBunch.Web/src/api/types.ts`
- Inspect: `src/WildBunch.Web/src/api/wildBunchApi.ts`
- Inspect: `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
- Create or modify: `src/WildBunch.Web/src/tests/StartFlow.test.tsx` or the BUNCH-102 start-flow test surface
- Create or modify: `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`

**Interfaces:**
- Consumes: the React-selected starting town and the existing `startNewGame` mutation from BUNCH-102.
- Produces: proof that the final `POST /api/games` call still comes from React-owned confirmation, not Phaser.

- [ ] **Step 1: Verify the selected-town request and command seams already exist from BUNCH-102.**

Do not re-add `StartingTownId` to the request chain; just confirm the upstream seam and reuse it.

- [ ] **Step 2: Keep Phaser out of the game-creation path.**

React owns the selection state and the confirm action. Phaser must not call `POST /api/games`.

- [ ] **Step 3: Add tests proving the final confirmation still happens through the normal start flow.**

The map can select a town, but the game should only start after React-owned confirmation.

- [ ] **Step 4: Add falsifiable proof that Phaser does not own game truth.**

Tests should prove Phaser does not call `POST /api/games`, does not decide eligibility, does not store selected-town truth, and does not bypass the React-owned final confirmation.

## Global Constraints (binding for this task)

- `GameSession` remains the live-play aggregate root; Phaser must not own gameplay truth.
- The Phaser layer is presentation/input only. It may emit `townSelected` intent, but it must not calculate legal moves, start eligibility, or route truth.
- React owns the selection state and the confirm action. Phaser must not call `POST /api/games`.
- Do not add comments unless asked. Follow existing code style.

## Task 3 output (already landed on this branch)

Task 3 (commits `412c6db` + `4f48590`) added:
- `PhaserMapHost.tsx` — Phaser scene receives `mapData`/`selectedTownId`/`onTownSelected` props; emits intent via callback; does NOT call any API. The scene has a `selectTown(townId)` method that validates `selectable` and calls `onTownSelected`.
- `StartingTownStep.tsx` — uses `getStartingTownMap` query, renders `PhaserMapHost`, keeps button fallback (filtered to selectable towns), calls `onSelectTown` for both map and buttons.
- `PreSessionSurface.tsx` (unchanged from BUNCH-102) — `handleStartWithTown(townId)` → `flow.setSelectedTownId` → `flow.goToStep("creating")` → `startNewGame(request)`. This is the React-owned confirmation path.
- `StartFlow.test.tsx` — already mocks `getStartingTownMap` + `phaser`, primes map data, and asserts `createGame` is called with `startingTownId` after selecting a town.

## What this task adds

This is primarily a **test-coverage task** to add falsifiable proof that:
1. The final `POST /api/games` (`createGame`) call comes from React-owned confirmation, not Phaser.
2. Phaser does not call `createGame` / `POST /api/games`.
3. Phaser does not decide eligibility (the `selectable` flag comes from the backend, not Phaser).
4. Phaser does not store selected-town truth (it receives `selectedTownId` as a prop, emits intent, stores nothing).
5. The game only starts after React-owned confirmation (selecting a town on the map → `onSelectTown` → `handleStartWithTown` → `createGame`).

### Test additions

In `PhaserMapHost.test.tsx` (or a new `PhaserMapHostTruthBoundary.test.tsx` if cleaner):
- Assert the Phaser scene instance does NOT have access to `createGame` or any API function (verify the scene constructor only receives `mapData`, `selectedTownId`, `onTownSelected` — no API client).
- Assert `selectTown` only calls `onTownSelected` and does NOT call any fetch/API.
- Assert the scene does not store selected-town truth mutably (the `selectedTownId` field is `readonly`).

In `StartFlow.test.tsx` (extend existing tests):
- Add a test that proves selecting a town on the map (via the map host's `onTownSelected`) triggers `createGame` with the correct `startingTownId` — i.e. the full React-owned chain works end-to-end.
- Add a test that proves `createGame` is NOT called if the map host mounts but no town is selected (no premature game creation).
- If practical, add a test asserting the Phaser mock's `Game` constructor is called (map mounts) but `createGame` is not called until a town is selected.

### Inspection (no changes needed unless drift found)

- `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts` — verify `startNewGame` is the React-owned mutation that calls `createGame` (`POST /api/games`).
- `src/WildBunch.Web/src/api/wildBunchApi.ts` — verify `createGame` is the only function that POSTs to `/api/games`.
- `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx` — verify no API imports, no `createGame` reference, no fetch calls.

## Validation

Run from `src/WildBunch.Web/`:
- `npm run typecheck` — must pass
- `npm test` — must pass (all tests including new ones)

## Architecture rules (binding)

- Phaser must not call `POST /api/games`, must not decide eligibility, must not store selected-town truth.
- React owns the selection state and the confirm action.
- Do not add comments. Follow existing code style.
- Tests should prove behavior and safety, not implementation trivia.
