import { describe, expect, it } from "vitest";
import { resolveAppProfile } from "../shell/appProfile";

describe("resolveAppProfile", () => {
  it.each([
    { input: { dev: true, mode: "development" }, expected: "playtest" },
    { input: { dev: false, mode: "playtest" }, expected: "playtest" },
    { input: { dev: false, mode: "production" }, expected: "public" },
  ])("selects $expected for Vite $input.mode mode", ({ input, expected }) => {
    expect(resolveAppProfile(input)).toBe(expected);
  });
});
