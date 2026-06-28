import styled from "styled-components";
import type { FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button, BackButton } from "../ui/sharedStyled";
import { getPrologue } from "../../api/wildBunchApi";

interface StorySoFarStepProps {
  onContinue: () => void;
  onBack: () => void;
  seedCode?: string | null;
}

export function StorySoFarStep({ onContinue, onBack, seedCode }: StorySoFarStepProps) {
  const prologueQuery = useQuery({
    queryKey: ["prologue", seedCode ?? null],
    queryFn: () => getPrologue(seedCode),
    staleTime: Infinity,
    retry: false,
  });

  const heading = prologueQuery.data?.heading ?? "The story so far";
  const body = prologueQuery.data?.body ?? null;
  const primaryAction = prologueQuery.data?.primaryAction ?? "Ride on";
  const canAdvance = !prologueQuery.isLoading && !prologueQuery.isError;

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canAdvance) {
      return;
    }
    onContinue();
  }

  return (
    <StepCard>
      <StepHeading>{heading}</StepHeading>

      {prologueQuery.isLoading ? (
        <ProloguePending>The trail ahead is still coming into focus…</ProloguePending>
      ) : prologueQuery.isError ? (
        <PrologueError>
          The trail fades into dust before you can make sense of it. Give it a moment and try
          again.
          <RetryButton type="button" onClick={() => prologueQuery.refetch()}>
            Try again
          </RetryButton>
        </PrologueError>
      ) : (
        <PrologueBody>{body}</PrologueBody>
      )}

      <StepForm onSubmit={handleSubmit}>
        <Button type="submit" $variant="primary" disabled={!canAdvance}>
          {primaryAction}
        </Button>
        <BackButton type="button" onClick={onBack}>
          Back
        </BackButton>
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

const PrologueBody = styled.p`
  margin: 0;
  padding: 14px 16px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: color-mix(in srgb, var(--text) 88%, transparent);
  font-size: 0.96rem;
  line-height: 1.5;
  white-space: pre-wrap;
  max-width: 60ch;
`;

const ProloguePending = styled.p`
  margin: 0;
  padding: 14px 16px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: var(--muted);
  font-size: 0.92rem;
`;

const PrologueError = styled.div`
  display: grid;
  gap: 10px;
  padding: 14px 16px;
  border-radius: 14px;
  background: color-mix(in srgb, var(--danger) 12%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 26%, transparent);
  color: var(--danger-text);
  font-size: 0.92rem;
`;

const RetryButton = styled.button`
  justify-self: start;
  border: 1px solid color-mix(in srgb, var(--danger) 40%, transparent);
  background: transparent;
  color: var(--danger-text);
  border-radius: 999px;
  padding: 6px 14px;
  font-size: 0.84rem;
  font-weight: 600;
  cursor: pointer;
  transition: border-color 0.15s;

  &:hover {
    border-color: var(--danger);
  }
`;

const StepForm = styled.form`
  display: grid;
  gap: 16px;
`;
