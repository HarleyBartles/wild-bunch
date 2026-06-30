# BUNCH-117: Improve Player Feedback and Notifications — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three player-feedback UX issues: (1) Store purchases show no confirmation, (2) the archive notice persists into a new game, and (3) the Game Settings modal stays open after a successful Start Over archive.

**Architecture:** All three fixes are frontend-only changes in `src/WildBunch.Web`. Issue 1 is a missing notice/error banner render in `StorePlace.tsx` — the purchase handler already calls `setNotice(result.message)` but the store surface never displays it. Issue 2 is a missing `setNotice("")` clear in `setupGameMutation.onSuccess`. Issue 3 is a missing `onOpenOverlay(null)` call after a successful archive in `GlobalOverlays.tsx`. No backend, domain, or persistence changes.

**Tech Stack:** React 18, styled-components, TanStack Query/Router, Vitest, React Testing Library.

## Global Constraints

- Use `styled-components` for all component styling — no plain CSS classes in `className`.
- Reference design tokens via `var(--token-name)`.
- Re-use shared primitives from `src/components/ui/sharedStyled.tsx` (`FlowNotice`, `FlowError`, `FlowSurface`, `BackButton`, etc.).
- All player-facing surfaces must follow `src/WildBunch.Web/.agents/unslop/play-surface-ui.md` — no dashboard drift, no product chrome copy, no frontend-invented truth.
- Tests use Vitest + React Testing Library + jsdom. Mock `../api/wildBunchApi` and `../dev/devApi` per existing test patterns.
- Validation: `npm test` (runs `vitest run`), `npm run typecheck` (runs `tsc --noEmit`).

---

## File Structure

| File | Responsibility | Action |
| --- | --- | --- |
| `src/WildBunch.Web/src/flow/places/StorePlace.tsx` | Store place surface — render store offers, inventory, and now the notice/error banner | Modify |
| `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts` | Session hook — owns notice/error state and all mutations | Modify |
| `src/WildBunch.Web/src/flow/GlobalOverlays.tsx` | Global overlay layer — owns the Game Settings modal and Start Over confirm dialog | Modify |
| `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx` | New test file — verifies store purchase notice renders and archive notice clears on new game setup | Create |
| `src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx` | Existing test — add test verifying Game Settings overlay closes after successful archive | Modify |

---

### Task 1: Add notice/error banner to StorePlace

The store purchase handler in `GameSessionProvider.tsx` already calls `setNotice(result.message)` on success and `setError(result.message)` on failure. Every other place surface (SheriffPlace, SaloonPlace, TravelPrepSurface, PreSessionSurface) renders these via `FlowNotice`/`FlowError`. StorePlace is the only place surface that omits the banner, so the purchase confirmation is set but never shown.

**Files:**
- Modify: `src/WildBunch.Web/src/flow/places/StorePlace.tsx`
- Test: `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx` (created in Task 3)

**Interfaces:**
- Consumes: `notice` and `error` from `useGameSession()` (already returned by `useCurrentGameSession`, already threaded through `GameSessionProvider`).
- Produces: StorePlace now renders the same `{notice ? <FlowNotice>{notice}</FlowNotice> : null}` / `{error ? <FlowError>{error}</FlowError> : null}` pair used by SheriffPlace and SaloonPlace.

- [ ] **Step 1: Add notice and error to the useGameSession destructure in StorePlace**

In `src/WildBunch.Web/src/flow/places/StorePlace.tsx`, update the `useGameSession()` destructure to include `notice` and `error`:

```tsx
export function StorePlace({ onLeave }: StorePlaceProps) {
  const { session, storeOffers, storeOffersLoading, loading, handleBuyOffer, notice, error } = useGameSession();
```

- [ ] **Step 2: Add FlowNotice and FlowError imports**

Update the import from `../../components/ui/sharedStyled` to include `FlowNotice` and `FlowError`:

```tsx
import { FlowSurface, BackButton, FlowNotice, FlowError } from "../../components/ui/sharedStyled";
```

- [ ] **Step 3: Render the notice/error banner inside PlaceBody**

Add the banner pair at the end of `<PlaceBody>`, after `<InventoryPanel>`:

