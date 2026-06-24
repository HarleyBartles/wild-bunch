import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DebugCockpitRoute } from "../routes/DebugCockpitRoute";
import { GameSessionProvider } from "../state/GameSessionProvider";
import {
  AvailableActionKind,
  JourneyStatus,
  SaloonPersonOfInterestKind,
  WantedPosterFeatureRenderMode,
  WantedPosterFeatureSalience,
  type GameSessionDto,
  type JournalDto,
  type TownStoreOffersDto,
  type WantedPosterDto,
} from "../api/types";
import {
  buyStoreItem,
  checkLocalRecords,
  createGame,
  followTelegraphLeads,
  gatherLocalGossip,
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  inspectNoticeBoard,
  confrontSaloonPersonOfInterest,
  lookAroundSaloon,
  readWantedPosters,
  travel,
} from "../api/wildBunchApi";

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
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedCreateGame = vi.mocked(createGame);
const mockedBuyStoreItem = vi.mocked(buyStoreItem);
const mockedCheckLocalRecords = vi.mocked(checkLocalRecords);
const mockedInspectNoticeBoard = vi.mocked(inspectNoticeBoard);
const mockedConfrontSaloonPersonOfInterest = vi.mocked(confrontSaloonPersonOfInterest);
const mockedLookAroundSaloon = vi.mocked(lookAroundSaloon);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);
const mockedFollowTelegraphLeads = vi.mocked(followTelegraphLeads);
const mockedGatherLocalGossip = vi.mocked(gatherLocalGossip);
const mockedTravel = vi.mocked(travel);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderInProvider() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <DebugCockpitRoute />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    travelDifficulty: 0,
    player: {
      name: "Ruth",
      currentTownId: "t-town",
      health: 9,
    },
    world: {
      towns: [
        { id: "t-town", name: "Tumbleweed", services: 0 },
        { id: "dust-fork", name: "Dust Fork", services: 0 },
      ],
      trails: [],
    },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: {
        statusText: "Still chasing leads.",
      },
      discoveredSuspects: [],
      caseBoard: {
        namedRecords: [
          {
            id: "record-gus",
            displayName: "Gus Mercer",
            kind: 4,
            status: 2,
            resolvedToDisplayName: null,
            evidenceIds: ["clue-1"],
            summaryLines: ["Discovered from wanted poster"],
            relatedLabels: ["Red Wren"],
            knownAliases: ["Red Wren"],
            distinguishingFeatures: ["Pale scar across the left cheek"],
            warrantDisposition: 1,
            bountyAmount: 2500.5,
            issuingAuthority: "County marshal",
            crimeSummary: "Wanted for a string of robberies near the county line.",
          },
        ],
        looseLeads: [],
        evidenceItems: [],
      },
      knownClues: [],
    },
    inventory: {
      wallet: { cash: 14 },
      items: [],
      horseState: null,
      canteenState: null,
      capabilities: {
        mountedTravelAvailable: false,
        horseUpkeepRequired: false,
        normalRouteWaterSecure: false,
        trailUtility: false,
        closeThreatAvailable: false,
        firearmThreatAvailable: false,
        gunfightCapable: false,
        revolverUsable: false,
        rifleUsable: false,
      },
    },
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    pursuitState: { heat: 1 },
    journey: null,
    travelDiary: null,
    logEntries: [{ kind: 0, message: "Booted", day: 1, turn: 0 }],
    activeSaloonPersonOfInterest: null,
  };
}

