import { expect, test, type Page } from "@playwright/test";
import { GameStatus, JourneyStatus } from "../src/api/types";
import { startIsolatedApi } from "./support/postgresApi";
import { seedWorldSeedCode } from "./support/seedWorldSeedCode";

const productionWebUrl = "http://127.0.0.1:4174";
const apiBaseUrl = "http://127.0.0.1:5275";

interface WorldMapResponse {
  towns: Array<{ id: string; name: string; x: number; y: number }>;
  trails: Array<{ id: string; fromTownId: string; toTownId: string; rideDayDistance: number }>;
}

async function clickTownOnWorldMap(
  page: Page,
  map: WorldMapResponse,
  town: WorldMapResponse["towns"][number],
) {
  const canvas = page.getByRole("img", { name: "World map" }).locator("canvas");
  await expect(canvas).toBeVisible();
  const bounds = await canvas.boundingBox();
  expect(bounds).not.toBeNull();
  if (!bounds) throw new Error("Expected the world map canvas to have browser bounds.");

  const canvasSize = await canvas.evaluate((element) => ({
    width: (element as HTMLCanvasElement).width,
    height: (element as HTMLCanvasElement).height,
  }));
  const minX = Math.min(...map.towns.map((candidate) => candidate.x));
  const maxX = Math.max(...map.towns.map((candidate) => candidate.x));
  const minY = Math.min(...map.towns.map((candidate) => candidate.y));
  const maxY = Math.max(...map.towns.map((candidate) => candidate.y));
  const padding = Math.min(70, canvasSize.width * 0.15, canvasSize.height * 0.15);
  const scale = Math.min(
    (canvasSize.width - padding * 2) / (maxX - minX || 1),
    (canvasSize.height - padding * 2) / (maxY - minY || 1),
  );
  const offsetX = (canvasSize.width - (maxX - minX) * scale) / 2;
  const offsetY = (canvasSize.height - (maxY - minY) * scale) / 2;
  const markerX = offsetX + (town.x - minX) * scale;
  const markerY = offsetY + (town.y - minY) * scale;

  await page.mouse.click(
    bounds.x + (markerX / canvasSize.width) * bounds.width,
    bounds.y + (markerY / canvasSize.height) * bounds.height,
  );
}

let stopIsolatedApi: (() => Promise<void>) | undefined;

test.beforeEach(async () => {
  test.setTimeout(120_000);
  stopIsolatedApi = await startIsolatedApi();
});

test.afterEach(async () => {
  await stopIsolatedApi?.();
  stopIsolatedApi = undefined;
});

test("a player can start, choose a town, and resume the server state after reopening", async ({
  browser,
}) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  context.on("page", (openedPage) => {
    openedPage.on("pageerror", (error) => console.error("Browser page error:", error.stack));
    openedPage.on("console", (message) => {
      if (message.type() === "error") console.error("Browser console error:", message.text());
    });
  });
  let page = await context.newPage();

  try {
    await page.goto(productionWebUrl);
    await page.getByLabel("Your name").fill(`Browser rider ${Date.now()}`);

    const setupResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/setup` &&
        response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Ride on" }).click();
    const setupResponse = await setupResponsePromise;
    expect(setupResponse.ok(), await setupResponse.text()).toBe(true);
    const setup = (await setupResponse.json()) as { id: string };

    await page.close();
    page = await context.newPage();
    await page.goto(productionWebUrl);

    const restoredPrologueResponse = await page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
      { timeout: 5_000 },
    );
    expect(restoredPrologueResponse.ok()).toBe(true);
    const restoredPrologue = (await restoredPrologueResponse.json()) as {
      startFlowPhase: number;
    };
    expect(restoredPrologue.startFlowPhase).toBe(1);
    const prologueHeading = page.getByRole("heading", { level: 2 });
    if (!(await prologueHeading.isVisible())) {
      throw new Error(
        `Expected restored prologue. Page text: ${await page.locator("body").innerText()}`,
      );
    }
    await expect(page.getByRole("button", { name: "Ride on" })).toBeVisible();

    const mapResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/world-map` &&
        response.request().method() === "GET",
    );
    await page.getByRole("button", { name: "Ride on" }).click();
    const mapResponse = await mapResponsePromise;
    expect(mapResponse.ok()).toBe(true);
    const map = (await mapResponse.json()) as WorldMapResponse;
    expect(map.towns.length).toBeGreaterThan(0);

    const town = map.towns[0];
    const startResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/start` &&
        response.request().method() === "POST",
    );
    await clickTownOnWorldMap(page, map, town);
    const startResponse = await startResponsePromise;
    expect(startResponse.ok()).toBe(true);
    const started = (await startResponse.json()) as {
      player: { currentTownId: string };
    };
    expect(started.player.currentTownId).toBe(town.id);
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    await page.close();
    page = await context.newPage();
    await page.goto(productionWebUrl);

    const restoredTownResponse = await page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    expect(restoredTownResponse.ok()).toBe(true);
    const restored = (await restoredTownResponse.json()) as {
      player: { currentTownId: string };
    };
    expect(restored.player.currentTownId).toBe(town.id);
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    await page.getByRole("button", { name: "Sheriff Office" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Sheriff Office" })).toBeVisible();

    const posterReadResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/wanted-posters/read` &&
        response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Read wanted posters" }).click();
    const posterReadResponse = await posterReadResponsePromise;
    expect(posterReadResponse.status()).toBe(200);
    const posterRead = (await posterReadResponse.json()) as {
      success: boolean;
      wantedPosters: Array<{ targetDisplayName: string }>;
    };
    expect(posterRead.success).toBe(true);
    expect(posterRead.wantedPosters).toHaveLength(1);
    const targetName = posterRead.wantedPosters[0].targetDisplayName;
    expect(targetName).toBeTruthy();
    await expect(page.getByRole("heading", { level: 4, name: targetName })).toBeVisible();

    await page.close();
    page = await context.newPage();
    await page.goto(productionWebUrl);

    const restoredPosterResponse = await page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    expect(restoredPosterResponse.ok()).toBe(true);
    const restoredGame = (await restoredPosterResponse.json()) as {
      wantedPosters: Array<{ targetDisplayName: string }>;
    };
    expect(restoredGame.wantedPosters.map((poster) => poster.targetDisplayName)).toEqual([
      targetName,
    ]);
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();
    await page.getByRole("button", { name: "Sheriff Office" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Sheriff Office" })).toBeVisible();
    await expect(page.getByRole("heading", { level: 4, name: targetName })).toBeVisible();
  } finally {
    await context.close();
  }
});

