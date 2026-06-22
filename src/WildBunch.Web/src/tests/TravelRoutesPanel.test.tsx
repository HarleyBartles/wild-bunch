import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { GameSessionDto } from "../api/types";
import { previewTravel } from "../api/wildBunchApi";
import { TravelRoutesPanel } from "../components/TravelRoutesPanel";

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
        { id: "dryfork", name: "Dry Fork", services: 0 },
      ],
      trails: [
        {
          id: "trail-1",
          fromTownId: "pinecross",
          toTownId: "holloway",
          risk: 2,
          terrain: 1,
          waterFeature: 2,
          rideDayDistance: 4,
        },
        {
          id: "trail-2",
          fromTownId: "pinecross",
          toTownId: "dryfork",
          risk: 3,
          terrain: 2,
          waterFeature: 0,
          rideDayDistance: 2,
        },
      ],
    },
    caseFile: {
      accusationId: null,
      openingLead: "",
      caseState: {
        statusText: "",
      },
      discoveredSuspects: [],
      caseBoard: {
        namedRecords: [],
        looseLeads: [],
        evidenceItems: [],
      },
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
    activeSaloonPersonOfInterest: null,
    ...overrides,
  };
}

describe("TravelRoutesPanel", () => {
  it("shows preview-backed route details before the player starts traveling", async () => {
    const user = userEvent.setup();
    const session = createSession();
    const onTravel = vi.fn().mockResolvedValue(undefined);

    mockedPreviewTravel.mockImplementation(async (_gameId, destinationTownId) => {
      if (destinationTownId === "holloway") {
        return {
          success: true,
          message: "Preview ready.",
          preview: {
            originTownId: "pinecross",
            originTownName: "Pinecross",
            destinationTownId: "holloway",
            destinationTownName: "Holloway",
            travelMode: 1,
            mountedTravelAvailable: false,
            waterSecure: true,
            rideDayDistance: 4,
            remainingRideDayDistance: 4,
            baselineRideDays: 3,
            expectedDays: 6,
            remainingDays: 6,
            canteenChargesPerDay: 0,
            requiredCanteenCharges: 0,
            availableCanteenCharges: 10,
            canteenReserveCharges: 10,
            delayMarginDays: 0,
            delayRisk: false,
            requiredFood: 6,
            availableFood: 6,
            requiredHorseFeed: 0,
            availableHorseFeed: 0,
            horseState: null,
            warnings: ["Route water is secure."],
            routeProfile: {
              trailId: "trail-1",
              risk: 1,
              terrain: 0,
              waterFeature: 1,
              rideDayDistance: 4,
              mountedRideDayProgress: 1.5,
              footRideDayProgress: 0.75,
              warnings: ["Open range and creek water keep this route manageable."],
            },
          },
        };
      }

      return {
        success: true,
        message: "Preview ready.",
        preview: {
          originTownId: "pinecross",
          originTownName: "Pinecross",
          destinationTownId: "dryfork",
          destinationTownName: "Dry Fork",
          travelMode: 1,
          mountedTravelAvailable: false,
          waterSecure: false,
          rideDayDistance: 2,
          remainingRideDayDistance: 2,
          baselineRideDays: 2,
          expectedDays: 2,
          remainingDays: 2,
          canteenChargesPerDay: 1,
          requiredCanteenCharges: 2,
          availableCanteenCharges: 2,
          canteenReserveCharges: 0,
          delayMarginDays: 0,
          delayRisk: true,
          requiredFood: 2,
          availableFood: 2,
          requiredHorseFeed: 0,
          availableHorseFeed: 0,
          horseState: null,
          warnings: ["Water is sparse along this trail."],
          routeProfile: {
            trailId: "trail-2",
            risk: 3,
            terrain: 2,
            waterFeature: 0,
            rideDayDistance: 2,
            mountedRideDayProgress: 1.5,
            footRideDayProgress: 0.75,
            warnings: ["Rough trail conditions may stress the horse.", "Water is sparse along this trail."],
          },
        },
      };
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
      expect(mockedPreviewTravel).toHaveBeenCalledWith("game-1", "dryfork");
    });

    expect(await screen.findByRole("button", { name: /holloway/i })).toBeInTheDocument();
    expect(screen.getByText(/3 days ride \| Open range \| Creek \| Low risk/i)).toBeInTheDocument();
    expect(screen.getByText(/2 days ride \| Badlands \| None \| High risk/i)).toBeInTheDocument();
    expect(screen.queryByText(/ride-day units/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /holloway/i }));
    expect(onTravel).toHaveBeenCalledWith("holloway");
  });
});
