import { describe, expect, it } from "vitest";
import { BuildingKind, TownProsperity } from "../../api/types";
import { getSpriteUrl } from "./sprite-loader";

describe("getSpriteUrl", () => {
  it("returns the correct URL for a prosperous store front-oblique view", () => {
    const url = getSpriteUrl(BuildingKind.Store, 3, TownProsperity.Prosperous);
    expect(url).toBe("/assets/town-buildings/prosperous/general-store/front-oblique.png");
  });

  it("returns the correct URL for a boomtown sheriff profile view", () => {
    const url = getSpriteUrl(BuildingKind.Sheriff, 1, TownProsperity.Boomtown);
    expect(url).toBe("/assets/town-buildings/boomtown/sheriff-office/profile.png");
  });

  it("returns the correct URL for a destitute saloon front view", () => {
    const url = getSpriteUrl(BuildingKind.Saloon, 0, TownProsperity.Destitute);
    expect(url).toBe("/assets/town-buildings/destitute/saloon/front.png");
  });

  it("returns null for Trailhead (which has no sprite assets)", () => {
    const url = getSpriteUrl(BuildingKind.Trailhead, 0, TownProsperity.Prosperous);
    expect(url).toBeNull();
  });

  it("maps Poor prosperity to poor sprites", () => {
    const url = getSpriteUrl(BuildingKind.Store, 0, TownProsperity.Poor);
    expect(url).toBe("/assets/town-buildings/poor/general-store/front.png");
  });

  it("returns the correct URL for telegraph office", () => {
    const url = getSpriteUrl(BuildingKind.Telegraph, 4, TownProsperity.Boomtown);
    expect(url).toBe("/assets/town-buildings/boomtown/telegraph-office/rear-oblique.png");
  });

  it("handles all view angles correctly", () => {
    const views = [0, 1, 2, 3, 4];
    const viewNames = ["front", "profile", "rear", "front-oblique", "rear-oblique"];
    views.forEach((view, i) => {
      const url = getSpriteUrl(BuildingKind.Store, view, TownProsperity.Prosperous);
      expect(url).toBe(`/assets/town-buildings/prosperous/general-store/${viewNames[i]}.png`);
    });
  });
});
