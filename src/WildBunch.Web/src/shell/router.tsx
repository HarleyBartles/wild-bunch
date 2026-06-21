import { createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { AppShell } from "./AppShell";
import { CampRoute } from "../routes/CampRoute";
import { CaseFileRoute } from "../routes/CaseFileRoute";
import { DebugCockpitRoute } from "../routes/DebugCockpitRoute";
import { HuntRoute } from "../routes/HuntRoute";
import { TrailRoute } from "../routes/TrailRoute";
import { WantedRoute } from "../routes/WantedRoute";

const rootRoute = createRootRoute({
  component: AppShell,
});

const campRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: CampRoute,
});

const huntRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/hunt",
  component: HuntRoute,
});

const caseFileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/case",
  component: CaseFileRoute,
});

const wantedRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/wanted",
  component: WantedRoute,
});

const trailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/trail",
  component: TrailRoute,
});

const debugRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/debug",
  component: DebugCockpitRoute,
});

const routeTree = rootRoute.addChildren([
  campRoute,
  huntRoute,
  caseFileRoute,
  wantedRoute,
  trailRoute,
  debugRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: "intent",
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

export type { rootRoute, campRoute, huntRoute, caseFileRoute, wantedRoute, trailRoute, debugRoute };