function createJournal(): JournalDto {
  return {
    id: "game-1",
    status: 0,
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    currentTown: { id: "t-town", name: "Tumbleweed" },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: {
        statusText: "Still chasing leads.",
      },
      caseSummary: "Find the culprit before the law closes in.",
      discoveredSuspects: [
        {
          id: "suspect-1",
          name: "Gus Mercer",
          status: 0,
        },
        {
          id: "suspect-2",
          name: "Mabel Quinn",
          status: 1,
        },
      ],
      caseBoard: {
        namedRecords: [
          {
            id: "record-gus",
            displayName: "Gus Mercer",
            kind: 4,
            status: 2,
            resolvedToDisplayName: null,
            evidenceIds: ["clue-1"],
            summaryLines: ["Discovered from wanted poster"],
            relatedLabels: ["Red Wren"],
            knownAliases: ["Red Wren"],
            distinguishingFeatures: ["Pale scar across the left cheek"],
            warrantDisposition: 1,
            bountyAmount: 2500.5,
            issuingAuthority: "County marshal",
            crimeSummary: "Wanted for a string of robberies near the county line.",
          },
        ],
        looseLeads: [
          {
            id: "lead-grey-jay",
            displayName: "Grey Jay",
            kind: 1,
            status: 0,
            resolvedToDisplayName: null,
            evidenceIds: ["clue-1"],
            summaryLines: ["Alias lead: A poster links the alias Grey Jay without naming Butch Cassidy."],
            relatedLabels: [],
            knownAliases: [],
            distinguishingFeatures: [],
            warrantDisposition: null,
            bountyAmount: null,
            issuingAuthority: null,
            crimeSummary: null,
          },
        ],
        evidenceItems: [
          {
            id: "clue-1",
            kindLabel: "Alias",
            summary: "A poster links the alias Grey Jay to a rider who has a limp in the right leg.",
            sourceLabel: "Wanted poster",
            identityBearing: true,
            anchors: {
              subjects: [{ label: "Grey Jay", alias: "Grey Jay", feature: "has a limp in the right leg", fact: null }],
              locations: [{ label: "Red Mesa road", place: "Red Mesa road", route: "Red Mesa road" }],
              times: [{ recency: 1, day: 5, turn: 1 }],
              directions: [{ label: "Eastbound dust line", movement: "Moved off-road", route: "Back trail" }],
            },
            handleIds: ["lead-grey-jay"],
          },
          {
            id: "clue-2",
            kindLabel: "Contradiction",
            summary: "The witness says the rider wore a blue coat, not the brown coat listed on the warrant.",
            sourceLabel: "Telegraph lead",
            identityBearing: true,
            anchors: {
              subjects: [{ label: "Blue coat rider", alias: null, feature: "Blue coat", fact: "Witness memory" }],
              locations: [{ label: "Depot road", place: null, route: "South spur" }],
              times: [{ recency: 2, day: 4, turn: null }],
              directions: [{ label: "Southbound", movement: "Rode away", route: null }],
            },
            handleIds: [],
          },
        ],
      },
      knownClues: [
        {
          id: "clue-1",
          kind: 4,
          description: "A boot print with Gus Mercer's ranch brand was found near the creek.",
          sourceLabel: "Sheriff records",
          context: "Logged after the notice board search.",
          anchors: {
            subjects: [{ label: "Gus Mercer", alias: "The ranch hand", feature: null, fact: "Seen near the creek" }],
            locations: [{ label: "Red Mesa road", place: "Red Mesa road", route: "Red Mesa road" }],
            times: [{ recency: 1, day: 5, turn: 1 }],
            directions: [{ label: "Eastbound dust line", movement: "Moved off-road", route: "Back trail" }],
          },
        },
        {
          id: "clue-2",
          kind: 9,
          description: "The witness says the rider wore a blue coat, not the brown coat listed on the warrant.",
          sourceLabel: "Telegraph lead",
          context: null,
          anchors: {
            subjects: [{ label: "Blue coat rider", alias: null, feature: "Blue coat", fact: "Witness memory" }],
            locations: [{ label: "Depot road", place: null, route: "South spur" }],
            times: [{ recency: 2, day: 4, turn: null }],
            directions: [{ label: "Southbound", movement: "Rode away", route: null }],
          },
        },
      ],
      knownWarrants: [
        {
          targetName: "Gus Mercer",
          summary: "Wanted for a string of robberies near the county line.",
          issuingSource: "County marshal",
          disposition: 1,
          bountyAmount: 2500.5,
        },
      ],
      wantedPosters: [
        {
          posterId: "warrant-gus",
          targetDisplayName: "Gus Mercer",
          aliases: ["Red Wren"],
          legalTerms: {
            disposition: 1,
            bountyAmount: 2500.5,
            issuingAuthority: "County marshal",
          },
          quickView: {
            headlineNameOrAlias: "Gus Mercer",
            headlineFeatureOrDescriptor: "Pale scar across the left cheek",
            pocketCheckDescriptor: "Dead or alive, $2,500.50 bounty",
          },
          details: {
            summary: "Wanted for a string of robberies near the county line.",
            publicOrigin: "County marshal",
            features: [
              {
                text: "Pale scar across the left cheek",
                salience: WantedPosterFeatureSalience.Headline,
                renderMode: WantedPosterFeatureRenderMode.PortraitRenderable,
              },
              {
                text: "Known as Red Wren",
                salience: WantedPosterFeatureSalience.Supporting,
                renderMode: WantedPosterFeatureRenderMode.TextOnly,
              },
            ],
          },
          publicSafeClassification: "gang-affiliated wanted criminal",
        },
      ],
    },
    logEntries: [{ kind: 0, message: "Booted", day: 1, turn: 0 }],
  };
}

