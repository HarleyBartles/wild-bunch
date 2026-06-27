import styled from "styled-components";
import { Eyebrow, BackButton, Button } from "../ui/sharedStyled";

interface StartingTownStepProps {
  selectedTownId: string | null;
  onSelectTown: (townId: string) => void;
  onBack: () => void;
}

const placeholderTowns = [
  { id: "t-town", name: "Tumbleweed" },
  { id: "dust-fork", name: "Dust Fork" },
];

export function StartingTownStep({ selectedTownId, onSelectTown, onBack }: StartingTownStepProps) {
  return (
    <StepCard>
      <Eyebrow>Step 3 of 3</Eyebrow>
      <StepHeading>Pick your starting town</StepHeading>
      <StepLead>
        Every hunt begins somewhere. Choose the town where you'll pick up the trail.
      </StepLead>

      <PlaceholderBody>
        Starting towns will be fetched from the backend in a later task. For now, pick from the
        placeholder list.
      </PlaceholderBody>

      <TownList>
        {placeholderTowns.map((town) => (
          <TownCard key={town.id}>
            <TownName>{town.name}</TownName>
            <Button
              type="button"
              $variant={selectedTownId === town.id ? "primary" : "ghost"}
              onClick={() => onSelectTown(town.id)}
            >
              Start here
            </Button>
          </TownCard>
        ))}
      </TownList>

      <StepActions>
        <BackButton type="button" onClick={onBack}>
          Back
        </BackButton>
      </StepActions>
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

const TownList = styled.ul`
  display: grid;
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
`;

const TownCard = styled.li`
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
`;

const TownName = styled.span`
  font-weight: 600;
`;

const StepActions = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: center;
`;
