import { useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { clearSaloonOverride, forceSaloonOverride, getSaloonDevContext } from "../devApi";

const POI_KINDS = ["Suspect", "Citizen", "FalseLead"] as const;

export function SaloonDevPanel() {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();

  const [forcedKind, setForcedKind] = useState<string>("Citizen");
  const [forcedSuspectId, setForcedSuspectId] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["dev-saloon-context", gameId],
    queryFn: () => getSaloonDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }

  if (isLoading) {
    return <MutedText>Loading saloon context...</MutedText>;
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["dev-saloon-context", gameId] });

  const handleForce = async () => {
    setError(null);
    setActionPending(true);
    try {
      await forceSaloonOverride(gameId, {
        forcedKind,
        forcedSuspectId: forcedKind === "Suspect" && forcedSuspectId.trim() !== ""
          ? forcedSuspectId.trim()
          : null,
      });
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to force override.");
    } finally {
      setActionPending(false);
    }
  };

  const handleClear = async () => {
    setError(null);
    setActionPending(true);
    try {
      await clearSaloonOverride(gameId);
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to clear override.");
    } finally {
      setActionPending(false);
    }
  };

  return (
    <Container>
      <Section>
        <SectionTitle>Saloon context</SectionTitle>
        <Row>
          <Label>Town:</Label>
          <Value>{data?.currentTownName ?? "-"}</Value>
        </Row>
        <Row>
          <Label>Context:</Label>
          <Value>{data?.currentActionContext ?? "None"}</Value>
        </Row>
        <Row>
          <Label>Source spent:</Label>
          <Value>{data?.sourceSpent ? "Yes" : "No"}</Value>
        </Row>
      </Section>

      <Section>
        <SectionTitle>Hidden truth (dev only)</SectionTitle>
        {data?.hiddenTruth ? (
          <Row>
            <Label>True culprit:</Label>
            <Value>
              {data.hiddenTruth.trueCulpritName} ({data.hiddenTruth.trueCulpritId})
            </Value>
          </Row>
        ) : (
          <MutedText>Not available.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Suspects</SectionTitle>
        {data?.suspects && data.suspects.length > 0 ? (
          data.suspects.map((s) => (
            <SuspectRow key={s.suspectId}>
              <SuspectName>
                {s.name} ({s.suspectId})
                {s.isTrueCulprit && <CulpritBadge>culprit</CulpritBadge>}
              </SuspectName>
              <SuspectDetail>
                {s.isEligibleSaloonPoi ? (
                  <EligibleTag>eligible</EligibleTag>
                ) : (
                  <IneligibleTag>ineligible</IneligibleTag>
                )}
                {s.hasKnownWarrant && <span> | warrant</span>}
                {s.presenceState && <span> | {s.presenceState}</span>}
              </SuspectDetail>
              {s.ineligibilityReason && (
                <SuspectReason>{s.ineligibilityReason}</SuspectReason>
              )}
            </SuspectRow>
          ))
        ) : (
          <MutedText>No suspects.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Pending dev override</SectionTitle>
        {data?.pendingDevOverride ? (
          <Row>
            <Label>Override:</Label>
            <Value>
              {data.pendingDevOverride.forcedKind}
              {data.pendingDevOverride.forcedSuspectId
                ? ` (${data.pendingDevOverride.forcedSuspectId})`
                : ""}
            </Value>
          </Row>
        ) : (
          <MutedText>None pending.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Force next saloon override</SectionTitle>
        <Field>
          <Label>Kind:</Label>
          <Select value={forcedKind} onChange={(e) => setForcedKind(e.target.value)}>
            {POI_KINDS.map((k) => (
              <option key={k} value={k}>
                {k}
              </option>
            ))}
          </Select>
        </Field>
        {forcedKind === "Suspect" && (
          <Field>
            <Label>Suspect ID:</Label>
            <Input
              type="text"
              value={forcedSuspectId}
              onChange={(e) => setForcedSuspectId(e.target.value)}
              placeholder="(blank = first eligible)"
            />
          </Field>
        )}
        <ButtonRow>
          <Button type="button" onClick={handleForce} disabled={actionPending}>
            Force override
          </Button>
          <Button type="button" onClick={handleClear} disabled={actionPending}>
            Clear override
          </Button>
        </ButtonRow>
        {error && <ErrorText>{error}</ErrorText>}
      </Section>
    </Container>
  );
}

const Container = styled.div`
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

const Select = styled.select`
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

const SuspectRow = styled.div`
  display: grid;
  gap: 2px;
  font-size: 0.82rem;
  padding: 4px 0;
  border-bottom: 1px solid var(--border);
`;

const SuspectName = styled.span`
  color: var(--text);
`;

const SuspectDetail = styled.span`
  color: var(--muted);
  font-size: 0.78rem;
`;

const SuspectReason = styled.span`
  color: var(--danger);
  font-size: 0.76rem;
`;

const CulpritBadge = styled.span`
  display: inline-block;
  margin-left: 6px;
  padding: 1px 6px;
  border-radius: 4px;
  background: var(--accent);
  color: var(--bg);
  font-size: 0.7rem;
  font-weight: 700;
`;

const EligibleTag = styled.span`
  color: var(--success, #4caf50);
  font-weight: 600;
`;

const IneligibleTag = styled.span`
  color: var(--danger);
  font-weight: 600;
`;
