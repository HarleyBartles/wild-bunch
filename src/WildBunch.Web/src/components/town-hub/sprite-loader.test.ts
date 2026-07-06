import { describe, expect, it } from "vitest";
import { BuildingKind, TownProsperity } from "../../api/types";
import { getSpriteUrl } from "./sprite-loader";

describe("getSpriteUrl", () => {
  it("returns the correct URL for a prosperous store front-oblique view", () => {
    const url = getSpriteUrl(BuildingKind.Store, "front-oblique", TownProsperity.Prosperous);
    expect(url).toBe("/assets/town-buildings/prosperous/general-store/front-oblique.png");
  });

  it("returns the correct URL for a boomtown sheriff profile view", () => {
    const url = getSpriteUrl(BuildingKind.Sheriff, "profile", TownProsperity.Boomtown);
    expect(url).toBe("/assets/town-buildings/boomtown/sheriff-office/profile.png");
  });

  it("returns the correct URL for a destitute saloon front view", () => {
    const url = getSpriteUrl(BuildingKind.Saloon, "front", TownProsperity.Destitute);
    expect(url).toBe("/assets/town-buildings/destitute/saloon/front.png");
  });

  it("returns null for Trailhead (which has no sprite assets)", () => {
    const url = getSpriteUrl(BuildingKind.Trailhead, "front", TownProsperity.Prosperous);
    expect(url).toBeNull();
  });

  it("maps Poor prosperity to prosperous sprites (no dedicated poor tier)", () => {
    const url = getSpriteUrl(BuildingKind.Store, "front", TownProsperity.Poor);
    expect(url).toBe("/assets/town-buildings/prosperous/general-store/front.png");
  });

  it("returns the correct URL for telegraph office", () => {
    const url = getSpriteUrl(BuildingKind.Telegraph, "rear-oblique", TownProsperity.Boomtown);
    expect(url).toBe("/assets/town-buildings/boomtown/telegraph-office/rear-oblique.png");
  });

  it("handles all view angles correctly", () => {
    const views = ["front", "profile", "rear", "front-oblique", "rear-oblique"];
    views.forEach((view) => {
      const url = getSpriteUrl(BuildingKind.Store, view, TownProsperity.Prosperous);
      expect(url).toBe(`/assets/town-buildings/prosperous/general-store/${view}.png`);
    });
  });
});