function createCapturedJournal(): JournalDto {
  const journal = createJournal();

  journal.caseFile.caseBoard.namedRecords = [
    {
      ...journal.caseFile.caseBoard.namedRecords[0],
      displayName: "Gus Mercer",
      status: 3,
      evidenceIds: ["warrant-gus", "clue-1"],
      summaryLines: ["Captured alive on Day 5, Morning. Sheriff paid $2,500.50."],
    },
    {
      id: "record-mabel",
      displayName: "Mabel Quinn",
      kind: 4,
      status: 2,
      resolvedToDisplayName: null,
      evidenceIds: ["warrant-mabel", "clue-2"],
      summaryLines: ["Dead or alive warrant - Silver Creek Sheriff - 300.00 bounty"],
      relatedLabels: ["The Magpie"],
      knownAliases: ["The Magpie"],
      distinguishingFeatures: ["Mismatched spurs"],
      warrantDisposition: 1,
      bountyAmount: 300,
      issuingAuthority: "Silver Creek Sheriff",
      crimeSummary: "Wanted for cattle theft.",
    },
  ];
  journal.caseFile.caseBoard.evidenceItems = [
    {
      id: "clue-2",
      kindLabel: "Contradiction",
      summary: "The witness says the rider wore a blue coat, not the brown coat listed on the warrant.",
      sourceLabel: "Telegraph lead",
      identityBearing: true,
      anchors: {
        subjects: [{ label: "Blue coat rider", alias: null, feature: "Blue coat", fact: "Witness memory" }],
        locations: [{ label: "Depot road", place: null, route: "South spur" }],
        times: [{ recency: 2, day: 4, turn: null }],
        directions: [{ label: "Southbound", movement: "Rode away", route: null }],
      },
      handleIds: [],
    },
  ];
  journal.caseFile.knownWarrants = [
    ...journal.caseFile.knownWarrants,
    {
      targetName: "Mabel Quinn",
      summary: "Wanted for cattle theft.",
      issuingSource: "Silver Creek Sheriff",
      disposition: 1,
      bountyAmount: 300,
    },
  ];
  journal.caseFile.wantedPosters = [
    ...journal.caseFile.wantedPosters,
    createWantedPoster({
      posterId: "warrant-mabel",
      targetDisplayName: "Mabel Quinn",
      aliases: ["The Magpie"],
      legalTerms: {
        disposition: 1,
        bountyAmount: 300,
        issuingAuthority: "Silver Creek Sheriff",
      },
      quickView: {
        headlineNameOrAlias: "Mabel Quinn",
        headlineFeatureOrDescriptor: "Mismatched spurs",
        pocketCheckDescriptor: "Dead or alive, $300.00 bounty",
      },
      details: {
        summary: "Wanted for cattle theft.",
        publicOrigin: "Silver Creek Sheriff",
        features: [
          {
            text: "Mismatched spurs",
            salience: WantedPosterFeatureSalience.Headline,
            renderMode: WantedPosterFeatureRenderMode.TextOnly,
          },
        ],
      },
    }),
  ];

  return journal;
}

function createStoreOffers(): TownStoreOffersDto {
  return {
    townId: "t-town",
    townName: "Tumbleweed",
    available: true,
    sourceNote: "General store",
    offers: [
      {
        itemKind: 0,
        displayName: "Food",
        price: 2,
        vendorType: 0,
        availability: 0,
        sourceNote: "Fresh provisions",
      },
    ],
  };
}

function createWantedPoster(overrides: Partial<WantedPosterDto> = {}): WantedPosterDto {
  return {
    posterId: "warrant-public-1",
    targetDisplayName: "Mira Cline",
    aliases: ["Red Wren", "Aunt Tess"],
    legalTerms: {
      disposition: 1,
      bountyAmount: 2500.5,
      issuingAuthority: "County marshal",
    },
    quickView: {
      headlineNameOrAlias: "Mira Cline",
      headlineFeatureOrDescriptor: "Raven-feather pin",
      pocketCheckDescriptor: "Dead or alive, $2,500.50 bounty",
    },
    details: {
      summary: "Wanted for a string of robberies near the county line.",
      publicOrigin: "County marshal",
      features: [
        {
          text: "Raven-feather pin",
          salience: WantedPosterFeatureSalience.Headline,
          renderMode: WantedPosterFeatureRenderMode.PortraitRenderable,
        },
        {
          text: "Limp in the right leg",
          salience: WantedPosterFeatureSalience.Buried,
          renderMode: WantedPosterFeatureRenderMode.TextOnly,
        },
      ],
    },
    publicSafeClassification: "gang-affiliated wanted criminal",
    ...overrides,
  };
}

