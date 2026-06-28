import styled from "styled-components";
import { Eyebrow } from "../ui/sharedStyled";

interface CreatingStepProps {
  busy: boolean;
}

export function CreatingStep({ busy }: CreatingStepProps) {
  return (
    <StepCard>
      <Eyebrow>Starting</Eyebrow>
      <StepHeading>Starting your hunt</StepHeading>
      <StepLead>
        {busy
          ? "The backend is building your world. Hang tight."
          : "Your game is being created."}
      </StepLead>
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
