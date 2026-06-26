import { WantedPosterSurface } from "../components/WantedPosterSurface";
import { Panel, PanelHead, PanelSubtitle } from "../components/ui/sharedStyled";
import { useGameSession } from "../state/useGameSession";

export function WantedRoute() {
  const { wantedPosters } = useGameSession();

  return (
    <Panel $wide>
      <PanelHead>
        <h2>Wanted posters</h2>
        <PanelSubtitle as="span">{wantedPosters.length} posted</PanelSubtitle>
      </PanelHead>
      <WantedPosterSurface wantedPosters={wantedPosters} />
    </Panel>
  );
}
