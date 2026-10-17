import { StrictMode, useState, type ContextType } from "react";
import { createRoot } from "react-dom/client";
import { GameSessionContext } from "../src/state/GameSessionProvider";
import { GlobalOverlays, type OverlayKind } from "../src/flow/GlobalOverlays";
import "../src/styles/index.scss";

const gameSessionValue = {
  journal: null,
  journalLoading: false,
  journalRefreshing: false,
  journalError: null,
  retryJournal: async () => undefined,
  wantedPosters: [],
  error: "",
  setError: () => undefined,
  archivePlaythrough: async () => undefined,
  archiving: false,
} as unknown as NonNullable<ContextType<typeof GameSessionContext>>;

function ModalStackFixture() {
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);
  const [outsideActionCount, setOutsideActionCount] = useState(0);

  return (
    <GameSessionContext.Provider value={gameSessionValue}>
      <main>
        <h1>Modal browser fixture</h1>
        <button type="button" onClick={() => setOpenOverlay("game-settings")}>
          Game Settings
        </button>
        <button type="button" onClick={() => setOutsideActionCount((count) => count + 1)}>
          Outside action
        </button>
        <p role="status">Outside action count: {outsideActionCount}</p>
      </main>
      <GlobalOverlays openOverlay={openOverlay} onOpenOverlay={setOpenOverlay} />
    </GameSessionContext.Provider>
  );
}

const root = document.getElementById("root");
if (!root) {
  throw new Error("Modal browser fixture root element was not found.");
}

createRoot(root).render(
  <StrictMode>
    <ModalStackFixture />
  </StrictMode>,
);
