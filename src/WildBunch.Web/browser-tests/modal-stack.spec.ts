import { expect, test, type Locator, type Page } from "@playwright/test";

async function openConfirmation(page: Page) {
  const settingsTrigger = page.getByRole("button", { name: "Game Settings", exact: true });
  await settingsTrigger.click();

  const settingsCloseButton = page.getByRole("button", { name: "Close", exact: true });
  await expect(settingsCloseButton).toBeFocused();

  const startOverButton = page.getByRole("button", { name: "Start Over", exact: true });
  await startOverButton.click();

  const confirmation = page.getByRole("dialog", { name: "Start over?" });
  return { confirmation, settingsTrigger, settingsCloseButton, startOverButton };
}

async function expectWithinViewport(locator: Locator, page: Page) {
  const bounds = await locator.boundingBox();
  expect(bounds).not.toBeNull();
  if (!bounds) {
    throw new Error("Expected the visible control to have a browser bounding box.");
  }

  const viewport = page.viewportSize();
  expect(viewport).not.toBeNull();
  if (!viewport) {
    throw new Error("Expected Playwright to have a fixed viewport.");
  }

  expect(bounds.x).toBeGreaterThanOrEqual(0);
  expect(bounds.y).toBeGreaterThanOrEqual(0);
  expect(bounds.x + bounds.width).toBeLessThanOrEqual(viewport.width);
  expect(bounds.y + bounds.height).toBeLessThanOrEqual(viewport.height);
}

test("keeps nested confirmation modal and restores focus through both dialogs", async ({
  page,
}) => {
  await page.goto("/browser-tests/modal-stack.html");
  const outsideAction = page.getByRole("button", { name: "Outside action", exact: true });
  const { confirmation, settingsTrigger, settingsCloseButton, startOverButton } =
    await openConfirmation(page);

  const confirmButton = confirmation.getByRole("button", { name: "Archive and start over" });
  const cancelButton = confirmation.getByRole("button", { name: "Keep riding" });
  await expect(confirmButton).toBeFocused();

  await outsideAction.evaluate((element) => (element as HTMLButtonElement).focus());
  await expect(confirmButton).toBeFocused();

  await startOverButton.evaluate((element) => (element as HTMLButtonElement).focus());
  await expect(confirmButton).toBeFocused();

  await page.keyboard.press("Tab");
  await expect(cancelButton).toBeFocused();
  await page.keyboard.press("Shift+Tab");
  await expect(confirmButton).toBeFocused();

  await page.keyboard.press("Escape");
  await expect(confirmation).not.toBeVisible();
  await expect(startOverButton).toBeFocused();

  await page.keyboard.press("Escape");
  await expect(settingsCloseButton).not.toBeVisible();
  await expect(settingsTrigger).toBeFocused();
});

test("keeps the confirmation actions inside a narrow viewport", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 600 });
  await page.goto("/browser-tests/modal-stack.html");
  const { confirmation } = await openConfirmation(page);

  await expectWithinViewport(confirmation.getByRole("heading", { name: "Start over?" }), page);
  await expectWithinViewport(confirmation.getByRole("button", { name: "Keep riding" }), page);
  await expectWithinViewport(
    confirmation.getByRole("button", { name: "Archive and start over" }),
    page,
  );

  const documentWidth = await page.evaluate(() => document.documentElement.scrollWidth);
  expect(documentWidth).toBeLessThanOrEqual(320);
});
