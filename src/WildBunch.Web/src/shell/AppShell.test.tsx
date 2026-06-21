import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppShell } from "./AppShell";
import { GameSessionProvider } from "../state/GameSessionContext";
import { AvailableActionKind, type GameSessionDto, type JournalDto } from "../api/types";
import {
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
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
  previewTravel: vi.fn(),
  travel: vi.fn(),
}));

const mockedGetGame = vi.mocked(getGame);
const mockedGetAvailableActions = vi.mocked(getAvailableActions);
const mockedGetJournal = vi.mocked(getJournal);
const mockedGetTownStoreOffers = vi.mocked(getTownStoreOffers);

function createSession(): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    travelDifficulty: 0,
    player: { name: "Ruth", currentTownId: "t-town", health: 9 },
    world: { towns: [{ id: "t-town", name: "Tumbleweed", services: 0 }], trails: [] },
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
    clock: { day: 5, turn: 2 },
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
    clock: { day: 5, turn: 2 },
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
    logEntries: [{ kind: 0, message: "Booted", day: 1, turn: 0 }],
  };
}

function renderShell() {
  return render(
    <GameSessionProvider>
      <AppShell />
    </GameSessionProvider>,
  );
}

beforeEach(() => {
  window.location.hash = "";
  mockedGetGame.mockResolvedValue(createSession());
  mockedGetAvailableActions.mockResolvedValue([
    { kind: AvailableActionKind.ReadWantedPosters, label: "Read wanted posters" },
  ]);
  mockedGetJournal.mockResolvedValue(createJournal());
  mockedGetTownStoreOffers.mockResolvedValue({
    townId: "t-town",
    townName: "Tumbleweed",
    available: true,
    sourceNote: "General store",
    offers: [],
  });
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
  window.location.hash = "";
});

describe("AppShell", () => {
  it("renders the persistent HUD and player navigation with the camp route as default", async () => {
    renderShell();

    await waitFor(() => expect(mockedGetGame).toHaveBeenCalledWith("game-1"));

    const hud = within(screen.getByRole("banner", { name: /game status/i }));
    expect(hud.getByText("Ruth")).toBeInTheDocument();
    expect(hud.getByText("5 / 2")).toBeInTheDocument();
    expect(hud.getByText("$14.00")).toBeInTheDocument();

    const nav = screen.getByRole("navigation", { name: /primary/i });
    expect(nav).toBeInTheDocument();
    for (const label of ["Camp", "Hunt", "Case file", "Wanted", "Trail", "Dev tools"]) {
      expect(screen.getByRole("button", { name: label })).toBeInTheDocument();
    }

    expect(screen.getByRole("heading", { name: /saddle up a new hunt/i })).toBeInTheDocument();
  });

  it("navigates to the promoted case-file route and the separated debug cockpit", async () => {
    renderShell();
    const user = userEvent.setup();

    await waitFor(() => expect(mockedGetJournal).toHaveBeenCalledWith("game-1"));

    await user.click(screen.getByRole("button", { name: "Case file" }));
    expect(await screen.findByRole("heading", { name: /investigation board/i })).toBeInTheDocument();
    expect(screen.getByText("Find the culprit before the law closes in.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Wanted" }));
    expect(await screen.findByRole("heading", { level: 1, name: /wanted posters/i })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Dev tools" }));
    expect(await screen.findByRole("heading", { name: /field cockpit/i })).toBeInTheDocument();
  });
});