```tsx
      <PlaceBody>
        <StoreOffersPanel
          storeOffers={storeOffers}
          loading={storeOffersLoading}
          busy={loading}
          onBuyOffer={handleBuyOffer}
        />
        <InventoryPanel inventory={session.inventory} />
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </PlaceBody>
```

- [ ] **Step 4: Run typecheck to verify the change compiles**

Run: `cd src/WildBunch.Web && npm run typecheck`
Expected: PASS with no errors.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/flow/places/StorePlace.tsx
git commit -m "BUNCH-117: Show purchase notice/error banner on StorePlace

StorePlace was the only place surface that did not render the notice/error
banner. The purchase handler already set the notice via setNotice, but the
store surface never displayed it. Add FlowNotice/FlowError following the
same pattern as SheriffPlace, SaloonPlace, and TravelPrepSurface."
```

---

### Task 2: Clear notice state when starting a new game

After archiving a playthrough, `archivePlaythroughMutation.onSuccess` sets `setNotice("Your old playthrough has been archived. Start a new one when you are ready.")`. When the player then creates a new game via `setupGameMutation`, only `setError("")` is cleared — the archive notice persists through the prologue and town-selection steps. Fix: clear the notice in `setupGameMutation.onSuccess`.

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts:142-153`

**Interfaces:**
- Consumes: `setNotice` (already in scope from `useState`).
- Produces: `setupGameMutation.onSuccess` now clears both `setError("")` and `setNotice("")` so stale archive notices do not persist into the new game setup flow.

- [ ] **Step 1: Add setNotice("") to setupGameMutation.onSuccess**

In `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`, find the `setupGameMutation` definition (around line 142) and add `setNotice("")` alongside the existing `setError("")` in `onSuccess`:

```tsx
  const setupGameMutation = useMutation({
    mutationFn: (request: SetupGameRequest) => setupGame(request),
    onSuccess: async (createdSession) => {
      window.localStorage.setItem(storageKey, createdSession.id);
      setStoredGameId(createdSession.id);
      setNotice("");
      setError("");
      await invalidateGameQueries(createdSession.id);
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to complete setup.");
    },
  });
```

- [ ] **Step 2: Run typecheck to verify the change compiles**

Run: `cd src/WildBunch.Web && npm run typecheck`
Expected: PASS with no errors.

- [ ] **Step 3: Run existing tests to verify no regressions**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS — all existing tests still pass. The `StartOverConfirmation` test "shows the success notice after archiving" still passes because the notice is set by the archive mutation and displayed by PreSessionSurface before any new setup occurs.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Web/src/hooks/useCurrentGameSession.ts
git commit -m "BUNCH-117: Clear notice state when starting a new game

