import type { ReactNode } from "react";
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";
import { TravelDevPanel } from "./panels/TravelDevPanel";
import { SaloonDevPanel } from "./panels/SaloonDevPanel";
import type { DevSurface } from "./DevSurfaceContext";

export interface DevPanelDefinition {
  id: string;
  label: string;
  render: () => ReactNode;
  /**
   * Surfaces where this panel is contextually available.
   * If undefined, the panel is available on all surfaces.
   */
  surfaces?: DevSurface[];
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
  },
  {
    id: "saloon-dev",
    label: "Saloon dev",
    render: () => <SaloonDevPanel />,
    surfaces: ["saloon"],
  },
];

/**
 * Returns the panels available for the given dev surface.
 * Panels without a `surfaces` filter are always available.
 */
export function getAvailablePanels(surface: DevSurface): DevPanelDefinition[] {
  return devPanels.filter((panel) => !panel.surfaces || panel.surfaces.includes(surface));
}
