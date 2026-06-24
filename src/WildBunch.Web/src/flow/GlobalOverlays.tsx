import { CockpitOverlayFrame } from "../components/CockpitOverlayFrame";
import { CaseFileSurface } from "../components/CaseFileSurface";
import { JournalSurface } from "../components/JournalSurface";
import { WantedPosterSurface } from "../components/WantedPosterSurface";
import { useGameSession } from "../state/useGameSession";

export type OverlayKind = "case-file" | "wanted" | "journal" | null;

interface GlobalOverlaysProps {
  openOverlay: OverlayKind;
  onOpenOverlay: (overlay: OverlayKind) => void;
}

export function GlobalOverlays({ openOverlay, onOpenOverlay }: GlobalOverlaysProps) {
  const { journal, wantedPosters, loading, error } = useGameSession();

  return (
    <>
      <div className="global-overlay-buttons" role="toolbar" aria-label="Reference overlays">
        <button
          type="button"
          className="overlay-button"
          onClick={() => onOpenOverlay("case-file")}
          disabled={!journal}
        >
          Case file
        </button>
        <button
          type="button"
          className="overlay-button"
          onClick={() => onOpenOverlay("wanted")}
          disabled={wantedPosters.length === 0}
        >
          Wanted
        </button>
      </div>

      <CockpitOverlayFrame
        open={openOverlay === "case-file"}
        eyebrow="Investigation"
        title="Case file"
        description="Clues, suspects, and evidence."
        onClose={() => onOpenOverlay(null)}
      >
        <CaseFileSurface journal={journal} loading={loading} error={error} />
      </CockpitOverlayFrame>

      <CockpitOverlayFrame
        open={openOverlay === "wanted"}
        eyebrow="Sheriff Office"
        title="Wanted posters"
        description="Posters read from town notice boards."
        onClose={() => onOpenOverlay(null)}
      >
        <WantedPosterSurface wantedPosters={wantedPosters} />
      </CockpitOverlayFrame>

      <CockpitOverlayFrame
        open={openOverlay === "journal"}
        eyebrow="Journal"
        title="Journal"
        onClose={() => onOpenOverlay(null)}
      >
        <JournalSurface journal={journal} loading={loading} error={error} sessionLogEntries={journal?.logEntries ?? []} />
      </CockpitOverlayFrame>
    </>
  );
}
