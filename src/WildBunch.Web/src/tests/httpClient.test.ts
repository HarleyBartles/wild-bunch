import { afterEach, describe, expect, it, vi } from "vitest";
import { requestJson } from "../api/httpClient";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("requestJson HTTP failures", () => {
  it("preserves an HTTP status separately from a transport failure", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 404,
        headers: { get: () => "application/problem+json" },
        json: async () => ({ error: "Saved playthrough was not found." }),
      }),
    );

    const httpFailure = await requestJson("/api/games/missing").catch((error: unknown) => error);

    expect(httpFailure).toMatchObject({
      message: "Saved playthrough was not found.",
      status: 404,
    });

    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    const transportFailure = await requestJson("/api/games/missing").catch(
      (error: unknown) => error,
    );

    expect(transportFailure).toBeInstanceOf(TypeError);
    expect(transportFailure).not.toHaveProperty("status");
  });
});
