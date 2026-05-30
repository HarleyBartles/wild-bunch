import { useEffect, useMemo, useState } from "react";
import type { GameSessionDto, TownDto, TravelPreviewDto } from "../api/types";
import { previewTravel } from "../api/wildBunchApi";
import { formatRisk, formatTrailTerrain, formatWaterFeature } from "../ui/formatters";

interface TravelRoutesPanelProps {
  gameId: string | null;
  session: GameSessionDto | null;
  busy: boolean;
  onTravel: (destinationTownId: string) => Promise<void>;
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

function formatPreviewSummary(preview: TravelPreviewDto) {
  return [
    `${preview.expectedDays} day${preview.expectedDays === 1 ? "" : "s"}`,
    formatTrailTerrain(preview.routeProfile.terrain),
    formatWaterFeature(preview.routeProfile.waterFeature),
    `${formatRisk(preview.routeProfile.risk)} risk`,
    `${preview.routeProfile.rideDayDistance.toFixed(2)} ride-day units`,
  ].join(" | ");
}

export function TravelRoutesPanel({ gameId, session, busy, onTravel }: TravelRoutesPanelProps) {
  const destinations = useMemo(() => (session ? connectedDestinations(session) : []), [session]);
  const [previews, setPreviews] = useState<Record<string, TravelPreviewDto>>({});

  useEffect(() => {
    if (!gameId || !session) {
      setPreviews({});
      return;
    }

    let cancelled = false;
    const destinationIds = destinations.map((destination) => destination.town.id);

    if (destinationIds.length === 0) {
      setPreviews({});
      return;
    }

    setPreviews({});

    void (async () => {
      const loadedPreviews = await Promise.all(
        destinationIds.map(async (destinationTownId) => {
          try {
            const result = await previewTravel(gameId, destinationTownId);
            if (result.success && result.preview) {
              return [destinationTownId, result.preview] as const;
            }
          } catch {
            // Keep the route card useful even if one preview fails.
          }

          return null;
        }),
      );

      if (cancelled) {
        return;
      }

      const nextPreviews: Record<string, TravelPreviewDto> = {};
      for (const previewEntry of loadedPreviews) {
        if (previewEntry) {
          nextPreviews[previewEntry[0]] = previewEntry[1];
        }
      }

      setPreviews(nextPreviews);
    })();

    return () => {
      cancelled = true;
    };
  }, [destinations, gameId, session]);

  return (
    <section className="panel">
      <div className="panel-head">
        <h2>Travel routes</h2>
        <span className="panel-subtitle">{destinations.length} connected</span>
      </div>
      <div className="stack">
        {destinations.length > 0 ? (
          destinations.map(({ town, trailCount }) => {
            const preview = previews[town.id];

            return (
              <button
                key={town.id}
                type="button"
                className="destination-card"
                onClick={() => void onTravel(town.id)}
                disabled={!gameId || busy}
              >
                <div className="destination-card__body">
                  <strong>{town.name}</strong>
                  <p>{town.id}</p>
                  <p className="destination-route">{preview ? formatPreviewSummary(preview) : "Loading route preview..."}</p>
                </div>
                <div className="destination-meta">
                  <span>
                    {trailCount} trail{trailCount === 1 ? "" : "s"}
                  </span>
                  <span>{preview ? preview.routeProfile.trailId : "Previewing..."}</span>
                </div>
              </button>
            );
          })
        ) : (
          <p className="muted">Travel destinations derive from the current town and world trails.</p>
        )}
      </div>
    </section>
  );
}
