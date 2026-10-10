import styled from "styled-components";

type SessionRecoverySurfaceProps = {
  missing: boolean;
  onRetry: () => void;
  onStartNewGame: () => void;
};

export function SessionRecoverySurface({
  missing,
  onRetry,
  onStartNewGame,
}: SessionRecoverySurfaceProps) {
  return (
    <RecoveryPage>
      <RecoveryCard role="alert">
        <h1>{missing ? "Saved playthrough wasn't found" : "Couldn't open your saved game"}</h1>
        <p>
          {missing
            ? "This saved playthrough is no longer available. You can start a new game when you're ready."
            : "Your saved game is still here, but the server could not open it. Try again when the connection is available."}
        </p>
        {missing ? (
          <button type="button" onClick={onStartNewGame}>
            Start a new game
          </button>
        ) : (
          <button type="button" onClick={onRetry}>
            Try again
          </button>
        )}
      </RecoveryCard>
    </RecoveryPage>
  );
}

const RecoveryPage = styled.main`
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 24px;
  background: var(--bg);
`;

const RecoveryCard = styled.section`
  width: min(100%, 560px);
  display: grid;
  gap: 16px;
  padding: 28px;
  border: 1px solid var(--border-strong, #444);
  border-radius: 16px;
  background: var(--bg-panel);
  color: var(--text);

  h1,
  p {
    margin: 0;
  }

  p {
    color: var(--muted);
    line-height: 1.5;
  }

  button {
    justify-self: start;
    min-height: 40px;
    padding: 8px 18px;
    border: 1px solid var(--accent, #df9f4f);
    border-radius: 999px;
    background: var(--accent, #df9f4f);
    color: var(--accent-ink);
    font: inherit;
    font-weight: 700;
    cursor: pointer;
  }
`;
