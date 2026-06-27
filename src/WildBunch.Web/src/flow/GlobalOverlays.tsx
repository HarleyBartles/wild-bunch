import { useState } from "react";
import styled from "styled-components";
import { CockpitOverlayFrame } from "../components/CockpitOverlayFrame";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { CaseFileSurface } from "../components/CaseFileSurface";
import { JournalSurface } from "../components/JournalSurface";
import { WantedPosterSurface } from "../components/WantedPosterSurface";
import { Button } from "../components/ui/sharedStyled";
import { useGameSession } from "../state/useGameSession";

export type OverlayKind = "case-file" | "wanted" | "journal" | "game-settings" | null;

interface GlobalOverlaysProps {
  openOverlay: OverlayKind;
  onOpenOverlay: (overlay: OverlayKind) => void;
}

export function GlobalOverlays({ openOverlay, onOpenOverlay }: GlobalOverlaysProps) {
  const { journal, wantedPosters, loading, error, archivePlaythrough, archiving } = useGameSession();
  const [confirmOpen, setConfirmOpen] = useState(false);

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

      <CockpitOverlayFrame
        open={openOverlay === "game-settings"}
        eyebrow="Settings"
        title="Game Settings"
        description="Manage your playthrough."
        onClose={() => onOpenOverlay(null)}
      >
        <SettingsSection>
          <SettingsHead>
            <h3>Playthrough</h3>
          </SettingsHead>
          <SettingsRow>
            <SettingsCopy>
              <strong>Start Over</strong>
              <p>Archive this playthrough and begin again from the start.</p>
            </SettingsCopy>
            <Button type="button" $variant="secondary" onClick={() => setConfirmOpen(true)}>
              Start Over
            </Button>
          </SettingsRow>
        </SettingsSection>
      </CockpitOverlayFrame>

      <ConfirmDialog
        open={confirmOpen}
        title="Start over?"
        body="This will archive your current playthrough. You will not be able to return to it. A new hunt will begin from the start."
        confirmLabel="Archive and start over"
        cancelLabel="Keep riding"
        busy={archiving}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => {
          void archivePlaythrough().then(() => setConfirmOpen(false)).catch(() => { /* onError already handled in mutation */ });
        }}
      />
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

const SettingsSection = styled.section`
  display: grid;
  gap: 14px;
  padding: 4px 0 8px;
`;

const SettingsHead = styled.header`
  h3 {
    margin: 0;
    font-size: 1.05rem;
    letter-spacing: 0.02em;
  }
`;

const SettingsRow = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
`;

const SettingsCopy = styled.div`
  display: grid;
  gap: 4px;
  max-width: 460px;

  strong {
    font-size: 0.94rem;
  }

  p {
    margin: 0;
    color: var(--muted);
    font-size: 0.86rem;
    line-height: 1.45;
  }
`;
