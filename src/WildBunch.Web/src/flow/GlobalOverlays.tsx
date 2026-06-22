import { useState } from "react";
import { CockpitOverlayFrame } from "../components/CockpitOverlayFrame";
import { CaseFileSurface } from "../components/CaseFileSurface";
import { WantedPosterSurface } from "../components/WantedPosterSurface";
import { LogPanel } from "../components/LogPanel";
import { useGameSession } from "../state/useGameSession";

export type OverlayKind = "case-file" | "wanted" | "activity-log" | null;

export function GlobalOverlays() {
  const { journal, wantedPosters, session, loading, error } = useGameSession();
  const [openOverlay, setOpenOverlay] = useState<OverlayKind>(null);

  return (
    <>
      <div className="global-overlay-buttons" role="toolbar" aria-label="Reference overlays">
        <button
          type="button"
          className="overlay-button"
          onClick={() => setOpenOverlay("case-file")}
          disabled={!journal}
        >
          Case file
        </button>
        <button
          type="button"
          className="overlay-button"
          onClick={() => setOpenOverlay("wanted")}
          disabled={wantedPosters.length === 0}
        >
          Wanted
        </button>
        <button
          type="button"
          className="overlay-button"
          onClick={() => setOpenOverlay("activity-log")}
          disabled={!session}
        >
          Activity log
        </button>
      </div>

      <CockpitOverlayFrame
        open={openOverlay === "case-file"}
        eyebrow="Investigation"
        title="Case file"
        description="Clues, suspects, and evidence."
        onClose={() => setOpenOverlay(null)}
      >
        <CaseFileSurface journal={journal} loading={loading} error={error} />
      </CockpitOverlayFrame>

      <CockpitOverlayFrame
        open={openOverlay === "wanted"}
        eyebrow="Sheriff Office"
        title="Wanted posters"
        description="Posters read from town notice boards."
        onClose={() => setOpenOverlay(null)}
      >
        <WantedPosterSurface wantedPosters={wantedPosters} />
      </CockpitOverlayFrame>

      <CockpitOverlayFrame
        open={openOverlay === "activity-log"}
        eyebrow="Journal"
        title="Activity log"
        description="Recent events from the trail."
        onClose={() => setOpenOverlay(null)}
      >
        <LogPanel journal={journal} sessionLogEntries={session?.logEntries ?? []} />
      </CockpitOverlayFrame>
    </>
  );
}
