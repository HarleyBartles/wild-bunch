import { useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { clearRng, getSessionDevContext, lockRng } from "../devApi";

interface SessionDevPanelProps {
  expanded?: boolean;
}

export function SessionDevPanel({ expanded = false }: SessionDevPanelProps) {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();
  const [saltInput, setSaltInput] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["dev-session-context", gameId],
    queryFn: () => getSessionDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }

  if (isLoading) {
    return <MutedText>Loading session context...</MutedText>;
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["dev-session-context", gameId] });

  // Salt contract: blank input → null (handler generates a fresh fixed salt).
  // Non-empty input → trimmed value sent as the exact reproducibility token.
  const handleLock = async () => {
    setError(null);
    setActionPending(true);
    try {
      await lockRng(gameId, { salt: saltInput.trim() === "" ? null : saltInput.trim() });
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to lock RNG.");
    } finally {
      setActionPending(false);
    }
  };

  const handleClear = async () => {
    setError(null);
    setActionPending(true);
    try {
      await clearRng(gameId);
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to clear RNG.");
    } finally {
      setActionPending(false);
    }
  };

  const sessionIdShort = data?.sessionId ? data.sessionId.slice(0, 8) : "";

  return (
    <Container $expanded={expanded}>
      <LeftColumn>
        <Section>
          <SectionTitle>Session</SectionTitle>
          <Row>
            <Label>Session ID:</Label>
            <Value>{sessionIdShort}</Value>
          </Row>
          <Row>
            <Label>Status:</Label>
            <Value>{data?.status}</Value>
          </Row>
          <Row>
            <Label>Action context:</Label>
            <Value>{data?.currentActionContext}</Value>
          </Row>
          <Row>
            <Label>Active journey:</Label>
            <Value>{data?.hasActiveJourney ? "Yes" : "No"}</Value>
          </Row>
        </Section>

        <Section>
          <SectionTitle>Clock</SectionTitle>
          <Row>
            <Label>Day:</Label>
            <Value>{data?.clock?.day}</Value>
          </Row>
          <Row>
            <Label>Turn:</Label>
            <Value>{data?.clock?.turn}</Value>
          </Row>
          <Row>
            <Label>Time of day:</Label>
            <Value>{data?.clock?.timeOfDay}</Value>
          </Row>
        </Section>

        <Section>
          <SectionTitle>Location</SectionTitle>
          <Row>
            <Label>Town ID:</Label>
            <Value>{data?.currentTownId ?? "—"}</Value>
          </Row>
          <Row>
            <Label>Town name:</Label>
            <Value>{data?.currentTownName ?? "—"}</Value>
          </Row>
        </Section>
      </LeftColumn>

      <RightColumn>
        <Section>
          <SectionTitle>Setup posture</SectionTitle>
          <Row>
            <Label>Difficulty (inspect):</Label>
            <Value>{data?.gameDifficulty}</Value>
          </Row>
          <Row>
            <Label>Entropy (inspect):</Label>
            <Value>{data?.gameEntropy}</Value>
          </Row>
          <Row>
            <Label>Salt mode:</Label>
            <Value>{data?.saltPosture?.mode}</Value>
          </Row>
          <Row>
            <Label>Salt:</Label>
            <Value>{data?.saltPosture?.salt ?? "—"}</Value>
          </Row>
          <Row>
            <Label>Seed code:</Label>
            <Value>{data?.seedCodeRetained ? data.seedCodeText : "No — not retained on live session"}</Value>
          </Row>
        </Section>

        <Section>
          <SectionTitle>RNG controls</SectionTitle>
          <Field>
            <Label>Salt (optional):</Label>
            <Input
              type="text"
              value={saltInput}
              onChange={(e) => setSaltInput(e.target.value)}
              placeholder="(blank = generate)"
            />
          </Field>
          <ButtonRow>
            <Button type="button" onClick={handleLock} disabled={actionPending}>
              Lock RNG
            </Button>
            <Button type="button" onClick={handleClear} disabled={actionPending}>
              Clear RNG
            </Button>
          </ButtonRow>
          <MutedText>
            Locking RNG makes the run reproducible. It does not force encounter outcomes.
          </MutedText>
          {error && <ErrorText>{error}</ErrorText>}
        </Section>
      </RightColumn>
    </Container>
  );
}

const Container = styled.div<{ $expanded: boolean }>`
  display: grid;
  gap: 16px;
  grid-template-columns: ${({ $expanded }) => ($expanded ? "1fr 1fr" : "1fr")};

  @media (max-width: 700px) {
    grid-template-columns: 1fr;
  }
`;

const LeftColumn = styled.div`
  display: grid;
  gap: 16px;
`;

const RightColumn = styled.div`
  display: grid;
  gap: 16px;
`;

const Section = styled.section`
  display: grid;
  gap: 6px;
`;

const SectionTitle = styled.h3`
  margin: 0 0 4px;
  font-size: 0.88rem;
  color: var(--accent);
`;

const Row = styled.div`
  display: flex;
  gap: 8px;
  font-size: 0.82rem;
`;

const Label = styled.span`
  color: var(--muted);
  flex-shrink: 0;
  min-width: 120px;
`;

const Value = styled.span`
  color: var(--text);
`;

const Field = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.82rem;
`;

const Input = styled.input`
  flex: 1;
  padding: 4px 8px;
  border-radius: 6px;
  border: 1px solid var(--border-strong);
  background: var(--bg);
  color: var(--text);
  font-size: 0.82rem;
`;

const ButtonRow = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 6px;
`;

const Button = styled.button`
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.8rem;
  font-weight: 600;
  min-height: 32px;
  transition-property: background-color, border-color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &:hover:not(:disabled) {
    background: var(--bg-hover);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const MutedText = styled.p`
  color: var(--muted);
  font-size: 0.82rem;
  margin: 0;
`;

const ErrorText = styled.p`
  color: var(--danger);
  font-size: 0.8rem;
  margin: 4px 0 0;
`;
