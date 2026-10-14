import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StartingTownStep } from "../components/start-flow/StartingTownStep";
import { getWorldMap } from "../api/wildBunchApi";
import type { WorldMapDto } from "../api/types";

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
    }
    destroy() {
      this.destroyed = true;
    }
  }
  class Scene {
    constructor(_key?: string) {}
  }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

vi.mock("../api/wildBunchApi", () => ({
  getWorldMap: vi.fn(),
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
  it("renders the populated world map", async () => {
    mockedGetWorldMap.mockResolvedValue(createMapData());

    renderStep();

    expect(
      await screen.findByRole("img", { name: /trail map of starting towns/i }),
    ).toBeInTheDocument();
  });

  it("shows the loading state copy while the map is fetching", () => {
    const pending = deferred<WorldMapDto>();
    mockedGetWorldMap.mockReturnValue(pending.promise);

    renderStep();

    expect(screen.getByText("Saddling up the map…")).toBeInTheDocument();
    pending.resolve(createMapData());
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
