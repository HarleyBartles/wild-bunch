import { TravelPanel } from "../components/TravelPanel";
import { TravelRoutesPanel } from "../components/TravelRoutesPanel";
import { Panel, PanelHead } from "../components/ui/sharedStyled";
import { useGameSession } from "../state/useGameSession";

export function TrailRoute() {
  const { session, gameId, loading, handleTravel, handleTravelTurnResult } = useGameSession();

  return (
    <>
      {session?.journey ? (
        <Panel $wide>
          <PanelHead>
            <h2>Active journey</h2>
          </PanelHead>
          <TravelPanel
            gameId={gameId ?? session.id}
            session={session}
            busy={loading}
            onTurnResult={handleTravelTurnResult}
          />
        </Panel>
      ) : null}

      <TravelRoutesPanel gameId={gameId ?? session?.id ?? null} session={session} busy={loading} onTravel={handleTravel} />
    </>
  );
}
