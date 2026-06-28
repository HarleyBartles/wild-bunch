import { useState } from "react";
import styled from "styled-components";
import type { FormEvent } from "react";
import type { AdventureRandomnessPolicy, TravelDifficulty } from "../../api/types";
import { Button, FlowError } from "../ui/sharedStyled";

interface SetupHuntStepProps {
  playerName: string;
  travelDifficulty: TravelDifficulty;
  entropy: AdventureRandomnessPolicy;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  onPlayerNameChange: (value: string) => void;
  onTravelDifficultyChange: (difficulty: TravelDifficulty) => void;
  onEntropyChange: (entropy: AdventureRandomnessPolicy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}

const difficultyOptions: ReadonlyArray<{ value: TravelDifficulty; label: string }> = [
  { value: 0, label: "Normal" },
  { value: 1, label: "Easy" },
  { value: 2, label: "Hard" },
];

const entropyOptions: ReadonlyArray<{ value: AdventureRandomnessPolicy; label: string }> = [
  { value: 0, label: "Boring" },
  { value: 1, label: "Classic" },
  { value: 2, label: "Adventurous" },
  { value: 3, label: "Wild" },
];

export function SetupHuntStep({
  playerName,
  travelDifficulty,
  entropy,
  seedDraft,
  seedDirty,
  decodeError,
  onPlayerNameChange,
  onTravelDifficultyChange,
  onEntropyChange,
  onSeedDraftChange,
  onApplySeed,
  onRandomizeSeed,
  onContinue,
}: SetupHuntStepProps) {
  const trimmed = playerName.trim();
  const [touched, setTouched] = useState(false);
  const showNameError = touched && !trimmed;

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setTouched(true);
    if (!trimmed) {
      return;
    }
    onContinue();
  }

  return (
    <StepCard>
      <StepHeading>Set up your hunt</StepHeading>
      <StepLead>
        Name yourself, pick your difficulty and entropy, and set the seed for the world
        you will chase the culprit through.
      </StepLead>

      <StepForm onSubmit={handleSubmit}>
        <Field>
          <Label htmlFor="start-flow-player-name">Your name</Label>
          <Input
            id="start-flow-player-name"
            type="text"
            value={playerName}
            onChange={(event) => {
              onPlayerNameChange(event.target.value);
              setTouched(true);
            }}
            onBlur={() => setTouched(true)}
            placeholder="Enter a rider name"
            autoComplete="off"
            aria-invalid={showNameError}
            aria-describedby={showNameError ? "start-flow-player-name-validation" : undefined}
          />
          {showNameError && (
            <FlowError id="start-flow-player-name-validation" role="alert">
              Tell me what name you go by before we ride on.
            </FlowError>
          )}
        </Field>

        <FieldGroup>
          <GroupLabel>Difficulty</GroupLabel>
          <OptionRow>
            {difficultyOptions.map((option) => (
              <OptionButton
                key={option.value}
                type="button"
                $selected={travelDifficulty === option.value}
                onClick={() => onTravelDifficultyChange(option.value)}
                aria-pressed={travelDifficulty === option.value}
              >
                <OptionLabel>{option.label}</OptionLabel>
              </OptionButton>
            ))}
          </OptionRow>
        </FieldGroup>

        <FieldGroup>
          <GroupLabel>Entropy</GroupLabel>
          <OptionRow>
            {entropyOptions.map((option) => (
              <OptionButton
                key={option.value}
                type="button"
                $selected={entropy === option.value}
                onClick={() => onEntropyChange(option.value)}
                aria-pressed={entropy === option.value}
              >
                <OptionLabel>{option.label}</OptionLabel>
              </OptionButton>
            ))}
          </OptionRow>
        </FieldGroup>

        <Field>
          <Label htmlFor="start-flow-seed-code">World seed</Label>
          <SeedRow>
            <Input
              id="start-flow-seed-code"
              type="text"
              value={seedDraft}
              onChange={(event) => onSeedDraftChange(event.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
              autoComplete="off"
              aria-invalid={Boolean(decodeError)}
              aria-describedby={decodeError ? "start-flow-seed-validation" : undefined}
            />
            <Button
              type="button"
              $variant="ghost"
              onClick={() => void onApplySeed()}
              disabled={!seedDirty}
            >
              Apply
            </Button>
            <Button type="button" $variant="ghost" onClick={onRandomizeSeed}>
              Randomize
            </Button>
          </SeedRow>
          {decodeError && (
            <FlowError id="start-flow-seed-validation" role="alert">
              {decodeError}
            </FlowError>
          )}
        </Field>

        <StepActions>
          <Button type="submit" $variant="primary" disabled={!trimmed}>
            Ride on
          </Button>
        </StepActions>
      </StepForm>
    </StepCard>
  );
}

const StepCard = styled.article`
  display: grid;
  gap: 16px;
  padding: 22px;
  border-radius: 24px;
  border: 1px solid color-mix(in srgb, var(--accent-strong) 20%, transparent);
  background:
    radial-gradient(circle at top left, color-mix(in srgb, var(--accent-strong) 14%, transparent), transparent 28%),
    linear-gradient(180deg, rgba(29, 23, 16, 0.98), rgba(16, 12, 8, 0.98));
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.34);
`;

const StepHeading = styled.h2`
  margin: 0;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: clamp(1.6rem, 3vw, 2.2rem);
  line-height: 1.02;
`;

const StepLead = styled.p`
  margin: 0;
  color: color-mix(in srgb, var(--text) 75%, transparent);
  max-width: 60ch;
`;

const StepForm = styled.form`
  display: grid;
  gap: 18px;
`;

const Field = styled.div`
  display: grid;
  gap: 6px;
`;

const FieldGroup = styled.div`
  display: grid;
  gap: 8px;
`;

const GroupLabel = styled.span`
  color: color-mix(in srgb, var(--text) 62%, transparent);
  font-size: 0.92rem;
`;

const Label = styled.label`
  color: color-mix(in srgb, var(--text) 62%, transparent);
  font-size: 0.92rem;
`;

const Input = styled.input`
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

const SeedRow = styled.div`
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  align-items: stretch;

  input {
    flex: 1 1 280px;
  }
`;

const OptionRow = styled.div`
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
`;

const OptionButton = styled.button<{ $selected: boolean }>`
  display: grid;
  gap: 2px;
  flex: 1 1 140px;
  padding: 10px 12px;
  border-radius: 14px;
  border: 1px solid
    ${({ $selected }) =>
      $selected ? "color-mix(in srgb, var(--accent) 55%, transparent)" : "rgba(255, 255, 255, 0.12)"};
  background: ${({ $selected }) =>
    $selected ? "color-mix(in srgb, var(--accent) 14%, transparent)" : "rgba(255, 255, 255, 0.03)"};
  color: var(--text);
  cursor: pointer;
  text-align: left;
  transition: border-color 0.15s, background 0.15s;

  &:hover {
    border-color: color-mix(in srgb, var(--accent) 40%, transparent);
  }
`;

const OptionLabel = styled.span`
  font-weight: 600;
  font-size: 0.94rem;
`;

const StepActions = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
`;
