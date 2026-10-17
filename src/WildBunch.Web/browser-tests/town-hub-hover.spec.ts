import { readFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

const testDirectory = dirname(fileURLToPath(import.meta.url));
const storeSprite = await readFile(
  resolve(
    testDirectory,
    "../../WildBunch.Assets/production/sprites/town-hub-buildings/prosperous/general-store/profile.png",
  ),
);
const transparentPixel = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScL8WQAAAABJRU5ErkJggg==",
  "base64",
);

test("real Phaser sprite hover preserves fitted artwork size after pointerout", async ({
  page,
}, testInfo) => {
  await page.route("**/assets/**", async (route) => {
    const requestPath = new URL(route.request().url()).pathname;
    if (requestPath === "/assets/town-hub-buildings/prosperous/general-store/profile.png") {
      await route.fulfill({ contentType: "image/png", body: storeSprite });
      return;
    }

    await route.fulfill({ contentType: "image/png", body: transparentPixel });
  });

  await page.goto("/browser-tests/town-hub-hover.html");
  const canvas = page.locator("canvas");
  await expect(canvas).toBeVisible();
  await expect
    .poll(() => page.locator("html").getAttribute("data-store-sprite-loaded"))
    .toBe("true");

  const bounds = await canvas.boundingBox();
  expect(bounds).not.toBeNull();
  if (!bounds) {
    throw new Error("Expected the real Phaser canvas to have browser bounds.");
  }

  const baseline = await canvas.screenshot({ path: testInfo.outputPath("town-hub-baseline.png") });
  await page.mouse.move(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
  const hovered = await canvas.screenshot({ path: testInfo.outputPath("town-hub-hovered.png") });
  expect(hovered.equals(baseline)).toBe(false);

  await page.mouse.move(0, 0);
  await expect
    .poll(async () => {
      const restored = await canvas.screenshot({
        path: testInfo.outputPath("town-hub-restored.png"),
      });
      return restored.equals(baseline);
    })
    .toBe(true);
});
