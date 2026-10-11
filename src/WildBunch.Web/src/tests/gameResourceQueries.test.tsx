import { QueryClient, QueryClientProvider, useQuery } from "@tanstack/react-query";
import { act, cleanup, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, describe, expect, it } from "vitest";
import { gameResourceQueryKeys, refreshGameResources } from "../state/gameResourceQueries";

afterEach(cleanup);

const resources: Array<{ name: string; key: (id: string) => readonly unknown[] }> = [
  { name: "session", key: gameResourceQueryKeys.session },
  { name: "actions", key: gameResourceQueryKeys.actions },
  { name: "journal", key: gameResourceQueryKeys.journal },
  { name: "store offers", key: (id) => gameResourceQueryKeys.storeOffers(id, "town-1") },
  {
    name: "travel preview",
    key: (id) => gameResourceQueryKeys.travelPreview(id, "town-1", "town-2", 1),
  },
  { name: "session dev", key: gameResourceQueryKeys.devSession },
  { name: "saloon dev", key: gameResourceQueryKeys.devSaloon },
  { name: "travel dev", key: gameResourceQueryKeys.devTravel },
  { name: "session audit", key: gameResourceQueryKeys.devSessionAudit },
];

function ResourceReader({
  name,
  gameId,
  queryKey,
  counts,
}: {
  name: string;
  gameId: string;
  queryKey: readonly unknown[];
  counts: Map<string, number>;
}) {
  const { data } = useQuery({
    queryKey,
    queryFn: async () => {
      const key = `${name}:${gameId}`;
      const count = (counts.get(key) ?? 0) + 1;
      counts.set(key, count);
      return `${name}:${gameId}:${count}`;
    },
  });
  return <output aria-label={`${name} ${gameId}`}>{data}</output>;
}

function ResourceReaders({ counts }: { counts: Map<string, number> }) {
  return (
    <>
      {resources.flatMap(({ name, key }) =>
        ["game-a", "game-b"].map((gameId) => (
          <ResourceReader
            key={`${name}:${gameId}`}
            name={name}
            gameId={gameId}
            queryKey={key(gameId)}
            counts={counts}
          />
        )),
      )}
    </>
  );
}

describe("game resource refresh", () => {
  it("refetches every active mutable reader for the changed game only", async () => {
    const counts = new Map<string, number>();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false, staleTime: Infinity } },
    });
    function Wrapper({ children }: { children: ReactNode }) {
      return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
    }
    render(
      <Wrapper>
        <ResourceReaders counts={counts} />
      </Wrapper>,
    );

    await screen.findByText("session:game-a:1");
    await screen.findByText("session:game-b:1");
    await waitFor(() => expect(counts.size).toBe(resources.length * 2));

    await act(async () => refreshGameResources(queryClient, "game-a"));

    await waitFor(() => {
      for (const { name } of resources) {
        expect(screen.getByLabelText(`${name} game-a`).textContent).toBe(`${name}:game-a:2`);
        expect(screen.getByLabelText(`${name} game-b`).textContent).toBe(`${name}:game-b:1`);
        expect(counts.get(`${name}:game-b`)).toBe(1);
      }
    });
    queryClient.clear();
  });

  it("preserves the established query key values", () => {
    expect(gameResourceQueryKeys.session("g")).toEqual(["session", "g"]);
    expect(gameResourceQueryKeys.storeOffers("g", "t")).toEqual(["store-offers", "g", "t"]);
    expect(gameResourceQueryKeys.storeOffers("g", undefined)).toEqual([
      "store-offers",
      "g",
      undefined,
    ]);
    expect(gameResourceQueryKeys.travelPreview("g", "a", "b", 42)).toEqual([
      "travel-preview",
      "g",
      "a",
      "b",
      42,
    ]);
    expect(gameResourceQueryKeys.travelPreview(null, null, null, 0)).toEqual([
      "travel-preview",
      null,
      null,
      null,
      0,
    ]);
  });
});
