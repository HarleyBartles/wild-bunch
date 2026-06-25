import type { ReactNode } from "react";
import { SessionAuditDevPanel } from "./panels/SessionAuditDevPanel";

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
];
