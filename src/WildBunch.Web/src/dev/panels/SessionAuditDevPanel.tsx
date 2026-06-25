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
  gap: 6px;
`;

const AuditEntry = styled.div`
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 10px;
  padding: 8px 12px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.08);
  font-size: 0.82rem;
`;

const Sequence = styled.span`
  color: rgba(242, 239, 232, 0.5);
  font-variant-numeric: tabular-nums;
`;

const EventType = styled.span`
  color: #efc37e;
  font-weight: 600;
`;

const Summary = styled.span`
  color: rgba(242, 239, 232, 0.92);
`;

const MutedText = styled.p`
  color: rgba(242, 239, 232, 0.5);
`;

const ErrorText = styled.p`
  color: #f07e6e;
`;
