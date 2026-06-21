import { Link, Outlet, useRouterState } from "@tanstack/react-router";
import { Hud } from "./Hud";

interface NavEntry {
  to: string;
  label: string;
  dev?: boolean;
}

const navEntries: NavEntry[] = [
  { to: "/", label: "Camp" },
  { to: "/hunt", label: "Hunt" },
  { to: "/case", label: "Case file" },
  { to: "/wanted", label: "Wanted" },
  { to: "/trail", label: "Trail" },
  { to: "/debug", label: "Dev tools", dev: true },
];

function ShellChrome() {
  const path = useRouterState({ select: (state) => state.location.pathname });

  return (
    <div className="v0-1-shell">
      <Hud />
      <nav className="shell-nav" aria-label="Game navigation">
        <ul className="shell-nav__group">
          {navEntries
            .filter((entry) => !entry.dev)
            .map((entry) => (
              <li key={entry.to}>
                <Link
                  to={entry.to}
                  className={`shell-nav__link${path === entry.to ? " shell-nav__link--active" : ""}`}
                  aria-current={path === entry.to ? "page" : undefined}
                >
                  {entry.label}
                </Link>
              </li>
            ))}
        </ul>
        <ul className="shell-nav__group shell-nav__group--dev">
          {navEntries
            .filter((entry) => entry.dev)
            .map((entry) => (
              <li key={entry.to}>
                <Link
                  to={entry.to}
                  className={`shell-nav__link shell-nav__link--dev${path === entry.to ? " shell-nav__link--active" : ""}`}
                  aria-current={path === entry.to ? "page" : undefined}
                >
                  {entry.label}
                </Link>
              </li>
            ))}
        </ul>
      </nav>
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
