import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { useGameSession } from "../state/useGameSession";
import type { WantedPosterDto } from "../api/types";

const mocks = vi.hoisted(() => ({
  currentSession: {},
}));

vi.mock("../hooks/useCurrentGameSession", () => ({
  useCurrentGameSession: () => mocks.currentSession,
}));

vi.mock("../hooks/useTownStoreOffers", () => ({
  useTownStoreOffers: () => ({
    storeOffers: null,
    loading: false,
    refreshStoreOffers: vi.fn(),
  }),
}));

vi.mock("../api/wildBunchApi", () => ({ buyStoreItem: vi.fn() }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

const poster: WantedPosterDto = {
  posterId: "poster-mira",
  targetDisplayName: "Mira Cline",
} as WantedPosterDto;

function ContextReader() {
  const { selectedWantedPoster } = useGameSession();
  return <output>{selectedWantedPoster?.targetDisplayName ?? "No identity selected"}</output>;
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

  render(
    <Wrapper>
      <ContextReader />
    </Wrapper>,
  );
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
