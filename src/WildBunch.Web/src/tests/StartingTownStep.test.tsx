import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StartingTownStep } from "../components/start-flow/StartingTownStep";
import { getWorldMap } from "../api/wildBunchApi";
import type { WorldMapDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  getWorldMap: vi.fn(),
}));

vi.mock("../components/start-flow/PhaserMapHost", () => ({
  PhaserMapHost: ({ mapData }: { mapData: WorldMapDto }) => (
    <div role="img" aria-label="Trail map of starting towns" data-testid="map-host-input">
      {mapData.towns.map((town) => (
        <span key={town.id}>{town.name}</span>
      ))}
    </div>
  ),
}));

const mockedGetWorldMap = vi.mocked(getWorldMap);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function createMapData(overrides: Partial<WorldMapDto> = {}): WorldMapDto {
  return {
    towns: [
      { id: "t-town", name: "Tumbleweed", x: 150, y: 500 },
      { id: "dust-fork", name: "Dust Fork", x: 450, y: 400 },
    ],
    trails: [{ id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 }],
    ...overrides,
  };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((resolvePromise) => {
    resolve = resolvePromise;
  });
  return { promise, resolve };
}

function renderStep(
  overrides: {
    sessionId?: string;
    selectedTownId?: string | null;
    onSelectTown?: (townId: string) => void;
  } = {},
) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  const onSelectTown = overrides.onSelectTown ?? vi.fn();

  render(
    <QueryClientProvider client={queryClient}>
      <StartingTownStep
        sessionId={overrides.sessionId ?? "test-session-id"}
        selectedTownId={overrides.selectedTownId ?? null}
        onSelectTown={onSelectTown}
      />
    </QueryClientProvider>,
  );

  return { onSelectTown, queryClient };
}

describe("StartingTownStep", () => {
  it("passes the current session's loaded world map to its renderer", async () => {
    const pending = deferred<WorldMapDto>();
    const sessionMap = createMapData({
      towns: [
        { id: "coyote-flats", name: "Coyote Flats", x: 120, y: 360 },
        { id: "dry-gulch", name: "Dry Gulch", x: 520, y: 240 },
      ],
    });
    mockedGetWorldMap.mockReturnValue(pending.promise);
    const sessionId = "session-with-coyote-flats";

    renderStep({ sessionId });

    expect(await screen.findByText("Saddling up the map…")).toBeInTheDocument();
    expect(mockedGetWorldMap).toHaveBeenCalledWith(sessionId);
    expect(screen.queryByTestId("map-host-input")).not.toBeInTheDocument();

    await act(async () => {
      pending.resolve(sessionMap);
      await pending.promise;
    });

    const mapHost = await screen.findByTestId("map-host-input");
    expect(within(mapHost).getByText("Coyote Flats")).toBeInTheDocument();
    expect(within(mapHost).getByText("Dry Gulch")).toBeInTheDocument();
    expect(within(mapHost).queryByText("Tumbleweed")).not.toBeInTheDocument();
  });

  it("offers recovery when the settled map contains no selectable towns", async () => {
    mockedGetWorldMap
      .mockResolvedValueOnce({ towns: [], trails: [] })
      .mockResolvedValueOnce(createMapData());

    renderStep();

    expect(await screen.findByText(/the trail map is unavailable/i)).toBeInTheDocument();
    expect(screen.queryByText("Saddling up the map…")).not.toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /try again/i }));

    expect(
      await screen.findByRole("img", { name: /trail map of starting towns/i }),
    ).toBeInTheDocument();
    expect(mockedGetWorldMap).toHaveBeenCalledTimes(2);
  });

  it("offers recovery when loading the settled map fails", async () => {
    const retry = deferred<WorldMapDto>();
    mockedGetWorldMap
      .mockRejectedValueOnce(new Error("Network down"))
      .mockReturnValueOnce(retry.promise);

    renderStep();

    expect(await screen.findByText(/the trail map is unavailable/i)).toBeInTheDocument();
    expect(screen.queryByText("Saddling up the map…")).not.toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /try again/i }));

    expect(screen.getByText("Saddling up the map…")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /try again/i })).not.toBeInTheDocument();
    expect(mockedGetWorldMap).toHaveBeenCalledTimes(2);

    retry.resolve(createMapData());
    expect(
      await screen.findByRole("img", { name: /trail map of starting towns/i }),
    ).toBeInTheDocument();
  });

  it("does not fetch or offer retry without a session identity", () => {
    renderStep({ sessionId: "" });

    expect(screen.getByText(/the trail map is unavailable/i)).toBeInTheDocument();
    expect(screen.queryByText("Saddling up the map…")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /try again/i })).not.toBeInTheDocument();
    expect(mockedGetWorldMap).not.toHaveBeenCalled();
  });

  it("renders the heading copy from the copy doc", async () => {
    mockedGetWorldMap.mockResolvedValue(createMapData());

    renderStep();

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }),
    ).toBeInTheDocument();
  });

  it("renders the body copy from the copy doc", async () => {
    mockedGetWorldMap.mockResolvedValue(createMapData());

    renderStep();

    expect(
      await screen.findByText(
        /you cannot go back to the town where the dying man fell\. the sheriff will have that place locked down by now\./i,
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        /so pick the town where your run begins proper\. from there, you will follow leads, read wanted posters, ride the trails, and hunt for the wild bunch killer before the law catches up with you\./i,
      ),
    ).toBeInTheDocument();
  });

  it("does not render a Back button", async () => {
    mockedGetWorldMap.mockResolvedValue(createMapData());

    renderStep();

    await screen.findByRole("img", { name: /trail map of starting towns/i });
    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });

  it("renders the map legend copy below the map", async () => {
    mockedGetWorldMap.mockResolvedValue(createMapData());

    renderStep();

    expect(
      await screen.findByText(/click a town on the map to ride out from there\./i),
    ).toBeInTheDocument();
  });
});
