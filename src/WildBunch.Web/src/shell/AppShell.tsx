import { useState } from "react";
import { Outlet } from "@tanstack/react-router";
import styled from "styled-components";
import { Hud } from "./Hud";
import { GlobalOverlays, type OverlayKind } from "../flow/GlobalOverlays";
import { DevOverlay } from "../dev/DevOverlay";

function ShellChrome() {
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);
  const [devOverlayOpen, setDevOverlayOpen] = useState(false);

  return (
    <Shell>
      <Hud onOpenJournal={() => setOpenOverlay("journal")} />
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
      <RouteOutlet aria-live="polite">
        <Route>
          <Outlet />
        </Route>
      </RouteOutlet>
      <DevOverlay open={devOverlayOpen} onClose={() => setDevOverlayOpen(false)} />
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

const OverlayBar = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 8px 24px;
  border-bottom: 1px solid var(--border);
  background: rgba(15, 17, 21, 0.6);
`;

const DevNav = styled.nav`
  display: flex;
  gap: 6px;
`;

const DevToggleButton = styled.button<{ $active: boolean }>`
  padding: 6px 12px;
  border-radius: 999px;
  border: 1px solid ${(props) => (props.$active ? "var(--border-strong)" : "transparent")};
  background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.12)" : "transparent")};
  color: ${(props) => (props.$active ? "var(--accent-strong)" : "var(--muted)")};
  font-size: 0.8rem;
  font-style: italic;
  transition: background 120ms ease, color 120ms ease, border-color 120ms ease;

  &:hover {
    color: var(--text);
    background: ${(props) => (props.$active ? "rgba(223, 159, 79, 0.18)" : "rgba(255, 255, 255, 0.05)")};
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
