import { readFileSync } from "node:fs";
import { expect, test } from "@playwright/test";

test.use({ baseURL: "http://127.0.0.1:4174" });

const authoredVersion = readFileSync(
  new URL("../../../Directory.Build.props", import.meta.url),
  "utf8",
).match(/<Version>([^<]+)<\/Version>/)?.[1];

if (!authoredVersion) throw new Error("Directory.Build.props must author the application version");

test("production preview boots the built application and serves its authored identity", async ({
  page,
  request,
}) => {
  const identity = await request.get("/version.json");
  expect(identity.ok()).toBe(true);
  expect(identity.headers()["content-type"]).toContain("application/json");
  expect(await identity.json()).toEqual({ version: authoredVersion });

  const response = await page.goto("/");
  expect(response?.ok()).toBe(true);
  await expect(page.locator("#root")).not.toBeEmpty();
});
