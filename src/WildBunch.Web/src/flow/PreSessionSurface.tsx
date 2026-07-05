import { lazy, Suspense } from "react";
import styled from "styled-components";
import { useGamePhase } from "../hooks/useGamePhase";
import { useGameSession } from "../state/useGameSession";
import { useStartFlow } from "../hooks/useStartFlow";
import { encodeGameSetupSeed } from "../ui/gameSetupSeedCodec";
import { FlowSurface, FlowNotice, FlowError } from "../components/ui/sharedStyled";
import { SetupHuntStep } from "../components/start-flow/SetupHuntStep";
import { StorySoFarStep } from "../components/start-flow/StorySoFarStep";
import { CreatingStep } from "../components/start-flow/CreatingStep";

const StartingTownStep = lazy(() =>
  import("../components/start-flow/StartingTownStep").then((m) => ({ default: m.StartingTownStep })),
);

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
  const { phase } = useGamePhase();
  const {
    session,
    loading,
    resetToken,
    notice,
    error,
    handleSetupGame,
    handleMarkPrologueViewed,
    handleStartGameWithTown,
  } = useGameSession();
  const flow = useStartFlow({ session, resetToken });

  // Determine which step to show based on backend start flow phase.
  // On refresh, the backend phase is the source of truth.
  const effectiveStep = deriveEffectiveStep(phase, flow.step);

  async function handleSetupComplete() {
    const seedCode = await encodeGameSetupSeed(flow.seedState);
    const trimmedName = flow.playerName.trim();
    await handleSetupGame({
      playerName: trimmedName,
      gameDifficulty: flow.gameDifficulty,
      seedCode,
      gameEntropy: flow.gameEntropy,
    });
    flow.advance();
  }

  async function handlePrologueViewed() {
    await handleMarkPrologueViewed();
    flow.advance();
  }

  async function handleStartWithTown(townId: string) {
    flow.setSelectedTownId(townId);
    flow.goToStep("creating");
    await handleStartGameWithTown(townId);
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

      {effectiveStep === "name" && (
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
          onContinue={handleSetupComplete}
        />
      )}

      {effectiveStep === "story" && (
        <StorySoFarStep
          onContinue={handlePrologueViewed}
          seedCode={flow.seedState.seedCode}
          gameDifficulty={flow.gameDifficulty}
          gameEntropy={flow.gameEntropy}
        />
      )}

      {effectiveStep === "town" && (
        <Suspense fallback={<div>Loading town selection…</div>}>
          <StartingTownStep
            sessionId={session?.id ?? ""}
            selectedTownId={flow.selectedTownId}
            onSelectTown={handleStartWithTown}
          />
        </Suspense>
      )}

      {effectiveStep === "creating" && <CreatingStep busy={loading} />}

      {notice ? <FlowNotice>{notice}</FlowNotice> : null}
      {error ? <FlowError>{error}</FlowError> : null}
    </FlowSurface>
  );
}

function deriveEffectiveStep(
  phase: ReturnType<typeof useGamePhase>["phase"],
  localStep: ReturnType<typeof useStartFlow>["step"],
): "name" | "story" | "town" | "creating" {
  // If the backend says we're in the prologue phase, show the story step
  if (phase === "prologue") return "story";
  // If the backend says we're in town-selection, show the town step
  if (phase === "town-selection") return "town";
  // Otherwise use the local step (for the initial name step before setup is saved)
  return localStep;
}
