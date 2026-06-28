import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SaloonDevPanel } from "../dev/panels/SaloonDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { clearSaloonOverride, forceSaloonOverride, getSaloonDevContext } from "../dev/devApi";
import type { SaloonDevContextDto, SaloonSuspectDevDto } from "../dev/types";

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

function makeSuspect(overrides: Partial<SaloonSuspectDevDto> = {}): SaloonSuspectDevDto {
  return {
    suspectId: "suspect-1",
    name: "Mira Cline",
    isTrueCulprit: false,
    isEligibleSaloonPoi: true,
    ineligibilityReason: null,
    hasKnownWarrant: false,
    presenceState: null,
    aliases: [],
    identifyingFacts: [],
    traitTags: [],
    bountyAmount: null,
    warrantDisposition: null,
    warrantKnownFeatures: [],
    warrantSummary: null,
    ...overrides,
  };
}

function makeContext(overrides: Partial<SaloonDevContextDto> = {}): SaloonDevContextDto {
  return {
    sessionId: "test-game-1",
    currentActionContext: "Saloon",
    currentTownId: "pinecross",
    currentTownName: "Pinecross",
    sourceSpent: false,
    activeSaloonPoi: null,
    pendingDevOverride: null,
    hiddenTruth: {
      trueCulpritId: "suspect-2",
      trueCulpritName: "Reno Pike",
      killerReleaseStatus: "The killer trail is locked until 2 more public clue(s) surface.",
      killerIsReleased: false,
      saloonLoopExplanation: "Saloon look-around source is available. The killer trail is locked.",
    },
    citizenInfo: {
      descriptor: "a stranger with a distinguishing feature from the shared suspect vocabulary",
      hasNamedArchetypes: true,
      availableArchetypes: [
        { roleKey: "butcher", displayName: "the town butcher" },
        { roleKey: "mortician", displayName: "the town mortician" },
        { roleKey: "doctor", displayName: "the town doctor" },
      ],
    },
    suspects: [],
    ...overrides,
  };
}

