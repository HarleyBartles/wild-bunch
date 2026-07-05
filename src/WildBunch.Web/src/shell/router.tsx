import { lazy } from "react";
import { createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { AppShell } from "./AppShell";

const PreSessionSurface = lazy(() =>
  import("../flow/PreSessionSurface").then((m) => ({ default: m.PreSessionSurface })),
);
const TownHubSurface = lazy(() =>
  import("../flow/TownHubSurface").then((m) => ({ default: m.TownHubSurface })),
);
const StorePlace = lazy(() =>
  import("../flow/places/StorePlace").then((m) => ({ default: m.StorePlace })),
);
const SheriffPlace = lazy(() =>
  import("../flow/places/SheriffPlace").then((m) => ({ default: m.SheriffPlace })),
);
const SaloonPlace = lazy(() =>
  import("../flow/places/SaloonPlace").then((m) => ({ default: m.SaloonPlace })),
);
const TravelPrepSurface = lazy(() =>
  import("../flow/TravelPrepSurface").then((m) => ({ default: m.TravelPrepSurface })),
);
const TrailFlowSurface = lazy(() =>
  import("../flow/TrailFlowSurface").then((m) => ({ default: m.TrailFlowSurface })),
);

const rootRoute = createRootRoute({
  component: AppShell,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: PreSessionSurface,
});

const townRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/town",
  validateSearch: (search: Record<string, unknown>): { arrived?: "1" } => {
    if (search.arrived === "1") {
      return { arrived: "1" };
    }
    return {};
  },
  component: TownHubSurface,
});

// Town place routes are siblings under rootRoute, NOT children of townRoute.
// townRoute's component is TownHubSurface which renders the hub directly
// (no <Outlet />), so child routes would not render. Flat paths under root
// keep each place as an independently-rendered route.
const storeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/town/store",
  component: StorePlace,
});

const sheriffRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/town/sheriff",
  component: SheriffPlace,
});

const saloonRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/town/saloon",
  component: SaloonPlace,
});

const trailheadRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/town/trailhead",
  component: TravelPrepSurface,
});

const trailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/trail",
  component: TrailFlowSurface,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  townRoute,
  storeRoute,
  sheriffRoute,
  saloonRoute,
  trailheadRoute,
  trailRoute,
]);

export function createAppRouter() {
  return createRouter({
    routeTree,
    defaultPreload: "intent",
  });
}

export const router = createAppRouter();

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
