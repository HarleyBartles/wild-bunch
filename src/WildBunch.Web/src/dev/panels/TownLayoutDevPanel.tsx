import { useState, useEffect } from "react";
import styled from "styled-components";
import { useGameSession } from "../../state/useGameSession";
import { getTownLayoutSalts, setTownLayoutSalts, generateRandomTownLayoutSalts } from "../devApi";

interface TownLayoutSalts {
  resolverVersion: string | null;
  buildingsSalt: string | null;
  roadsSalt: string | null;
  dirtSalt: string | null;
  propsSalt: string | null;
}

interface TownLayoutDevPanelProps {
  expanded?: boolean;
}

export function TownLayoutDevPanel({ expanded = false }: TownLayoutDevPanelProps) {
  const { gameId } = useGameSession();
  const [salts, setSalts] = useState<TownLayoutSalts | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [statusType, setStatusType] = useState<"success" | "error" | null>(null);

  useEffect(() => {
    if (!gameId) return;

    const loadSalts = async () => {
      setIsLoading(true);
      setStatusMessage(null);
      setStatusType(null);
      try {
        const loadedSalts = await getTownLayoutSalts(gameId);
        setSalts(loadedSalts);
      } catch (error) {
        console.error("Failed to load town layout salts:", error);
        setStatusMessage("Failed to load salts");
        setStatusType("error");
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
    setStatusMessage("Bundle copied to clipboard");
    setStatusType("success");
    setTimeout(() => {
      setStatusMessage(null);
      setStatusType(null);
    }, 2000);
  };

  const handleSetSalts = async () => {
    if (!salts || !gameId) return;
    setIsLoading(true);
    setStatusMessage(null);
    setStatusType(null);
    try {
      await setTownLayoutSalts(gameId, salts);
      setStatusMessage("Salts set successfully");
      setStatusType("success");
      setTimeout(() => {
        setStatusMessage(null);
        setStatusType(null);
      }, 2000);
    } catch (error) {
      console.error("Failed to set town layout salts:", error);
      setStatusMessage("Failed to set salts - check session status");
      setStatusType("error");
    } finally {
      setIsLoading(false);
    }
  };

  const handleGenerateRandom = async () => {
    if (!gameId) return;
    setIsLoading(true);
    setStatusMessage(null);
    setStatusType(null);
    try {
      const randomSalts = await generateRandomTownLayoutSalts(gameId);
      setSalts(randomSalts);
      setStatusMessage("Random salts generated");
      setStatusType("success");
      setTimeout(() => {
        setStatusMessage(null);
        setStatusType(null);
      }, 2000);
    } catch (error) {
      console.error("Failed to generate random town layout salts:", error);
      setStatusMessage("Failed to generate random salts");
      setStatusType("error");
    } finally {
      setIsLoading(false);
    }
  };

  const handleSaltChange = (field: keyof TownLayoutSalts, value: string) => {
    if (salts) {
      setSalts({ ...salts, [field]: value });
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
              <Value>{salts.resolverVersion || "Not set"}</Value>
            </Row>
            <Row>
              <Label>Buildings Salt:</Label>
              <Input
                type="text"
                value={salts.buildingsSalt || ""}
                onChange={(e) => handleSaltChange("buildingsSalt", e.target.value)}
                placeholder="Buildings salt"
              />
            </Row>
            <Row>
              <Label>Roads Salt:</Label>
              <Input
                type="text"
                value={salts.roadsSalt || ""}
                onChange={(e) => handleSaltChange("roadsSalt", e.target.value)}
                placeholder="Roads salt"
              />
            </Row>
            <Row>
              <Label>Dirt Salt:</Label>
              <Input
                type="text"
                value={salts.dirtSalt || ""}
                onChange={(e) => handleSaltChange("dirtSalt", e.target.value)}
                placeholder="Dirt salt"
              />
            </Row>
            <Row>
              <Label>Props Salt:</Label>
              <Input
                type="text"
                value={salts.propsSalt || ""}
                onChange={(e) => handleSaltChange("propsSalt", e.target.value)}
                placeholder="Props salt"
              />
            </Row>
            {statusMessage && (
              <StatusMessage $type={statusType}>{statusMessage}</StatusMessage>
            )}
            <ButtonRow>
              <Button type="button" onClick={handleCopyBundle}>
                Copy Bundle
              </Button>
              <Button type="button" onClick={handleSetSalts} disabled={isLoading}>
                Set Salts
              </Button>
              <Button type="button" onClick={handleGenerateRandom} disabled={isLoading}>
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
  align-items: center;
`;

const Label = styled.span`
  color: var(--muted);
  flex-shrink: 0;
  min-width: 120px;
`;

const Value = styled.span`
  color: var(--text);
`;

const Input = styled.input`
  flex: 1;
  padding: 4px 8px;
  border-radius: 4px;
  border: 1px solid var(--border);
  background: var(--bg);
  color: var(--text);
  font-size: 0.82rem;
  min-width: 0;

  &:focus {
    outline: none;
    border-color: var(--accent);
  }
`;

const StatusMessage = styled.div<{ $type: "success" | "error" }>`
  font-size: 0.82rem;
  padding: 4px 8px;
  border-radius: 4px;
  background: ${props => props.$type === "success" ? "rgba(76, 175, 80, 0.1)" : "rgba(244, 67, 54, 0.1)"};
  color: ${props => props.$type === "success" ? "#4caf50" : "#f44336"};
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
