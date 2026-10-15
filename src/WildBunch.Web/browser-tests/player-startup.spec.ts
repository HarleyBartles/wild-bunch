import { expect, test } from "@playwright/test";
import { startIsolatedApi } from "./support/postgresApi";
import { seedWorldSeedCode } from "./support/seedWorldSeedCode";

const productionWebUrl = "http://127.0.0.1:4174";
const apiBaseUrl = "http://127.0.0.1:5275";

interface WorldMapResponse {
  towns: Array<{ id: string; name: string; x: number; y: number }>;
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
    const canvas = page.getByRole("img", { name: "Trail map of starting towns" }).locator("canvas");
    await expect(canvas).toBeVisible();
    const bounds = await canvas.boundingBox();
    expect(bounds).not.toBeNull();
    if (!bounds) {
      throw new Error("Expected the world map canvas to have browser bounds.");
    }

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
    const startResponsePromise = page.waitForResponse(
      (response) =>
        response.url() === `${apiBaseUrl}/api/games/${setup.id}/start` &&
        response.request().method() === "POST",
    );
    await page.mouse.click(
      bounds.x + (markerX / canvasSize.width) * bounds.width,
      bounds.y + (markerY / canvasSize.height) * bounds.height,
    );
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
  const canvas = page.getByRole("img", { name: "Trail map of starting towns" }).locator("canvas");
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
  const startResponsePromise = page.waitForResponse(
    (response) =>
      response.url() === `${apiBaseUrl}/api/games/${setup.id}/start` &&
      response.request().method() === "POST",
  );
  await page.mouse.click(
    bounds.x + ((offsetX + (town.x - minX) * scale) / canvasSize.width) * bounds.width,
    bounds.y + ((offsetY + (town.y - minY) * scale) / canvasSize.height) * bounds.height,
  );
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
