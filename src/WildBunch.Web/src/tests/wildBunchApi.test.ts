import { afterEach, describe, expect, it, vi } from "vitest";
import { createGame, previewTravel } from "../api/wildBunchApi";

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("wildBunchApi", () => {
  it("submits the UUID seed code when creating a game", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      headers: new Headers({ "content-type": "application/json" }),
      json: async () => ({ id: "game-1" }),
      text: async () => "",
    });

    vi.stubGlobal("fetch", fetchMock);

    await createGame({
      playerName: "Ranger Vale",
      travelDifficulty: 2,
      seedCode: "7d455293-f269-a642-72af-0193fdbdfb51",
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5275/api/games",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          playerName: "Ranger Vale",
          travelDifficulty: 2,
          seedCode: "7d455293-f269-a642-72af-0193fdbdfb51",
        }),
      }),
    );
  });

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
