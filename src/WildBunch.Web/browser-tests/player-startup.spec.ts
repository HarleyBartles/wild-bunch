import { expect, test, type Page } from "@playwright/test";
import { GameStatus, InvestigationSourceKind, JourneyStatus } from "../src/api/types";
import type { GameLogEntryDto, JournalDto } from "../src/api/types";
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

function expectCapturedCaseFileProjection(
  journal: JournalDto,
  targetName: string,
  knownFeature: string,
  bountyAmount: number,
) {
  const capturedSuspects = journal.caseFile.discoveredSuspects.filter(
    (candidate) => candidate.status === 1,
  );
  expect(capturedSuspects).toHaveLength(1);

  const warrant = journal.caseFile.caseBoard.warrants.find(
    (candidate) => candidate.targetName === targetName,
  );
  expect(warrant).toBeDefined();
  expect(warrant?.knownFeatures).toContain(knownFeature);
  expect([warrant?.targetName, ...(warrant?.knownAliases ?? [])]).toContain(
    capturedSuspects[0].name,
  );
  expect(warrant?.settlement).toMatchObject({ isAlive: true, bountyAmount });
}

async function expectCapturedCaseFileSurface(page: Page, targetName: string, knownFeature: string) {
  const caseFile = page.getByRole("dialog", { name: "Case file" });
  await expect(caseFile).toBeVisible();
  await expect(caseFile.getByText(targetName, { exact: true }).last()).toBeVisible();
  await expect(caseFile.getByText("Captured", { exact: true })).toBeVisible();
  await expect(caseFile.getByText(knownFeature, { exact: false }).first()).toBeVisible();
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

test("a player can read the case file and start over before town selection", async ({
  browser,
}) => {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const forbiddenPosts: string[] = [];
  page.on("request", (request) => {
    if (
      request.method() === "POST" &&
      /\/(?:prologue-viewed|start)$/.test(new URL(request.url()).pathname)
    ) {
      forbiddenPosts.push(request.url());
    }
  });

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
    const restoredPage = await context.newPage();
    restoredPage.on("request", (request) => {
      if (
        request.method() === "POST" &&
        /\/(?:prologue-viewed|start)$/.test(new URL(request.url()).pathname)
      ) {
        forbiddenPosts.push(request.url());
      }
    });
    await restoredPage.goto(productionWebUrl);

    const [sessionResponse, prologueResponse, journalResponse] = await Promise.all([
      restoredPage.waitForResponse(
        (response) =>
          response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
          response.request().method() === "GET",
      ),
      restoredPage.waitForResponse(
        (response) =>
          response.url() === `${apiBaseUrl}/api/games/${setup.id}/prologue` &&
          response.request().method() === "GET",
      ),
      restoredPage.waitForResponse(
        (response) =>
          response.url() === `${apiBaseUrl}/api/games/${setup.id}/journal` &&
          response.request().method() === "GET",
      ),
    ]);
    expect(sessionResponse.ok()).toBe(true);
    expect(prologueResponse.ok()).toBe(true);
    expect(journalResponse.ok()).toBe(true);

    const session = (await sessionResponse.json()) as {
      startFlowPhase: number;
      player: { currentTownId: string | null };
    };
    const prologue = (await prologueResponse.json()) as { body: string };
    const journal = (await journalResponse.json()) as JournalDto;
    expect(session.startFlowPhase).toBe(1);
    expect(session.player.currentTownId).toBeNull();
    expect(journal.currentTown).toBeNull();
    expect(journal.caseFile.openingLead.trim()).not.toBe("");
    await expect(restoredPage.getByText(prologue.body, { exact: true })).toBeVisible();
    await expect(restoredPage.getByRole("button", { name: "Ride on" })).toBeVisible();

    const caseFileButton = restoredPage.getByRole("button", {
      name: "Case file",
      exact: true,
    });
    await expect(caseFileButton).toBeEnabled();
    await caseFileButton.click();
    const caseFile = restoredPage.getByRole("dialog", { name: "Case file" });
    await expect(caseFile).toBeVisible();
    await expect(
      caseFile.getByText(`Opening lead: ${journal.caseFile.openingLead}`, { exact: true }),
    ).toBeVisible();

    expect(forbiddenPosts).toEqual([]);
    const unchangedSessionResponse = await restoredPage.request.get(
      `${apiBaseUrl}/api/games/${setup.id}`,
    );
    expect(unchangedSessionResponse.ok()).toBe(true);
    const unchangedSession = (await unchangedSessionResponse.json()) as typeof session;
    expect(unchangedSession.startFlowPhase).toBe(1);
    expect(unchangedSession.player.currentTownId).toBeNull();

    await restoredPage.keyboard.press("Escape");
    await restoredPage.setViewportSize({ width: 390, height: 844 });
    await restoredPage.getByRole("button", { name: "Game Settings", exact: true }).click();
    await restoredPage.getByRole("button", { name: "Start Over", exact: true }).click();
    await restoredPage.getByRole("button", { name: "Archive and start over" }).click();
    await expect(restoredPage.getByLabel("Your name")).toBeVisible();
    const archivedResponse = await restoredPage.request.get(`${apiBaseUrl}/api/games/${setup.id}`);
    expect(archivedResponse.ok()).toBe(true);
    expect((await archivedResponse.json()).status).toBe(GameStatus.Archived);
    expect(forbiddenPosts).toEqual([]);
  } finally {
    await context.close();
  }
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
    const restoredPrologueResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
      { timeout: 5_000 },
    );
    await page.goto(productionWebUrl);
    const restoredPrologueResponse = await restoredPrologueResponsePromise;
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
    const restoredTownResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const restoredTownResponse = await restoredTownResponsePromise;
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
    const restoredPosterResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const restoredPosterResponse = await restoredPosterResponsePromise;
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
          journeyEntries: GameLogEntryDto[];
          days: Array<{
            dayNumber: number;
            encounterResolution: {
              choiceId: string;
              choiceLabel: string;
              ammoSpent: number;
            } | null;
          }>;
        } | null;
        logEntries: GameLogEntryDto[];
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
    expect(resolved.currentSession.travelDiary?.journeyEntries).toContainEqual(resolutionEntry);
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
    const resolvedJournalResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${trip.gameId}/journal` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const resolvedResponse = await resolvedResponsePromise;
    const resolvedJournalResponse = await resolvedJournalResponsePromise;
    expect(resolvedResponse.ok()).toBe(true);
    expect(resolvedJournalResponse.ok()).toBe(true);
    const restored = (await resolvedResponse.json()) as {
      journey: { pendingEncounter: Encounter | null; status: number } | null;
      travelDiary: {
        journeyEntries: GameLogEntryDto[];
        days: Array<{
          dayNumber: number;
          encounterResolution: { choiceId: string; choiceLabel: string; ammoSpent: number } | null;
        }>;
      } | null;
      logEntries: GameLogEntryDto[];
    };
    const restoredJournal = (await resolvedJournalResponse.json()) as JournalDto;
    const restoredDay = restored.travelDiary?.days.find(
      (day) => day.dayNumber === resolvedDay.dayNumber,
    );
    expect(restoredDay?.encounterResolution).toEqual(resolvedDay.encounterResolution);
    expect(restored.logEntries).toContainEqual(resolutionEntry);
    expect(restored.travelDiary?.journeyEntries).toContainEqual(resolutionEntry);
    expect(restoredJournal.logEntries).toContainEqual(resolutionEntry);
    expect(restored.journey?.pendingEncounter).toBeNull();
    expect(restored.journey?.status).toBe(JourneyStatus.Active);
    const travelView = page.locator("article").filter({ hasText: "Trail notebook" });
    await expect(travelView.getByText(resolutionEntry.message, { exact: true })).toBeVisible();
    await expect(
      travelView.getByText(resolvedDay.encounterResolution.choiceLabel, { exact: true }),
    ).toBeVisible();
    await expect(travelView.getByText(/Ammo spent 6/)).toBeVisible();
    await page.getByRole("button", { name: "Journal", exact: true }).click();
    await expect(
      page.getByRole("dialog").getByText(resolutionEntry.message, { exact: true }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Close" }).click();
    await expect(page.getByText("Encounter resolved", { exact: true })).toBeVisible();
    await expect(page.getByRole("button", { name: "Advance travel day" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Step into town" })).toHaveCount(0);
    expect(resolvePosts).toHaveLength(1);
    expect(advancePosts).toHaveLength(lastJourney.daysTravelled);
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
  let confrontationPostCount = 0;
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (
        request.method() === "POST" &&
        /\/api\/games\/[^/]+\/investigations\/saloon\/confront$/.test(request.url())
      ) {
        confrontationPostCount += 1;
      }
    });
  });
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
      legalTerms: { bountyAmount: number };
      details: { features: Array<{ text: string }> };
    }>;
  };
  expect(posterRead.success).toBe(true);
  expect(posterRead.wantedPosters).toHaveLength(1);
  const poster = posterRead.wantedPosters[0];
  await expect(
    page.getByRole("heading", { level: 4, name: poster.targetDisplayName }),
  ).toBeVisible();

  const devContextResponse = await page.request.get(
    `${apiBaseUrl}/api/dev/sessions/${setup.id}/saloon-context`,
  );
  expect(devContextResponse.ok()).toBe(true);
  const devContext = (await devContextResponse.json()) as {
    suspects: Array<{ suspectId: string; name: string; hasKnownWarrant: boolean }>;
  };
  const matchingSuspect = devContext.suspects.find(
    (suspect) => suspect.name === poster.targetDisplayName && suspect.hasKnownWarrant,
  );
  expect(matchingSuspect).toBeDefined();
  if (!matchingSuspect)
    throw new Error("The read public poster should resolve to its exact suspect identity.");
  const forceSuspectResponse = await page.request.post(
    `${apiBaseUrl}/api/dev/sessions/${setup.id}/saloon/force-override`,
    { data: { forcedKind: "Suspect", forcedSuspectId: matchingSuspect.suspectId } },
  );
  expect(forceSuspectResponse.status()).toBe(204);

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

    const knownFeature = poster.details.features[0]?.text;
    expect(knownFeature).toBeTruthy();
    if (!knownFeature) throw new Error("The public poster should disclose at least one feature.");
    await reopenedPage.getByRole("button", { name: "Case file" }).click();
    await expectCapturedCaseFileSurface(reopenedPage, poster.targetDisplayName, knownFeature);

    await reopenedPage.close();
    const casebookReloadedPage = await context.newPage();
    const restoredCaseFileJournalResponsePromise = casebookReloadedPage.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/journal` &&
        response.request().method() === "GET",
    );
    await casebookReloadedPage.goto(productionWebUrl);
    const restoredCaseFileJournalResponse = await restoredCaseFileJournalResponsePromise;
    expect(restoredCaseFileJournalResponse.ok()).toBe(true);
    const restoredCaseFileJournal = (await restoredCaseFileJournalResponse.json()) as JournalDto;
    expectCapturedCaseFileProjection(
      restoredCaseFileJournal,
      poster.targetDisplayName,
      knownFeature,
      poster.legalTerms.bountyAmount,
    );
    await expect(
      casebookReloadedPage.getByRole("heading", { level: 1, name: town.name }),
    ).toBeVisible();
    await casebookReloadedPage.getByRole("button", { name: "Case file" }).click();
    await expectCapturedCaseFileSurface(
      casebookReloadedPage,
      poster.targetDisplayName,
      knownFeature,
    );
    expect(confrontationPostCount).toBe(1);
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

