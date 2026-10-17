import { Suspense, useEffect, useRef, useState, type ReactNode } from "react";
import { Outlet } from "@tanstack/react-router";
import styled from "styled-components";
import { Hud } from "./Hud";
import { GlobalOverlays, type OverlayKind } from "../flow/GlobalOverlays";
import { usePhaseRouteSync } from "./usePhaseRouteSync";
import { RouteLoading } from "./RouteLoading";
import { useGameSession } from "../state/useGameSession";
import { SessionRecoverySurface } from "../components/session-recovery/SessionRecoverySurface";

export interface ShellChromeSlots {
  toolbar?: ReactNode;
  renderOverlay?: (top: number) => ReactNode;
}

export function ShellChrome({ toolbar, renderOverlay }: ShellChromeSlots = {}) {
  const { sessionRestoreStatus, retrySessionRestore, abandonMissingSession } = useGameSession();
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);
  const chromeBarRef = useRef<HTMLDivElement>(null);
  const [chromeBarHeight, setChromeBarHeight] = useState(0);

  useEffect(() => {
    if (chromeBarRef.current) {
      setChromeBarHeight(chromeBarRef.current.offsetHeight);
    }
  }, [toolbar]);

  usePhaseRouteSync();

  if (sessionRestoreStatus === "loading") {
    return <RouteLoading role="status">Opening your saved game...</RouteLoading>;
  }

  if (sessionRestoreStatus === "failed" || sessionRestoreStatus === "missing") {
    return (
      <SessionRecoverySurface
        missing={sessionRestoreStatus === "missing"}
        onRetry={retrySessionRestore}
        onStartNewGame={abandonMissingSession}
      />
    );
  }

  return (
    <Shell>
      <ChromeBar ref={chromeBarRef}>
        <Hud
          onOpenJournal={() => setOpenOverlay("journal")}
          onOpenGameSettings={() => setOpenOverlay("game-settings")}
        />
        <OverlayBar>
          <GlobalOverlays openOverlay={openOverlay} onOpenOverlay={setOpenOverlay} />
          {toolbar}
        </OverlayBar>
      </ChromeBar>
      <RouteOutlet aria-live="polite">
        <Route>
          <Suspense fallback={<RouteLoading />}>
            <Outlet />
          </Suspense>
        </Route>
      </RouteOutlet>
      {renderOverlay?.(chromeBarHeight)}
    </Shell>
  );
}

export function AppShell() {
  return <ShellChrome />;
}

const Shell = styled.div`
  min-height: 100vh;
  display: flex;
  flex-direction: column;
`;

const ChromeBar = styled.div`
  position: sticky;
  top: 0;
  z-index: 1100;
  flex-shrink: 0;
`;

const OverlayBar = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 24px;
  border-bottom: 1px solid var(--border);
  background: rgba(15, 17, 21, 0.6);

  @media (max-width: 640px) {
    padding: 6px 14px;
  }
`;

const RouteOutlet = styled.main`
  flex: 1;
  padding: 24px;

  @media (max-width: 640px) {
    padding: 14px;
  }
`;

const Route = styled.div`
  max-width: 1100px;
  margin: 0 auto;
  display: grid;
  gap: 20px;

  @media (max-width: 960px) {
    grid-template-columns: 1fr;
  }
`;
