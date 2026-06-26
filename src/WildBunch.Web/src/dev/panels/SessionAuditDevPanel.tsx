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
    return <StatusText aria-live="polite">Loading session audit...</StatusText>;
  }

  if (error) {
    return (
      <ErrorText role="alert">
        {error instanceof Error ? error.message : "Failed to load audit."}
      </ErrorText>
    );
  }

  if (!data || data.entries.length === 0) {
    return <StatusText>No audit entries yet.</StatusText>;
  }

  return (
    <AuditList aria-label="Session audit entries">
      {data.entries.map((entry) => (
        <AuditEntry key={entry.sequence}>
          <EntryHeader>
            <Sequence>#{entry.sequence}</Sequence>
            <EventType>{entry.eventType}</EventType>
          </EntryHeader>
          <Summary>{entry.summary}</Summary>
        </AuditEntry>
      ))}
    </AuditList>
  );
}

const AuditList = styled.ol`
  display: grid;
  gap: 5px;
  margin: 0;
  padding: 0;
  list-style: none;
`;

const AuditEntry = styled.li`
  display: grid;
  gap: 4px;
  padding: 7px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.025);
  border: 1px solid var(--border);
  font-size: 0.8rem;
`;

const EntryHeader = styled.div`
  display: flex;
  gap: 10px;
  align-items: baseline;
  min-width: 0;
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
  line-height: 1.35;
`;

const MutedText = styled.p`
  color: var(--muted);
  margin: 0;
`;

const StatusText = styled(MutedText)`
  font-style: italic;
`;

const ErrorText = styled.p`
  color: var(--danger);
  margin: 0;
`;