test("a player can make a Store purchase and restore it after reopening", async ({ browser }) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const setupPosts: string[] = [];
  const purchases: Array<{
    url: string;
    body: { itemKind: number; quantity: number };
  }> = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (request.method() !== "POST") return;
      if (request.url() === `${apiBaseUrl}/api/games/setup`) setupPosts.push(request.url());
      if (/\/api\/games\/[^/]+\/towns\/[^/]+\/store\/buy$/.test(request.url())) {
        purchases.push({
          url: request.url(),
          body: request.postDataJSON() as { itemKind: number; quantity: number },
        });
      }
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
    expect(startResponse.ok(), await startResponse.text()).toBe(true);
    const started = (await startResponse.json()) as {
      player: { currentTownId: string };
      inventory: {
        wallet: { cash: number };
        items: Array<{ kind: number; quantity: number }>;
      } | null;
    };
    expect(started.player.currentTownId).toBe(town.id);
    expect(started.inventory).not.toBeNull();
    if (!started.inventory) throw new Error("Expected the started game to include inventory.");
    const startingCash = started.inventory.wallet.cash;
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    const offersResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/towns/${town.id}/store-offers` &&
        response.request().method() === "GET",
    );
    await page.getByRole("button", { name: "Store", exact: true }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Store" })).toBeVisible();
    const offersResponse = await offersResponsePromise;
    expect(offersResponse.ok(), await offersResponse.text()).toBe(true);
    const offers = (await offersResponse.json()) as {
      offers: Array<{ itemKind: number; displayName: string; price: number }>;
    };
    const foodOffer = offers.offers.find((offer) => offer.displayName === "Food");
    expect(foodOffer).toBeDefined();
    if (!foodOffer) throw new Error("Expected the current town store to offer Food.");
    const startingFood =
      started.inventory.items.find((item) => item.kind === foodOffer.itemKind)?.quantity ?? 0;
    expect(startingCash).toBeGreaterThanOrEqual(foodOffer.price * 2);
    const quantityInput = page.getByLabel(`Quantity for ${foodOffer.displayName}`);
    await quantityInput.fill("2");
    const offerForm = quantityInput.locator("xpath=..");
    const purchaseResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/towns/${town.id}/store/buy` &&
        response.request().method() === "POST",
    );
    await offerForm.getByRole("button", { name: "Buy", exact: true }).click();
    const purchaseResponse = await purchaseResponsePromise;
    expect(purchaseResponse.ok(), await purchaseResponse.text()).toBe(true);
    const purchase = (await purchaseResponse.json()) as {
      success: boolean;
      message: string;
      currentSession: {
        inventory: {
          wallet: { cash: number };
          items: Array<{ kind: number; quantity: number }>;
        } | null;
      };
    };
    expect(purchase.success, purchase.message).toBe(true);
    expect(purchases).toHaveLength(1);
    expect(purchases[0]).toEqual({
      url: `${apiBaseUrl}/api/games/${setup.id}/towns/${town.id}/store/buy`,
      body: { itemKind: foodOffer.itemKind, quantity: 2 },
    });
    expect(purchase.currentSession.inventory).not.toBeNull();
    if (!purchase.currentSession.inventory) {
      throw new Error("Expected the purchase result to include updated inventory.");
    }
    const purchasedCash = purchase.currentSession.inventory.wallet.cash;
    const purchasedFood =
      purchase.currentSession.inventory.items.find((item) => item.kind === foodOffer.itemKind)
        ?.quantity ?? 0;
    expect(purchasedCash).toBeCloseTo(startingCash - foodOffer.price * 2, 2);
    expect(purchasedFood).toBe(startingFood + 2);
    await expect(page.getByRole("main").getByText(purchase.message, { exact: true })).toBeVisible();
    expect(setupPosts).toHaveLength(1);

    await page.close();
    page = await context.newPage();
    const restoredResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const restoredResponse = await restoredResponsePromise;
    expect(restoredResponse.ok()).toBe(true);
    const restored = (await restoredResponse.json()) as {
      player: { currentTownId: string };
      inventory: {
        wallet: { cash: number };
        items: Array<{ kind: number; quantity: number }>;
      } | null;
    };
    expect(restored.player.currentTownId).toBe(town.id);
    expect(restored.inventory).not.toBeNull();
    if (!restored.inventory) throw new Error("Expected the restored game to include inventory.");
    const restoredCash = restored.inventory.wallet.cash;
    const restoredFood =
      restored.inventory.items.find((item) => item.kind === foodOffer.itemKind)?.quantity ?? 0;
    expect(restoredCash).toBeCloseTo(purchasedCash, 2);
    expect(restoredFood).toBe(purchasedFood);
    await expect(page.getByRole("heading", { level: 1, name: town.name })).toBeVisible();

    await page.getByRole("button", { name: "Store", exact: true }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Store" })).toBeVisible();
    await expect(
      page.getByRole("main").getByText(purchase.message, { exact: true }),
    ).not.toBeVisible();
    const inventoryPanel = page
      .getByRole("heading", { name: "Inventory", exact: true })
      .locator("..");
    await expect(inventoryPanel).toContainText(`$${restoredCash.toFixed(2)}`);
    await expect(inventoryPanel).toContainText(`Food x ${restoredFood}`);
    expect(purchases).toHaveLength(1);
    expect(setupPosts).toHaveLength(1);
  } finally {
    await context.close();
  }
});

