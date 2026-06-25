import styled from "styled-components";
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
    return <MutedText>No active session.</MutedText>;
  }

  if (isLoading) {
    return <MutedText>Loading audit...</MutedText>;
  }

  if (error) {
    return <ErrorText>{error instanceof Error ? error.message : "Failed to load audit."}</ErrorText>;
  }

  if (!data || data.entries.length === 0) {
    return <MutedText>No audit entries.</MutedText>;
  }

  return (
    <AuditList>
      {data.entries.map((entry) => (
        <AuditEntry key={entry.sequence}>
          <Sequence>#{entry.sequence}</Sequence>
          <EventType>{entry.eventType}</EventType>
          <Summary>{entry.summary}</Summary>
        </AuditEntry>
      ))}
    </AuditList>
  );
}

const AuditList = styled.div`
  display: grid;
  gap: 5px;
`;

const AuditEntry = styled.div`
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 10px;
  padding: 7px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.025);
  border: 1px solid var(--border);
  font-size: 0.8rem;
`;

const Sequence = styled.span`
  color: var(--muted);
  font-variant-numeric: tabular-nums;
`;

const EventType = styled.span`
  color: var(--accent);
  font-weight: 600;
`;

const Summary = styled.span`
  color: var(--text);
`;

const MutedText = styled.p`
  color: var(--muted);
  margin: 0;
`;

const ErrorText = styled.p`
  color: var(--danger);
  margin: 0;
`;
