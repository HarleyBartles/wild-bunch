import styled from "styled-components";
import type { TravelDifficulty } from "../api/types";
import type { GameSetupLoadoutProfile, GameSetupSeedState } from "../ui/gameSetupSeedCodec";
import { SeedCodeEditor } from "./SeedCodeEditor";

interface StartGameOptionsFormProps {
  playerName: string;
  seedState: GameSetupSeedState;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  onPlayerNameChange: (value: string) => void;
  onSeedDraftChange: (value: string) => void;
  onDifficultyChange: (difficulty: TravelDifficulty) => void;
  onStartWithHorseChange: (value: boolean) => void;
  onLoadoutProfileChange: (profile: GameSetupLoadoutProfile) => void;
}

export function StartGameOptionsForm({
  playerName,
  seedState,
  seedDraft,
  seedDirty,
  decodeError,
  onPlayerNameChange,
  onSeedDraftChange,
  onDifficultyChange,
  onStartWithHorseChange,
  onLoadoutProfileChange,
}: StartGameOptionsFormProps) {
  return (
    <DraftGrid>
      <Field>
        <Label htmlFor="player-name">Player name</Label>
        <Input
          id="player-name"
          type="text"
          value={playerName}
          onChange={(event) => onPlayerNameChange(event.target.value)}
          placeholder="Enter a rider name"
          autoComplete="off"
        />
      </Field>

      <SeedCodeEditor seedDraft={seedDraft} seedDirty={seedDirty} decodeError={decodeError} onSeedDraftChange={onSeedDraftChange} />

      <Field>
        <Label htmlFor="difficulty">Difficulty</Label>
        <Select id="difficulty" value={seedState.difficulty} onChange={(event) => onDifficultyChange(Number(event.target.value) as TravelDifficulty)}>
          <option value={0}>Normal</option>
          <option value={1}>Easy</option>
          <option value={2}>Hard</option>
        </Select>
      </Field>

      <OptionsRow>
        <Field>
          <Label htmlFor="start-with-horse">Start with horse</Label>
          <ToggleRow>
            <ToggleInput
              id="start-with-horse"
              type="checkbox"
              checked={seedState.startWithHorse}
              onChange={(event) => onStartWithHorseChange(event.target.checked)}
            />
            <ToggleLabel htmlFor="start-with-horse">{seedState.startWithHorse ? "Enabled" : "Disabled"}</ToggleLabel>
          </ToggleRow>
        </Field>

        <Field>
          <Label htmlFor="loadout-profile">Loadout profile</Label>
          <Select
            id="loadout-profile"
            value={seedState.loadoutProfile}
            onChange={(event) => onLoadoutProfileChange(Number(event.target.value) as GameSetupLoadoutProfile)}
          >
            <option value={0}>Standard</option>
            <option value={1}>Light</option>
            <option value={2}>Stocked</option>
          </Select>
        </Field>
      </OptionsRow>
    </DraftGrid>
  );
}

const DraftGrid = styled.div`
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
`;

const Field = styled.div`
  display: grid;
  gap: 6px;
`;

const Label = styled.label`
  color: rgba(242, 239, 232, 0.62);
  font-size: 0.92rem;
`;

const baseControl = `
  width: 100%;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
  color: #f2efe8;
  padding: 12px 14px;
  outline: none;

  &:focus {
    border-color: rgba(223, 159, 79, 0.55);
    box-shadow: 0 0 0 3px rgba(223, 159, 79, 0.18);
  }
`;

const Input = styled.input`
  ${baseControl}
`;

const Select = styled.select`
  ${baseControl}
`;

const OptionsRow = styled.div`
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
`;

const ToggleRow = styled.div`
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 48px;
  padding: 0 2px;
`;

const ToggleInput = styled.input`
  width: 18px;
  height: 18px;
  accent-color: #df9f4f;
`;

const ToggleLabel = styled.label`
  color: rgba(242, 239, 232, 0.82);
`;
