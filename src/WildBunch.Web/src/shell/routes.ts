export type ShellRouteId = "camp" | "hunt" | "case" | "wanted" | "trail" | "debug";

export interface ShellRoute {
  id: ShellRouteId;
  path: string;
  label: string;
  eyebrow: string;
  title: string;
  description: string;
  audience: "player" | "debug";
}

export const SHELL_ROUTES: ShellRoute[] = [
  {
    id: "camp",
    path: "/",
    label: "Camp",
    eyebrow: "Make camp",
    title: "Saddle up a new hunt",
    description: "Start a fresh hunt or pick the trail back up where the last session left it.",
    audience: "player",
  },
  {
    id: "hunt",
    path: "/hunt",
    label: "Hunt",
    eyebrow: "On the hunt",
    title: "Field actions",
    description: "Work the current town: read the board, chase leads, and watch the saloon.",
    audience: "player",
  },
  {
    id: "case",
    path: "/case",
    label: "Case file",
    eyebrow: "Case file",
    title: "Investigation board",
    description: "A read-only summary of player-known clues, suspects, and warrants.",
    audience: "player",
  },
  {
    id: "wanted",
    path: "/wanted",
    label: "Wanted",
    eyebrow: "Wanted board",
    title: "Wanted posters",
    description: "Public-safe sheriff notices, quick views, and feature notes from the current board.",
    audience: "player",
  },
  {
    id: "trail",
    path: "/trail",
    label: "Trail",
    eyebrow: "Trail",
    title: "Travel routes",
    description: "Pick a connected town and ride. Active journeys keep their trail diary here.",
    audience: "player",
  },
  {
    id: "debug",
    path: "/debug",
    label: "Dev tools",
    eyebrow: "Debug cockpit",
    title: "Cockpit dev shell",
    description: "Dense developer cockpit with every panel on one page. Not a player-facing route.",
    audience: "debug",
  },
];

export const DEFAULT_ROUTE = SHELL_ROUTES[0];

export function resolveRoute(path: string): ShellRoute {
  return SHELL_ROUTES.find((route) => route.path === path) ?? DEFAULT_ROUTE;
}

export const PLAYER_ROUTES = SHELL_ROUTES.filter((route) => route.audience === "player");
export const DEBUG_ROUTES = SHELL_ROUTES.filter((route) => route.audience === "debug");
