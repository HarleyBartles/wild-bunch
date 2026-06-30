import { afterEach, describe, expect, it, vi } from "vitest";
import { previewTravel } from "../api/wildBunchApi";

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("wildBunchApi", () => {
  it("requests preview data for a travel destination", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      headers: new Headers({ "content-type": "application/json" }),
      json: async () => ({ success: true, message: "ok", preview: null }),
      text: async () => "",
    });

    vi.stubGlobal("fetch", fetchMock);

    await previewTravel("game-1", "holloway");

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5275/api/games/game-1/travel/preview/holloway",
      expect.any(Object),
    );
  });
});
