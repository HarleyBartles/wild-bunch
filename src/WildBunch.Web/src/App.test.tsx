import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "./App";
import { AvailableActionKind, JourneyStatus, type GameSessionDto, type JournalDto, type TownStoreOffersDto } from "./api/types";
import {
  buyStoreItem,
  checkSheriffRecords,
  createGame,
  followTelegraphLeads,
  gatherLocalGossip,
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  inspectNoticeBoard,
  readWantedPosters,
  travel,
} from "./api/wildBunchApi";

vi.mock("./api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  createGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  checkSheriffRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
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
const mockedCheckSheriffRecords = vi.mocked(checkSheriffRecords);
const mockedInspectNoticeBoard = vi.mocked(inspectNoticeBoard);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);
const mockedFollowTelegraphLeads = vi.mocked(followTelegraphLeads);
const mockedGatherLocalGossip = vi.mocked(gatherLocalGossip);
const mockedTravel = vi.mocked(travel);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

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
    clock: { day: 5, turn: 2 },
    pursuitState: { heat: 1 },
    journey: null,
    travelDiary: null,
    logEntries: [{ kind: 0, message: "Booted", day: 1, turn: 0 }],
  };
}

function createJournal(): JournalDto {
  return {
    id: "game-1",
    status: 0,
    clock: { day: 5, turn: 2 },
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
    },
    logEntries: [{ kind: 0, message: "Booted", day: 1, turn: 0 }],
  };
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

describe("App", () => {
  it("hydrates the current session from local storage and loads store offers for the current town", async () => {
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
      { kind: AvailableActionKind.InspectNoticeBoard, label: "Inspect notice board" },
      { kind: AvailableActionKind.CheckSheriffRecords, label: "Check sheriff records" },
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
    });
    mockedInspectNoticeBoard.mockResolvedValue({
      success: true,
      message: "Inspect notice board",
      currentJournal: createJournal(),
    });
    mockedCheckSheriffRecords.mockResolvedValue({
      success: true,
      message: "Check sheriff records",
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

    render(<App />);

    await waitFor(() => {
      expect(mockedGetGame).toHaveBeenCalledWith("game-1");
      expect(mockedGetAvailableActions).toHaveBeenCalledWith("game-1");
      expect(mockedGetJournal).toHaveBeenCalledWith("game-1");
      expect(mockedGetTownStoreOffers).toHaveBeenCalledWith("game-1", "t-town");
    });

    expect(await screen.findByRole("heading", { name: /current session/i })).toBeInTheDocument();
    expect(screen.getByText("Tumbleweed (t-town)")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /log/i })).toBeInTheDocument();
    expect(screen.getByText("Booted")).toBeInTheDocument();
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
    expect(dialogScope.getByText("Day 5, turn 2")).toBeInTheDocument();
    expect(dialogScope.getByText("Tumbleweed")).toBeInTheDocument();
    expect(dialogScope.getByText("At large")).toBeInTheDocument();
    expect(dialogScope.getAllByText("Gus Mercer")).toHaveLength(3);
    expect(dialogScope.getAllByText("Discovered from wanted poster")).toHaveLength(3);
    expect(dialogScope.queryByText("Mabel Quinn")).not.toBeInTheDocument();
    expect(dialogScope.getByText("No named record links this lead yet.")).toBeInTheDocument();
    expect(dialogScope.getByText("Dead or alive")).toBeInTheDocument();
    expect(dialogScope.getByText("$2,500.50")).toBeInTheDocument();
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

    await user.click(screen.getByRole("button", { name: /close/i }));

    await waitFor(() => {
      expect(dialog).not.toBeInTheDocument();
    });
  });
});
