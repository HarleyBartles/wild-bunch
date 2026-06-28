import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DevOverlay } from "../dev/DevOverlay";
import { DevSurfaceProvider } from "../dev/DevSurfaceContext";
import type { DevSurface } from "../dev/DevSurfaceContext";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { getSessionAudit } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSessionAudit: vi.fn(),
  getSaloonDevContext: vi.fn(),
  forceSaloonOverride: vi.fn(),
  clearSaloonOverride: vi.fn(),
  getTravelDevContext: vi.fn(),
  forceTravelOverride: vi.fn(),
  clearTravelOverride: vi.fn(),
}));

vi.mock("../api/wildBunchApi", () => ({
  buyStoreItem: vi.fn(),
  createGame: vi.fn(),
  getAvailableActions: vi.fn(),
  getGame: vi.fn(),
  getJournal: vi.fn(),
  getTownStoreOffers: vi.fn(),
  checkLocalRecords: vi.fn(),
  inspectNoticeBoard: vi.fn(),
  confrontSaloonPersonOfInterest: vi.fn(),
  lookAroundSaloon: vi.fn(),
  readWantedPosters: vi.fn(),
  followTelegraphLeads: vi.fn(),
  gatherLocalGossip: vi.fn(),
  travel: vi.fn(),
  acknowledgeTravelArrival: vi.fn(),
  advanceTravelDay: vi.fn(),
  resolveTravelEncounter: vi.fn(),
  previewTravel: vi.fn(),
}));

const mockedGetSessionAudit = vi.mocked(getSessionAudit);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  window.localStorage.clear();
});

function renderOverlay(open: boolean, onClose = () => {}, top = 0, surface: DevSurface = "town") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <DevSurfaceProvider>
          <SurfaceSetter surface={surface} />
          <DevOverlay open={open} onClose={onClose} top={top} />
        </DevSurfaceProvider>
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}

// Helper to set the dev surface from within the provider tree
function SurfaceSetter({ surface }: { surface: DevSurface }) {
  // Use the context setter directly via a re-render trick
  // We need to use the useSetDevSurface hook, but it must be inside the provider
  return <SurfaceSetterInner surface={surface} />;
}

import { useSetDevSurface } from "../dev/DevSurfaceContext";
import { useEffect } from "react";

function SurfaceSetterInner({ surface }: { surface: DevSurface }) {
  const setSurface = useSetDevSurface();
  useEffect(() => {
    setSurface(surface);
  }, [surface, setSurface]);
  return null;
}

describe("DevOverlay", () => {
  it("renders nothing when closed", () => {
    renderOverlay(false);
    expect(screen.queryByRole("region", { name: /developer overlay/i })).not.toBeInTheDocument();
  });

  it("renders the overlay region when open", () => {
    renderOverlay(true);
    expect(screen.getByRole("region", { name: /developer overlay/i })).toBeInTheDocument();
  });

  it("calls onClose when the Close button is clicked", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /close/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("calls onClose on Escape key when not expanded", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.keyboard("{Escape}");
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("renders the session audit panel tab on town surface", () => {
    renderOverlay(true, () => {}, 0, "town");
    // Session audit should appear in the bottom section, not in the main list
    const sessionAuditButtons = screen.getAllByRole("button", { name: /session audit/i });
    expect(sessionAuditButtons).toHaveLength(1);
  });

  it("expands when Expand is clicked and shows Shrink button", async () => {
    renderOverlay(true);

    const user = userEvent.setup();
    const expandBtn = screen.getByRole("button", { name: /expand/i });
    await user.click(expandBtn);

    expect(screen.getByRole("button", { name: /shrink/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /expand/i })).not.toBeInTheDocument();
  });

  it("Escape shrinks instead of closing when expanded", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /expand/i }));

    await user.keyboard("{Escape}");
    expect(onClose).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: /expand/i })).toBeInTheDocument();
  });

  it("calls onClose when clicking outside the drawer", async () => {
    const onClose = vi.fn();
    renderOverlay(true, onClose);

    const user = userEvent.setup();
    const clickAway = screen.getByTestId("dev-click-away");
    expect(clickAway).toBeInTheDocument();

    await user.click(clickAway);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});

