import { CaseFileSurface } from "../components/CaseFileSurface";
import { Panel, PanelHead, PanelSubtitle, Muted } from "../components/ui/sharedStyled";
import { useGameSession } from "../state/useGameSession";

export function CaseFileRoute() {
  const { journal, loading, error } = useGameSession();

  return (
    <Panel $wide>
      <PanelHead>
        <h2>Case file</h2>
        <PanelSubtitle as="span">Investigation board</PanelSubtitle>
      </PanelHead>
      <Muted>A read-only summary of player-known clues, suspects, and warrants.</Muted>
      <CaseFileSurface journal={journal} loading={loading} error={error} />
    </Panel>
  );
}
