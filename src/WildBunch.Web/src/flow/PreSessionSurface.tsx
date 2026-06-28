import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { useStartFlow } from "../hooks/useStartFlow";
import { FlowSurface, FlowNotice, FlowError } from "../components/ui/sharedStyled";
import { SetupHuntStep } from "../components/start-flow/SetupHuntStep";
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
    resetToken,
    notice,
    error,
    startNewGame,
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
        <SetupHuntStep
          playerName={flow.playerName}
          gameDifficulty={flow.gameDifficulty}
          gameEntropy={flow.gameEntropy}
          seedDraft={flow.seedDraft}
          seedDirty={flow.seedDirty}
          decodeError={flow.decodeError}
          onPlayerNameChange={flow.setPlayerName}
          onGameDifficultyChange={flow.setGameDifficulty}
          onGameEntropyChange={flow.setGameEntropy}
          onSeedDraftChange={flow.setSeedDraft}
          onRandomizeSeed={flow.randomizeSeed}
          onContinue={flow.advance}
        />
      )}

      {flow.step === "story" && (
        <StorySoFarStep
          onContinue={flow.advance}
          seedCode={flow.seedState.seedCode}
          gameDifficulty={flow.gameDifficulty}
          gameEntropy={flow.gameEntropy}
        />
      )}

      {flow.step === "town" && (
        <StartingTownStep
          selectedTownId={flow.selectedTownId}
          onSelectTown={handleStartWithTown}
        />
      )}

      {flow.step === "creating" && <CreatingStep busy={loading} />}

      {notice ? <FlowNotice>{notice}</FlowNotice> : null}
      {error ? <FlowError>{error}</FlowError> : null}
    </FlowSurface>
  );
}
