import { useEffect, useMemo, useState } from "react";
import type { GameSessionDto, TownDto, TravelPreviewDto } from "../api/types";
import { previewTravel } from "../api/wildBunchApi";
import { useGameSession } from "../state/useGameSession";
import { InventoryPanel } from "../components/InventoryPanel";

interface TravelPrepSurfaceProps {
  onBack: () => void;
}

interface ConnectedDestination {
  town: TownDto;
  trailCount: number;
}

function connectedDestinations(session: GameSessionDto) {
  const currentTownId = session.player.currentTownId;
  const townMap = new Map(session.world.towns.map((town) => [town.id, town]));
  const destinations = new Map<string, ConnectedDestination>();

  for (const trail of session.world.trails) {
    if (trail.fromTownId !== currentTownId && trail.toTownId !== currentTownId) {
      continue;
    }

    const destinationTownId = trail.fromTownId === currentTownId ? trail.toTownId : trail.fromTownId;
    const town = townMap.get(destinationTownId);
    if (!town) {
      continue;
    }

    const entry = destinations.get(destinationTownId);
    if (entry) {
      destinations.set(destinationTownId, { town, trailCount: entry.trailCount + 1 });
      continue;
    }

    destinations.set(destinationTownId, { town, trailCount: 1 });
  }

  return Array.from(destinations.values()).sort((left, right) => left.town.name.localeCompare(right.town.name));
}

export function TravelPrepSurface({ onBack }: TravelPrepSurfaceProps) {
  const { session, gameId, loading, handleTravel, notice, error } = useGameSession();
  const [selectedDestId, setSelectedDestId] = useState<string | null>(null);
  const [preview, setPreview] = useState<TravelPreviewDto | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);

  const destinations = useMemo(() => (session ? connectedDestinations(session) : []), [session]);

  useEffect(() => {
    if (!gameId || !selectedDestId) {
      setPreview(null);
      return;
    }

    let cancelled = false;
    setPreviewLoading(true);

    void (async () => {
      try {
        const result = await previewTravel(gameId, selectedDestId);
        if (!cancelled) {
          setPreview(result.success && result.preview ? result.preview : null);
        }
      } catch {
        if (!cancelled) {
          setPreview(null);
        }
      } finally {
        if (!cancelled) {
          setPreviewLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [gameId, selectedDestId]);

  if (!session) {
    return null;
  }

  // If a destination is selected, show the preparation/confirmation screen
  if (selectedDestId && preview) {
    const destination = destinations.find((d) => d.town.id === selectedDestId);
    const rideDays = preview.baselineRideDays;

    return (
      <div className="flow-surface flow-surface--travel-prep">
        <div className="place-header">
          <button
            type="button"
            className="back-button"
            onClick={() => setSelectedDestId(null)}
          >
            ← Pick another destination
          </button>
          <h1>Prepare to ride</h1>
        </div>
        <div className="travel-prep-body">
          <section className="panel">
            <div className="panel-head">
              <h2>{destination?.town.name ?? selectedDestId}</h2>
            </div>
            <div className="stack">
              <p className="travel-prep-ride">
                That's a <strong>{rideDays}-day ride</strong>
                {preview.travelMode === 1 ? " on horseback" : " on foot"}.
              </p>
              <InventoryPanel inventory={session.inventory} />
              <div className="travel-prep-actions">
                <button
                  type="button"
                  className="button button--secondary"
                  onClick={() => setSelectedDestId(null)}
                  disabled={loading}
                >
                  Back to town
                </button>
                <button
                  type="button"
                  className="button button--primary"
                  onClick={() => void handleTravel(selectedDestId)}
                  disabled={loading}
                >
                  {loading ? "Setting out..." : "Start the ride"}
                </button>
              </div>
            </div>
          </section>
        </div>
        {notice ? <p className="flow-notice">{notice}</p> : null}
        {error ? <p className="flow-error">{error}</p> : null}
      </div>
    );
  }

  // Destination selection screen
  return (
    <div className="flow-surface flow-surface--travel-prep">
      <div className="place-header">
        <button type="button" className="back-button" onClick={onBack}>
          ← Back to town
        </button>
        <h1>Hit the trail</h1>
      </div>
      <div className="travel-prep-body">
        <div className="stack">
          {destinations.length > 0 ? (
            destinations.map(({ town, trailCount }) => (
              <button
                key={town.id}
                type="button"
                className="destination-card"
                onClick={() => setSelectedDestId(town.id)}
                disabled={!gameId || loading}
              >
                <div className="destination-card__body">
                  <strong>{town.name}</strong>
                  <p className="destination-route">
                    {previewLoading && selectedDestId === town.id
                      ? "Checking the route..."
                      : "Click to check the ride"}
                  </p>
                </div>
                <div className="destination-meta">
                  <span>{trailCount} trail{trailCount === 1 ? "" : "s"}</span>
                </div>
              </button>
            ))
          ) : (
            <p className="muted">No trails lead out of this town.</p>
          )}
        </div>
      </div>
    </div>
  );
}
