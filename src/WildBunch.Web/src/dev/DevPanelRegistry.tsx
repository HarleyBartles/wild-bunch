import type { ReactNode } from "react";
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";
import { TravelDevPanel } from "./panels/TravelDevPanel";
import { SaloonDevPanel } from "./panels/SaloonDevPanel";
import type { DevSurface } from "./DevSurfaceContext";

export interface DevPanelRenderProps {
  /**
   * Whether the dev overlay is in expanded (workbench) mode.
   * Panels should use horizontal space in expanded mode rather than
   * becoming a tall single-column stack. Per the dev-overlay doctrine.
   */
  expanded: boolean;
}

export interface DevPanelDefinition {
  id: string;
  label: string;
  render: (props: DevPanelRenderProps) => ReactNode;
  /**
   * Surfaces where this panel is contextually available.
   * If undefined, the panel is available on all surfaces.
   */
  surfaces?: DevSurface[];
  /**
   * If true, this panel is the surface owner and should be the default
   * selected panel when its surface is active. Per the dev-overlay doctrine,
   * the surface owner wins the default over global panels like Session Audit.
   */
  isSurfaceOwner?: boolean;
}

export const devPanels: DevPanelDefinition[] = [
  {
    id: "session-audit",
    label: "Session audit",
    render: () => <SessionAuditDevPanel />,
    // Session audit is broadly available on all gameplay surfaces
  },
  {
    id: "travel-dev",
    label: "Travel dev",
    render: () => <TravelDevPanel />,
    surfaces: ["trail", "arrival", "trailhead"],
    isSurfaceOwner: true,
  },
  {
    id: "saloon-dev",
    label: "Saloon dev",
    render: ({ expanded }) => <SaloonDevPanel expanded={expanded} />,
    surfaces: ["saloon"],
    isSurfaceOwner: true,
  },
];

/**
 * Returns the panels available for the given dev surface.
 * Panels without a `surfaces` filter are always available.
 * Surface owner panels are ordered first so they win default selection.
 */
export function getAvailablePanels(surface: DevSurface): DevPanelDefinition[] {
  const available = devPanels.filter(
    (panel) => !panel.surfaces || panel.surfaces.includes(surface),
  );
  // Surface owner panels first, then others
  return available.sort((a, b) => {
    if (a.isSurfaceOwner && !b.isSurfaceOwner) return -1;
    if (!a.isSurfaceOwner && b.isSurfaceOwner) return 1;
    return 0;
  });
}

/**
 * Returns the default panel for the given surface.
 * Prefers the surface owner; falls back to the first available panel.
 */
export function getDefaultPanelId(surface: DevSurface): string | null {
  const panels = getAvailablePanels(surface);
  if (panels.length === 0) return null;
  const owner = panels.find((p) => p.isSurfaceOwner);
  return (owner ?? panels[0]).id;
}
