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
  { value: 1, label: "Easy" },
  { value: 0, label: "Normal" },
  { value: 2, label: "Hard" },
];

const entropyOptions: ReadonlyArray<{ value: AdventureRandomnessPolicy; label: string }> = [
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
          <SegmentedToggle>
            {difficultyOptions.map((option) => (
              <Segment
                key={option.value}
                type="button"
                $selected={travelDifficulty === option.value}
                onClick={() => onTravelDifficultyChange(option.value)}
                aria-pressed={travelDifficulty === option.value}
              >
                {option.label}
              </Segment>
            ))}
          </SegmentedToggle>
        </FieldGroup>

        <FieldGroup>
          <GroupLabel>Entropy</GroupLabel>
          <SegmentedToggle>
            {entropyOptions.map((option) => (
              <Segment
                key={option.value}
                type="button"
                $selected={entropy === option.value}
                onClick={() => onEntropyChange(option.value)}
                aria-pressed={entropy === option.value}
              >
                {option.label}
              </Segment>
            ))}
          </SegmentedToggle>
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

const SegmentedToggle = styled.div`
  display: flex;
  width: 100%;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.03);
  padding: 3px;
  gap: 0;
  overflow: hidden;
`;

const Segment = styled.button<{ $selected: boolean }>`
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 10px;
  border: none;
  border-radius: 999px;
  background: ${({ $selected }) =>
    $selected ? "color-mix(in srgb, var(--accent) 22%, transparent)" : "transparent"};
  color: ${({ $selected }) =>
    $selected ? "var(--text)" : "color-mix(in srgb, var(--text) 65%, transparent)"};
  font-weight: 600;
  font-size: 0.88rem;
  cursor: pointer;
  white-space: nowrap;
  transition: background-color 0.15s ease, color 0.15s ease;

  &:hover {
    color: var(--text);
  }

  &:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--accent) 55%, transparent);
    outline-offset: -2px;
  }

  @media (max-width: 480px) {
    font-size: 0.8rem;
    padding: 8px 6px;
  }
`;

const StepActions = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
`;
