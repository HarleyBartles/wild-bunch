import { afterEach, describe, expect, it, vi } from "vitest";
import { requestJson } from "../api/httpClient";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("requestJson HTTP failures", () => {
  it.each([
    {
      name: "prefers the problem title",
      response: () =>
        new Response(JSON.stringify({ title: "The title.", error: "The error." }), {
          status: 400,
          headers: { "content-type": "application/problem+json" },
        }),
      message: "The title.",
      status: 400,
    },
    {
      name: "uses the error field",
      response: () =>
        new Response(JSON.stringify({ error: "The error." }), {
          status: 401,
          headers: { "content-type": "application/problem+json" },
        }),
      message: "The error.",
      status: 401,
    },
    {
      name: "uses the first validation message",
      response: () =>
        new Response(JSON.stringify({ errors: { PlayerName: ["A name is required."] } }), {
          status: 422,
          headers: { "content-type": "application/problem+json" },
        }),
      message: "A name is required.",
      status: 422,
    },
    {
      name: "uses the problem detail",
      response: () =>
        new Response(JSON.stringify({ detail: "The detail." }), {
          status: 409,
          headers: { "content-type": "application/problem+json" },
        }),
      message: "The detail.",
      status: 409,
    },
    {
      name: "uses a plain-text response body",
      response: () =>
        new Response("The plain-text error.", {
          status: 503,
          headers: { "content-type": "text/plain" },
        }),
      message: "The plain-text error.",
      status: 503,
    },
    {
      name: "falls back for an unrecognized JSON response",
      response: () =>
        new Response(JSON.stringify({ message: "Not a supported field." }), {
          status: 418,
          headers: { "content-type": "application/json" },
        }),
      message: "Request failed with status 418",
      status: 418,
    },
    {
      name: "falls back when a JSON error body is malformed",
      response: () =>
        new Response("{invalid", {
          status: 502,
          headers: { "content-type": "application/problem+json" },
        }),
      message: "Request failed with status 502",
      status: 502,
    },
  ])("$name with its HTTP status", async ({ response, message, status }) => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(response()));

    const failure = await requestJson("/api/failure").catch((error: unknown) => error);

    expect(failure).toMatchObject({ message, status });
  });

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
