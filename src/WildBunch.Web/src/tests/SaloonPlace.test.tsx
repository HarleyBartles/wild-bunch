import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { GameSessionContext, GameSessionProvider } from "../state/GameSessionProvider";
import { SaloonPlace } from "../flow/places/SaloonPlace";
import { getAvailableActions, getGame, getJournal } from "../api/wildBunchApi";
import type { GameSessionDto, JournalDto, WantedPosterDto } from "../api/types";
import { gameSessionQueryKey } from "../state/gameSessionQuery";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => mockNavigate,
}));

vi.mock("../api/wildBunchApi", () => ({
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  archiveGame: vi.fn(),
  buyStoreItem: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  gatherLocalGossip: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  lookAroundSaloon: vi.fn(),
  markPrologueViewed: vi.fn(),
  previewTravel: vi.fn(),
  readWantedPosters: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  setupGame: vi.fn(),
  startGameWithTown: vi.fn(),
  travel: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

const poster: WantedPosterDto = {
  posterId: "poster-mira",
  targetDisplayName: "Mira Cline",
} as WantedPosterDto;

const otherPoster: WantedPosterDto = {
  posterId: "poster-ada",
  targetDisplayName: "Ada Vale",
} as WantedPosterDto;

function createSession(wantedPosters: WantedPosterDto[] = []): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 4,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: { towns: [{ id: "t-town", name: "Tumbleweed" }], trails: [] },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
      discoveredSuspects: [],
      caseBoard: { warrants: [], clues: [] },
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
      },
    },
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: null,
    travelDiary: null,
    logEntries: [],
    activeSaloonPersonOfInterest: {
      descriptor: "a stranger with a raven-feather pin",
      kind: 1,
    },
    wantedPosters,
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
      caseBoard: { warrants: [], clues: [] },
      knownClues: [],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  };
}

function renderProviderBackedSaloon() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
  vi.mocked(getGame).mockResolvedValue(createSession());
  vi.mocked(getAvailableActions).mockResolvedValue([]);
  vi.mocked(getJournal).mockResolvedValue(createJournal());

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SaloonPlace />
      </GameSessionProvider>
    </QueryClientProvider>,
  );

  return queryClient;
}

function StatefulSaloon() {
  const [declaredIdentity, setDeclaredIdentity] = useState("");
  const [notice, setNotice] = useState("");
  const value = {
    session: {
      activeSaloonPersonOfInterest: { descriptor: "a stranger with a raven-feather pin" },
    },
    wantedPosters: [poster],
    declaredWantedIdentityHandle: declaredIdentity,
    setDeclaredWantedIdentityHandle: setDeclaredIdentity,
    loading: false,
    busyMode: "idle",
    gameId: "game-1",
    selectedWantedPoster: declaredIdentity ? poster : null,
    canLookAroundSaloon: false,
    canGatherLocalGossip: false,
    canConfrontSaloonPersonOfInterest: Boolean(declaredIdentity),
    handleLookAroundSaloon: vi.fn(),
    handleGatherLocalGossip: vi.fn(),
    handleConfrontSaloonPersonOfInterest: () =>
      setNotice("They refuse to be taken in without a gun."),
    notice,
    error: "",
  } as unknown as React.ContextType<typeof GameSessionContext>;

  return (
    <GameSessionContext.Provider value={value}>
      <SaloonPlace />
    </GameSessionContext.Provider>
  );
}

describe("SaloonPlace", () => {
  it("requires the player to declare an identity and displays the server outcome", async () => {
    const user = userEvent.setup();
    render(<StatefulSaloon />);

    const identity = screen.getByRole("combobox", { name: "Declare wanted identity" });
    const takeIn = screen.getByRole("button", {
      name: /Take a stranger with a raven-feather pin to sheriff/,
    });

    expect(identity).toHaveValue("");
    expect(takeIn).toBeDisabled();

    await user.selectOptions(identity, "poster-mira");

    const declaredTakeIn = screen.getByRole("button", {
      name: "Take a stranger with a raven-feather pin to sheriff as Mira Cline",
    });
    expect(declaredTakeIn).toBeEnabled();

    await user.click(declaredTakeIn);

    expect(screen.getByText("They refuse to be taken in without a gun.")).toBeInTheDocument();
  });

  it("does not imply an identity when the session query reveals wanted posters", async () => {
    const user = userEvent.setup();
    const queryClient = renderProviderBackedSaloon();

    await waitFor(() => expect(vi.mocked(getGame)).toHaveBeenCalledWith("game-1"));
    act(() => {
      queryClient.setQueryData(gameSessionQueryKey("game-1"), createSession([poster]));
    });

    const identity = await screen.findByRole("combobox", { name: "Declare wanted identity" });
    const takeIn = screen.getByRole("button", {
      name: /Take a stranger with a raven-feather pin to sheriff/,
    });
    expect(identity).toHaveValue("");
    expect(takeIn).toBeDisabled();

    await user.selectOptions(identity, poster.posterId);
    expect(
      screen.getByRole("button", {
        name: "Take a stranger with a raven-feather pin to sheriff as Mira Cline",
      }),
    ).toBeEnabled();

    act(() => {
      queryClient.setQueryData(gameSessionQueryKey("game-1"), createSession([otherPoster]));
    });

    await waitFor(() => expect(identity).toHaveValue(""));
    expect(takeIn).toBeDisabled();
  });
});
