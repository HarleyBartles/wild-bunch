import { afterEach, describe, expect, it, vi } from "vitest";
import { act, cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { getAvailableActions, getGame, getJournal } from "../api/wildBunchApi";
import { JournalSurface } from "../components/JournalSurface";
import { useGameSessionQueries } from "../hooks/useGameSessionQueries";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";
import { createJournal, createSession } from "./test-utils/factories";

vi.mock("../api/wildBunchApi", () => ({
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
}));

const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetGame = vi.mocked(getGame);
const mockedGetJournal = vi.mocked(getJournal);

afterEach(() => {
  cleanup();
  vi.resetAllMocks();
});

function QueryBackedJournal() {
  const game = useGameSessionQueries("game-1");

  return (
    <JournalSurface
      journal={game.journal}
      loading={game.journalLoading}
      refreshing={game.journalRefreshing}
      error=""
      readError={game.journalError}
      onRetry={game.retryJournal}
    />
  );
}

describe("JournalSurface", () => {
  it("renders grouped journal entries with the current town name", () => {
    const journal = createJournal({
      clock: { day: 5, turn: 2, timeOfDay: "Morning" },
      currentTown: { id: "t-town", name: "Tumbleweed" },
      logEntries: [
        { kind: 0, message: "The hunt begins in Tumbleweed.", day: 1, turn: 0 },
        { kind: 1, message: "Travelled to Red Mesa", day: 5, turn: 1 },
        { kind: 2, message: "Found a public lead", day: 5, turn: 2 },
        { kind: 2, message: "Bought food", day: 6, turn: 0 },
      ],
    });

    render(<JournalSurface journal={journal} loading={false} error="" />);

    expect(
      screen.getByRole("heading", { name: "Day 5, Morning in Tumbleweed" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Day 1")).toBeInTheDocument();
    expect(screen.getByText("Day 5")).toBeInTheDocument();
    expect(screen.getByText("Day 6")).toBeInTheDocument();
    expect(screen.getByText("Started out in Tumbleweed.")).toBeInTheDocument();
    expect(screen.queryByText("The hunt begins in Tumbleweed.")).not.toBeInTheDocument();
    expect(screen.getByText("Travelled to Red Mesa")).toBeInTheDocument();
    expect(screen.getByText("Found a public lead", { selector: "p" })).toBeInTheDocument();
    expect(screen.getByText("Bought food")).toBeInTheDocument();
    expect(screen.queryByText("t-town")).not.toBeInTheDocument();
  });

  it("renders before the player has chosen a starting town", () => {
    const journal = createJournal({ currentTown: null, logEntries: [] });

    render(<JournalSurface journal={journal} loading={false} error="" />);

    expect(screen.getByRole("heading", { name: "Day 5, Evening" })).toBeInTheDocument();
    expect(screen.getByText("No notes yet. The trail is still being written.")).toBeInTheDocument();
  });

  it("keeps the last journal visible when a refresh fails and offers retry", async () => {
    const journal = createJournal({
      currentTown: { id: "t-town", name: "Tumbleweed" },
      logEntries: [{ kind: 2, message: "Found a public lead", day: 5, turn: 2 }],
    });
    const onRetry = vi.fn();

    render(
      <JournalSurface
        journal={journal}
        loading={false}
        error=""
        readError="Could not read the journal."
        onRetry={onRetry}
      />,
    );

    expect(screen.getAllByText("Found a public lead").length).toBeGreaterThan(0);
    expect(screen.getByRole("alert")).toHaveTextContent("Could not read the journal.");
    await userEvent.setup().click(screen.getByRole("button", { name: /try again/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("keeps a cached journal visible when its active query refetch fails", async () => {
    const lastJournal = createJournal({
      logEntries: [{ kind: 2, message: "Last known journal entry", day: 5, turn: 2 }],
    });
    const recoveredJournal = createJournal({
      logEntries: [{ kind: 2, message: "Recovered journal entry", day: 6, turn: 1 }],
    });
    mockedGetAvailableActions.mockResolvedValue([]);
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetJournal.mockResolvedValue(lastJournal);
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const user = userEvent.setup();

    render(
      <QueryClientProvider client={queryClient}>
        <QueryBackedJournal />
      </QueryClientProvider>,
    );

    expect(await screen.findByText("Last known journal entry")).toBeInTheDocument();
    mockedGetJournal.mockRejectedValueOnce(new Error("Connection unavailable"));
    await act(async () => {
      await queryClient.invalidateQueries({
        queryKey: gameResourceQueryKeys.journal("game-1"),
      });
    });

    expect(await screen.findByRole("alert")).toHaveTextContent(/could not read the journal/i);
    expect(screen.getByText("Last known journal entry")).toBeInTheDocument();
    mockedGetJournal.mockResolvedValue(recoveredJournal);
    await user.click(screen.getByRole("button", { name: /try again/i }));

    expect(await screen.findByText("Recovered journal entry")).toBeInTheDocument();
  });
});
