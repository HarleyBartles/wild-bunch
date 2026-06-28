# Task 4 Report: Prove React owns the final confirmation and game creation

## What I implemented

This was a test-coverage task. The architecture was already in place from Tasks 1-3. I added falsifiable boundary tests proving React owns game creation and Phaser does not own game truth.

### PhaserMapHost.test.tsx — new `PhaserMapHost truth boundary` describe block (4 tests)

1. **`does not give the scene access to createGame or any API function`** — asserts the scene instance has no `createGame`, `api`, `requestJson`, `fetch`, or `getStartingTownMap` property. Proves the Phaser scene constructor only receives `mapData`/`selectedTownId`/`onTownSelected` and has no API client.
2. **`selectTown only calls onTownSelected and does not call fetch or any API`** — spies on `globalThis.fetch`, calls `scene.selectTown("t-town")`, asserts `onTownSelected` called once with `"t-town"` and `fetch` never called. Proves `selectTown` emits intent only, no network side effects.
3. **`does not mutate selectedTownId when selectTown is called`** — calls `selectTown` on a scene with `selectedTownId: null`, asserts the field stays `null`. Proves Phaser does not store selected-town truth mutably.
4. **`receives selectedTownId as a readonly prop, not as stored truth`** — renders with `selectedTownId: "dust-fork"`, calls `selectTown("t-town")`, asserts `selectedTownId` remains `"dust-fork"`. Proves the field is a readonly prop, not Phaser-owned mutable truth.

### StartFlow.test.tsx — extended with 3 new tests + phaser mock state

Added `phaserMockState` (hoisted) to capture the Phaser `Game` constructor's config so tests can drive the mounted scene's `selectTown` directly. Added `phaserMockState.games.length = 0` reset in `afterEach`.

1. **`calls createGame with the correct startingTownId when a town is selected via the map host`** — drives the full React-owned chain: navigate to town step → wait for Phaser map to mount → call `scene.selectTown("dust-fork")` (the map host's `onTownSelected`) → assert `createGame` called once with `startingTownId: "dust-fork"` and `playerName: "Ranger Vale"`. Proves the end-to-end React-owned confirmation path: map intent → `onTownSelected` → `handleStartWithTown` → `createGame`.
2. **`mounts the Phaser map but does not call createGame until a town is selected`** — navigates to town step, waits for Phaser `Game` constructor to fire (map mounts), asserts `createGame` NOT called, then calls `scene.selectTown("t-town")` and asserts `createGame` called once. Proves the Phaser mock's `Game` constructor is called (map mounts) but `createGame` is not called until a town is selected.
3. **`does not call createGame when the map mounts and no town is selected`** — navigates to town step, waits for Phaser mount, asserts `createGame` NOT called. Proves no premature game creation from map mount alone.

## Inspection results (no drift found)

### `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- Imports: `useEffect`, `useRef` (react), `styled` (styled-components), `Phaser` (phaser), `StartingTownMapDto` (api/types — type-only).
- NO API imports. NO `createGame` reference. NO `fetch` calls. NO `requestJson`. NO `wildBunchApi` import.
- `StartingTownMapScene` constructor receives only `mapData`, `selectedTownId`, `onTownSelected`.
- `selectedTownId` is `public readonly`.
- `selectTown` validates `town?.selectable` (backend-provided flag) and calls `onTownSelected` — no eligibility calculation, no truth storage.
- **Verdict: clean. No drift.**

### `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- `startGameMutation` (line 131-145) uses `mutationFn: (request: StartGameRequest) => createGame(request)`. This is the React-owned mutation that calls `createGame` (`POST /api/games`).
- **Verdict: clean. No drift.**

### `src/WildBunch.Web/src/api/wildBunchApi.ts`
- `createGame` (line 24-29) is the only function that POSTs to `/api/games` (the game-creation root). All other POSTs target sub-routes (`/api/games/{id}/travel`, `/api/games/{id}/archive`, etc.).
- **Verdict: clean. No drift.**

## What I tested + actual command output

Run from `src/WildBunch.Web/`:

### `npm run typecheck`
```
> wildbunch-web@0.0.0 typecheck
> tsc --noEmit
```
Exit code 0. Pass.

### `npm test`
```
Test Files  21 passed (21)
     Tests  173 passed (173)
  Duration  8.22s
```
Relevant files:
- `src/tests/PhaserMapHost.test.tsx` — 11 tests passed (7 existing + 4 new boundary tests)
- `src/tests/StartFlow.test.tsx` — 10 tests passed (7 existing + 3 new React-owned confirmation proofs)

Exit code 0. All tests pass.

## Files changed

- `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx` — added `PhaserMapHost truth boundary` describe block (4 tests)
- `src/WildBunch.Web/src/tests/StartFlow.test.tsx` — added `phaserMockState` hoisted state, `afterEach` reset, 3 new React-owned confirmation tests
- `.agents/superpowers/sdd/progress.md` — marked Task 4 complete, added completion log entry
- `.agents/superpowers/sdd/task-4-report.md` — this report

## Self-review findings

- The tests prove behavior and safety, not implementation trivia: they assert the absence of API surface on the scene, the absence of fetch on intent emission, the immutability of `selectedTownId`, and the end-to-end React-owned confirmation chain.
- The `selectTown` non-selectable guard is already covered by existing tests in `PhaserMapHost.test.tsx` (the "does not emit onTownSelected for a non-selectable town" test), proving Phaser does not decide eligibility — it only reads the backend-provided `selectable` flag.
- No comments added to test files (per binding rule). Tests follow existing code style (vitest, `vi.fn()`, `waitFor`, `userEvent`).
- No implementation files were modified — this is purely a test-coverage task, as scoped.
- The Phaser mock captures `config.scene` so tests can drive `selectTown` directly, mirroring how the real map host wires the scene.

## Concerns

None. The architecture was already clean from Tasks 1-3; these tests lock in the boundary as falsifiable proof. All validation passes.
