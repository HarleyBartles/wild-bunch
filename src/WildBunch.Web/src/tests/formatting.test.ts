import { afterEach, describe, expect, it, vi } from "vitest";
import { formatMoney, readStoredGameId } from "../utils/formatting";

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("formatMoney", () => {
  it("formats a whole dollar amount with two decimals", () => {
    expect(formatMoney(14)).toBe("$14.00");
  });

  it("formats a fractional amount", () => {
    expect(formatMoney(14.5)).toBe("$14.50");
  });

  it("formats zero", () => {
    expect(formatMoney(0)).toBe("$0.00");
  });
});

describe("readStoredGameId", () => {
  it("returns the stored game id when present", () => {
    vi.stubGlobal("localStorage", {
      getItem: vi.fn().mockReturnValue("game-abc"),
    });
    expect(readStoredGameId()).toBe("game-abc");
  });

  it("returns null when no game id is stored", () => {
    vi.stubGlobal("localStorage", {
      getItem: vi.fn().mockReturnValue(null),
    });
    expect(readStoredGameId()).toBe(null);
  });
});
