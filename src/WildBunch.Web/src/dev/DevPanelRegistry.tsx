import type { ReactNode } from "react";
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";
import { TravelDevPanel } from "./panels/TravelDevPanel";
import { SaloonDevPanel } from "./panels/SaloonDevPanel";

export interface DevPanelDefinition {
  id: string;
  label: string;
  render: () => ReactNode;
}

export const devPanels: DevPanelDefinition[] = [
  {
    id: "session-audit",
    label: "Session audit",
    render: () => <SessionAuditDevPanel />,
  },
  {
    id: "travel-dev",
    label: "Travel dev",
    render: () => <TravelDevPanel />,
  },
  {
    id: "saloon-dev",
    label: "Saloon dev",
    render: () => <SaloonDevPanel />,
  },
];