The archive notice ('Your old playthrough has been archived...') persisted
into the new game setup flow because setupGameMutation.onSuccess only
cleared the error state, not the notice state. Add setNotice(\"\") to
onSuccess so stale archive notices do not carry through prologue and
town-selection steps."
```

---

### Task 3: Close Game Settings overlay after successful archive

After a successful Start Over archive, the ConfirmDialog closes but the parent Game Settings modal stays open. The player sees an empty settings modal over the pre-session surface. Fix: close the Game Settings overlay after the archive succeeds.

**Files:**
- Modify: `src/WildBunch.Web/src/flow/GlobalOverlays.tsx:101-103`

**Interfaces:**
- Consumes: `onOpenOverlay` (prop, already in scope) and `archivePlaythrough` (from `useGameSession`).
- Produces: After a successful archive, both the ConfirmDialog and the Game Settings overlay close. The player lands on the pre-session surface with the archive notice visible.

- [ ] **Step 1: Update the onConfirm handler to close the parent overlay after archive**

In `src/WildBunch.Web/src/flow/GlobalOverlays.tsx`, find the `ConfirmDialog` `onConfirm` handler (around line 101) and add `onOpenOverlay(null)` after the archive resolves:

```tsx
        onConfirm={() => {
          void archivePlaythrough()
            .then(() => {
              setConfirmOpen(false);
              onOpenOverlay(null);
            })
            .catch(() => { /* onError already handled in mutation */ });
        }}
```

- [ ] **Step 2: Run typecheck to verify the change compiles**

Run: `cd src/WildBunch.Web && npm run typecheck`
Expected: PASS with no errors.

- [ ] **Step 3: Run existing tests to verify no regressions**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS — all existing tests still pass.

The existing test "clears localStorage and storedGameId on Confirm, returning to the start flow" still passes: it checks that the Game Settings button becomes disabled and the pre-session heading appears. The overlay closing is compatible — the button is disabled because the session is gone.

The existing test "leaves session state unchanged when Cancel is clicked" still passes: it only tests the cancel path, which is unchanged.

The existing test "shows the success notice after archiving" still passes: the notice text is rendered by PreSessionSurface (the session is gone after archive), which is independent of whether the Game Settings overlay is open or closed.

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.Web/src/flow/GlobalOverlays.tsx
git commit -m "BUNCH-117: Close Game Settings overlay after successful archive

The Game Settings modal stayed open after a successful Start Over archive,
leaving an empty settings modal over the pre-session surface. Close the
parent overlay alongside the confirm dialog after the archive resolves."
```

---

### Task 4: Add test coverage for all three fixes

Add tests that verify each fix behaviorally. These tests follow the existing test pattern in `StartOverConfirmation.test.tsx` and `GameSettingsOverlay.test.tsx`: mock the API, prime a session, render the full shell, and drive the UI with `userEvent`.

**Files:**
- Create: `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx`

**Interfaces:**
- Consumes: The existing test helpers and mock patterns from `StartOverConfirmation.test.tsx`.
- Produces: Three new test cases — store purchase notice renders, archive notice clears on new game setup, Game Settings overlay closes after archive.

- [ ] **Step 1: Create StorePlaceFeedback.test.tsx with the store purchase notice test**

Create `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx`. This test verifies that after a successful store purchase, the purchase confirmation notice appears on the store surface. It also verifies that the archive notice is cleared when a new game is set up.

The test file follows the same mock/prime/render pattern as `StartOverConfirmation.test.tsx`. It needs a session with `BuySupplies` available so the store place card appears, and a store offer so the buy button is clickable.

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { router } from "../shell/router";
import { GameSessionProvider } from "../state/GameSessionProvider";
import {
  AvailableActionKind,
  type GameSessionDto,
  type JournalDto,
  type TownStoreOffersDto,
} from "../api/types";
import {
  archiveGame,
  buyStoreItem,
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  checkLocalRecords,
  followTelegraphLeads,
  gatherLocalGossip,
  inspectNoticeBoard,
  confrontSaloonPersonOfInterest,
  lookAroundSaloon,
  readWantedPosters,
  travel,
  setupGame,
  startGameWithTown,
  markPrologueViewed,
} from "../api/wildBunchApi";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../api/wildBunchApi", () => ({
  archiveGame: vi.fn(),
  buyStoreItem: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  setupGame: vi.fn(),
  startGameWithTown: vi.fn(),
  markPrologueViewed: vi.fn(),
}));

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
}));

const mockedArchiveGame = vi.mocked(archiveGame);
const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedBuyStoreItem = vi.mocked(buyStoreItem);
const mockedSetupGame = vi.mocked(setupGame);
const mockedStartGameWithTown = vi.mocked(startGameWithTown);
const mockedMarkPrologueViewed = vi.mocked(markPrologueViewed);
const mockedCheckLocalRecords = vi.mocked(checkLocalRecords);
const mockedInspectNoticeBoard = vi.mocked(inspectNoticeBoard);
const mockedConfrontSaloonPersonOfInterest = vi.mocked(confrontSaloonPersonOfInterest);
const mockedLookAroundSaloon = vi.mocked(lookAroundSaloon);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);
const mockedFollowTelegraphLeads = vi.mocked(followTelegraphLeads);
const mockedGatherLocalGossip = vi.mocked(gatherLocalGossip);
const mockedTravel = vi.mocked(travel);
const mockedGetSessionAudit = vi.mocked(getSessionAudit);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  window.history.replaceState({}, "", "/");
});

function renderShell() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <RouterProvider router={router} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 3,
    player: {
      name: "Ruth",
      currentTownId: "t-town",
      health: 9,
    },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [],
    },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
      knownClues: [],
    },
    inventory: {
      wallet: { cash: 14 },
      items: [],
      horseState: null,
      canteenState: null,
      capabilities: {
        mountedTravelAvailable: false,
        horseUpkeepRequired: false,
        normalRouteWaterSecure: false,
        trailUtility: false,
        closeThreatAvailable: false,
        firearmThreatAvailable: false,
        gunfightCapable: false,
        revolverUsable: false,
        rifleUsable: false,
      },
    },
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: null,
    travelDiary: null,
    logEntries: [],
    activeSaloonPersonOfInterest: null,
    wantedPosters: [],
  };
}