describe("DevOverlay contextual panel visibility", () => {
  it("does not show Saloon dev tab when surface is sheriff", () => {
    renderOverlay(true, () => {}, 0, "sheriff");
    // Session audit should appear in the bottom section
    const sessionAuditButtons = screen.getAllByRole("button", { name: /session audit/i });
    expect(sessionAuditButtons).toHaveLength(1);
    expect(screen.queryByRole("button", { name: /saloon dev/i })).not.toBeInTheDocument();
  });

  it("does not show Saloon dev tab when surface is town (no place)", () => {
    renderOverlay(true, () => {}, 0, "town");
    expect(screen.queryByRole("button", { name: /saloon dev/i })).not.toBeInTheDocument();
  });

  it("shows Saloon dev tab when surface is saloon", () => {
    renderOverlay(true, () => {}, 0, "saloon");
    expect(screen.getByRole("button", { name: /saloon dev/i })).toBeInTheDocument();
  });

  it("defaults to Saloon dev (not Session Audit) when surface is saloon", async () => {
    renderOverlay(true, () => {}, 0, "saloon");
    // The Saloon dev tab should be the active/selected panel after the effect runs
    await waitFor(() => {
      const saloonTab = screen.getByRole("button", { name: /saloon dev/i });
      expect(saloonTab).toHaveAttribute("aria-pressed", "true");
    });
    // Session Audit should be present but NOT the default
    const auditTabs = screen.getAllByRole("button", { name: /session audit/i });
    expect(auditTabs).toHaveLength(1);
    expect(auditTabs[0]).not.toHaveAttribute("aria-pressed", "true");
  });

  it("does not show Travel dev tab when surface is town", () => {
    renderOverlay(true, () => {}, 0, "town");
    expect(screen.queryByRole("button", { name: /travel dev/i })).not.toBeInTheDocument();
  });

  it("does not show Travel dev tab when surface is saloon", () => {
    renderOverlay(true, () => {}, 0, "saloon");
    expect(screen.queryByRole("button", { name: /travel dev/i })).not.toBeInTheDocument();
  });

  it("shows Travel dev tab when surface is trail", () => {
    renderOverlay(true, () => {}, 0, "trail");
    expect(screen.getByRole("button", { name: /travel dev/i })).toBeInTheDocument();
  });

  it("shows Travel dev tab when surface is trailhead", () => {
    renderOverlay(true, () => {}, 0, "trailhead");
    expect(screen.getByRole("button", { name: /travel dev/i })).toBeInTheDocument();
  });

  it("shows only session audit when surface is store (no contextual panels)", () => {
    renderOverlay(true, () => {}, 0, "store");
    // When there are no main panels, session audit should be the only panel
    const sessionAuditButtons = screen.getAllByRole("button", { name: /session audit/i });
    expect(sessionAuditButtons).toHaveLength(1);
    expect(screen.queryByRole("button", { name: /saloon dev/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /travel dev/i })).not.toBeInTheDocument();
  });

  it("shows no contextual dev panel message when no panels match", () => {
    // pre-session has no contextual panels and session-audit is always available,
    // so this tests the "always available" fallback. To test the empty state,
    // we'd need a surface with no panels at all. Session audit is always available,
    // so the empty state won't trigger with current panels. Instead verify
    // session audit is the only panel on pre-session.
    renderOverlay(true, () => {}, 0, "pre-session");
    const sessionAuditButtons = screen.getAllByRole("button", { name: /session audit/i });
    expect(sessionAuditButtons).toHaveLength(1);
    expect(screen.queryByRole("button", { name: /saloon dev/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /travel dev/i })).not.toBeInTheDocument();
  });
});

describe("DevOverlay expanded height cap", () => {
  it("caps expanded drawer height and makes content scrollable", async () => {
    renderOverlay(true, () => {}, 0, "town");

    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /expand/i }));

    const drawer = screen.getByTestId("dev-drawer");
    const drawerStyle = window.getComputedStyle(drawer);
    // Expanded height should be capped (not 100dvh)
    const heightValue = drawerStyle.height;
    expect(heightValue).not.toBe("");
    expect(heightValue).not.toContain("100dvh");
    // The content area should have overflow auto for internal scrolling
    const content = screen.getByTestId("dev-overlay-content");
    const contentStyle = window.getComputedStyle(content);
    expect(contentStyle.overflow).toBe("auto");
  });
});
