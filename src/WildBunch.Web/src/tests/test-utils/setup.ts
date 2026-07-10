import "@testing-library/jest-dom/vitest";
import { vi } from "vitest";

vi.mock("phaser", () => {
  class Game {
    public config: unknown;
    public destroyed = false;
    constructor(config: unknown) {
      this.config = config;
    }
    destroy() {
      this.destroyed = true;
    }
  }
  class Scene {
    constructor(_key?: string) {}
  }
  const Scale = { FIT: 0, CENTER_BOTH: 0 };
  return { default: { Game, Scene, Scale }, Game, Scene, Scale };
});

vi.mock("../dev/devApi", () => ({
  getTownLayoutSalts: vi.fn(),
  setTownLayoutSalts: vi.fn(),
  generateRandomTownLayoutSalts: vi.fn(),
  getSessionAudit: vi.fn(),
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
  getSaloonDevContext: vi.fn(),
  forceSaloonOverride: vi.fn(),
  getSessionDevContext: vi.fn(),
  forceDevDifficulty: vi.fn(),
  setDevEntropy: vi.fn(),
}));
