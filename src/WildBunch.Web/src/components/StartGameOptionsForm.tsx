import styled from "styled-components";
import type { TravelDifficulty } from "../api/types";
import { SeedCodeEditor } from "./SeedCodeEditor";

interface StartGameOptionsFormProps {
  playerName: string;
  travelDifficulty: TravelDifficulty;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  onPlayerNameChange: (value: string) => void;
  onSeedDraftChange: (value: string) => void;
  onTravelDifficultyChange: (difficulty: TravelDifficulty) => void;
}

export function StartGameOptionsForm({
  playerName,
  travelDifficulty,
  seedDraft,
  seedDirty,
  decodeError,
  onPlayerNameChange,
  onSeedDraftChange,
  onTravelDifficultyChange,
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

      <Field>
        <Label htmlFor="difficulty">Travel difficulty</Label>
        <Select id="difficulty" value={travelDifficulty} onChange={(event) => onTravelDifficultyChange(Number(event.target.value) as TravelDifficulty)}>
          <option value={0}>Normal</option>
          <option value={1}>Easy</option>
          <option value={2}>Hard</option>
        </Select>
      </Field>

      <SeedCodeEditor seedDraft={seedDraft} seedDirty={seedDirty} decodeError={decodeError} onSeedDraftChange={onSeedDraftChange} />
    </DraftGrid>
  );
}

const DraftGrid = styled.div`
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(2, minmax(0, 1fr));

  @media (max-width: 840px) {
    grid-template-columns: 1fr;
  }
`;

const Field = styled.div`
  display: grid;
  gap: 6px;
`;

const Label = styled.label`
  color: color-mix(in srgb, var(--text) 62%, transparent);
  font-size: 0.92rem;
`;

const baseControl = `
  width: 100%;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
  color: var(--text);
  padding: 12px 14px;
  outline: none;

  &:focus {
    border-color: color-mix(in srgb, var(--accent) 55%, transparent);
    box-shadow: 0 0 0 3px color-mix(in srgb, var(--accent) 18%, transparent);
  }
`;

const Input = styled.input`
  ${baseControl}
`;

const Select = styled.select`
  ${baseControl}
`;
