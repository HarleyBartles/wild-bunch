import styled from "styled-components";
import type { FormEvent } from "react";
import { Eyebrow, Button, BackButton } from "../ui/sharedStyled";

interface StorySoFarStepProps {
  storyAcknowledged: boolean;
  onStoryAcknowledgedChange: (value: boolean) => void;
  onContinue: () => void;
  onBack: () => void;
}

export function StorySoFarStep({
  storyAcknowledged,
  onStoryAcknowledgedChange,
  onContinue,
  onBack,
}: StorySoFarStepProps) {
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!storyAcknowledged) {
      return;
    }
    onContinue();
  }

  return (
    <StepCard>
      <Eyebrow>Step 2 of 3</Eyebrow>
      <StepHeading>The story so far</StepHeading>
      <StepLead>
        A culprit is on the run. The trail is fresh, but it won't stay that way for long. Read the
        posters, follow the clues, and bring them in before the law closes the file.
      </StepLead>

      <PlaceholderBody>
        Prologue copy will be fetched from the backend in a later task. For now, acknowledge the
        story to continue.
      </PlaceholderBody>

      <StepForm onSubmit={handleSubmit}>
        <Field>
          <CheckboxRow>
            <input
              id="start-flow-story-ack"
              type="checkbox"
              checked={storyAcknowledged}
              onChange={(event) => onStoryAcknowledgedChange(event.target.checked)}
            />
            <Label htmlFor="start-flow-story-ack">I've read the story so far</Label>
          </CheckboxRow>
        </Field>

        <StepActions>
          <BackButton type="button" onClick={onBack}>
            Back
          </BackButton>
          <Button type="submit" $variant="primary" disabled={!storyAcknowledged}>
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

const PlaceholderBody = styled.p`
  margin: 0;
  padding: 14px 16px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: var(--muted);
  font-size: 0.92rem;
`;

const StepForm = styled.form`
  display: grid;
  gap: 16px;
`;

const Field = styled.div`
  display: grid;
  gap: 6px;
`;

const CheckboxRow = styled.div`
  display: flex;
  align-items: center;
  gap: 10px;

  input[type="checkbox"] {
    width: 18px;
    height: 18px;
    accent-color: var(--accent-strong);
  }
`;

const Label = styled.label`
  color: color-mix(in srgb, var(--text) 75%, transparent);
  font-size: 0.94rem;
`;

const StepActions = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
`;
