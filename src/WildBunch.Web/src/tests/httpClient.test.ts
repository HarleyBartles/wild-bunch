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

  it("preserves explicit caller headers when they are provided as Headers", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      headers: new Headers({ "content-type": "application/json" }),
      json: async () => ({}),
    });
    vi.stubGlobal("fetch", fetchMock);

    await requestJson("/api/example", {
      method: "POST",
      headers: new Headers({
        "Content-Type": "application/vnd.wild-bunch+json",
        "X-Trace": "kept",
      }),
      body: "{}",
    });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = new Headers(init.headers);
    expect(headers.get("Content-Type")).toBe("application/vnd.wild-bunch+json");
    expect(headers.get("X-Trace")).toBe("kept");
  });
});
