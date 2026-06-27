import styled from "styled-components";
import { useGameSession } from "../state/useGameSession";
import { StartGamePanel } from "../components/StartGamePanel";
import { FlowSurface, FlowNotice, FlowError } from "../components/ui/sharedStyled";

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

  return (
    <FlowSurface $variant="pre-session">
      <FlowHero>
        <h1>Wild Bunch</h1>
        <FlowHeroLead>
          Track a culprit across frontier towns. Read the posters, follow the clues, and bring them
          in.
        </FlowHeroLead>
      </FlowHero>
      <StartGamePanel
        session={session}
        busy={loading}
        gameId={gameId}
        resetToken={resetToken}
        onStartGame={startNewGame}
        onRefresh={reloadCurrentGame}
      />
      {notice ? <FlowNotice>{notice}</FlowNotice> : null}
      {error ? <FlowError>{error}</FlowError> : null}
    </FlowSurface>
  );
}
