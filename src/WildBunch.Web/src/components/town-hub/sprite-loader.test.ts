import { describe, expect, it } from "vitest";
import { BuildingKind, TownProsperity } from "../../api/types";
import { getBackgroundSpriteKey, getBackgroundSpriteUrl, getSpriteUrl } from "./sprite-loader";

describe("getSpriteUrl", () => {
  it("returns the correct URL for a prosperous store front-oblique view", () => {
    const url = getSpriteUrl(BuildingKind.Store, 3, TownProsperity.Prosperous);
    expect(url).toBe("/assets/town-hub-buildings/prosperous/general-store/front-oblique.png");
  });

  it("returns the correct URL for a boomtown sheriff profile view", () => {
    const url = getSpriteUrl(BuildingKind.Sheriff, 1, TownProsperity.Boomtown);
    expect(url).toBe("/assets/town-hub-buildings/boomtown/sheriff-office/profile.png");
  });

  it("returns the correct URL for a destitute saloon front view", () => {
    const url = getSpriteUrl(BuildingKind.Saloon, 0, TownProsperity.Destitute);
    expect(url).toBe("/assets/town-hub-buildings/destitute/saloon/front.png");
  });

  it("returns null for Trailhead (which has no sprite assets)", () => {
    const url = getSpriteUrl(BuildingKind.Trailhead, 0, TownProsperity.Prosperous);
    expect(url).toBeNull();
  });

  it("maps Poor prosperity to poor sprites", () => {
    const url = getSpriteUrl(BuildingKind.Store, 0, TownProsperity.Poor);
    expect(url).toBe("/assets/town-hub-buildings/poor/general-store/front.png");
  });

  it("returns the correct URL for telegraph office", () => {
    const url = getSpriteUrl(BuildingKind.Telegraph, 4, TownProsperity.Boomtown);
    expect(url).toBe("/assets/town-hub-buildings/boomtown/telegraph-office/rear-oblique.png");
  });

  it("handles all view angles correctly", () => {
    const views = [0, 1, 2, 3, 4];
    const viewNames = ["front", "profile", "rear", "front-oblique", "rear-oblique"];
    views.forEach((view, i) => {
      const url = getSpriteUrl(BuildingKind.Store, view, TownProsperity.Prosperous);
      expect(url).toBe(`/assets/town-hub-buildings/prosperous/general-store/${viewNames[i]}.png`);
    });
  });

  it("maps background building families to the shipped urls and keys", () => {
    expect(getBackgroundSpriteKey("background-house", 3, TownProsperity.Prosperous)).toBe(
      "background-house-prosperous-front-oblique",
    );
    expect(getBackgroundSpriteUrl("background-house", 3, TownProsperity.Prosperous)).toBe(
      "/assets/town-hub-buildings/prosperous/background-house/front-oblique.png",
    );
    expect(getBackgroundSpriteUrl("background-shop", 4, TownProsperity.Boomtown)).toBe(
      "/assets/town-hub-buildings/boomtown/background-shop/rear-oblique.png",
    );
  });

  it("maps every prosperity tier to the matching sprite directories", () => {
    const tiers = [
      { prosperity: TownProsperity.Boomtown, dir: "boomtown" },
      { prosperity: TownProsperity.Prosperous, dir: "prosperous" },
      { prosperity: TownProsperity.Poor, dir: "poor" },
      { prosperity: TownProsperity.Destitute, dir: "destitute" },
    ] as const;

    for (const { prosperity, dir } of tiers) {
      expect(getSpriteUrl(BuildingKind.Store, 1, prosperity)).toBe(
        `/assets/town-hub-buildings/${dir}/general-store/profile.png`,
      );
      expect(getSpriteUrl(BuildingKind.Telegraph, 4, prosperity)).toBe(
        `/assets/town-hub-buildings/${dir}/telegraph-office/rear-oblique.png`,
      );
      expect(getBackgroundSpriteKey("background-house", 0, prosperity)).toBe(
        `background-house-${dir}-front`,
      );
      expect(getBackgroundSpriteUrl("background-shop", 2, prosperity)).toBe(
        `/assets/town-hub-buildings/${dir}/background-shop/rear.png`,
      );
    }
  });
});
