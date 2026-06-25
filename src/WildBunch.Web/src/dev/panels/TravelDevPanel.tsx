import { useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { clearTravelOverride, forceTravelOverride, getTravelDevContext } from "../devApi";

const ENCOUNTER_CATEGORIES = [
  "Foe",
  "Npc",
  "Lucky",
  "Unlucky",
  "Environmental",
  "Resource",
  "HorseTrouble",
  "Quiet",
] as const;

export function TravelDevPanel() {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();

  const [category, setCategory] = useState<string>("Foe");
  const [foeSpeed, setFoeSpeed] = useState<string>("");
  const [foeFight, setFoeFight] = useState<string>("");
  const [foeBribe, setFoeBribe] = useState<string>("");
  const [message, setMessage] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["dev-travel-context", gameId],
    queryFn: () => getTravelDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }

  if (isLoading) {
    return <MutedText>Loading travel context...</MutedText>;
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["dev-travel-context", gameId] });

  const handleForce = async () => {
    setError(null);
    setActionPending(true);
    try {
      await forceTravelOverride(gameId, {
        forcedCategory: category,
        foeSpeed: foeSpeed.trim() === "" ? null : Number(foeSpeed),
        foeFightStrength: foeFight.trim() === "" ? null : Number(foeFight),
        foeMinimumBribe: foeBribe.trim() === "" ? null : Number(foeBribe),
        encounterMessage: message.trim() === "" ? null : message,
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
      await clearTravelOverride(gameId);
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
        <SectionTitle>Journey state</SectionTitle>
        <Row>
          <Label>Active:</Label>
          <Value>{data?.hasActiveJourney ? "Yes" : "No"}</Value>
        </Row>
        {data?.journeyStatus && (
          <Row>
            <Label>Status:</Label>
            <Value>{data.journeyStatus}</Value>
          </Row>
        )}
        {data?.daysTravelled != null && (
          <Row>
            <Label>Days travelled:</Label>
            <Value>{data.daysTravelled}</Value>
          </Row>
        )}
        {data?.remainingDays != null && (
          <Row>
            <Label>Remaining days:</Label>
            <Value>{data.remainingDays}</Value>
          </Row>
        )}
        {data?.pendingEncounterKind && (
          <Row>
            <Label>Pending encounter:</Label>
            <Value>{data.pendingEncounterKind}</Value>
          </Row>
        )}
        {data?.pendingEncounterMessage && (
          <Row>
            <Label>Message:</Label>
            <Value>{data.pendingEncounterMessage}</Value>
          </Row>
        )}
        {data?.pendingFoeProfile && (
          <Row>
            <Label>Foe:</Label>
            <Value>
              S{data.pendingFoeProfile.speed} F{data.pendingFoeProfile.fightStrength} bribe $
              {data.pendingFoeProfile.minimumBribe}
            </Value>
          </Row>
        )}
      </Section>

      <Section>
        <SectionTitle>Pending dev override</SectionTitle>
        {data?.pendingDevOverride ? (
          <Row>
            <Label>Override:</Label>
            <Value>
              {data.pendingDevOverride.forcedCategory}
              {data.pendingDevOverride.foeProfile
                ? ` (S${data.pendingDevOverride.foeProfile.speed} F${data.pendingDevOverride.foeProfile.fightStrength} $${data.pendingDevOverride.foeProfile.minimumBribe})`
                : ""}
            </Value>
          </Row>
        ) : (
          <MutedText>None pending.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Force next travel override</SectionTitle>
        <Field>
          <Label>Category:</Label>
          <Select value={category} onChange={(e) => setCategory(e.target.value)}>
            {ENCOUNTER_CATEGORIES.map((cat) => (
              <option key={cat} value={cat}>
                {cat}
              </option>
            ))}
          </Select>
        </Field>
        {category === "Foe" && (
          <>
            <Field>
              <Label>Foe speed:</Label>
              <Input
                type="number"
                value={foeSpeed}
                onChange={(e) => setFoeSpeed(e.target.value)}
                placeholder="3"
              />
            </Field>
            <Field>
              <Label>Foe fight:</Label>
              <Input
                type="number"
                value={foeFight}
                onChange={(e) => setFoeFight(e.target.value)}
                placeholder="3"
              />
            </Field>
            <Field>
              <Label>Foe min bribe:</Label>
              <Input
                type="number"
                step="0.01"
                value={foeBribe}
                onChange={(e) => setFoeBribe(e.target.value)}
                placeholder="5.00"
              />
            </Field>
          </>
        )}
        <Field>
          <Label>Message:</Label>
          <Input
            type="text"
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            placeholder="(default)"
          />
        </Field>
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
    background: rgba(255, 255, 255, 0.06);
    border-color: var(--accent);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const MutedText = styled.p`
  color: var(--muted);
  margin: 0;
`;

const ErrorText = styled.p`
  color: var(--danger);
  margin: 4px 0 0;
  font-size: 0.82rem;
`;
