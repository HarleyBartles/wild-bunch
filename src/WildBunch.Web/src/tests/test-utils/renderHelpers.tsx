import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import type { ReactElement } from "react";
import { GameSessionProvider } from "../../state/GameSessionProvider";

/**
 * Creates a QueryClient with retry disabled for deterministic test behavior.
 */
export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

/**
 * Renders a node inside QueryClientProvider + GameSessionProvider.
 * Use this for tests that exercise the full session context.
 */
export function renderInSessionProvider(node: ReactElement) {
  const queryClient = createTestQueryClient();
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>{node}</GameSessionProvider>
    </QueryClientProvider>,
  );
  return { queryClient };
}

/**
 * Renders a node inside QueryClientProvider only (no session provider).
 * Use this for component tests that only need React Query.
 */
export function renderWithQueryClient(node: ReactElement) {
  const queryClient = createTestQueryClient();
  render(<QueryClientProvider client={queryClient}>{node}</QueryClientProvider>);
  return { queryClient };
}
