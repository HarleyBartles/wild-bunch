import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StartingTownStep } from "../components/start-flow/StartingTownStep";
import { getStartingTownMap } from "../api/wildBunchApi";
import type { StartingTownMapDto } from "../api/types";

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
  getStartingTownMap: vi.fn(),
}));

const mockedGetStartingTownMap = vi.mocked(getStartingTownMap);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function createMapData(overrides: Partial<StartingTownMapDto> = {}): StartingTownMapDto {
  return {
    towns: [
      { id: "t-town", name: "Tumbleweed", services: 0, x: 150, y: 500, selectable: true },
      { id: "dust-fork", name: "Dust Fork", services: 0, x: 450, y: 400, selectable: true },
    ],
    trails: [
      { id: "trail-1", fromTownId: "t-town", toTownId: "dust-fork", rideDayDistance: 3 },
    ],
    ...overrides,
  };
}

function renderStep(overrides: {
  selectedTownId?: string | null;
  onSelectTown?: (townId: string) => void;
} = {}) {
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
        selectedTownId={overrides.selectedTownId ?? null}
        onSelectTown={onSelectTown}
      />
    </QueryClientProvider>,
  );

  return { onSelectTown, queryClient };
}

describe("StartingTownStep", () => {
  it("renders towns fetched from the backend", async () => {
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

    renderStep();

    expect(await screen.findByText("Tumbleweed")).toBeInTheDocument();
    expect(screen.getByText("Dust Fork")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /start in tumbleweed/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /start in dust fork/i }),
    ).toBeInTheDocument();
  });

  it("renders the Phaser map host", async () => {
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

    renderStep();

    expect(await screen.findByRole("img", { name: /trail map of starting towns/i })).toBeInTheDocument();
  });

  it("calls onSelectTown with the town id when a town button is selected", async () => {
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

    const onSelectTown = vi.fn();
    const user = userEvent.setup();

    renderStep({ onSelectTown });

    const button = await screen.findByRole("button", { name: /start in dust fork/i });
    await user.click(button);

    await waitFor(() => {
      expect(onSelectTown).toHaveBeenCalledWith("dust-fork");
    });
  });

  it("shows the loading state copy while the map is fetching", () => {
    mockedGetStartingTownMap.mockReturnValue(new Promise(() => {}));

    renderStep();

    expect(screen.getByText("Saddling up the map…")).toBeInTheDocument();
  });

  it("shows the loading state copy when the fetch resolves to an empty town list", async () => {
    mockedGetStartingTownMap.mockResolvedValue({ towns: [], trails: [] });

    renderStep();

    await waitFor(() => {
      expect(screen.getByText("Saddling up the map…")).toBeInTheDocument();
    });
  });

  it("renders the heading copy from the copy doc", async () => {
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

    renderStep();

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }),
    ).toBeInTheDocument();
  });

  it("renders the body copy from the copy doc", async () => {
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

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
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

    renderStep();

    await screen.findByText("Tumbleweed");
    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });

  it("does not render town buttons while loading", () => {
    mockedGetStartingTownMap.mockReturnValue(new Promise(() => {}));

    renderStep();

    expect(screen.queryByRole("button", { name: /start in /i })).not.toBeInTheDocument();
  });

  it("renders the map legend copy below the map", async () => {
    mockedGetStartingTownMap.mockResolvedValue(createMapData());

    renderStep();

    expect(
      await screen.findByText(/click a town on the map to ride out from there\./i),
    ).toBeInTheDocument();
  });
});
