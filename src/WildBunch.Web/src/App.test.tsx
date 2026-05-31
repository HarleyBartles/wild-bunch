import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
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
      killerReleaseState: {
        isReleased: false,
        progress: 0,
        requiredPublicClues: 3,
        statusText: "Still chasing leads.",
      },
      discoveredSuspects: [],
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
      killerReleaseState: {
        isReleased: false,
        progress: 0,
        requiredPublicClues: 3,
        statusText: "Still chasing leads.",
      },
      caseSummary: "Find the culprit before the law closes in.",
      discoveredSuspects: [],
      knownClues: [],
      knownWarrants: [],
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
    expect(screen.getByRole("heading", { name: /case file/i })).toBeInTheDocument();
    expect(screen.getByText("Find the culprit before the law closes in.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /log/i })).toBeInTheDocument();
    expect(screen.getByText("Booted")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /store offers/i })).toBeInTheDocument();
    expect(screen.getByText("Food $2.00")).toBeInTheDocument();
  });
});
