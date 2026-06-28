import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { useStartFlow } from "../hooks/useStartFlow";
import { FlowSurface, FlowNotice, FlowError } from "../components/ui/sharedStyled";
import { NameEntryStep } from "../components/start-flow/NameEntryStep";
import { StorySoFarStep } from "../components/start-flow/StorySoFarStep";
import { StartingTownStep } from "../components/start-flow/StartingTownStep";
import { CreatingStep } from "../components/start-flow/CreatingStep";

const FlowHero = styled.div`
  display: grid;
  gap: 8px;
  padding: 28px 0 4px;
`;

const FlowHeroLead = styled.p`
  margin: 0;
  font-size: 1.15rem;
  color: var(--muted);
  text-wrap: balance;
  max-width: 60ch;
`;

export function PreSessionSurface() {
  const {
    session,
    loading,
    gameId,
    resetToken,
    notice,
    error,
    startNewGame,
    reloadCurrentGame,
  } = useGameSession();
  const flow = useStartFlow({ session, resetToken });

  async function handleStartWithTown(townId: string) {
    flow.setSelectedTownId(townId);
    flow.goToStep("creating");
    const request = await flow.buildStartGameRequest(townId);
    await startNewGame(request);
  }

  return (
    <FlowSurface $variant="pre-session">
      <FlowHero>
        <h1>Wild Bunch</h1>
        <FlowHeroLead>
          Track a culprit across frontier towns. Read the posters, follow the clues, and bring them
          in.
        </FlowHeroLead>
      </FlowHero>

      {flow.step === "name" && (
        <NameEntryStep
          playerName={flow.playerName}
          onPlayerNameChange={flow.setPlayerName}
          onContinue={flow.advance}
          onBack={flow.goBack}
        />
      )}

      {flow.step === "story" && (
        <StorySoFarStep
          onContinue={flow.advance}
          seedCode={flow.seedState.seedCode}
        />
      )}

      {flow.step === "town" && (
        <StartingTownStep
          selectedTownId={flow.selectedTownId}
          onSelectTown={handleStartWithTown}
          onBack={flow.goBack}
        />
      )}

      {flow.step === "creating" && <CreatingStep busy={loading} />}

      {gameId ? (
        <RefreshRow>
          <RefreshButton type="button" onClick={() => void reloadCurrentGame()} disabled={loading}>
            Refresh session
          </RefreshButton>
        </RefreshRow>
      ) : null}

      {notice ? <FlowNotice>{notice}</FlowNotice> : null}
      {error ? <FlowError>{error}</FlowError> : null}
    </FlowSurface>
  );
}

const RefreshRow = styled.div`
  display: flex;
  justify-content: flex-start;
`;

const RefreshButton = styled.button`
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  border-radius: 999px;
  padding: 8px 16px;
  font-weight: 600;
  cursor: pointer;
  transition: border-color 0.15s;

  &:hover:not(:disabled) {
    border-color: var(--accent);
  }

  &:disabled {
    opacity: 0.55;
    cursor: not-allowed;
  }
`;
