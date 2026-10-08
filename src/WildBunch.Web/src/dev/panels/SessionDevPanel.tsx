import { useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { SegmentedToggle } from "../../components/start-flow/SegmentedToggle";
import { forceDevDifficulty, getSessionDevContext, setDevEntropy } from "../devApi";

interface SessionDevPanelProps {
  expanded?: boolean;
}

const difficultyOptions: ReadonlyArray<{ value: string; label: string }> = [
  { value: "Easy", label: "Easy" },
  { value: "Standard", label: "Standard" },
  { value: "Challenging", label: "Challenging" },
  { value: "Brutal", label: "Brutal" },
];

const entropyOptions: ReadonlyArray<{ value: string; label: string }> = [
  { value: "Boring", label: "Boring" },
  { value: "Classic", label: "Classic" },
  { value: "Adventurous", label: "Adventurous" },
  { value: "Wild", label: "Wild" },
];

export function SessionDevPanel({ expanded = false }: SessionDevPanelProps) {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

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

  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: ["dev-session-context", gameId] });

  const handleForceDifficulty = async (value: string) => {
    setError(null);
    try {
      await forceDevDifficulty(gameId, { difficulty: value });
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to force difficulty.");
    }
  };

  const handleSetEntropy = async (value: string) => {
    setError(null);
    try {
      await setDevEntropy(gameId, { entropy: value });
      await refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to set entropy.");
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
          <Field>
            <Label>Difficulty:</Label>
            <SegmentedToggle
              options={difficultyOptions}
              value={data?.gameDifficulty ?? "Standard"}
              onSelect={handleForceDifficulty}
            />
          </Field>
          <MutedText>
            Forcing difficulty changes travel rules going forward. It does not change starting
            health or cash.
          </MutedText>
          <TravelRulesGrid>
            <Row>
              <Label>Canteen capacity:</Label>
              <Value>{data?.travelRules?.canteenCapacity ?? "—"}</Value>
            </Row>
            <Row>
              <Label>Mounted ride/day:</Label>
              <Value>{data?.travelRules?.mountedRideDayProgress ?? "—"}</Value>
            </Row>
            <Row>
              <Label>Foot ride/day:</Label>
              <Value>{data?.travelRules?.footRideDayProgress ?? "—"}</Value>
            </Row>
            <Row>
              <Label>Encounter fight (ammo) health loss:</Label>
              <Value>{data?.travelRules?.encounterFightAmmoHealthLoss ?? "—"}</Value>
            </Row>
            <Row>
              <Label>Encounter fight (unarmed) health loss:</Label>
              <Value>{data?.travelRules?.encounterFightUnarmedHealthLoss ?? "—"}</Value>
            </Row>
            <Row>
              <Label>Encounter run (foot) health loss:</Label>
              <Value>{data?.travelRules?.encounterRunFootHealthLoss ?? "—"}</Value>
            </Row>
          </TravelRulesGrid>
          <Field>
            <Label>Entropy:</Label>
            <SegmentedToggle
              options={entropyOptions}
              value={data?.gameEntropy ?? "Classic"}
              onSelect={handleSetEntropy}
            />
          </Field>
          <MutedText>
            Setting entropy changes travel variance going forward. It does not change past travel
            outcomes or hidden truth.
          </MutedText>
          <Row>
            <Label>Salt mode:</Label>
            <Value>{data?.saltPosture?.mode}</Value>
          </Row>
          <Row>
            <Label>Salt value:</Label>
            <Value>{data?.saltPosture?.salt ?? "—"}</Value>
          </Row>
          <Row>
            <Label>Seed code:</Label>
            <Value>{data?.seedCodeRetained ? data.seedCodeText : "No seed provided"}</Value>
          </Row>
        </Section>

        {error && <ErrorText>{error}</ErrorText>}
      </RightColumn>
    </Container>
  );
}

const Container = styled.div<{ $expanded: boolean }>`
  display: grid;
  gap: 16px;
  grid-template-columns: 1fr 1fr;

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

const TravelRulesGrid = styled.div`
  display: grid;
  gap: 0.25rem;
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  border-top: 1px solid color-mix(in srgb, var(--border) 50%, transparent);
`;