interface StartedMultiDayJourney {
  gameId: string;
  origin: WorldMapResponse["towns"][number];
  destination: WorldMapResponse["towns"][number];
  departureDay: number;
  initialRemainingDistance: number;
  expectedDays: number;
}

async function startMultiDayJourney(
  page: Page,
  difficulty: "Standard" | "Brutal" = "Standard",
): Promise<StartedMultiDayJourney> {
  await page.goto(productionWebUrl);
  await page.getByLabel("Your name").fill(`Browser rider ${Date.now()}`);
  await page.getByRole("button", { name: difficulty }).click();
  await page.getByRole("button", { name: "Boring" }).click();
  await page.getByLabel("World seed").fill(seedWorldSeedCode());

  const setupResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/setup` && response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Ride on" }).click();
  const setupResponse = await setupResponsePromise;
  expect(setupResponse.ok(), await setupResponse.text()).toBe(true);
  const setup = (await setupResponse.json()) as { id: string };

  await page.getByRole("button", { name: "Ride on" }).click();
  await expect(page.getByRole("heading", { level: 2 })).toBeVisible();
  const mapResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/world-map` &&
      response.request().method() === "GET",
  );
  await page.getByRole("button", { name: "Ride on" }).click();
  const mapResponse = await mapResponsePromise;
  expect(mapResponse.ok()).toBe(true);
  const map = (await mapResponse.json()) as WorldMapResponse;
  const longestTrail = map.trails.reduce<WorldMapResponse["trails"][number] | null>(
    (longest, candidate) =>
      !longest || candidate.rideDayDistance > longest.rideDayDistance ? candidate : longest,
    null,
  );
  expect(longestTrail, "The world map should have a connected trail.").not.toBeNull();
  if (!longestTrail) throw new Error("Expected the world map to contain a connected trail.");

  const origin = map.towns.find((town) => town.id === longestTrail.fromTownId);
  const destination = map.towns.find((town) => town.id === longestTrail.toTownId);
  expect(origin, "The longest trail should have a mapped origin town.").toBeDefined();
  expect(destination, "The longest trail should have a mapped destination town.").toBeDefined();
  if (!origin || !destination)
    throw new Error("Expected both towns of the longest trail on the map.");

  const startResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/start` &&
      response.request().method() === "POST",
  );
  await clickTownOnWorldMap(page, map, origin);
  const startResponse = await startResponsePromise;
  expect(startResponse.ok(), await startResponse.text()).toBe(true);
  const startResult = (await startResponse.json()) as { player: { currentTownId: string } };
  expect(startResult.player.currentTownId).toBe(origin.id);
  await expect(page.getByRole("heading", { level: 1, name: origin.name })).toBeVisible();

  await page.getByRole("button", { name: "Hit the trail" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Hit the trail" })).toBeVisible();
  const previewResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/travel/preview/${destination.id}` &&
      response.request().method() === "GET",
  );
  await clickTownOnWorldMap(page, map, destination);
  const previewResponse = await previewResponsePromise;
  expect(previewResponse.ok()).toBe(true);
  const previewResult = (await previewResponse.json()) as {
    success: boolean;
    preview: {
      originTownId: string;
      destinationTownId: string;
      expectedDays: number;
      remainingRideDayDistance: number;
    } | null;
  };
  expect(previewResult.success).toBe(true);
  expect(previewResult.preview?.originTownId).toBe(origin.id);
  expect(previewResult.preview?.destinationTownId).toBe(destination.id);
  expect(previewResult.preview?.expectedDays).toBeGreaterThan(1);
  await expect(page.getByRole("heading", { level: 1, name: "Prepare to ride" })).toBeVisible();

  const travelResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/travel` &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Start the ride" }).click();
  const travelResponse = await travelResponsePromise;
  expect(travelResponse.ok(), await travelResponse.text()).toBe(true);
  const travelResult = (await travelResponse.json()) as {
    success: boolean;
    currentSession: {
      clock: { day: number };
      journey: {
        originTownId: string;
        destinationTownId: string;
        status: number;
        remainingRideDayDistance: number;
      } | null;
    };
  };
  expect(travelResult.success).toBe(true);
  expect(travelResult.currentSession.journey?.originTownId).toBe(origin.id);
  expect(travelResult.currentSession.journey?.destinationTownId).toBe(destination.id);
  expect(travelResult.currentSession.journey?.status).toBe(JourneyStatus.Active);
  expect(travelResult.currentSession.journey?.remainingRideDayDistance).toBeGreaterThan(0);

  return {
    gameId: setup.id,
    origin,
    destination,
    departureDay: travelResult.currentSession.clock.day,
    initialRemainingDistance: travelResult.currentSession.journey!.remainingRideDayDistance,
    expectedDays: previewResult.preview!.expectedDays,
  };
}

test("a player can start travel and restore the active journey after reopening", async ({
  browser,
}) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const travelPosts: string[] = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (
        request.method() === "POST" &&
        /\/api\/games\/[^/]+\/travel(?:\/advance)?$/.test(request.url())
      ) {
        travelPosts.push(request.url());
      }
    });
  });
  let page = await context.newPage();

  try {
    const trip = await startMultiDayJourney(page);
    expect(
      travelPosts.filter((url) => url.endsWith(`/api/games/${trip.gameId}/travel`)),
    ).toHaveLength(1);

    await page.close();
    page = await context.newPage();
    const restoredResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const restoredResponse = await restoredResponsePromise;
    expect(restoredResponse.ok()).toBe(true);
    const restored = (await restoredResponse.json()) as {
      journey: {
        originTownId: string;
        destinationTownId: string;
        status: number;
      } | null;
    };
    expect(restored.journey?.originTownId).toBe(trip.origin.id);
    expect(restored.journey?.destinationTownId).toBe(trip.destination.id);
    expect(restored.journey?.status).toBe(JourneyStatus.Active);
    await expect(page.getByRole("button", { name: "Advance travel day" })).toBeVisible();
    expect(
      travelPosts.filter((url) => url.endsWith(`/api/games/${trip.gameId}/travel`)),
    ).toHaveLength(1);
  } finally {
    await context.close();
  }
});

test("a player can advance one travel day and restore the updated trail state", async ({
  browser,
}) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const advancePosts: string[] = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (
        request.method() === "POST" &&
        /\/api\/games\/[^/]+\/travel\/advance$/.test(request.url())
      ) {
        advancePosts.push(request.url());
      }
    });
  });
  let page = await context.newPage();

  try {
    const trip = await startMultiDayJourney(page);
    await page.close();
    page = await context.newPage();

    const preAdvanceResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const preAdvanceResponse = await preAdvanceResponsePromise;
    expect(preAdvanceResponse.ok()).toBe(true);
    const preAdvance = (await preAdvanceResponse.json()) as {
      journey: {
        status: number;
        daysTravelled: number;
        remainingRideDayDistance: number;
      } | null;
    };
    expect(preAdvance.journey?.status).toBe(JourneyStatus.Active);
    expect(preAdvance.journey?.daysTravelled).toBe(0);
    expect(preAdvance.journey?.remainingRideDayDistance).toBe(trip.initialRemainingDistance);
    await expect(page.getByRole("button", { name: "Advance travel day" })).toBeVisible();

    const advanceResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}/travel/advance` &&
        response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Advance travel day" }).click();
    const advanceResponse = await advanceResponsePromise;
    expect(advanceResponse.ok(), await advanceResponse.text()).toBe(true);
    const advanced = (await advanceResponse.json()) as {
      success: boolean;
      currentSession: {
        clock: { day: number };
        journey: {
          originTownId: string;
          destinationTownId: string;
          status: number;
          daysTravelled: number;
          remainingRideDayDistance: number;
          pendingEncounter: {
            message: string;
            choices: Array<{ id: string; label: string }>;
          } | null;
        } | null;
        travelDiary: { days: Array<{ dayNumber: number }> } | null;
      };
    };
    expect(advanced.success).toBe(true);
    expect(advanced.currentSession.clock.day).toBe(trip.departureDay + 1);
    expect(advanced.currentSession.journey?.originTownId).toBe(trip.origin.id);
    expect(advanced.currentSession.journey?.destinationTownId).toBe(trip.destination.id);
    expect(advanced.currentSession.journey?.daysTravelled).toBe(1);
    expect(advanced.currentSession.journey?.remainingRideDayDistance).toBeGreaterThan(0);
    expect(advanced.currentSession.journey?.remainingRideDayDistance).toBeLessThan(
      trip.initialRemainingDistance,
    );
    expect(advanced.currentSession.travelDiary?.days).toHaveLength(1);
    expect(advanced.currentSession.travelDiary?.days[0]?.dayNumber).toBe(1);
    expect(advanced.currentSession.journey?.status).not.toBe(JourneyStatus.Completed);
    await expect(page.getByRole("button", { name: "Step into town" })).toHaveCount(0);
    await expect(page.getByRole("heading", { name: "Day 1", exact: true })).toBeVisible();

    const pendingEncounter = advanced.currentSession.journey?.pendingEncounter;
    if (pendingEncounter) {
      expect(advanced.currentSession.journey?.status).toBe(JourneyStatus.Interrupted);
      await expect(page.getByText(pendingEncounter.message)).toBeVisible();
      const firstChoice = pendingEncounter.choices[0];
      expect(firstChoice).toBeDefined();
      if (!firstChoice) throw new Error("Expected the pending encounter to offer a legal choice.");
      await expect(page.getByRole("button", { name: firstChoice.label })).toBeVisible();
      await expect(page.getByRole("button", { name: "Advance travel day" })).toHaveCount(0);
    } else {
      expect(advanced.currentSession.journey?.status).toBe(JourneyStatus.Active);
      await expect(page.getByRole("button", { name: "Advance travel day" })).toBeVisible();
    }

    expect(advancePosts).toEqual([`${apiBaseUrl}/api/games/${trip.gameId}/travel/advance`]);
    await page.close();
    page = await context.newPage();
    const restoredResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const restoredResponse = await restoredResponsePromise;
    expect(restoredResponse.ok()).toBe(true);
    const restored = (await restoredResponse.json()) as {
      clock: { day: number };
      journey: {
        originTownId: string;
        destinationTownId: string;
        status: number;
        daysTravelled: number;
        remainingRideDayDistance: number;
        pendingEncounter: {
          message: string;
          choices: Array<{ id: string; label: string }>;
        } | null;
      } | null;
      travelDiary: { days: Array<{ dayNumber: number }> } | null;
    };
    expect(restored.clock.day).toBe(trip.departureDay + 1);
    expect(restored.journey?.originTownId).toBe(trip.origin.id);
    expect(restored.journey?.destinationTownId).toBe(trip.destination.id);
    expect(restored.journey?.daysTravelled).toBe(1);
    expect(restored.journey?.remainingRideDayDistance).toBe(
      advanced.currentSession.journey?.remainingRideDayDistance,
    );
    expect(restored.travelDiary?.days.map((day) => day.dayNumber)).toContain(1);
    await expect(page.getByRole("heading", { name: "Day 1", exact: true })).toBeVisible();

    if (restored.journey?.pendingEncounter) {
      await expect(page.getByText(restored.journey.pendingEncounter.message)).toBeVisible();
      const firstChoice = restored.journey.pendingEncounter.choices[0];
      expect(firstChoice).toBeDefined();
      if (!firstChoice) throw new Error("Expected the restored encounter to offer a legal choice.");
      await expect(page.getByRole("button", { name: firstChoice.label })).toBeVisible();
      await expect(page.getByRole("button", { name: "Advance travel day" })).toHaveCount(0);
    } else {
      expect(restored.journey?.status).toBe(JourneyStatus.Active);
      await expect(page.getByRole("button", { name: "Advance travel day" })).toBeVisible();
    }

    expect(advancePosts).toHaveLength(1);
  } finally {
    await context.close();
  }
});

