import { useState, type FormEvent } from "react";
import styled from "styled-components";
import type { GameSessionDto, StartGameRequest } from "../api/types";
import { useStartGameSeed } from "../hooks/useStartGameSeed";
import { encodeGameSetupSeed } from "../ui/gameSetupSeedCodec";
import { SetupSeedSummary } from "./SetupSeedSummary";
import { StartGameOptionsForm } from "./StartGameOptionsForm";

interface StartGamePanelProps {
  session: GameSessionDto | null;
  busy: boolean;
  gameId: string | null;
  resetToken: number;
  onStartGame: (request: StartGameRequest) => Promise<void>;
  onRefresh: () => Promise<void>;
}

function StartGameHeader({ busy, gameId, seedDirty, session }: { busy: boolean; gameId: string | null; seedDirty: boolean; session: GameSessionDto | null }) {
  return (
    <StartGameHeaderCard>
      <div>
        <Eyebrow>Seeded setup</Eyebrow>
        <Title>{gameId ? "Refine the next hunt" : "Start a new hunt"}</Title>
        <Lead>
          The setup seed stays visible and editable as a UUID replay key. Paste one, randomize one, or let the backend resolve it into a legal starting world.
        </Lead>
      </div>
      <HeaderMeta>
        <MetaCard>
          <span>Session</span>
          <strong>{session ? session.player.name : "No active hunt"}</strong>
        </MetaCard>
        <MetaCard>
          <span>Status</span>
          <strong>{busy ? "Working" : seedDirty ? "Seed pending" : gameId ? "Ready to refresh" : "Ready to start"}</strong>
        </MetaCard>
      </HeaderMeta>
    </StartGameHeaderCard>
  );
}

function ActionBar({
  busy,
  gameId,
  onRandomizeSeed,
  onRefresh,
}: {
  busy: boolean;
  gameId: string | null;
  onRandomizeSeed: () => void;
  onRefresh: () => Promise<void>;
}) {
  return (
    <ActionBarRow>
      <PrimaryButton type="submit" disabled={busy}>
        {busy ? "Starting..." : "Start new game"}
      </PrimaryButton>
      <GhostButton type="button" onClick={() => void onRandomizeSeed()} disabled={busy}>
        Randomize seed
      </GhostButton>
      <GhostButton type="button" onClick={() => void onRefresh()} disabled={!gameId || busy}>
        Refresh session
      </GhostButton>
    </ActionBarRow>
  );
}

export function StartGamePanel({ session, busy, gameId, resetToken, onStartGame, onRefresh }: StartGamePanelProps) {
  const [submitError, setSubmitError] = useState<string | null>(null);
  const {
    playerName,
    gameDifficulty,
    seedState,
    seedDraft,
    seedDirty,
    decodeError,
    setPlayerName,
    setSeedDraft,
    setGameDifficulty,
    randomizeSeed,
  } = useStartGameSeed({ session, resetToken });

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedName = playerName.trim();
    if (!trimmedName) {
      setSubmitError("Enter a player name to start.");
      return;
    }

    setSubmitError(null);
    const seedCode = await encodeGameSetupSeed(seedState);
    await onStartGame({
      playerName: trimmedName,
      gameDifficulty,
      seedCode,
    });
  }

  return (
    <StartGameStage>
      <StartGameHeader busy={busy} gameId={gameId} seedDirty={seedDirty} session={session} />

      {submitError ? <PanelError>{submitError}</PanelError> : null}

      <StartGameForm onSubmit={(event: FormEvent<HTMLFormElement>) => void handleSubmit(event)}>
        <StartGameOptionsForm
          playerName={playerName}
          gameDifficulty={gameDifficulty}
          seedDraft={seedDraft}
          seedDirty={seedDirty}
          decodeError={decodeError}
          onPlayerNameChange={setPlayerName}
          onSeedDraftChange={setSeedDraft}
          onGameDifficultyChange={setGameDifficulty}
        />

        <SetupSeedSummary seedState={seedState} />
        <ActionBar busy={busy} gameId={gameId} onRandomizeSeed={randomizeSeed} onRefresh={onRefresh} />
      </StartGameForm>
    </StartGameStage>
  );
}

const StartGameStage = styled.article`
  display: grid;
  gap: 18px;
  padding: 22px;
  border-radius: 28px;
  border: 1px solid color-mix(in srgb, var(--accent-strong) 20%, transparent);
  background:
    radial-gradient(circle at top left, color-mix(in srgb, var(--accent-strong) 14%, transparent), transparent 28%),
    linear-gradient(180deg, rgba(29, 23, 16, 0.98), rgba(16, 12, 8, 0.98)); /* no token match — surface gradient */
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.34);
`;

const StartGameHeaderCard = styled.header`
  display: flex;
  justify-content: space-between;
  gap: 18px;
  align-items: end;
`;

const Eyebrow = styled.p`
  margin: 0 0 6px;
  color: var(--accent-strong);
  text-transform: uppercase;
  letter-spacing: 0.22em;
  font-size: 0.74rem;
`;

const Title = styled.h3`
  margin: 0 0 8px;
  font-family: "Iowan Old Style", Georgia, serif;
  font-size: clamp(2rem, 3.8vw, 3rem);
  line-height: 0.98;
`;

const Lead = styled.p`
  max-width: 72ch;
  margin: 0;
  color: color-mix(in srgb, var(--text) 75%, transparent);
`;

const HeaderMeta = styled.div`
  display: grid;
  gap: 10px;
  width: min(100%, 280px);
`;

const MetaCard = styled.div`
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);

  span {
    color: color-mix(in srgb, var(--text) 62%, transparent);
    text-transform: uppercase;
    letter-spacing: 0.08em;
    font-size: 0.75rem;
  }
`;

const StartGameForm = styled.form`
  display: grid;
  gap: 16px;
`;

const PanelError = styled.div`
  padding: 12px 14px;
  border-radius: 16px;
  background: color-mix(in srgb, var(--danger) 14%, transparent);
  border: 1px solid color-mix(in srgb, var(--danger) 26%, transparent);
  color: var(--danger-text);
`;

const ActionBarRow = styled.div`
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
`;

const ButtonBase = styled.button`
  border-radius: 999px;
  padding: 10px 16px;
  font-weight: 700;
  border: 1px solid transparent;

  &:disabled {
    cursor: not-allowed;
    opacity: 0.55;
  }
`;

const PrimaryButton = styled(ButtonBase)`
  background: linear-gradient(180deg, var(--accent-strong), var(--accent-strong-dark));
  color: var(--accent-ink);
  border-color: color-mix(in srgb, var(--accent-strong) 55%, transparent);
`;

const GhostButton = styled(ButtonBase)`
  background: transparent;
  color: var(--text);
  border-color: rgba(255, 255, 255, 0.16);
`;
