import "@testing-library/jest-dom/vitest";
import { configure } from "@testing-library/react";
import { vi } from "vitest";

// Lazy-loaded routes can take longer to resolve in a cold full-suite run
// (especially after npm ci), so give async queries more time before failing.
configure({ asyncUtilTimeout: 5000 });

Object.defineProperty(window, "scrollTo", {
  configurable: true,
  value: vi.fn(),
  writable: true,
});

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
