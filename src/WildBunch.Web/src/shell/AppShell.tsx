import { useEffect, useRef, useState } from "react";
import { Outlet } from "@tanstack/react-router";
import styled from "styled-components";
import { Hud } from "./Hud";
import { GlobalOverlays, type OverlayKind } from "../flow/GlobalOverlays";
import { DevOverlay } from "../dev/DevOverlay";
import { DevSurfaceProvider } from "../dev/DevSurfaceContext";

function ShellChrome() {
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);
  const [devOverlayOpen, setDevOverlayOpen] = useState(false);
  const chromeBarRef = useRef<HTMLDivElement | null>(null);
  const [chromeBarHeight, setChromeBarHeight] = useState(0);

  useEffect(() => {
    const el = chromeBarRef.current;
    if (!el) return;
    const update = () => setChromeBarHeight(el.offsetHeight);
    update();
    if (typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(update);
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <DevSurfaceProvider>
      <Shell>
        <ChromeBar ref={chromeBarRef}>
          <Hud
            onOpenJournal={() => setOpenOverlay("journal")}
            onOpenGameSettings={() => setOpenOverlay("game-settings")}
          />
          <OverlayBar>
            <GlobalOverlays openOverlay={openOverlay} onOpenOverlay={setOpenOverlay} />
            <DevNav aria-label="Developer tools">
              <DevToggleButton
                type="button"
                $active={devOverlayOpen}
                onClick={() => setDevOverlayOpen((prev) => !prev)}
                aria-expanded={devOverlayOpen}
              >
                {devOverlayOpen ? "Hide dev" : "Dev"}
              </DevToggleButton>
            </DevNav>
          </OverlayBar>
        </ChromeBar>
        <RouteOutlet aria-live="polite" $chromeBarHeight={chromeBarHeight}>
          <Route>
            <Outlet />
          </Route>
        </RouteOutlet>
        <DevOverlay
          open={devOverlayOpen}
          onClose={() => setDevOverlayOpen(false)}
          top={chromeBarHeight}
        />
      </Shell>
    </DevSurfaceProvider>
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

const DevNav = styled.nav`
  display: flex;
  gap: 6px;
`;

const DevToggleButton = styled.button<{ $active: boolean }>`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid ${(props) => (props.$active ? "var(--accent)" : "var(--border-strong)")};
  background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.14)" : "rgba(255, 255, 255, 0.03)")};
  color: ${(props) => (props.$active ? "var(--accent-strong)" : "var(--text)")};
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  min-height: 32px;
  cursor: pointer;
  transition-property: background-color, border-color, color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &::before {
    content: "";
    width: 7px;
    height: 7px;
    border-radius: 50%;
    background: ${(props) => (props.$active ? "var(--accent)" : "var(--muted)")};
    flex-shrink: 0;
  }

  &:hover {
    border-color: var(--accent);
    background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.2)" : "rgba(223, 159, 79, 0.08)")};
    color: ${(props) => (props.$active ? "var(--accent-strong)" : "var(--accent-strong)")};
  }

  &:active {
    transform: scale(0.97);
  }
`;

const RouteOutlet = styled.main<{ $chromeBarHeight: number }>`
  flex: 1;
  padding: 24px;
  margin-top: ${(props) => props.$chromeBarHeight}px;

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
