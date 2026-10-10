import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { StartFlowPhase, type GameSessionDto } from "../api/types";
import { useGameSession } from "../state/useGameSession";
import { createSession } from "./test-utils/factories";
import { Hud } from "../shell/Hud";
import { InventoryPanel } from "../components/InventoryPanel";

vi.mock("../state/useGameSession", () => ({
  useGameSession: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function renderHud(session: GameSessionDto) {
  vi.mocked(useGameSession).mockReturnValue({
    session,
    currentTown: session.player.currentTownId
      ? (session.world.towns[0] as ReturnType<typeof useGameSession>["currentTown"])
      : null,
    cockpitMode: "town",
  } as unknown as ReturnType<typeof useGameSession>);

  render(<Hud onOpenJournal={vi.fn()} onOpenGameSettings={vi.fn()} />);
}

describe("Hud", () => {
  it("omits gameplay state while the setup phase has no health or inventory", () => {
    const session = {
      ...createSession(),
      startFlowPhase: StartFlowPhase.PrologueViewed,
      player: { name: "Ruth", currentTownId: null, health: null },
      inventory: null,
    } satisfies GameSessionDto;

    renderHud(session);

    expect(screen.queryByRole("banner", { name: "Game status" })).not.toBeInTheDocument();
    expect(screen.queryByText("Health")).not.toBeInTheDocument();
    expect(screen.queryByText("Cash")).not.toBeInTheDocument();
  });

  it("omits the inventory panel when the server has not established inventory", () => {
    const { container } = render(<InventoryPanel inventory={null} />);

    expect(container).toBeEmptyDOMElement();
  });

  it("shows gameplay state once the game has started", () => {
    renderHud(createSession());

    expect(screen.getByRole("banner", { name: "Game status" })).toBeInTheDocument();
    expect(screen.getByText("9")).toBeInTheDocument();
    expect(screen.getByText("$14.00")).toBeInTheDocument();
  });
});