test("a player can gather saloon gossip and restore it in the journal after reopening", async ({
  browser,
}) => {
  test.setTimeout(120_000);
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const gossipPosts: string[] = [];
  context.on("page", (openedPage) => {
    openedPage.on("request", (request) => {
      if (
        request.method() === "POST" &&
        /\/api\/games\/[^/]+\/investigations\/local-gossip\/gather$/.test(request.url())
      ) {
        gossipPosts.push(request.url());
      }
    });
  });
  let page = await context.newPage();

  try {
    await page.goto(productionWebUrl);
    await page.getByLabel("Your name").fill(`Browser rider ${Date.now()}`);
    await page.getByRole("button", { name: "Boring" }).click();
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

    await page.getByRole("button", { name: "Saloon" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Saloon" })).toBeVisible();
    const gossipResponsePromise = page.waitForResponse(
      (response) =>
        response.url() ===
          `${apiBaseUrl}/api/games/${setup.id}/investigations/local-gossip/gather` &&
        response.request().method() === "POST",
    );
    await page.getByRole("button", { name: "Gather gossip" }).click();
    const gossipResponse = await gossipResponsePromise;
    expect(gossipResponse.ok(), await gossipResponse.text()).toBe(true);
    const gossip = (await gossipResponse.json()) as {
      success: boolean;
      currentJournal: {
        caseFile: {
          knownClues: Array<{
            id: string;
            description: string;
            sourceKind: InvestigationSourceKind | null;
          }>;
        };
        logEntries: Array<{ message: string }>;
      };
    };
    expect(gossip.success).toBe(true);
    const gossipEntry = gossip.currentJournal.logEntries.find((entry) =>
      entry.message.includes("uncover a public lead:"),
    );
    expect(gossipEntry).toBeDefined();
    if (!gossipEntry) throw new Error("Expected the recorded gossip result in the journal.");
    const lead = gossipEntry.message.match(/uncover a public lead: (.+)\.$/)?.[1];
    expect(lead).toBeTruthy();
    const gossipClue = gossip.currentJournal.caseFile.knownClues.find((clue) =>
      clue.description.includes(lead!),
    );
    expect(gossipClue).toBeDefined();
    if (!gossipClue) throw new Error("Expected the disclosed lead to be a learned clue.");
    expect(gossipClue.sourceKind).toBe(InvestigationSourceKind.LocalGossip);
    await page.getByRole("button", { name: "Journal", exact: true }).click();
    await expect(page.getByText(gossipEntry.message, { exact: true })).toBeVisible();
    expect(gossipPosts).toEqual([
      `${apiBaseUrl}/api/games/${setup.id}/investigations/local-gossip/gather`,
    ]);

    await page.close();
    page = await context.newPage();
    const restoredJournalResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/journal` &&
        response.request().method() === "GET",
    );
    await page.goto(productionWebUrl);
    const restoredJournalResponse = await restoredJournalResponsePromise;
    expect(restoredJournalResponse.ok()).toBe(true);
    const restoredJournal = (await restoredJournalResponse.json()) as {
      caseFile: {
        knownClues: Array<{
          id: string;
          description: string;
          sourceKind: InvestigationSourceKind | null;
        }>;
      };
      logEntries: Array<{ message: string }>;
    };
    expect(restoredJournal.logEntries).toContainEqual(gossipEntry);
    const restoredGossipClue = restoredJournal.caseFile.knownClues.find(
      (clue) => clue.id === gossipClue.id,
    );
    expect(restoredGossipClue).toEqual(gossipClue);
    expect(restoredGossipClue?.sourceKind).toBe(InvestigationSourceKind.LocalGossip);
    await page.getByRole("button", { name: "Journal", exact: true }).click();
    await expect(page.getByText(gossipEntry.message, { exact: true })).toBeVisible();
    expect(gossipPosts).toHaveLength(1);
  } finally {
    await context.close();
  }
});
