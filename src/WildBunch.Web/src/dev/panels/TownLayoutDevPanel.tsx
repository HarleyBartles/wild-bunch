import { useState, useEffect } from "react";
import styled from "styled-components";
import { useGameSession } from "../../state/useGameSession";
import { getTownLayoutSalts, setTownLayoutSalts, generateRandomTownLayoutSalts } from "../devApi";

interface TownLayoutSalts {
  resolverVersion: string;
  buildingsSalt: string;
  roadsSalt: string;
  dirtSalt: string;
  propsSalt: string;
}

interface TownLayoutDevPanelProps {
  expanded?: boolean;
}

export function TownLayoutDevPanel({ expanded = false }: TownLayoutDevPanelProps) {
  const { gameId } = useGameSession();
  const [salts, setSalts] = useState<TownLayoutSalts | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!gameId) return;

    const loadSalts = async () => {
      setIsLoading(true);
      try {
        const loadedSalts = await getTownLayoutSalts(gameId);
        setSalts(loadedSalts);
      } catch (error) {
        console.error("Failed to load town layout salts:", error);
      } finally {
        setIsLoading(false);
      }
    };

    loadSalts();
  }, [gameId]);

  const handleCopyBundle = () => {
    if (!salts) return;
    const bundle = JSON.stringify(salts, null, 2);
    navigator.clipboard.writeText(bundle);
  };

  const handleSetSalts = async () => {
    if (!salts || !gameId) return;
    setIsLoading(true);
    try {
      await setTownLayoutSalts(gameId, salts);
    } catch (error) {
      console.error("Failed to set town layout salts:", error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleGenerateRandom = async () => {
    if (!gameId) return;
    setIsLoading(true);
    try {
      const randomSalts = await generateRandomTownLayoutSalts(gameId);
      setSalts(randomSalts);
    } catch (error) {
      console.error("Failed to generate random town layout salts:", error);
    } finally {
      setIsLoading(false);
    }
  };

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }

  return (
    <Container $expanded={expanded}>
      <Section>
        <SectionTitle>Town Layout</SectionTitle>
        {isLoading ? (
          <MutedText>Loading...</MutedText>
        ) : salts ? (
          <>
            <Row>
              <Label>Resolver Version:</Label>
              <Value>{salts.resolverVersion}</Value>
            </Row>
            <Row>
              <Label>Buildings Salt:</Label>
              <Value>{salts.buildingsSalt}</Value>
            </Row>
            <Row>
              <Label>Roads Salt:</Label>
              <Value>{salts.roadsSalt}</Value>
            </Row>
            <Row>
              <Label>Dirt Salt:</Label>
              <Value>{salts.dirtSalt}</Value>
            </Row>
            <Row>
              <Label>Props Salt:</Label>
              <Value>{salts.propsSalt}</Value>
            </Row>
            <ButtonRow>
              <Button type="button" onClick={handleCopyBundle}>
                Copy Bundle
              </Button>
              <Button type="button" onClick={handleSetSalts}>
                Set Salts
              </Button>
              <Button type="button" onClick={handleGenerateRandom}>
                Generate Random
              </Button>
            </ButtonRow>
          </>
        ) : (
          <MutedText>No salts loaded</MutedText>
        )}
      </Section>
    </Container>
  );
}

const Container = styled.div<{ $expanded: boolean }>`
  display: grid;
  gap: 16px;
`;

const Section = styled.section`
  display: grid;
  gap: 6px;
`;

const SectionTitle = styled.h3`
  margin: 0 0 4px;
  font-size: 0.88rem;
  color: var(--accent);
`;

const Row = styled.div`
  display: flex;
  gap: 8px;
  font-size: 0.82rem;
`;

const Label = styled.span`
  color: var(--muted);
  flex-shrink: 0;
  min-width: 120px;
`;

const Value = styled.span`
  color: var(--text);
`;

const ButtonRow = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 6px;
`;

const Button = styled.button`
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.8rem;
  font-weight: 600;
  min-height: 32px;
  transition-property: background-color, border-color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &:hover:not(:disabled) {
    background: var(--bg-soft);
    border-color: var(--border);
  }

  &:active:not(:disabled) {
    background: var(--bg-strong);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const MutedText = styled.span`
  color: var(--muted);
  font-size: 0.82rem;
`;