describe("DebugCockpitRoute", () => {
  it("hydrates the current session from local storage and loads store offers for the current town", async () => {
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
      { kind: AvailableActionKind.InspectNoticeBoard, label: "Inspect notice board" },
      { kind: AvailableActionKind.CheckSheriffRecords, label: "Check local records" },
    ]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedCreateGame.mockResolvedValue(createSession());
    mockedBuyStoreItem.mockResolvedValue({
      success: true,
      message: "Purchased",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "Read wanted posters",
      currentJournal: createJournal(),
      wantedPosters: [createWantedPoster()],
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: createJournal(),
    });
    mockedCheckLocalRecords.mockResolvedValue({
      success: true,
      message: "Check local records",
      currentJournal: createJournal(),
    });
    mockedFollowTelegraphLeads.mockResolvedValue({
      success: true,
      message: "Follow telegraph leads",
      currentJournal: createJournal(),
    });
    mockedGatherLocalGossip.mockResolvedValue({
      success: true,
      message: "Gather local gossip",
      currentJournal: createJournal(),
    });
    mockedLookAroundSaloon.mockResolvedValue({
      success: true,
      message: "Look around saloon",
      currentJournal: createJournal(),
    });
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Travelled",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });

    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderInProvider();

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
      expect(mockedGetAvailableActions).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
      expect(mockedGetTownStoreOffers).toHaveBeenCalledWith("game-1", "t-town");
    });

    expect(await screen.findByRole("heading", { name: /current session/i })).toBeInTheDocument();
    expect(screen.getByText("Tumbleweed (t-town)")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: /log/i })).not.toBeInTheDocument();
    expect(screen.queryByText("Booted")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /store offers/i })).toBeInTheDocument();
    expect(screen.getByText("Food $2.00")).toBeInTheDocument();

    expect(screen.queryByRole("heading", { name: /case file/i })).not.toBeInTheDocument();
    expect(screen.queryByText("Find the culprit before the law closes in.")).not.toBeInTheDocument();

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /open case file/i }));

    const dialog = await screen.findByRole("dialog", { name: /investigation board/i });
    const dialogScope = within(dialog);
    expect(screen.getByRole("heading", { name: /investigation board/i })).toBeInTheDocument();
    expect(dialogScope.getByText("Find the culprit before the law closes in.")).toBeInTheDocument();
    expect(dialogScope.getByText("The trail went cold outside town.")).toBeInTheDocument();
    expect(dialogScope.getByText("Day 5, Morning")).toBeInTheDocument();
    expect(dialogScope.getByText("Tumbleweed")).toBeInTheDocument();
    expect(dialogScope.getByText("At large")).toBeInTheDocument();
    expect(dialogScope.getAllByText("Gus Mercer").length).toBeGreaterThanOrEqual(5);
    expect(dialogScope.getAllByText("Discovered from wanted poster")).toHaveLength(3);
    expect(
      dialogScope.getAllByText((_, element) => element?.tagName === "P" && element.textContent?.includes("Known aliases: Red Wren")).length,
    ).toBeGreaterThanOrEqual(2);
    expect(
      dialogScope.getAllByText((_, element) => element?.tagName === "P" && element.textContent?.includes("Distinguishing features: Pale scar across the left cheek")).length,
    ).toBeGreaterThanOrEqual(2);
    expect(
      dialogScope.getAllByText((_, element) => element?.tagName === "P" && element.textContent?.includes("Issuing authority: County marshal")).length,
    ).toBeGreaterThanOrEqual(2);
    expect(
      dialogScope.getAllByText((_, element) => element?.tagName === "P" && element.textContent?.includes("Crime summary: Wanted for a string of robberies near the county line.")).length,
    ).toBeGreaterThanOrEqual(2);
    expect(dialogScope.queryByText("Also linked to: Red Wren")).not.toBeInTheDocument();
    expect(dialogScope.queryByText("Mabel Quinn")).not.toBeInTheDocument();
    expect(dialogScope.getByText("No named record links this lead yet.")).toBeInTheDocument();
    expect(dialogScope.getAllByText((_, element) => element?.tagName === "P" && element.textContent?.includes("Dead or alive")).length).toBeGreaterThanOrEqual(2);
    expect(dialogScope.getAllByText((_, element) => element?.tagName === "P" && element.textContent?.includes("$2,500.50")).length).toBeGreaterThanOrEqual(2);
    expect(dialogScope.getByText("Known name")).toBeInTheDocument();
    const wantedPosterSection = dialogScope.getByRole("heading", { name: "Wanted posters" }).closest("article");
    expect(wantedPosterSection).not.toBeNull();
    const wantedPosterScope = within(wantedPosterSection as HTMLElement);
    expect(wantedPosterScope.getByRole("heading", { name: "Gus Mercer" })).toBeInTheDocument();
    expect(wantedPosterScope.getByText("Red Wren")).toBeInTheDocument();
    expect(wantedPosterScope.getByText("Dead or alive, $2,500.50 bounty")).toBeInTheDocument();
    expect(wantedPosterScope.getByText("gang-affiliated wanted criminal")).toBeInTheDocument();
    expect(wantedPosterScope.getByText("Wanted for a string of robberies near the county line.")).toBeInTheDocument();
    expect(wantedPosterScope.getByText("Public-safe sheriff notices, quick views, and feature notes from the current board.")).toBeInTheDocument();
    expect(screen.queryByText("clue-1")).not.toBeInTheDocument();
    expect(screen.queryByText("suspect-1")).not.toBeInTheDocument();
    const redMesaRows = dialogScope.getAllByText((_, element) => element?.textContent === "Location: Red Mesa road");
    expect(redMesaRows.some((element) => element.tagName === "LI")).toBe(true);
    expect(redMesaRows.filter((element) => element.tagName === "LI")).toHaveLength(1);
    expect(
      dialogScope.queryAllByText((_, element) => element?.textContent === "Place: Red Mesa road"),
    ).toHaveLength(0);
    expect(
      dialogScope.queryAllByText((_, element) => element?.textContent === "Route: Back trail"),
    ).toHaveLength(1);

    await user.click(screen.getByRole("button", { name: /read wanted posters/i }));

    await waitFor(() => {
      expect(mockedReadWantedPosters).toHaveBeenCalledWith("game-1");
    });

    expect(wantedPosterScope.getByRole("heading", { name: "Gus Mercer" })).toBeInTheDocument();
    expect(wantedPosterScope.getByText("Red Wren")).toBeInTheDocument();
    expect(wantedPosterScope.getByText("Dead or alive, $2,500.50 bounty")).toBeInTheDocument();
    expect(wantedPosterScope.getAllByText("County marshal")).toHaveLength(2);
    expect(wantedPosterScope.getByText("Wanted for a string of robberies near the county line.")).toBeInTheDocument();
    expect(wantedPosterScope.getAllByText("Pale scar across the left cheek").length).toBeGreaterThanOrEqual(2);
    expect(wantedPosterScope.getByText("Known as Red Wren")).toBeInTheDocument();
    expect(wantedPosterScope.getByText("gang-affiliated wanted criminal")).toBeInTheDocument();
    expect(dialogScope.queryByText("Mira Cline")).not.toBeInTheDocument();
    expect(dialogScope.queryByText("targetKind")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /close/i }));

    await waitFor(() => {
      expect(dialog).not.toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /open journal/i }));

    const journalDialog = await screen.findByRole("dialog", { name: /journal/i });
    const journalScope = within(journalDialog);
    expect(journalScope.getByRole("heading", { level: 2, name: /journal/i })).toBeInTheDocument();
    expect(journalScope.getByText("Booted")).toBeInTheDocument();
    expect(journalScope.getByText("Day 5, Morning in Tumbleweed")).toBeInTheDocument();
    expect(journalScope.queryByText("Find the culprit before the law closes in.")).not.toBeInTheDocument();
    expect(journalScope.queryByText("trueCulpritId")).not.toBeInTheDocument();
    expect(journalScope.queryByText("killerReleaseState")).not.toBeInTheDocument();
    expect(journalScope.queryByText("clue-1")).not.toBeInTheDocument();
  });

  it("runs the saloon look-around action and refreshes the journal state", async () => {
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
      { kind: AvailableActionKind.LookAroundSaloon, label: "Look around saloon" },
    ]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedCreateGame.mockResolvedValue(createSession());
    mockedBuyStoreItem.mockResolvedValue({
      success: true,
      message: "Purchased",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    mockedLookAroundSaloon.mockResolvedValue({
      success: true,
      message: "I spot a suspicious rider near the bar.",
      currentJournal: createJournal(),
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: createJournal(),
    });
    mockedCheckLocalRecords.mockResolvedValue({
      success: true,
      message: "Check local records",
      currentJournal: createJournal(),
    });
    mockedFollowTelegraphLeads.mockResolvedValue({
      success: true,
      message: "Follow telegraph leads",
      currentJournal: createJournal(),
    });
    mockedGatherLocalGossip.mockResolvedValue({
      success: true,
      message: "Gather local gossip",
      currentJournal: createJournal(),
    });
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Travelled",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });

    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderInProvider();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
      expect(mockedGetAvailableActions).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    });

    await user.click(await screen.findByRole("button", { name: /look around saloon/i }));

    await waitFor(() => {
      expect(mockedLookAroundSaloon).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal.mock.calls.length).toBeGreaterThan(1);
    });

    expect(screen.getByText("I spot a suspicious rider near the bar.")).toBeInTheDocument();
  });

  it("shows a saloon person-of-interest action after a person surfaces and clears it after the person flees", async () => {
    const surfacedSession: GameSessionDto = {
      ...createSession(),
      activeSaloonPersonOfInterest: {
        descriptor: "Grey Jay",
        kind: SaloonPersonOfInterestKind.WantedSuspect,
      },
    };
    const clearedSession: GameSessionDto = {
      ...createSession(),
      activeSaloonPersonOfInterest: null,
    };

    mockedGetGame.mockResolvedValue(clearedSession);
    mockedGetGame.mockResolvedValueOnce(createSession());
    mockedGetGame.mockResolvedValueOnce(surfacedSession);
    mockedGetGame.mockResolvedValueOnce(surfacedSession);
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
      { kind: AvailableActionKind.LookAroundSaloon, label: "Look around saloon" },
    ]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedCreateGame.mockResolvedValue(createSession());
    mockedBuyStoreItem.mockResolvedValue({
      success: true,
      message: "Purchased",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    mockedLookAroundSaloon.mockResolvedValue({
      success: true,
      message: "Grey Jay is in the saloon.",
      currentJournal: createJournal(),
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "Read wanted posters",
      currentJournal: createJournal(),
      wantedPosters: [createWantedPoster()],
    });
    mockedConfrontSaloonPersonOfInterest.mockResolvedValue({
      success: true,
      message: "You confront Mira Cline, but they get away.",
      outcome: 1,
      currentSession: clearedSession,
      declaredWantedIdentityHandle: "warrant-public-1",
      targetName: "Mira Cline",
      disposition: 1,
      isAlive: true,
      isSecured: false,
      isCitizen: false,
      fineAmount: null,
      walletBefore: null,
      walletAfter: null,
      sessionChanged: true,
      personOfInterestKind: SaloonPersonOfInterestKind.WantedSuspect,
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: createJournal(),
    });
    mockedCheckLocalRecords.mockResolvedValue({
      success: true,
      message: "Check local records",
      currentJournal: createJournal(),
    });
    mockedFollowTelegraphLeads.mockResolvedValue({
      success: true,
      message: "Follow telegraph leads",
      currentJournal: createJournal(),
    });
    mockedGatherLocalGossip.mockResolvedValue({
      success: true,
      message: "Gather local gossip",
      currentJournal: createJournal(),
    });
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Travelled",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });

    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderInProvider();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
      expect(mockedGetAvailableActions).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    });

    await user.click(await screen.findByRole("button", { name: /read wanted posters/i }));

    await user.click(await screen.findByRole("button", { name: /look around saloon/i }));

    const confrontButton = await screen.findByRole("button", {
      name: /take grey jay to sheriff as mira cline/i,
    });
    await user.click(confrontButton);

    await waitFor(() => {
      expect(mockedConfrontSaloonPersonOfInterest).toHaveBeenCalledWith("game-1", "warrant-public-1");
      expect(screen.queryByRole("button", { name: /take grey jay to sheriff as mira cline/i })).not.toBeInTheDocument();
    });

    expect(screen.getByText("You confront Mira Cline, but they get away.")).toBeInTheDocument();
  });

  it("shows the citizen fine and wallet change after a wrong declaration in the saloon", async () => {
    const surfacedSession: GameSessionDto = {
      ...createSession(),
      activeSaloonPersonOfInterest: {
        descriptor: "a town clerk from Current Town",
        kind: SaloonPersonOfInterestKind.Citizen,
      },
    };
    const clearedSession: GameSessionDto = {
      ...createSession(),
      activeSaloonPersonOfInterest: null,
    };

    mockedGetGame.mockResolvedValue(clearedSession);
    mockedGetGame.mockResolvedValueOnce(createSession());
    mockedGetGame.mockResolvedValueOnce(surfacedSession);
    mockedGetGame.mockResolvedValueOnce(surfacedSession);
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
      { kind: AvailableActionKind.LookAroundSaloon, label: "Look around saloon" },
    ]);
    mockedGetJournal.mockResolvedValue(createJournal());
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedCreateGame.mockResolvedValue(createSession());
    mockedBuyStoreItem.mockResolvedValue({
      success: true,
      message: "Purchased",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    mockedLookAroundSaloon.mockResolvedValue({
      success: true,
      message: "You look around the saloon and spot a town clerk from Current Town.",
      currentJournal: createJournal(),
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "Read wanted posters",
      currentJournal: createJournal(),
      wantedPosters: [createWantedPoster()],
    });
    mockedConfrontSaloonPersonOfInterest.mockResolvedValue({
      success: true,
      message: "You bring a town clerk from Current Town to the sheriff, but the declaration is wrong. The sheriff releases them and fines you $4.00.",
      outcome: 5,
      currentSession: clearedSession,
      declaredWantedIdentityHandle: "warrant-public-1",
      targetName: "a town clerk from Current Town",
      disposition: null,
      isAlive: null,
      isSecured: null,
      isCitizen: true,
      fineAmount: 4,
      walletBefore: 4,
      walletAfter: 0,
      sessionChanged: true,
      personOfInterestKind: SaloonPersonOfInterestKind.Citizen,
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: createJournal(),
    });
    mockedCheckLocalRecords.mockResolvedValue({
      success: true,
      message: "Check local records",
      currentJournal: createJournal(),
    });
    mockedFollowTelegraphLeads.mockResolvedValue({
      success: true,
      message: "Follow telegraph leads",
      currentJournal: createJournal(),
    });
    mockedGatherLocalGossip.mockResolvedValue({
      success: true,
      message: "Gather local gossip",
      currentJournal: createJournal(),
    });
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Travelled",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });

    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderInProvider();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
      expect(mockedGetAvailableActions).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    });

    await user.click(await screen.findByRole("button", { name: /read wanted posters/i }));

    await user.click(await screen.findByRole("button", { name: /look around saloon/i }));

    const confrontButton = await screen.findByRole("button", {
      name: /take a town clerk from current town to sheriff as mira cline/i,
    });
    await user.click(confrontButton);

    await waitFor(() => {
      expect(mockedConfrontSaloonPersonOfInterest).toHaveBeenCalledWith("game-1", "warrant-public-1");
      expect(screen.queryByRole("button", { name: /take a town clerk from current town to sheriff as mira cline/i })).not.toBeInTheDocument();
    });

    expect(screen.getByText("You bring a town clerk from Current Town to the sheriff, but the declaration is wrong. The sheriff releases them and fines you $4.00. Wallet $4.00 -> $0.00.")).toBeInTheDocument();
  });

  it("keeps captured wanted identities compact and out of active case-file clutter", async () => {
    const capturedJournal = createCapturedJournal();

    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(capturedJournal);
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedCreateGame.mockResolvedValue(createSession());
    mockedBuyStoreItem.mockResolvedValue({
      success: true,
      message: "Purchased",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "Read wanted posters",
      currentJournal: capturedJournal,
      wantedPosters: capturedJournal.caseFile.wantedPosters,
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: capturedJournal,
    });
    mockedCheckLocalRecords.mockResolvedValue({
      success: true,
      message: "Check local records",
      currentJournal: capturedJournal,
    });
    mockedFollowTelegraphLeads.mockResolvedValue({
      success: true,
      message: "Follow telegraph leads",
      currentJournal: capturedJournal,
    });
    mockedGatherLocalGossip.mockResolvedValue({
      success: true,
      message: "Gather local gossip",
      currentJournal: capturedJournal,
    });
    mockedLookAroundSaloon.mockResolvedValue({
      success: true,
      message: "Look around saloon",
      currentJournal: capturedJournal,
    });
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Travelled",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });

    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderInProvider();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    });

    await user.click(await screen.findByRole("button", { name: /open case file/i }));
    const dialog = await screen.findByRole("dialog", { name: /investigation board/i });
    const dialogScope = within(dialog);

    expect(dialogScope.getAllByRole("heading", { name: "Gus Mercer" }).length).toBeGreaterThanOrEqual(1);
    expect(dialogScope.getAllByText("Captured").length).toBeGreaterThanOrEqual(1);
    expect(dialogScope.getAllByText("Captured alive on Day 5, Morning. Sheriff paid $2,500.50.").length).toBeGreaterThanOrEqual(1);

    const warrantsSection = dialogScope.getByRole("heading", { name: "Warrants" }).closest("article");
    expect(warrantsSection).not.toBeNull();
    const warrantsScope = within(warrantsSection as HTMLElement);
    expect(warrantsScope.queryByRole("heading", { name: "Gus Mercer" })).not.toBeInTheDocument();
    expect(warrantsScope.getByRole("heading", { name: "Mabel Quinn" })).toBeInTheDocument();

    const evidenceSection = dialogScope.getByRole("heading", { name: "Evidence stack" }).closest("article");
    expect(evidenceSection).not.toBeNull();
    const evidenceScope = within(evidenceSection as HTMLElement);
    expect(evidenceScope.queryByText("A boot print with Gus Mercer's ranch brand was found near the creek.")).not.toBeInTheDocument();
    expect(evidenceScope.getByText("The witness says the rider wore a blue coat, not the brown coat listed on the warrant.")).toBeInTheDocument();

    const wantedPosterSection = dialogScope.getByRole("heading", { name: "Wanted posters" }).closest("article");
    expect(wantedPosterSection).not.toBeNull();
    const wantedPosterScope = within(wantedPosterSection as HTMLElement);
    expect(wantedPosterScope.queryByRole("heading", { name: "Gus Mercer" })).not.toBeInTheDocument();
    expect(wantedPosterScope.getByRole("heading", { name: "Mabel Quinn" })).toBeInTheDocument();
    expect(dialogScope.queryByText("trueCulpritId")).not.toBeInTheDocument();
  });

  it("shows a clean empty wanted-poster state when the response is empty", async () => {
    const emptyJournal = createJournal();
    emptyJournal.caseFile.wantedPosters = [];

    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    ]);
    mockedGetJournal.mockResolvedValue(emptyJournal);
    mockedGetTownStoreOffers.mockResolvedValue(createStoreOffers());
    mockedCreateGame.mockResolvedValue(createSession());
    mockedBuyStoreItem.mockResolvedValue({
      success: true,
      message: "Purchased",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });
    mockedReadWantedPosters.mockResolvedValue({
      success: true,
      message: "Read wanted posters",
      currentJournal: emptyJournal,
      wantedPosters: [],
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: emptyJournal,
    });
    mockedCheckLocalRecords.mockResolvedValue({
      success: true,
      message: "Check local records",
      currentJournal: emptyJournal,
    });
    mockedFollowTelegraphLeads.mockResolvedValue({
      success: true,
      message: "Follow telegraph leads",
      currentJournal: emptyJournal,
    });
    mockedGatherLocalGossip.mockResolvedValue({
      success: true,
      message: "Gather local gossip",
      currentJournal: emptyJournal,
    });
    mockedLookAroundSaloon.mockResolvedValue({
      success: true,
      message: "Look around saloon",
      currentJournal: emptyJournal,
    });
    mockedTravel.mockResolvedValue({
      success: true,
      message: "Travelled",
      currentSession: createSession(),
      journeyStatus: null,
      journey: null,
      trailEvent: null,
      travelDiary: null,
    });

    window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

    renderInProvider();

    const user = userEvent.setup();

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
      expect(mockedGetAvailableActions).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
    });

    await user.click(await screen.findByRole("button", { name: /open case file/i }));
    const dialog = await screen.findByRole("dialog", { name: /investigation board/i });
    const dialogScope = within(dialog);

    await user.click(screen.getByRole("button", { name: /read wanted posters/i }));

    await waitFor(() => {
      expect(mockedReadWantedPosters).toHaveBeenCalledWith("game-1");
    });

    expect(dialogScope.getByRole("heading", { name: /wanted posters/i })).toBeInTheDocument();
    expect(dialogScope.getByText("No wanted posters are known yet.")).toBeInTheDocument();
    expect(dialogScope.queryByText("Mira Cline")).not.toBeInTheDocument();
  });
});
