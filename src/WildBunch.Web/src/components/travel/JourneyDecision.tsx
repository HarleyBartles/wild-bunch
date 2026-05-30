import type { JourneyEncounterChoiceDto, JourneyEncounterDto } from "../../api/types";
import { ButtonBase } from "./travelShared";
import styled from "styled-components";

interface JourneyDecisionProps {
  encounter: JourneyEncounterDto;
  busy: boolean;
  refreshing: boolean;
  onResolveEncounter: (choiceId: string) => Promise<void>;
}

export function JourneyDecision({ encounter, busy, refreshing, onResolveEncounter }: JourneyDecisionProps) {
  const disabled = busy || refreshing;

  return (
    <DecisionPanel>
      <DecisionHeading>
        <strong>Trail decision</strong>
        <span>{encounter.kind}</span>
      </DecisionHeading>
      <DecisionBody>{encounter.message}</DecisionBody>
      <ChoiceRow>
        {encounter.choices.map((choice: JourneyEncounterChoiceDto) => (
          <ChoiceButton key={choice.id} type="button" onClick={() => void onResolveEncounter(choice.id)} disabled={disabled}>
            {choice.label}
          </ChoiceButton>
        ))}
      </ChoiceRow>
      <DecisionHint>Resolve the encounter before advancing the next day.</DecisionHint>
    </DecisionPanel>
  );
}

const DecisionPanel = styled.div`
  display: grid;
  gap: 12px;
  padding: 16px;
  border-radius: 18px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
`;

const DecisionHeading = styled.div`
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: baseline;

  span {
    color: rgba(242, 239, 232, 0.6);
    font-size: 0.82rem;
  }
`;

const DecisionBody = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.86);
`;

const ChoiceRow = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
`;

const ChoiceButton = styled(ButtonBase)`
  background: transparent;
  color: #f2efe8;
  border-color: rgba(255, 255, 255, 0.16);

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;

const DecisionHint = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.58);
  font-size: 0.88rem;
`;
