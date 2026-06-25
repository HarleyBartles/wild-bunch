import { useQuery } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { getSessionAudit } from "../devApi";

export function SessionAuditDevPanel() {
  const { gameId } = useGameSession();

  const { data, isLoading, error } = useQuery({
    queryKey: ["dev-session-audit", gameId],
    queryFn: () => getSessionAudit(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <p className="dev-panel-muted">No active session.</p>;
  }

  if (isLoading) {
    return <p className="dev-panel-muted">Loading audit...</p>;
  }

  if (error) {
    return <p className="dev-panel-error">{error instanceof Error ? error.message : "Failed to load audit."}</p>;
  }

  if (!data || data.entries.length === 0) {
    return <p className="dev-panel-muted">No audit entries.</p>;
  }

  return (
    <div className="dev-audit-list">
      {data.entries.map((entry) => (
        <div key={entry.sequence} className="dev-audit-entry">
          <span className="dev-audit-sequence">#{entry.sequence}</span>
          <span className="dev-audit-type">{entry.eventType}</span>
          <span className="dev-audit-summary">{entry.summary}</span>
        </div>
      ))}
    </div>
  );
}