describe("SaloonDevPanel", () => {
  it("shows no active session message when gameId is missing", () => {
    renderPanel();
    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("renders saloon context when loaded", async () => {
    seedGameId("test-game-1");
    mockedGetContext.mockResolvedValue(
      makeContext({
        suspects: [
          makeSuspect({
            suspectId: "suspect-1",
            name: "Mira Cline",
            isTrueCulprit: false,
            isEligibleSaloonPoi: true,
          }),
          makeSuspect({
            suspectId: "suspect-2",
            name: "Reno Pike",
            isTrueCulprit: true,
            isEligibleSaloonPoi: false,
            ineligibilityReason: "The killer trail is locked until 2 more public clue(s) surface.",
          }),
        ],
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Pinecross")).toBeInTheDocument();
    });
    // Hidden truth section shows the culprit name and gate-aware eligibility
    const hiddenTruthSection = screen.getByText("Hidden truth (dev only)").closest("section");
    expect(hiddenTruthSection?.textContent).toMatch(/Reno Pike/);
    expect(hiddenTruthSection?.textContent).toMatch(/killer trail is locked/);
    // Suspects section shows the culprit badge and eligibility tags
    expect(screen.getByText("culprit")).toBeInTheDocument();
    expect(screen.getByText("eligible")).toBeInTheDocument();
    expect(screen.getByText("ineligible")).toBeInTheDocument();
  });

  it("renders active wanted-suspect POI with resolved name when one is present", async () => {
    seedGameId("test-game-poi-suspect");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-poi-suspect",
        sourceSpent: true,
        activeSaloonPoi: {
          suspectId: "suspect-1",
          suspectName: "Mira Cline",
          descriptor: "a scar-faced drifter with a raven-feather pin",
          personOfInterestKind: "WantedSuspect",
          citizenRole: null,
        },
        suspects: [makeSuspect()],
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Wanted suspect")).toBeInTheDocument();
    });
    // Mira Cline appears in both the active POI and suspects section
    expect(screen.getAllByText("Mira Cline").length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/scar-faced drifter/)).toBeInTheDocument();
  });

  it("renders active citizen POI with concealment descriptor and role", async () => {
    seedGameId("test-game-poi-citizen");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-poi-citizen",
        sourceSpent: true,
        activeSaloonPoi: {
          suspectId: null,
          suspectName: null,
          descriptor: "a dusty rancher nursing a sarsaparilla",
          personOfInterestKind: "Citizen",
          citizenRole: "butcher",
        },
      }),
    );

    renderPanel();

    await waitFor(() => {
      const poiSection = screen.getByText("Active saloon POI").closest("section");
      expect(poiSection?.textContent).toMatch(/Citizen/);
    });
    expect(screen.getAllByText(/dusty rancher/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/Citizen POI/)).toBeInTheDocument();
    expect(screen.getByText(/role: butcher/)).toBeInTheDocument();
  });

  it("shows no-active-POI message when source not spent", async () => {
    seedGameId("test-game-no-poi");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-no-poi",
        currentActionContext: "None",
        sourceSpent: false,
        hiddenTruth: null,
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/LookAroundSaloon not yet called/)).toBeInTheDocument();
    });
  });

  it("shows no-active-POI message when source spent but no POI", async () => {
    seedGameId("test-game-repeat");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-repeat",
        currentActionContext: "None",
        sourceSpent: true,
        hiddenTruth: null,
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/repeat visit or confrontation cleared/)).toBeInTheDocument();
    });
  });

  it("shows pending override with resolved suspect name when one is set", async () => {
    seedGameId("test-game-2");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-2",
        currentActionContext: "None",
        hiddenTruth: null,
        pendingDevOverride: {
          forcedKind: "Suspect",
          forcedSuspectId: "suspect-1",
          forcedSuspectName: "Mira Cline",
          forcedCitizenRoleKey: null,
        },
        suspects: [makeSuspect()],
      }),
    );

    renderPanel();

    await waitFor(() => {
      const overrideSection = screen.getByText("Pending dev override").closest("section");
      expect(overrideSection?.textContent).toMatch(/Suspect/);
      expect(overrideSection?.textContent).toMatch(/Mira Cline/);
    });
  });

  it("shows context mismatch warning when aggregate context is not Saloon", async () => {
    seedGameId("test-game-mismatch");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-mismatch",
        currentActionContext: "SheriffOffice",
        hiddenTruth: null,
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText(/Warning.*SheriffOffice/i)).toBeInTheDocument();
    });
  });

  it("does not show context mismatch warning when aggregate context is Saloon", async () => {
    seedGameId("test-game-match");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-match",
        currentActionContext: "Saloon",
        hiddenTruth: null,
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Pinecross")).toBeInTheDocument();
    });
    expect(screen.queryByText(/Warning.*aggregate context/i)).not.toBeInTheDocument();
  });

  it("shows force control with Suspect/Citizen dropdown (no FalseLead)", async () => {
    seedGameId("test-game-force");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-force",
        suspects: [
          makeSuspect({ suspectId: "suspect-1", name: "Mira Cline", isEligibleSaloonPoi: true }),
        ],
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Force next saloon look-around POI")).toBeInTheDocument();
    });

    const kindSelect = screen.getByTestId("force-kind-select") as HTMLSelectElement;
    const options = Array.from(kindSelect.options).map((o) => o.value);
    expect(options).toContain("Suspect");
    expect(options).toContain("Citizen");
    expect(options).not.toContain("FalseLead");
  });

  it("shows suspect candidate dropdown with resolved labels when Suspect is selected", async () => {
    seedGameId("test-game-candidates");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-candidates",
        suspects: [
          makeSuspect({
            suspectId: "suspect-1",
            name: "Mira Cline",
            isEligibleSaloonPoi: true,
            bountyAmount: 500,
            warrantDisposition: "DeadOrAlive",
            traitTags: ["armed", "desperate"],
          }),
        ],
      }),
    );

    renderPanel();

    await waitFor(() => {
      const suspectSelect = screen.getByTestId("force-suspect-select") as HTMLSelectElement;
      expect(suspectSelect).toBeInTheDocument();
      // The candidate label should include the name and useful context
      const candidateOption = Array.from(suspectSelect.options).find(
        (o) => o.value === "suspect-1",
      );
      expect(candidateOption).toBeDefined();
      expect(candidateOption?.textContent).toMatch(/Mira Cline/);
      expect(candidateOption?.textContent).toMatch(/\$500/);
    });
  });

  it("shows citizen role selector when Citizen is selected and hasNamedArchetypes is true", async () => {
    seedGameId("test-game-citizen-force");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-citizen-force",
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Force next saloon look-around POI")).toBeInTheDocument();
    });

    const user = userEvent.setup();
    const kindSelect = screen.getByTestId("force-kind-select") as HTMLSelectElement;
    await user.selectOptions(kindSelect, "Citizen");

    expect(screen.getByTestId("force-citizen-role-select")).toBeInTheDocument();
    expect(screen.getByText(/Source-backed cast/)).toBeInTheDocument();
    expect(screen.getByText(/shared suspect vocabulary/)).toBeInTheDocument();
  });

  it("shows generic citizen note when hasNamedArchetypes is false", async () => {
    seedGameId("test-game-citizen-generic");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-citizen-generic",
        citizenInfo: {
          descriptor: "a town clerk from Pinecross",
          hasNamedArchetypes: false,
          availableArchetypes: [],
        },
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getByText("Force next saloon look-around POI")).toBeInTheDocument();
    });

    const user = userEvent.setup();
    const kindSelect = screen.getByTestId("force-kind-select") as HTMLSelectElement;
    await user.selectOptions(kindSelect, "Citizen");

    expect(screen.getByText(/Generic citizen POI/)).toBeInTheDocument();
    expect(screen.getByText(/a town clerk from Pinecross/)).toBeInTheDocument();
  });

  it("calls forceSaloonOverride when Force next POI button is clicked", async () => {
    seedGameId("test-game-3");
    mockedGetContext.mockResolvedValue(makeContext({ sessionId: "test-game-3", hiddenTruth: null }));
    mockedForce.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /force next poi/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /force next poi/i }));

    await waitFor(() => {
      expect(mockedForce).toHaveBeenCalledWith(
        "test-game-3",
        expect.objectContaining({ forcedKind: "Suspect" }),
      );
    });
  });

  it("sends forcedCitizenRoleKey when a citizen role is selected and Force is clicked", async () => {
    seedGameId("test-game-citizen-role-force");
    mockedGetContext.mockResolvedValue(makeContext({ sessionId: "test-game-citizen-role-force", hiddenTruth: null }));
    mockedForce.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /force next poi/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    const kindSelect = screen.getByTestId("force-kind-select") as HTMLSelectElement;
    await user.selectOptions(kindSelect, "Citizen");

    const roleSelect = screen.getByTestId("force-citizen-role-select") as HTMLSelectElement;
    await user.selectOptions(roleSelect, "butcher");

    await user.click(screen.getByRole("button", { name: /force next poi/i }));

    await waitFor(() => {
      expect(mockedForce).toHaveBeenCalledWith(
        "test-game-citizen-role-force",
        expect.objectContaining({
          forcedKind: "Citizen",
          forcedCitizenRoleKey: "butcher",
        }),
      );
    });
  });

  it("sends forcedKind None with no suspect or citizen role when None is selected and Force is clicked", async () => {
    seedGameId("test-game-none-force");
    mockedGetContext.mockResolvedValue(makeContext({ sessionId: "test-game-none-force", hiddenTruth: null }));
    mockedForce.mockResolvedValue(undefined);

    renderPanel();

    await waitFor(() => {
      expect(screen.getByRole("button", { name: /force next poi/i })).toBeInTheDocument();
    });

    const user = userEvent.setup();
    const kindSelect = screen.getByTestId("force-kind-select") as HTMLSelectElement;
    await user.selectOptions(kindSelect, "None");

    await user.click(screen.getByRole("button", { name: /force next poi/i }));

    await waitFor(() => {
      expect(mockedForce).toHaveBeenCalledWith(
        "test-game-none-force",
        expect.objectContaining({
          forcedKind: "None",
          forcedSuspectId: null,
          forcedCitizenRoleKey: null,
        }),
      );
    });
  });

  it("calls clearSaloonOverride when Clear override button is clicked", async () => {
    seedGameId("test-game-4");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-4",
        currentActionContext: "None",
        hiddenTruth: null,
        pendingDevOverride: {
          forcedKind: "Suspect",
          forcedSuspectId: "suspect-1",
          forcedSuspectName: "Mira Cline",
          forcedCitizenRoleKey: null,
        },
        suspects: [makeSuspect()],
      }),
    );
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

  it("shows warrant facts in suspect descriptors", async () => {
    seedGameId("test-game-warrant");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-warrant",
        suspects: [
          makeSuspect({
            suspectId: "suspect-1",
            name: "Mira Cline",
            hasKnownWarrant: true,
            bountyAmount: 1500,
            warrantDisposition: "DeadOrAlive",
            warrantKnownFeatures: ["scar over left eye", "raven-feather pin"],
            warrantSummary: "Wanted for stagecoach robbery",
            aliases: ["The Raven"],
            identifyingFacts: ["Left-handed draw"],
            traitTags: ["armed", "local"],
          }),
        ],
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getAllByText("Mira Cline").length).toBeGreaterThanOrEqual(1);
    });
    // $1500 appears in both suspect detail and force candidate dropdown
    expect(screen.getAllByText(/\$1500/).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/scar over left eye/)).toBeInTheDocument();
    expect(screen.getByText(/The Raven/)).toBeInTheDocument();
    expect(screen.getByText(/stagecoach robbery/)).toBeInTheDocument();
  });

  it("shows gate-aware true culprit eligibility (not 'can never appear')", async () => {
    seedGameId("test-game-gate");
    mockedGetContext.mockResolvedValue(
      makeContext({
        sessionId: "test-game-gate",
        hiddenTruth: {
          trueCulpritId: "suspect-2",
          trueCulpritName: "Reno Pike",
          killerReleaseStatus: "The killer trail is locked until 2 more public clue(s) surface.",
          killerIsReleased: false,
          saloonLoopExplanation: "The killer trail is locked — the true culprit is gated out.",
        },
        suspects: [
          makeSuspect({
            suspectId: "suspect-2",
            name: "Reno Pike",
            isTrueCulprit: true,
            isEligibleSaloonPoi: false,
            ineligibilityReason: "The killer trail is locked until 2 more public clue(s) surface.",
          }),
        ],
      }),
    );

    renderPanel();

    await waitFor(() => {
      expect(screen.getAllByText("Reno Pike").length).toBeGreaterThanOrEqual(1);
    });
    // Must NOT say "can never appear"
    expect(screen.queryByText(/can never appear/i)).not.toBeInTheDocument();
    // Must say gate-aware language
    expect(screen.getAllByText(/killer trail is locked/i).length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/Gated out/i).length).toBeGreaterThanOrEqual(1);
  });
});
