import styled from "styled-components";
import { formatJourneyRandomnessMode, formatLoadoutProfile, formatTravelDifficulty } from "../ui/formatters";
import type { GameSetupSeedState } from "../ui/gameSetupSeedCodec";

interface SetupSeedSummaryProps {
  seedState: GameSetupSeedState;
}

export function SetupSeedSummary({ seedState }: SetupSeedSummaryProps) {
  return (
    <SummaryCard>
      <SummaryItem>
        <dt>Difficulty</dt>
        <dd>{formatTravelDifficulty(seedState.difficulty)}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Horse</dt>
        <dd>{seedState.startWithHorse ? "Enabled" : "Disabled"}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Loadout</dt>
        <dd>{formatLoadoutProfile(seedState.loadoutProfile)}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Journey randomness</dt>
        <dd>{formatJourneyRandomnessMode(seedState.journeyRandomnessMode)}</dd>
      </SummaryItem>
      <SummaryItem>
        <dt>Entropy</dt>
        <dd>{seedState.entropy.toString(16).toUpperCase().padStart(12, "0")}</dd>
      </SummaryItem>
    </SummaryCard>
  );
}

const SummaryCard = styled.dl`
  display: grid;
  gap: 10px;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  margin: 0;
  padding: 16px;
  border-radius: 20px;
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(255, 255, 255, 0.03);
`;

const SummaryItem = styled.div`
  dt {
    color: rgba(242, 239, 232, 0.58);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.74rem;
  }

  dd {
    margin: 4px 0 0;
    font-weight: 600;
  }
`;
