import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SaloonDevPanel } from "../dev/panels/SaloonDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { clearSaloonOverride, forceSaloonOverride, getSaloonDevContext } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSaloonDevContext: vi.fn(),
  forceSaloonOverride: vi.fn(),
  clearSaloonOverride: vi.fn(),
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
  getSessionAudit: vi.fn(),
}));

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  createGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetContext = vi.mocked(getSaloonDevContext);
const mockedForce = vi.mocked(forceSaloonOverride);
const mockedClear = vi.mocked(clearSaloonOverride);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderPanel() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SaloonDevPanel />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

function seedGameId(id: string) {
  window.localStorage.setItem("wild-bunch.current-game-id", id);
}

describe("SaloonDevPanel", () => {
  it("shows no active session message when gameId is missing", () => {
    renderPanel();
    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("renders saloon context when loaded", async () => {
    seedGameId("test-game-1");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-1",
      currentActionContext: "Saloon",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: false,
      activeSaloonPoi: null,
      pendingDevOverride: null,
      hiddenTruth: { trueCulpritId: "suspect-2", trueCulpritName: "Reno Pike" },
      suspects: [
        {
          suspectId: "suspect-1",
          name: "Mira Cline",
          isTrueCulprit: false,
          isEligibleSaloonPoi: true,
          ineligibilityReason: null,
          hasKnownWarrant: false,
          presenceState: null,
        },
        {
          suspectId: "suspect-2",
          name: "Reno Pike",
          isTrueCulprit: true,
          isEligibleSaloonPoi: false,
          ineligibilityReason: "True culprit - can never appear as a saloon POI.",
          hasKnownWarrant: false,
          presenceState: null,
        },
      ],
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Pinecross")).toBeInTheDocument();
    });
    // Hidden truth section shows the culprit
    const hiddenTruthSection = screen.getByText("Hidden truth (dev only)").closest("section");
    expect(hiddenTruthSection?.textContent).toMatch(/Reno Pike.*suspect-2/);
    // Suspects section shows the culprit badge and eligibility tags
    expect(screen.getByText("culprit")).toBeInTheDocument();
    expect(screen.getByText("eligible")).toBeInTheDocument();
    expect(screen.getByText("ineligible")).toBeInTheDocument();
  });

  it("renders active wanted-suspect POI when one is present", async () => {
    seedGameId("test-game-poi-suspect");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-poi-suspect",
      currentActionContext: "Saloon",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: true,
      activeSaloonPoi: {
        suspectId: "suspect-1",
        descriptor: "a scar-faced drifter with a raven-feather pin",
        personOfInterestKind: "WantedSuspect",
      },
      pendingDevOverride: null,
      hiddenTruth: null,
      suspects: [],
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("WantedSuspect")).toBeInTheDocument();
    });
    expect(screen.getByText("suspect-1")).toBeInTheDocument();
    expect(screen.getByText(/scar-faced drifter/)).toBeInTheDocument();
  });

  it("renders active citizen POI when one is present", async () => {
    seedGameId("test-game-poi-citizen");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-poi-citizen",
      currentActionContext: "Saloon",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: true,
      activeSaloonPoi: {
        suspectId: null,
        descriptor: "a dusty rancher nursing a sarsaparilla",
        personOfInterestKind: "Citizen",
      },
      pendingDevOverride: null,
      hiddenTruth: null,
      suspects: [],
    });

    renderPanel();

    await waitFor(() => {
      // The active POI section shows "Citizen" as the kind
      const poiSection = screen.getByText("Active saloon POI").closest("section");
      expect(poiSection?.textContent).toMatch(/Citizen/);
    });
    expect(screen.getByText(/dusty rancher/)).toBeInTheDocument();
  });

  it("shows no-active-POI message when source not spent", async () => {
    seedGameId("test-game-no-poi");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-no-poi",
      currentActionContext: "None",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: false,
      activeSaloonPoi: null,
      pendingDevOverride: null,
      hiddenTruth: null,
      suspects: [],
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/LookAroundSaloon not yet called/)).toBeInTheDocument();
    });
  });

  it("shows no-active-POI message when source spent but no POI", async () => {
    seedGameId("test-game-repeat");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-repeat",
      currentActionContext: "None",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: true,
      activeSaloonPoi: null,
      pendingDevOverride: null,
      hiddenTruth: null,
      suspects: [],
    });

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/repeat visit or confrontation cleared/)).toBeInTheDocument();
    });
  });

  it("shows pending override when one is set", async () => {
    seedGameId("test-game-2");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-2",
      currentActionContext: "None",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: false,
      activeSaloonPoi: null,
      pendingDevOverride: { forcedKind: "Citizen", forcedSuspectId: null },
      hiddenTruth: null,
      suspects: [],
    });

    renderPanel();

    await waitFor(() => {
      // The pending override section shows "Citizen" as the override kind
      const overrideSection = screen.getByText("Pending dev override").closest("section");
      expect(overrideSection?.textContent).toMatch(/Citizen/);
    });
  });

  it("calls forceSaloonOverride when Force button is clicked", async () => {
    seedGameId("test-game-3");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-3",
      currentActionContext: "None",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: false,
      activeSaloonPoi: null,
      pendingDevOverride: null,
      hiddenTruth: null,
      suspects: [],
    });
    mockedForce.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /force override/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /force override/i }));

    await waitFor(() => {
      expect(mockedForce).toHaveBeenCalledWith("test-game-3", expect.objectContaining({
        forcedKind: "Citizen",
      }));
    });
  });

  it("calls clearSaloonOverride when Clear button is clicked", async () => {
    seedGameId("test-game-4");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-4",
      currentActionContext: "None",
      currentTownId: "pinecross",
      currentTownName: "Pinecross",
      sourceSpent: false,
      activeSaloonPoi: null,
      pendingDevOverride: { forcedKind: "Suspect", forcedSuspectId: "suspect-1" },
      hiddenTruth: null,
      suspects: [],
    });
    mockedClear.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /clear override/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /clear override/i }));

    await waitFor(() => {
      expect(mockedClear).toHaveBeenCalledWith("test-game-4");
    });
  });
});
