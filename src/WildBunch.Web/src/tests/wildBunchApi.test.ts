import { afterEach, describe, expect, it, vi } from "vitest";
import { previewTravel, setupGame, startGameWithTown } from "../api/wildBunchApi";

function successfulResponse() {
  return {
    ok: true,
    headers: new Headers({ "content-type": "application/json" }),
    json: async () => ({}),
    text: async () => "",
  };
}

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

  it("sends the setup operation ID and exact request body", async () => {
    const fetchMock = vi.fn().mockResolvedValue(successfulResponse());
    vi.stubGlobal("fetch", fetchMock);
    const request = {
      playerName: "Ranger Vale",
      gameDifficulty: 2 as const,
      gameEntropy: 3 as const,
      seedCode: "a1234567-b123-c123-d123-e123456789ab",
    };

    await setupGame(request, "8b883c75-4e2e-4e82-90b4-95cb3867ae51");

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("http://localhost:5275/api/games/setup");
    expect(new Headers(init.headers).get("Idempotency-Key")).toBe(
      "8b883c75-4e2e-4e82-90b4-95cb3867ae51",
    );
    expect(JSON.parse(init.body as string)).toEqual(request);
  });

  it("sends the first-arrival operation ID and selected town", async () => {
    const fetchMock = vi.fn().mockResolvedValue(successfulResponse());
    vi.stubGlobal("fetch", fetchMock);

    await startGameWithTown(
      "game-1",
      { startingTownId: "dust-fork" },
      "fab21f5d-7f89-4382-9520-24a5b4f558d5",
    );

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("http://localhost:5275/api/games/game-1/start");
    expect(new Headers(init.headers).get("Idempotency-Key")).toBe(
      "fab21f5d-7f89-4382-9520-24a5b4f558d5",
    );
    expect(JSON.parse(init.body as string)).toEqual({ startingTownId: "dust-fork" });
  });
});
