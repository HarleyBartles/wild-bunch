import { afterEach, describe, expect, it, vi } from "vitest";
import { createGame } from "./wildBunchApi";

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("wildBunchApi", () => {
  it("submits the encoded seed code when creating a game", async () => {
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
      seedCode: "WB1-H-03-000000000000-ABCD",
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "http://localhost:5275/api/games",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          playerName: "Ranger Vale",
          travelDifficulty: 2,
          seedCode: "WB1-H-03-000000000000-ABCD",
        }),
      }),
    );
  });
});