test("a player can resolve a hostile trail encounter and restore its journal result", async ({
  browser,
}) => {
  test.setTimeout(180_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const advancePosts: string[] = [];
  const resolvePosts: string[] = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (request.method() !== "POST") return;
      if (/\/api\/games\/[^/]+\/travel\/advance$/.test(request.url())) {
        advancePosts.push(request.url());
      }
      if (/\/api\/games\/[^/]+\/travel\/encounter\/resolve$/.test(request.url())) {
        resolvePosts.push(request.url());
      }
    });
  });
  let page = await context.newPage();

  try {
    const trip = await startMultiDayJourney(page, "Brutal");
    type Encounter = {
      kind: string;
      message: string;
      choices: Array<{ id: string; label: string }>;
    };
    let pendingEncounter: Encounter | null = null;
    let lastJourney: {
      status: number;
      daysTravelled: number;
      pendingEncounter: Encounter | null;
    } | null = null;

    for (let day = 0; day < trip.expectedDays + 2 && !pendingEncounter; day += 1) {
      const advanceResponsePromise = page.waitForResponse(
        (response) =>
          response.url() === `${apiBaseUrl}/api/games/${trip.gameId}/travel/advance` &&
          response.request().method() === "POST",
      );
      await page.getByRole("button", { name: "Advance travel day" }).click();
      const advanceResponse = await advanceResponsePromise;
      expect(advanceResponse.ok(), await advanceResponse.text()).toBe(true);
      const advanced = (await advanceResponse.json()) as {
        success: boolean;
        message: string;
        currentSession: {
          journey: {
            status: number;
            daysTravelled: number;
            pendingEncounter: Encounter | null;
          } | null;
        };
      };
      lastJourney = advanced.currentSession.journey;
      pendingEncounter = lastJourney?.pendingEncounter ?? null;
      expect(advanced.success, advanced.message).toBe(pendingEncounter === null);
      expect(lastJourney?.status).not.toBe(JourneyStatus.Completed);
    }

    expect(pendingEncounter?.kind).toBe("foe");
    if (!pendingEncounter || !lastJourney) throw new Error("Expected a hostile trail encounter.");
    expect(lastJourney.daysTravelled).toBeGreaterThan(0);
    await page.close();
    page = await context.newPage();
    const pendingResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const pendingResponse = await pendingResponsePromise;
    expect(pendingResponse.ok()).toBe(true);
    const pendingSession = (await pendingResponse.json()) as {
      journey: { pendingEncounter: Encounter | null; status: number } | null;
    };
    expect(pendingSession.journey?.status).toBe(JourneyStatus.Interrupted);
    expect(pendingSession.journey?.pendingEncounter).toEqual(pendingEncounter);
    await expect(page.getByText(pendingEncounter.message).last()).toBeVisible();
    await expect(page.getByRole("button", { name: "Advance travel day" })).toHaveCount(0);

    const fightChoice = pendingEncounter.choices.find((choice) => choice.id === "fight");
    expect(fightChoice).toBeDefined();
    if (!fightChoice) throw new Error("Expected a hostile encounter to offer the Fight choice.");
    const resolveResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}/travel/encounter/resolve` &&
        response.request().method() === "POST",
    );
    await page.getByLabel("Bullets").fill("6");
    await page.getByRole("button", { name: fightChoice.label }).click();
    const resolveResponse = await resolveResponsePromise;
    expect(resolveResponse.ok(), await resolveResponse.text()).toBe(true);
    const resolved = (await resolveResponse.json()) as {
      success: boolean;
      currentSession: {
        journey: {
          pendingEncounter: Encounter | null;
          status: number;
          daysTravelled: number;
        } | null;
        travelDiary: {
          days: Array<{
            dayNumber: number;
            encounterResolution: {
              choiceId: string;
              choiceLabel: string;
              ammoSpent: number;
            } | null;
          }>;
        } | null;
        logEntries: Array<{ message: string; day: number }>;
      };
    };
    expect(resolved.success, JSON.stringify(resolved)).toBe(true);
    expect(resolved.currentSession.journey?.pendingEncounter).toBeNull();
    expect(resolved.currentSession.journey?.status).toBe(JourneyStatus.Active);
    const resolvedDay = resolved.currentSession.travelDiary?.days.at(-1);
    expect(resolvedDay?.encounterResolution).toMatchObject({ choiceId: "fight", ammoSpent: 6 });
    const resolutionEntry = resolved.currentSession.logEntries.find(
      (entry) => entry.message === "You push the rider behind you and keep moving.",
    );
    expect(resolutionEntry, JSON.stringify(resolved.currentSession.logEntries)).toBeDefined();
    if (!resolutionEntry || !resolvedDay?.encounterResolution) {
      throw new Error("Expected the successful encounter to reach the player's journal and diary.");
    }
    expect(resolvePosts).toEqual([
      `${apiBaseUrl}/api/games/${trip.gameId}/travel/encounter/resolve`,
    ]);
    expect(advancePosts).toHaveLength(lastJourney.daysTravelled);

    await page.close();
    page = await context.newPage();
    const resolvedResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const resolvedResponse = await resolvedResponsePromise;
    expect(resolvedResponse.ok()).toBe(true);
    const restored = (await resolvedResponse.json()) as {
      journey: { pendingEncounter: Encounter | null; status: number } | null;
      travelDiary: {
        days: Array<{
          dayNumber: number;
          encounterResolution: { choiceId: string; choiceLabel: string; ammoSpent: number } | null;
        }>;
      } | null;
      logEntries: Array<{ message: string; day: number }>;
    };
    const restoredDay = restored.travelDiary?.days.find(
      (day) => day.dayNumber === resolvedDay.dayNumber,
    );
    expect(restoredDay?.encounterResolution).toEqual(resolvedDay.encounterResolution);
    expect(restored.logEntries).toContainEqual(resolutionEntry);
    expect(restored.journey?.pendingEncounter).toBeNull();
    expect(restored.journey?.status).toBe(JourneyStatus.Active);
    await page.getByRole("button", { name: "Journal", exact: true }).click();
    await expect(
      page.getByRole("dialog").getByText(resolutionEntry.message, { exact: true }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Close" }).click();
    await expect(page.getByText("Encounter resolved", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Advance travel day" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Step into town" })).toHaveCount(0);
    expect(resolvePosts).toHaveLength(1);
  } finally {
    await context.close();
  }
});

test("a player can complete a trip and restore the acknowledged town after reopening", async ({
  browser,
}) => {
  test.setTimeout(240_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const advancePosts: string[] = [];
  const arrivalPosts: string[] = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (request.method() !== "POST") return;
      if (/\/api\/games\/[^/]+\/travel\/advance$/.test(request.url())) {
        advancePosts.push(request.url());
      }
      if (/\/api\/games\/[^/]+\/travel\/arrival\/acknowledge$/.test(request.url())) {
        arrivalPosts.push(request.url());
      }
    });
  });
  let page = await context.newPage();

  try {
    const trip = await startMultiDayJourney(page);
    type CompletedJourney = {
      originTownId: string;
      destinationTownId: string;
      destinationTownName: string;
      status: number;
      daysTravelled: number;
    };
    let completedJourney: CompletedJourney | null = null;
    let completedSession: {
      player: { currentTownId: string };
      journey: CompletedJourney | null;
    } | null = null;

    for (let day = 0; day < trip.expectedDays + 2 && !completedJourney; day += 1) {
      const advanceResponsePromise = page.waitForResponse(
        (response) =>
          response.url() === `${apiBaseUrl}/api/games/${trip.gameId}/travel/advance` &&
          response.request().method() === "POST",
      );
      await page.getByRole("button", { name: "Advance travel day" }).click();
      const advanceResponse = await advanceResponsePromise;
      expect(advanceResponse.ok(), await advanceResponse.text()).toBe(true);
      const advanced = (await advanceResponse.json()) as {
        success: boolean;
        message: string;
        currentSession: {
          player: { currentTownId: string };
          journey: (CompletedJourney & { pendingEncounter: { kind: string } | null }) | null;
        };
      };
      expect(advanced.success, advanced.message).toBe(true);
      const journey = advanced.currentSession.journey;
      expect(journey, JSON.stringify(advanced.currentSession)).not.toBeNull();
      if (!journey) throw new Error("Expected the server to return the active journey.");
      expect(journey.originTownId).toBe(trip.origin.id);
      expect(journey.destinationTownId).toBe(trip.destination.id);
      expect(journey.pendingEncounter).toBeNull();
      expect(journey.status).not.toBe(JourneyStatus.Interrupted);
      if (journey.status === JourneyStatus.Completed) {
        completedJourney = journey;
        completedSession = advanced.currentSession;
      } else {
        expect(journey.status).toBe(JourneyStatus.Active);
      }
    }

    expect(
      completedJourney,
      "The deterministic route should reach its destination.",
    ).not.toBeNull();
    if (!completedJourney || !completedSession) {
      throw new Error("The deterministic route did not complete within its expected travel days.");
    }
    expect(completedJourney.destinationTownName).toBe(trip.destination.name);
    expect(completedSession.player.currentTownId).toBe(trip.destination.id);
    expect(completedJourney.daysTravelled).toBe(advancePosts.length);

    await page.close();
    page = await context.newPage();
    const completedResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const completedResponse = await completedResponsePromise;
    expect(completedResponse.ok()).toBe(true);
    const restoredArrival = (await completedResponse.json()) as {
      player: { currentTownId: string };
      journey: CompletedJourney | null;
    };
    expect(restoredArrival.journey).toMatchObject(completedJourney);
    expect(restoredArrival.journey?.status).toBe(JourneyStatus.Completed);
    expect(restoredArrival.player.currentTownId).toBe(trip.destination.id);
    await expect(
      page.getByRole("heading", { level: 1, name: `You've arrived in ${trip.destination.name}` }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Step into town" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Sheriff Office" })).toHaveCount(0);
    expect(arrivalPosts).toHaveLength(0);

    const arrivalResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}/travel/arrival/acknowledge` &&
        response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Step into town" }).click();
    const arrivalResponse = await arrivalResponsePromise;
    expect(arrivalResponse.ok(), await arrivalResponse.text()).toBe(true);
    const acknowledged = (await arrivalResponse.json()) as {
      success: boolean;
      currentSession: { player: { currentTownId: string }; journey: CompletedJourney | null };
    };
    expect(acknowledged.success).toBe(true);
    expect(acknowledged.currentSession.player.currentTownId).toBe(trip.destination.id);
    expect(acknowledged.currentSession.journey).toBeNull();
    expect(arrivalPosts).toEqual([
      `${apiBaseUrl}/api/games/${trip.gameId}/travel/arrival/acknowledge`,
    ]);
    await expect(
      page.getByRole("heading", { level: 1, name: trip.destination.name }),
    ).toBeVisible();

    await page.close();
    page = await context.newPage();
    const arrivedResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const arrivedResponse = await arrivedResponsePromise;
    expect(arrivedResponse.ok()).toBe(true);
    const arrived = (await arrivedResponse.json()) as {
      player: { currentTownId: string };
      journey: CompletedJourney | null;
    };
    expect(arrived.player.currentTownId).toBe(trip.destination.id);
    expect(arrived.journey).toBeNull();
    await expect(
      page.getByRole("heading", { level: 1, name: trip.destination.name }),
    ).toBeVisible();
    expect(advancePosts).toHaveLength(completedJourney.daysTravelled);
    expect(arrivalPosts).toHaveLength(1);
  } finally {
    await context.close();
  }
});

test("a player can start, restore a public poster, and take in the named person through the saloon", async ({
  browser,
}) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const seedCode = seedWorldSeedCode();
  await page.goto(productionWebUrl);
  await page.getByLabel("Your name").fill(`Browser rider ${Date.now()}`);
  await page.getByRole("button", { name: "Boring" }).click();
  await page.getByLabel("World seed").fill(seedCode);

  const setupResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/setup` && response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Ride on" }).click();
  const setupResponse = await setupResponsePromise;
  expect(setupResponse.ok(), await setupResponse.text()).toBe(true);
  const setup = (await setupResponse.json()) as { id: string };

  await expect(page.getByRole("button", { name: "Ride on" })).toBeVisible();
  await page.getByRole("button", { name: "Ride on" }).click();
  await expect(page.getByRole("heading", { level: 2 })).toBeVisible();
  await page.getByRole("button", { name: "Ride on" }).click();

  const mapResponse = await page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/world-map` &&
      response.request().method() === "GET",
  );
  expect(mapResponse.ok()).toBe(true);
  const map = (await mapResponse.json()) as WorldMapResponse;
  const town = map.towns[0];
  const startResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/start` &&
      response.request().method() === "POST",
  );
  await clickTownOnWorldMap(page, map, town);
  const startResponse = await startResponsePromise;
  expect(startResponse.ok()).toBe(true);
  await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

  await page.getByRole("button", { name: "Sheriff Office" }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Sheriff Office" })).toBeVisible();
  const posterReadResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/wanted-posters/read` &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Read wanted posters" }).click();
  const posterReadResponse = await posterReadResponsePromise;
  expect(posterReadResponse.status()).toBe(200);
  const posterRead = (await posterReadResponse.json()) as {
    success: boolean;
    wantedPosters: Array<{
      posterId: string;
      targetDisplayName: string;
      details: { features: Array<{ text: string }> };
    }>;
  };
  expect(posterRead.success).toBe(true);
  expect(posterRead.wantedPosters).toHaveLength(1);
  const poster = posterRead.wantedPosters[0];
  await expect(
    page.getByRole("heading", { level: 4, name: poster.targetDisplayName }),
  ).toBeVisible();

  await page.close();
  const reopenedPage = await context.newPage();
  try {
    const restoredResponsePromise = reopenedPage.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    await reopenedPage.goto(productionWebUrl);
    const restoredResponse = await restoredResponsePromise;
    expect(restoredResponse.ok()).toBe(true);
    const restored = (await restoredResponse.json()) as {
      wantedPosters: Array<{ posterId: string; targetDisplayName: string }>;
    };
    expect(restored.wantedPosters.map((item) => item.posterId)).toEqual([poster.posterId]);
    await expect(reopenedPage.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    await reopenedPage.getByRole("button", { name: "Sheriff Office" }).click();
    await expect(
      reopenedPage.getByRole("heading", { level: 4, name: poster.targetDisplayName }),
    ).toBeVisible();
    await reopenedPage.getByRole("button", { name: "Back to town" }).click();
    await reopenedPage.getByRole("button", { name: "Saloon" }).click();
    await expect(reopenedPage.getByRole("heading", { level: 1, name: "Saloon" })).toBeVisible();

    const observationResponsePromise = reopenedPage.waitForResponse(
      (response) =>
        response.url() ===
          `${apiBaseUrl}/api/games/${setup.id}/investigations/saloon/look-around` &&
        response.request().method() === "POST",
    );
    const observationSessionPromise = reopenedPage.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    await reopenedPage.getByRole("button", { name: "Look around" }).click();
    const observationResponse = await observationResponsePromise;
    expect(observationResponse.ok(), await observationResponse.text()).toBe(true);
    const observation = (await observationResponse.json()) as {
      success: boolean;
    };
    expect(observation.success).toBe(true);
    const observationSessionResponse = await observationSessionPromise;
    expect(observationSessionResponse.ok()).toBe(true);
    const observationSession = (await observationSessionResponse.json()) as {
      activeSaloonPersonOfInterest: { descriptor: string } | null;
    };
    const person = observationSession.activeSaloonPersonOfInterest;
    expect(person).not.toBeNull();
    if (!person) throw new Error("Expected the saloon observation to reveal a person.");
    const matchingFeature = poster.details.features.find((feature) =>
      person.descriptor.toLowerCase().includes(feature.text.toLowerCase()),
    );
    expect(
      matchingFeature,
      `${person.descriptor} should match a feature on ${poster.targetDisplayName}'s public poster: ${poster.details.features.map((feature) => feature.text).join("; ")}`,
    ).toBeTruthy();
    await expect(reopenedPage.getByText(person.descriptor, { exact: true })).toBeVisible();

    await reopenedPage
      .getByRole("combobox", { name: "Declare wanted identity" })
      .selectOption(poster.posterId);
    const confrontResponsePromise = reopenedPage.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/investigations/saloon/confront` &&
        response.request().method() === "POST",
    );
    await reopenedPage
      .getByRole("button", {
        name: `Take ${person.descriptor} to sheriff as ${poster.targetDisplayName}`,
      })
      .click();
    const confrontResponse = await confrontResponsePromise;
    expect(confrontResponse.ok(), await confrontResponse.text()).toBe(true);
    const confrontation = (await confrontResponse.json()) as {
      success: boolean;
      outcome: number;
      targetName: string | null;
      isAlive: boolean | null;
      isSecured: boolean | null;
      message: string;
    };
    expect(confrontation.success, confrontation.message).toBe(true);
    expect(confrontation.outcome).toBe(0);
    expect(confrontation.targetName).toBe(poster.targetDisplayName);
    expect(confrontation.isAlive).toBe(true);
    expect(confrontation.isSecured).toBe(true);
    await expect(reopenedPage.getByText(confrontation.message, { exact: true })).toBeVisible();
  } finally {
    await context.close().catch(() => undefined);
  }
});

test("confirmed Start Over archive is durable, while cancellation leaves the game active", async ({
  browser,
}) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const setupPosts: string[] = [];
  const archivePosts: string[] = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (request.method() !== "POST") return;
      if (request.url() === `${apiBaseUrl}/api/games/setup`) setupPosts.push(request.url());
      if (/\/api\/games\/[^/]+\/archive$/.test(request.url())) archivePosts.push(request.url());
    });
  });
  let page = await context.newPage();

  try {
    await page.goto(productionWebUrl);
    await page.getByLabel("Your name").fill(`Browser rider ${Date.now()}`);
    await page.getByLabel("World seed").fill(seedWorldSeedCode());

    const setupResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/setup` &&
        response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Ride on" }).click();
    const setupResponse = await setupResponsePromise;
    expect(setupResponse.ok(), await setupResponse.text()).toBe(true);
    const setup = (await setupResponse.json()) as { id: string };

    await page.getByRole("button", { name: "Ride on" }).click();
    await expect(page.getByRole("heading", { level: 2 })).toBeVisible();
    const mapResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/world-map` &&
        response.request().method() === "GET",
    );
    await page.getByRole("button", { name: "Ride on" }).click();
    const mapResponse = await mapResponsePromise;
    expect(mapResponse.ok()).toBe(true);
    const map = (await mapResponse.json()) as WorldMapResponse;
    const town = map.towns[0];
    const startResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/start` &&
        response.request().method() === "POST",
    );
    await clickTownOnWorldMap(page, map, town);
    const startResponse = await startResponsePromise;
    expect(startResponse.ok()).toBe(true);
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    await page.getByRole("button", { name: "Game Settings" }).click();
    await page
      .getByRole("dialog", { name: "Game Settings" })
      .getByRole("button", { name: "Start Over" })
      .click();
    await page
      .getByRole("dialog", { name: "Start over?" })
      .getByRole("button", { name: "Keep riding" })
      .click();
    await expect(page.getByRole("dialog", { name: "Start over?" })).toBeHidden();
    await expect(page.getByRole("dialog", { name: "Game Settings" })).toBeVisible();

    const activeResponse = await context.request.get(`${apiBaseUrl}/api/games/${setup.id}`);
    expect(activeResponse.ok()).toBe(true);
    const activeGame = (await activeResponse.json()) as { status: number };
    expect(activeGame.status).toBe(GameStatus.Active);
    expect(archivePosts).toHaveLength(0);
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    const archiveResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/archive` &&
        response.request().method() === "POST",
    );
    await page
      .getByRole("dialog", { name: "Game Settings" })
      .getByRole("button", { name: "Start Over" })
      .click();
    await page
      .getByRole("dialog", { name: "Start over?" })
      .getByRole("button", { name: "Archive and start over" })
      .click();
    const archiveResponse = await archiveResponsePromise;
    expect(archiveResponse.ok(), await archiveResponse.text()).toBe(true);
    const archivedResult = (await archiveResponse.json()) as { status: number };
    expect(archivedResult.status).toBe(GameStatus.Archived);
    await expect(page.getByLabel("Your name")).toBeVisible();
    expect(archivePosts).toHaveLength(1);
    expect(setupPosts).toHaveLength(1);

    await page.close();
    page = await context.newPage();
    await page.goto(productionWebUrl);
    await expect(page.getByLabel("Your name")).toBeVisible();

    const persistedResponse = await context.request.get(`${apiBaseUrl}/api/games/${setup.id}`);
    expect(persistedResponse.ok()).toBe(true);
    const persistedGame = (await persistedResponse.json()) as { status: number };
    expect(persistedGame.status).toBe(GameStatus.Archived);
    expect(archivePosts).toHaveLength(1);
    expect(setupPosts).toHaveLength(1);
  } finally {
    await context.close();
  }
});
