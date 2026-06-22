import { createRootRoute, createRoute, createRouter } from "@tanstack/react-router";
import { AppShell } from "./AppShell";
import { GameFlowRouter } from "../flow/GameFlowRouter";
import { DebugCockpitRoute } from "../routes/DebugCockpitRoute";
import { Outlet } from "@tanstack/react-router";

const rootRoute = createRootRoute({
  component: AppShell,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: GameFlowRouter,
});

const debugRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/debug",
  component: DebugCockpitRoute,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
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

export type { rootRoute, indexRoute, debugRoute };
