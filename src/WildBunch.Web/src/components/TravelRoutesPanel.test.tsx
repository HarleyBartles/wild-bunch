import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { GameSessionDto } from "../api/types";
import { previewTravel } from "../api/wildBunchApi";
import { TravelRoutesPanel } from "./TravelRoutesPanel";

vi.mock("../api/wildBunchApi", () => ({
  previewTravel: vi.fn(),
}));

const mockedPreviewTravel = vi.mocked(previewTravel);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function createSession(overrides: Partial<GameSessionDto> = {}): GameSessionDto {
  return {
    id: "game-1",
    status: 0,
    travelDifficulty: 0,
    player: {
      name: "Ruth",
      currentTownId: "pinecross",
      health: 9,
    },
    world: {
      towns: [
        { id: "pinecross", name: "Pinecross", services: 0 },
        { id: "holloway", name: "Holloway", services: 0 },
      ],
      trails: [
        {
          id: "trail-1",
          fromTownId: "pinecross",
          toTownId: "holloway",
          risk: 2,
          terrain: 1,
          waterFeature: 2,
          rideDayDistance: 6,
        },
      ],
    },
    caseFile: {
      accusationId: null,
      openingLead: "",
      killerReleaseState: {
        isReleased: false,
        progress: 0,
        requiredPublicClues: 3,
        statusText: "",
      },
      discoveredSuspects: [],
      knownClues: [],
    },
    inventory: {
      wallet: { cash: 0 },
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
    clock: { day: 1, turn: 0 },
    pursuitState: { heat: 0 },
    journey: null,
    travelDiary: null,
    logEntries: [],
    ...overrides,
  };
}

describe("TravelRoutesPanel", () => {
  it("shows preview-backed route details before the player starts traveling", async () => {
    const user = userEvent.setup();
    const session = createSession();
    const onTravel = vi.fn().mockResolvedValue(undefined);

    mockedPreviewTravel.mockResolvedValue({
      success: true,
      message: "Preview ready.",
      preview: {
        originTownId: "pinecross",
        originTownName: "Pinecross",
        destinationTownId: "holloway",
        destinationTownName: "Holloway",
        travelMode: 0,
        mountedTravelAvailable: true,
        waterSecure: true,
        rideDayDistance: 6,
        remainingRideDayDistance: 6,
        expectedDays: 4,
        remainingDays: 4,
        canteenChargesPerDay: 0,
        requiredCanteenCharges: 0,
        availableCanteenCharges: 10,
        canteenReserveCharges: 10,
        delayMarginDays: 0,
        delayRisk: false,
        requiredFood: 4,
        availableFood: 4,
        requiredHorseFeed: 0,
        availableHorseFeed: 0,
        horseState: null,
        warnings: ["Route water is secure."],
        routeProfile: {
          trailId: "trail-1",
          risk: 2,
          terrain: 1,
          waterFeature: 2,
          rideDayDistance: 6,
          mountedRideDayProgress: 1.5,
          footRideDayProgress: 0.75,
          warnings: ["Rough trail conditions may stress the horse."],
        },
      },
    });

    render(
      <TravelRoutesPanel
        gameId={session.id}
        session={session}
        busy={false}
        onTravel={onTravel}
      />,
    );

    await waitFor(() => {
      expect(mockedPreviewTravel).toHaveBeenCalledWith("game-1", "holloway");
    });

    expect(await screen.findByRole("button", { name: /holloway/i })).toBeInTheDocument();
    expect(screen.getByText(/4 days/i)).toBeInTheDocument();
    expect(screen.getByText(/hills/i)).toBeInTheDocument();
    expect(screen.getByText(/river/i)).toBeInTheDocument();
    expect(screen.getByText(/moderate risk/i)).toBeInTheDocument();
    expect(screen.getByText(/6\.00 ride-day units/i)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /holloway/i }));
    expect(onTravel).toHaveBeenCalledWith("holloway");
  });
});
