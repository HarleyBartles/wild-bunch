import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { SheriffPlace } from "../flow/places/SheriffPlace";
import { GameSessionProvider } from "../state/GameSessionProvider";
import {
  AvailableActionKind,
  InvestigationSourceKind,
  type GameSessionDto,
  type JournalDto,
} from "../api/types";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  checkLocalRecords,
  readWantedPosters,
} from "../api/wildBunchApi";

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
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

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);
const mockedCheckLocalRecords = vi.mocked(checkLocalRecords);
const mockedReadWantedPosters = vi.mocked(readWantedPosters);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function seedGameId(id: string) {
  window.localStorage.setItem("wild-bunch.current-game-id", id);
}

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    gameDifficulty: 0,
    gameEntropy: 1,
    startFlowPhase: 4,
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
      caseState: { statusText: "Still chasing leads." },
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
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
    logEntries: [],
    activeSaloonPersonOfInterest: null,
    wantedPosters: [],
  };
}

function createJournalWithSheriffLeads(): JournalDto {
  return {
    id: "game-1",
    status: 0,
    clock: { day: 5, turn: 2, timeOfDay: "Morning" },
    currentTown: { id: "t-town", name: "Tumbleweed" },
    caseFile: {
      accusationId: null,
      openingLead: "The trail went cold outside town.",
      caseState: { statusText: "Still chasing leads." },
      caseSummary: "Find the culprit before the law closes in.",
      discoveredSuspects: [],
      caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
      knownClues: [
        {
          id: "clue-local-records-1",
          kind: 2,
          description: "A public notice describes an unnamed rider who wears a silver spur.",
          sourceLabel: "sheriff record",
          context: "Public notice",
          sourceKind: InvestigationSourceKind.LocalRecords,
          anchors: { subjects: [], locations: [], times: [], directions: [] },
        },
        {
          id: "clue-gossip-1",
          kind: 7,
          description: "Local gossip says the rider kept to the rail spur after dark.",
          sourceLabel: "saloon talk",
          context: "Town gossip",
          sourceKind: InvestigationSourceKind.LocalGossip,
          anchors: { subjects: [], locations: [], times: [], directions: [] },
        },
      ],
      knownWarrants: [],
      wantedPosters: [],
    },
    logEntries: [],
  };
}

function renderSheriffPlace() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SheriffPlace onLeave={() => {}} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

function primeMocksWithSheriffLeads() {
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
    { kind: AvailableActionKind.CheckSheriffRecords, label: "Check local records" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournalWithSheriffLeads());
  mockedGetTownStoreOffers.mockResolvedValue({
    townId: "t-town",
    townName: "Tumbleweed",
    available: true,
    sourceNote: "General store",
    offers: [],
  });
  mockedReadWantedPosters.mockResolvedValue({
    success: true,
    message: "You read the wanted posters.",
    currentJournal: createJournalWithSheriffLeads(),
    wantedPosters: [],
  });
  mockedCheckLocalRecords.mockResolvedValue({
    success: true,
    message: "You check the local records and uncover a public lead.",
    currentJournal: createJournalWithSheriffLeads(),
  });
}

describe("SheriffPlace", () => {
  it("shows sheriff office leads (LocalRecords clues) inline and excludes non-sheriff clues", async () => {
    seedGameId("game-1");
    primeMocksWithSheriffLeads();
    renderSheriffPlace();

    await waitFor(() => {
      expect(screen.getByText("Sheriff Office")).toBeInTheDocument();
    });

    // The LocalRecords clue should appear inline.
    expect(
      screen.getByText("A public notice describes an unnamed rider who wears a silver spur."),
    ).toBeInTheDocument();

    // The LocalGossip clue should NOT appear in the sheriff office.
    expect(
      screen.queryByText("Local gossip says the rider kept to the rail spur after dark."),
    ).not.toBeInTheDocument();
  });

  it("shows a placeholder when no sheriff leads have been discovered yet", async () => {
    seedGameId("game-1");
    mockedGetGame.mockResolvedValue(createSession());
    mockedGetAvailableActions.mockResolvedValue([
      { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
      { kind: AvailableActionKind.CheckSheriffRecords, label: "Check local records" },
    ]);
    mockedGetJournal.mockResolvedValue({
      id: "game-1",
      status: 0,
      clock: { day: 5, turn: 2, timeOfDay: "Morning" },
      currentTown: { id: "t-town", name: "Tumbleweed" },
      caseFile: {
        accusationId: null,
        openingLead: "The trail went cold outside town.",
        caseState: { statusText: "Still chasing leads." },
        caseSummary: "Find the culprit before the law closes in.",
        discoveredSuspects: [],
        caseBoard: { namedRecords: [], looseLeads: [], evidenceItems: [] },
        knownClues: [],
        knownWarrants: [],
        wantedPosters: [],
      },
      logEntries: [],
    });
    mockedGetTownStoreOffers.mockResolvedValue({
      townId: "t-town",
      townName: "Tumbleweed",
      available: true,
      sourceNote: "General store",
      offers: [],
    });

    renderSheriffPlace();

    await waitFor(() => {
      expect(screen.getByText("Sheriff Office")).toBeInTheDocument();
    });

    expect(screen.getByText("No leads from local records yet.")).toBeInTheDocument();
  });
});
