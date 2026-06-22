import { Link, Outlet, useRouterState } from "@tanstack/react-router";
import { Hud } from "./Hud";
import { GlobalOverlays } from "../flow/GlobalOverlays";

function ShellChrome() {
  const path = useRouterState({ select: (state) => state.location.pathname });
  const isDebug = path === "/debug";

  return (
    <div className="v0-1-shell v0-1-shell--flow">
      <Hud />
      <div className="shell-overlay-bar">
        <GlobalOverlays />
        <nav className="shell-dev-nav" aria-label="Developer tools">
          <Link
            to="/debug"
            className={`shell-nav__link shell-nav__link--dev${isDebug ? " shell-nav__link--active" : ""}`}
            aria-current={isDebug ? "page" : undefined}
          >
            Dev tools
          </Link>
        </nav>
      </div>
      <main className="route-outlet" aria-live="polite">
        <div className="route">
          <Outlet />
        </div>
      </main>
    </div>
  );
}

export function AppShell() {
  return <ShellChrome />;
}
