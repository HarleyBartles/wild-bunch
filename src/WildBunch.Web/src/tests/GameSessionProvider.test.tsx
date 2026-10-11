import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { useGameSession } from "../state/useGameSession";
import type { TownStoreOffersDto, WantedPosterDto } from "../api/types";

const mocks = vi.hoisted(() => ({
  currentSession: {},
  getTownStoreOffers: vi.fn(),
  buyStoreItem: vi.fn(),
}));

let purchaseCompletion: Promise<void> | null = null;

vi.mock("../hooks/useCurrentGameSession", () => ({
  useCurrentGameSession: () => mocks.currentSession,
}));

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: mocks.buyStoreItem,
  getTownStoreOffers: mocks.getTownStoreOffers,
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  purchaseCompletion = null;
});

function createOffers(townId: string, townName: string): TownStoreOffersDto {
  return {
    townId,
    townName,
    offers: [{ itemKind: 0, displayName: "Beans", price: 1 }],
  };
}

const poster: WantedPosterDto = {
  posterId: "poster-mira",
  targetDisplayName: "Mira Cline",
} as WantedPosterDto;

function ContextReader() {
  const { selectedWantedPoster, storeOffers, handleBuyOffer } = useGameSession();
  return (
    <>
      <output>{selectedWantedPoster?.targetDisplayName ?? "No identity selected"}</output>
      <output aria-label="Current store">{storeOffers?.townName ?? "No offers"}</output>
      <button
        type="button"
        onClick={() => {
          purchaseCompletion = handleBuyOffer({ itemKind: 0, displayName: "Beans", price: 1 }, 1);
        }}
      >
        Buy beans
      </button>
    </>
  );
}

function renderProvider(identityHandle = "") {
  mocks.currentSession = {
    gameId: "game-1",
    currentTown: { id: "town-1" },
    wantedPosters: [poster],
    declaredWantedIdentityHandle: identityHandle,
    setNotice: vi.fn(),
    setError: vi.fn(),
    setSession: vi.fn(),
    reloadCurrentGame: vi.fn(),
  };

  function Wrapper({ children }: { children: ReactNode }) {
    return <GameSessionProvider>{children}</GameSessionProvider>;
  }

  renderWithClient(
    <Wrapper>
      <ContextReader />
    </Wrapper>,
  );
}

function renderWithClient(node: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(<QueryClientProvider client={queryClient}>{node}</QueryClientProvider>);
  return queryClient;
}

describe("GameSessionProvider wanted identity selection", () => {
  it("does not imply the first poster is selected when the player has declared nobody", () => {
    renderProvider();
    expect(screen.getByText("No identity selected")).toBeInTheDocument();
  });

  it("resolves the poster only after the player's declaration selects it", () => {
    renderProvider("poster-mira");
    expect(screen.getByText("Mira Cline")).toBeInTheDocument();
  });
});

describe("GameSessionProvider store offers", () => {
  it("does not refresh the selected store when an earlier game's purchase completes", async () => {
    const user = userEvent.setup();
    const sessionA = {
      gameId: "game-a",
      currentTown: { id: "town-a" },
      wantedPosters: [],
      declaredWantedIdentityHandle: "",
      setNotice: vi.fn(),
      setError: vi.fn(),
      setSession: vi.fn(),
      reloadCurrentGame: vi.fn(),
    };
    const sessionB = {
      ...sessionA,
      gameId: "game-b",
      currentTown: { id: "town-b" },
    };
    mocks.getTownStoreOffers.mockImplementation(async (_gameId: string, townId: string) =>
      townId === "town-a" ? createOffers(townId, "Town A") : createOffers(townId, "Town B"),
    );
    let resolvePurchase!: (result: unknown) => void;
    mocks.buyStoreItem.mockReturnValue(
      new Promise((resolve) => {
        resolvePurchase = resolve;
      }),
    );
    mocks.currentSession = sessionA;

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, staleTime: Infinity },
        mutations: { retry: false },
      },
    });
    const tree = () => (
      <QueryClientProvider client={queryClient}>
        <GameSessionProvider>
          <ContextReader />
        </GameSessionProvider>
      </QueryClientProvider>
    );
    const { rerender } = render(tree());

    await screen.findByText("Town A");
    await user.click(screen.getByRole("button", { name: "Buy beans" }));

    mocks.currentSession = sessionB;
    rerender(tree());
    await screen.findByText("Town B");
    const callsForTownB = () =>
      mocks.getTownStoreOffers.mock.calls.filter(([, townId]) => townId === "town-b").length;
    expect(callsForTownB()).toBe(1);

    await act(async () => {
      resolvePurchase({
        success: true,
        message: "Bought beans.",
        currentSession: { id: "game-a" },
      });
      await purchaseCompletion;
    });
    expect(callsForTownB()).toBe(1);
    expect(mocks.getTownStoreOffers).toHaveBeenNthCalledWith(1, "game-a", "town-a");
    expect(mocks.getTownStoreOffers).toHaveBeenNthCalledWith(2, "game-b", "town-b");

    mocks.currentSession = sessionA;
    rerender(tree());
    await waitFor(() => {
      expect(
        mocks.getTownStoreOffers.mock.calls.filter(([, townId]) => townId === "town-a"),
      ).toHaveLength(2);
    });
    expect(mocks.getTownStoreOffers).toHaveBeenLastCalledWith("game-a", "town-a");
    queryClient.clear();
  });
});
