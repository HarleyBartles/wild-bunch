import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StartingTownStep } from "../components/start-flow/StartingTownStep";
import { getStartingTowns } from "../api/wildBunchApi";
import type { StartingTownDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  getStartingTowns: vi.fn(),
}));

const mockedGetStartingTowns = vi.mocked(getStartingTowns);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function createTown(overrides: Partial<StartingTownDto> = {}): StartingTownDto {
  return {
    id: "t-town",
    name: "Tumbleweed",
    services: 0,
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
    mockedGetStartingTowns.mockResolvedValue([
      createTown({ id: "t-town", name: "Tumbleweed" }),
      createTown({ id: "dust-fork", name: "Dust Fork" }),
    ]);

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

  it("calls onSelectTown with the town id when a town is selected", async () => {
    mockedGetStartingTowns.mockResolvedValue([
      createTown({ id: "t-town", name: "Tumbleweed" }),
      createTown({ id: "dust-fork", name: "Dust Fork" }),
    ]);

    const onSelectTown = vi.fn();
    const user = userEvent.setup();

    renderStep({ onSelectTown });

    const button = await screen.findByRole("button", { name: /start in dust fork/i });
    await user.click(button);

    await waitFor(() => {
      expect(onSelectTown).toHaveBeenCalledWith("dust-fork");
    });
  });

  it("shows the loading state copy while towns are fetching", () => {
    mockedGetStartingTowns.mockReturnValue(new Promise(() => {}));

    renderStep();

    expect(screen.getByText("Saddling up the map…")).toBeInTheDocument();
  });

  it("shows the loading state copy when the fetch resolves to an empty list", async () => {
    mockedGetStartingTowns.mockResolvedValue([]);

    renderStep();

    await waitFor(() => {
      expect(screen.getByText("Saddling up the map…")).toBeInTheDocument();
    });
  });

  it("renders the heading copy from the copy doc", async () => {
    mockedGetStartingTowns.mockResolvedValue([createTown()]);

    renderStep();

    expect(
      await screen.findByRole("heading", { name: /pick a starting town/i }),
    ).toBeInTheDocument();
  });

  it("renders the body copy from the copy doc", async () => {
    mockedGetStartingTowns.mockResolvedValue([createTown()]);

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
    mockedGetStartingTowns.mockResolvedValue([createTown()]);

    renderStep();

    await screen.findByText("Tumbleweed");
    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });

  it("does not render town buttons while loading", () => {
    mockedGetStartingTowns.mockReturnValue(new Promise(() => {}));

    renderStep();

    expect(screen.queryByRole("button", { name: /start in /i })).not.toBeInTheDocument();
  });
});
