import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { GameSessionContext } from "../state/GameSessionProvider";
import { SaloonPlace } from "../flow/places/SaloonPlace";
import type { WantedPosterDto } from "../api/types";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => mockNavigate,
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

const poster: WantedPosterDto = {
  posterId: "poster-mira",
  targetDisplayName: "Mira Cline",
} as WantedPosterDto;

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
      name: "Take a stranger with a raven-feather pin to sheriff",
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
});
