import { useState } from "react";
import styled from "styled-components";
import type { FormEvent } from "react";
import { Eyebrow, Button, BackButton, FlowError } from "../ui/sharedStyled";

interface NameEntryStepProps {
  playerName: string;
  onPlayerNameChange: (value: string) => void;
  onContinue: () => void;
  onBack: () => void;
}

export function NameEntryStep({ playerName, onPlayerNameChange, onContinue, onBack }: NameEntryStepProps) {
  const trimmed = playerName.trim();
  const [touched, setTouched] = useState(false);
  const showValidation = touched && !trimmed;

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
      <Eyebrow>Step 1 of 3</Eyebrow>
      <StepHeading>Howdy, pard&apos;ner. What name d&apos;you go by?</StepHeading>
      <StepLead>
        A name&apos;s a useful thing to have when folks start shouting after you.
      </StepLead>

      <StepForm onSubmit={handleSubmit}>
        <Field>
          <Label htmlFor="start-flow-player-name">Player name</Label>
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
            aria-invalid={showValidation}
            aria-describedby={showValidation ? "start-flow-player-name-validation" : undefined}
          />
          {showValidation && (
            <FlowError id="start-flow-player-name-validation" role="alert">
              Tell me what name you go by before we ride on.
            </FlowError>
          )}
        </Field>

        <StepActions>
          <BackButton type="button" onClick={onBack} disabled>
            Back
          </BackButton>
          <Button type="submit" $variant="primary" disabled={!trimmed}>
            Continue
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
  gap: 16px;
`;

const Field = styled.div`
  display: grid;
  gap: 6px;
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

const StepActions = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
`;
