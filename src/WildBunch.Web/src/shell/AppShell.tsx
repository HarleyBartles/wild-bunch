import { Hud } from "./Hud";
import { DEBUG_ROUTES, PLAYER_ROUTES, resolveRoute } from "./routes";
import { useHashRoute } from "./useHashRoute";
import { CampRoute } from "../routes/CampRoute";
import { HuntRoute } from "../routes/HuntRoute";
import { CaseFileRoute } from "../routes/CaseFileRoute";
import { WantedRoute } from "../routes/WantedRoute";
import { TrailRoute } from "../routes/TrailRoute";
import { DebugCockpitRoute } from "../routes/DebugCockpitRoute";

function RouteOutlet({ path }: { path: string }) {
  switch (resolveRoute(path).id) {
    case "hunt":
      return <HuntRoute />;
    case "case":
      return <CaseFileRoute />;
    case "wanted":
      return <WantedRoute />;
    case "trail":
      return <TrailRoute />;
    case "debug":
      return <DebugCockpitRoute />;
    case "camp":
    default:
      return <CampRoute />;
  }
}

export function AppShell() {
  const { path, navigate } = useHashRoute();
  const activeRoute = resolveRoute(path);

  return (
    <div className="shell">
      <Hud />
      <nav className="shell-nav" aria-label="Primary">
        <div className="shell-nav__group">
          {PLAYER_ROUTES.map((route) => (
            <button
              key={route.id}
              type="button"
              className={`shell-nav__link${activeRoute.id === route.id ? " shell-nav__link--active" : ""}`}
              aria-current={activeRoute.id === route.id ? "page" : undefined}
              onClick={() => navigate(route.path)}
            >
              {route.label}
            </button>
          ))}
        </div>
        <div className="shell-nav__group shell-nav__group--debug">
          {DEBUG_ROUTES.map((route) => (
            <button
              key={route.id}
              type="button"
              className={`shell-nav__link shell-nav__link--debug${
                activeRoute.id === route.id ? " shell-nav__link--active" : ""
              }`}
              aria-current={activeRoute.id === route.id ? "page" : undefined}
              onClick={() => navigate(route.path)}
            >
              {route.label}
            </button>
          ))}
        </div>
      </nav>
      <main className="shell-main">
        <RouteOutlet path={path} />
      </main>
    </div>
  );
}
