import { expect, test } from "@playwright/test";
import { startIsolatedApi } from "./support/postgresApi";

const productionWebUrl = "http://127.0.0.1:4174";
const apiBaseUrl = "http://127.0.0.1:5275";

interface WorldMapResponse {
  towns: Array<{ id: string; name: string; x: number; y: number }>;
}

let stopIsolatedApi: (() => Promise<void>) | undefined;

test.beforeAll(async () => {
  test.setTimeout(120_000);
  stopIsolatedApi = await startIsolatedApi();
});

test.afterAll(async () => {
  await stopIsolatedApi?.();
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
  } finally {
    await context.close();
  }
});
