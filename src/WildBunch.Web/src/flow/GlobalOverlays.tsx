import styled from "styled-components";
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
      <OverlayButtons role="toolbar" aria-label="Reference overlays">
        <OverlayButton
          type="button"
          onClick={() => onOpenOverlay("case-file")}
          disabled={!journal}
        >
          Case file
        </OverlayButton>
        <OverlayButton
          type="button"
          onClick={() => onOpenOverlay("wanted")}
          disabled={wantedPosters.length === 0}
        >
          Wanted
        </OverlayButton>
      </OverlayButtons>

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

const OverlayButtons = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
`;

const OverlayButton = styled.button`
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  border-radius: 999px;
  padding: 6px 14px;
  font-size: 0.82rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  cursor: pointer;
  transition-property: transform, background-color, border-color, color, box-shadow;
  transition-duration: 150ms;
  transition-timing-function: ease-out;

  &:hover:not(:disabled) {
    border-color: var(--accent);
    background: rgba(223, 159, 79, 0.08);
    box-shadow: 0 8px 18px rgba(0, 0, 0, 0.12);
  }

  &:active:not(:disabled) {
    transform: translateY(1px);
  }

  &:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
`;
