import { describe, expect, it } from "vitest";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { router, createAppRouter } from "../shell/router";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const routerPath = path.resolve(__dirname, "..", "shell", "router.tsx");
const source = fs.readFileSync(routerPath, "utf8");

describe("Routing Conventions Enforcement", () => {
  it("exports a createAppRouter factory for test isolation", () => {
    expect(typeof createAppRouter).toBe("function");
    const testRouter = createAppRouter();
    expect(testRouter).toBeDefined();
    expect(testRouter).not.toBe(router);
  });

  it("all route components are lazy-loaded via React.lazy", () => {
    // Extract all component: X references from createRoute calls
    const componentRefs = source.match(/component:\s*(\w+)/g) ?? [];
    expect(componentRefs.length, "should have route component references").toBeGreaterThan(0);

    // Every component used in a route (except AppShell which is the root) must be lazy-loaded
    for (const ref of componentRefs) {
      const name = ref.replace(/component:\s*/, "");
      if (name === "AppShell") continue; // root route component is not lazy

      const lazyPattern = new RegExp(`const ${name} = lazy\\(`);
      expect(
        lazyPattern.test(source),
        `Route component "${name}" must be lazy-loaded via React.lazy. ` +
          `Add: const ${name} = lazy(() => import("...").then((m) => ({ default: m.${name} })));`,
      ).toBe(true);
    }
  });

  it("town place routes are flat siblings under rootRoute, not children of townRoute", () => {
    // TownHubSurface renders the hub directly (no <Outlet />), so child routes
    // would not render. Place routes must be siblings of /town under rootRoute.
    const townChildRoutes = source.matchAll(
      /createRoute\(\{[\s\S]*?path:\s*"(\/town\/[^"]+)"[\s\S]*?getParentRoute:\s*\(\)\s*=>\s*(\w+)/g,
    );

    let foundAny = false;
    for (const match of townChildRoutes) {
      foundAny = true;
      const routePath = match[1];
      const parentRoute = match[2];
      expect(
        parentRoute,
        `Route "${routePath}" must have getParentRoute: () => rootRoute, ` +
          `not () => townRoute. TownHubSurface has no <Outlet />, so child routes won't render.`,
      ).toBe("rootRoute");
    }
    expect(foundAny, "should have at least one /town/* route").toBe(true);
  });

  it("townRoute validateSearch returns {} when arrived param is absent", () => {
    // If validateSearch returns { arrived: undefined } instead of {},
    // TanStack Router types the param as required, breaking navigate({ to: "/town" }).
    expect(
      source.includes("return {};"),
      "townRoute validateSearch must return {} when the arrived param is absent, " +
        "not { arrived: undefined }. The latter makes TanStack Router type the param as required.",
    ).toBe(true);
  });
});
