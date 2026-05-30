import { useEffect, useMemo, useState } from "react";
import type { JourneyEncounterChoiceDto, JourneyEncounterDto } from "../../api/types";
import { ButtonBase } from "./travelShared";
import styled from "styled-components";

interface JourneyDecisionProps {
  encounter: JourneyEncounterDto;
  ammo: number;
  cash: number;
  busy: boolean;
  refreshing: boolean;
  onResolveEncounter: (
    choiceId: string,
    options?: {
      bulletSpend?: number | null;
      bribeAmount?: number | null;
    },
  ) => Promise<void>;
}

export function JourneyDecision({ encounter, ammo, cash, busy, refreshing, onResolveEncounter }: JourneyDecisionProps) {
  const disabled = busy || refreshing;
  const maxBulletSpend = Math.min(6, ammo);
  const hasFightChoice = encounter.choices.some((choice) => choice.id === "fight");
  const hasBribeChoice = encounter.choices.some((choice) => choice.id === "bribe");
  const defaultFightBullets = useMemo(() => (maxBulletSpend > 0 ? 1 : 0), [maxBulletSpend]);
  const defaultBribeAmount = useMemo(() => (cash > 0 ? Math.min(cash, 5) : 0), [cash]);
  const [fightBullets, setFightBullets] = useState<number>(defaultFightBullets);
  const [bribeAmount, setBribeAmount] = useState<number>(defaultBribeAmount);

  useEffect(() => {
    setFightBullets(defaultFightBullets);
  }, [defaultFightBullets]);

  useEffect(() => {
    setBribeAmount(defaultBribeAmount);
  }, [defaultBribeAmount]);

  return (
    <DecisionPanel>
      <DecisionHeading>
        <strong>Trail decision</strong>
        <span>{encounter.kind}</span>
      </DecisionHeading>
      <DecisionBody>{encounter.message}</DecisionBody>

      {(hasFightChoice || hasBribeChoice) && (
        <EncounterControls>
          {hasFightChoice ? (
            <ControlCard>
              <ControlLabel htmlFor="journey-fight-bullets">Fight bullets</ControlLabel>
              <ControlInput
                id="journey-fight-bullets"
                type="number"
                min={maxBulletSpend > 0 ? 1 : 0}
                max={maxBulletSpend > 0 ? maxBulletSpend : 0}
                step={1}
                value={fightBullets}
                onChange={(event) => setFightBullets(Number.parseInt(event.target.value, 10) || 0)}
                disabled={disabled || maxBulletSpend === 0}
              />
              <ControlHint>
                {maxBulletSpend > 0
                  ? `Spend 1 to ${maxBulletSpend} bullet(s).`
                  : "No firearm ammo is on hand, so the fight will be with a knife."}
              </ControlHint>
            </ControlCard>
          ) : null}

          {hasBribeChoice ? (
            <ControlCard>
              <ControlLabel htmlFor="journey-bribe-amount">Bribe amount</ControlLabel>
              <ControlInput
                id="journey-bribe-amount"
                type="number"
                min={0}
                max={cash}
                step="0.01"
                value={bribeAmount}
                onChange={(event) => setBribeAmount(Number.parseFloat(event.target.value) || 0)}
                disabled={disabled || cash <= 0}
              />
              <ControlHint>{cash > 0 ? `Offer up to $${cash.toFixed(2)}.` : "No cash is left to offer."}</ControlHint>
            </ControlCard>
          ) : null}
        </EncounterControls>
      )}

      <ChoiceRow>
        {encounter.choices.map((choice: JourneyEncounterChoiceDto) => (
          <ChoiceButton
            key={choice.id}
            type="button"
            onClick={() =>
              void onResolveEncounter(
                choice.id,
                choice.id === "fight"
                  ? { bulletSpend: maxBulletSpend === 0 ? 0 : Math.max(1, Math.min(fightBullets, maxBulletSpend)) }
                  : choice.id === "bribe"
                    ? { bribeAmount: Math.max(0, Math.min(bribeAmount, cash)) }
                    : undefined,
              )
            }
            disabled={disabled}
          >
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

const EncounterControls = styled.div`
  display: grid;
  gap: 12px;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
`;

const ControlCard = styled.div`
  display: grid;
  gap: 6px;
  padding: 12px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.035);
  border: 1px solid rgba(255, 255, 255, 0.08);
`;

const ControlLabel = styled.label`
  color: rgba(242, 239, 232, 0.8);
  font-size: 0.88rem;
`;

const ControlInput = styled.input`
  width: 100%;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.16);
  background: rgba(12, 10, 8, 0.68);
  color: #f2efe8;
  padding: 10px 12px;

  &:disabled {
    cursor: not-allowed;
    opacity: 0.65;
  }
`;

const ControlHint = styled.p`
  margin: 0;
  color: rgba(242, 239, 232, 0.58);
  font-size: 0.82rem;
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
