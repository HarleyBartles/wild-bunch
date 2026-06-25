import { useState } from "react";
import { Outlet } from "@tanstack/react-router";
import { Hud } from "./Hud";
import { GlobalOverlays, type OverlayKind } from "../flow/GlobalOverlays";
import { DevOverlay } from "../dev/DevOverlay";

function ShellChrome() {
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);
  const [devOverlayOpen, setDevOverlayOpen] = useState(false);

  return (
    <div className="v0-1-shell v0-1-shell--flow">
      <Hud onOpenJournal={() => setOpenOverlay("journal")} />
      <div className="shell-overlay-bar">
        <GlobalOverlays openOverlay={openOverlay} onOpenOverlay={setOpenOverlay} />
        <nav className="shell-dev-nav" aria-label="Developer tools">
          <button
            type="button"
            className={`shell-nav__link shell-nav__link--dev${devOverlayOpen ? " shell-nav__link--active" : ""}`}
            onClick={() => setDevOverlayOpen(true)}
          >
            Dev overlay
          </button>
        </nav>
      </div>
      <main className="route-outlet" aria-live="polite">
        <div className="route">
          <Outlet />
        </div>
      </main>
      <DevOverlay open={devOverlayOpen} onClose={() => setDevOverlayOpen(false)} />
    </div>
  );
}

export function AppShell() {
  return <ShellChrome />;
}