function createJournal(): JournalDto {
  return {
    id: "game-1",
    status: 0,
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    currentTown: { id: "t-town", name: "Tumbleweed" },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
      caseSummary: "Find the culprit before the law closes in.",
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  };
}

function createStoreOffers(): TownStoreOffersDto {
  return {
    townId: "t-town",
    townName: "Tumbleweed",
    available: true,
    sourceNote: "General store",
    offers: [
      {
        vendorType: 0,
        itemKind: 0,
        displayName: "Canned beans",
        price: 1.5,
        availability: 0,
        sourceNote: "Shelf stock",
      },
    ],
  };
}

function primeMocks() {
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.BuySupplies, label: "Buy supplies" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
  mockedArchiveGame.mockResolvedValue(undefined);
  mockedBuyStoreItem.mockResolvedValue({
    success: true,
    message: "You bought 1 canned beans for $1.50.",
    currentSession: createSession(),
    journeyStatus: null,
    journey: null,
    trailEvent: null,
    travelDiary: null,
  });
  mockedSetupGame.mockResolvedValue(createSession());
  mockedStartGameWithTown.mockResolvedValue(createSession());
  mockedMarkPrologueViewed.mockResolvedValue(createSession());
  mockedReadWantedPosters.mockResolvedValue({
    success: true,
    message: "Read wanted posters",
    currentJournal: createJournal(),
    wantedPosters: [],
  });
  mockedInspectNoticeBoard.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedCheckLocalRecords.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedFollowTelegraphLeads.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedGatherLocalGossip.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedLookAroundSaloon.mockResolvedValue({ success: true, message: "ok", currentJournal: createJournal() });
  mockedConfrontSaloonPersonOfInterest.mockResolvedValue({
    success: true,
    message: "ok",
    outcome: 0,
    currentSession: createSession(),
    declaredWantedIdentityHandle: null,
    targetName: null,
    disposition: null,
    isAlive: null,
    isSecured: null,
    isCitizen: null,
    fineAmount: null,
    walletBefore: null,
    walletAfter: null,
    sessionChanged: false,
    personOfInterestKind: 0,
  });
  mockedTravel.mockResolvedValue({
    success: true,
    message: "Travelled",
    currentSession: createSession(),
    journeyStatus: null,
    journey: null,
    trailEvent: null,
    travelDiary: null,
  });
  mockedGetSessionAudit.mockResolvedValue({ sessionId: "game-1", entries: [] });
}

describe("Store purchase feedback", () => {
  it("shows the purchase confirmation notice on the store surface after a successful buy", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();

    // Wait for the town hub to render with the Store place card.
    const storeCard = await screen.findByRole("button", { name: /store/i });
    await user.click(storeCard);

    // The store surface should render with the buy button.
    const buyButton = await screen.findByRole("button", { name: /^buy$/i });
    await user.click(buyButton);

    // The purchase confirmation notice should appear.
    await waitFor(() => {
      expect(screen.getByText("You bought 1 canned beans for $1.50.")).toBeInTheDocument();
    });
  });
});

describe("Archive notice clears on new game setup", () => {
  it("clears the archive notice when a new game is set up", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();

    // Archive the playthrough via Game Settings.
    const hud = await screen.findByRole("banner", { name: /game status/i });
    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
    });
    await user.click(within(hud).getByRole("button", { name: /game settings/i }));
    const settingsOverlay = await screen.findByRole("dialog", { name: /game settings/i });
    await user.click(within(settingsOverlay).getByRole("button", { name: /start over/i }));
    const confirmDialog = await screen.findByRole("dialog", { name: /start over\?/i });
    await user.click(within(confirmDialog).getByRole("button", { name: /archive and start over/i }));

    // The archive notice should appear on the pre-session surface.
    await waitFor(() => {
      expect(
        screen.getByText("Your old playthrough has been archived. Start a new one when you are ready."),
      ).toBeInTheDocument();
    });

    // Now set up a new game. Enter a player name and continue.
    const nameInput = screen.getByLabelText(/name you go by/i);
    await user.type(nameInput, "Jesse");
    await user.click(screen.getByRole("button", { name: /ride out/i }));

    // Wait for setupGame to be called.
    await waitFor(() => {
      expect(mockedSetupGame).toHaveBeenCalled();
    });

    // The archive notice should be cleared — it should no longer be in the document.
    await waitFor(() => {
      expect(
        screen.queryByText("Your old playthrough has been archived. Start a new one when you are ready."),
      ).not.toBeInTheDocument();
    });
  });
});
```

- [ ] **Step 2: Run the new store feedback tests to verify they pass**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/StorePlaceFeedback.test.tsx`
Expected: PASS — both test cases pass.

