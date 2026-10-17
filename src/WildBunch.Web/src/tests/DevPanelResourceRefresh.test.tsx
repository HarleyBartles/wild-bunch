import { QueryClient, QueryClientProvider, useQuery } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SessionDevPanel } from "../dev/panels/SessionDevPanel";
import { SaloonDevPanel } from "../dev/panels/SaloonDevPanel";
import { TravelDevPanel } from "../dev/panels/TravelDevPanel";
import { GameSessionContext } from "../state/GameSessionProvider";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";
import {
  forceDevDifficulty,
  forceSaloonOverride,
  forceTravelOverride,
  getSaloonDevContext,
  getSessionDevContext,
  getTravelDevContext,
} from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  clearSaloonOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
  forceDevDifficulty: vi.fn(),
  forceSaloonOverride: vi.fn(),
  forceTravelOverride: vi.fn(),
  getSaloonDevContext: vi.fn(),
  getSessionDevContext: vi.fn(),
  getTravelDevContext: vi.fn(),
  setDevEntropy: vi.fn(),
  getSessionAudit: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function Reader({
  queryKey,
  label,
  count,
}: {
  queryKey: readonly unknown[];
  label: string;
  count: { value: number };
}) {
  const { data } = useQuery({
    queryKey,
    queryFn: async () => {
      count.value += 1;
      return `${label} ${count.value}`;
    },
  });
  return <output>{data}</output>;
}

function renderWithReaders(
  panel: ReactNode,
  gameId: string,
  otherPanel: "session" | "saloon" | "travel",
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: Infinity } },
  });
  const otherCount = { value: 0 };
  const playerCount = { value: 0 };
  const otherKeys = {
    session: gameResourceQueryKeys.devSession,
    saloon: gameResourceQueryKeys.devSaloon,
    travel: gameResourceQueryKeys.devTravel,
  };
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <GameSessionContext.Provider value={{ gameId } as never}>
          {children}
          <Reader
            queryKey={otherKeys[otherPanel](gameId)}
            label="Other developer reader"
            count={otherCount}
          />
          <Reader
            queryKey={gameResourceQueryKeys.actions(gameId)}
            label="Player reader"
            count={playerCount}
          />
        </GameSessionContext.Provider>
      </QueryClientProvider>
    );
  }
  render(<Wrapper>{panel}</Wrapper>);
  return { otherCount, playerCount, queryClient };
}

describe("developer command resource refresh", () => {
  it("refreshes another developer panel and player readers after a session command", async () => {
    vi.mocked(getSessionDevContext).mockResolvedValue({
      sessionId: "game-session",
      status: "Active",
      gameDifficulty: "Standard",
      gameEntropy: "Classic",
      saltPosture: { mode: "Runtime", salt: "salt" },
      clock: { day: 1, turn: 0, timeOfDay: "Dawn" },
      currentTownId: "town",
      currentTownName: "Dodge",
      currentActionContext: "None",
      hasActiveJourney: false,
      seedCodeRetained: false,
      seedCodeText: null,
      travelRules: null,
    });
    vi.mocked(forceDevDifficulty).mockResolvedValue();
    const reads = renderWithReaders(<SessionDevPanel />, "game-session", "saloon");
    await screen.findByRole("button", { name: "Brutal" });
    await screen.findByText("Other developer reader 1");
    await screen.findByText("Player reader 1");
    await userEvent.setup().click(screen.getByRole("button", { name: "Brutal" }));
    await screen.findByText("Other developer reader 2");
    await screen.findByText("Player reader 2");
    expect(reads.otherCount.value).toBe(2);
    expect(reads.playerCount.value).toBe(2);
    reads.queryClient.clear();
  });

  it("refreshes another developer panel and player readers after a saloon command", async () => {
    vi.mocked(getSaloonDevContext).mockResolvedValue({
      sessionId: "game-saloon",
      currentActionContext: "Saloon",
      currentTownId: "town",
      currentTownName: "Dodge",
      sourceSpent: false,
      activeSaloonPoi: null,
      pendingDevOverride: null,
      hiddenTruth: null,
      citizenInfo: null,
      suspects: [],
    });
    vi.mocked(forceSaloonOverride).mockResolvedValue();
    const reads = renderWithReaders(<SaloonDevPanel />, "game-saloon", "travel");
    await screen.findByRole("button", { name: /force next poi/i });
    await screen.findByText("Other developer reader 1");
    await screen.findByText("Player reader 1");
    await userEvent.setup().click(screen.getByRole("button", { name: /force next poi/i }));
    await screen.findByText("Other developer reader 2");
    await screen.findByText("Player reader 2");
    expect(reads.otherCount.value).toBe(2);
    expect(reads.playerCount.value).toBe(2);
    reads.queryClient.clear();
  });

  it("refreshes another developer panel and player readers after a travel command", async () => {
    vi.mocked(getTravelDevContext).mockResolvedValue({
      sessionId: "game-travel",
      hasActiveJourney: true,
      journeyStatus: "Active",
      daysTravelled: 0,
      remainingDays: 1,
      pendingEncounterKind: null,
      pendingEncounterMessage: null,
      pendingFoeProfile: null,
      pendingDevOverride: null,
    });
    vi.mocked(forceTravelOverride).mockResolvedValue();
    const reads = renderWithReaders(<TravelDevPanel />, "game-travel", "session");
    await screen.findByRole("button", { name: /force override/i });
    await screen.findByText("Other developer reader 1");
    await screen.findByText("Player reader 1");
    await userEvent.setup().click(screen.getByRole("button", { name: /force override/i }));
    await screen.findByText("Other developer reader 2");
    await screen.findByText("Player reader 2");
    expect(reads.otherCount.value).toBe(2);
    expect(reads.playerCount.value).toBe(2);
    reads.queryClient.clear();
  });
});
