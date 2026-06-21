import type { ReactNode } from "react";
import type { ShellRoute } from "./routes";

interface RouteHeaderProps {
  route: ShellRoute;
  actions?: ReactNode;
}

export function RouteHeader({ route, actions }: RouteHeaderProps) {
  return (
    <header className="route-header">
      <div>
        <p className="eyebrow">{route.eyebrow}</p>
        <h1>{route.title}</h1>
        <p className="route-header__copy">{route.description}</p>
      </div>
      {actions ? <div className="route-header__actions">{actions}</div> : null}
    </header>
  );
}