If the "clears the archive notice" test fails because the setup flow step names or button labels differ, inspect `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx` for the exact label text and adjust the test selectors. The key assertion is that after `setupGame` is called, the archive notice text is no longer in the document.

- [ ] **Step 3: Add Game Settings overlay close test to StartOverConfirmation.test.tsx**

In `src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx`, add a new test inside the existing `describe("Start Over confirmation", ...)` block, after the "shows the success notice after archiving" test:

```tsx
  it("closes the Game Settings overlay after a successful archive", async () => {
    primeMocks();
    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderShell();

    const user = userEvent.setup();
    const confirmDialog = await openConfirmDialog(user);

    await user.click(within(confirmDialog).getByRole("button", { name: /archive and start over/i }));

    // The ConfirmDialog should close.
    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: /start over\?/i })).not.toBeInTheDocument();
    });

    // The Game Settings overlay should also close.
    await waitFor(() => {
      expect(screen.queryByRole("dialog", { name: /game settings/i })).not.toBeInTheDocument();
    });
  });
```

- [ ] **Step 4: Run the StartOverConfirmation tests to verify the new test passes**

Run: `cd src/WildBunch.Web && npx vitest run src/tests/StartOverConfirmation.test.tsx`
Expected: PASS — all tests including the new one pass.

- [ ] **Step 5: Run the full test suite to verify no regressions**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS — all tests pass.

- [ ] **Step 6: Run typecheck**

Run: `cd src/WildBunch.Web && npm run typecheck`
Expected: PASS with no errors.

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx
git commit -m "BUNCH-117: Add test coverage for player feedback fixes

Add StorePlaceFeedback.test.tsx covering: (1) store purchase notice
renders on the store surface after a successful buy, (2) archive notice
clears when a new game is set up. Add a test to StartOverConfirmation
verifying the Game Settings overlay closes after a successful archive."
```

---

## Self-Review

**1. Spec coverage:**
- Issue 1 (no purchase confirmation): Task 1 adds the notice/error banner to StorePlace. The purchase handler already sets the notice. ✓
- Issue 2 (persistent notification): Task 2 clears the notice in `setupGameMutation.onSuccess`. ✓
- Issue 3 (Game Settings modal UX — immediate fix: close parent modal after archive): Task 3 closes the Game Settings overlay after successful archive. The "expandable submenu" long-term option is explicitly out of scope per the issue's "Immediate fix" language and scope discipline. ✓
- Validation ("Run npm test"): Task 4 runs `npm test` and `npm run typecheck`. ✓
- Manual playtest: deferred to PR review — the plan covers automated test coverage for all three fixes.

**2. Placeholder scan:** No TBD, TODO, or "implement later" found. All code blocks contain actual implementation code. Test code includes full mock setup and assertions.

**3. Type consistency:** `notice` and `error` are already returned by `useCurrentGameSession` and threaded through `GameSessionProvider` — no new types needed. `FlowNotice` and `FlowError` are existing exports from `sharedStyled.tsx`. `onOpenOverlay` is an existing prop on `GlobalOverlays`. `setNotice` is an existing state setter in `useCurrentGameSession`.

## Out of Scope

- **HUD dropdown notifications (5-second expiry, click-to-dismiss, hover-to-pause):** The issue marks this as "Long-term". Not in this slice.
- **Expandable submenu for Game Settings:** The issue marks this as the ideal but offers "close parent modal after successful archive" as the immediate fix. This plan implements the immediate fix only.
- **Backend, domain, or persistence changes:** None needed — all three issues are frontend-only.
