import { useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import type { GameSessionDto, TownDto, TravelPreviewDto } from "../api/types";
import { previewTravel } from "../api/wildBunchApi";
import { formatRisk, formatTrailTerrain, formatWaterFeature } from "../ui/formatters";
import {
  Panel,
  PanelHead,
  PanelSubtitle,
  Stack,
  ItemCard,
  Muted,
} from "./ui/sharedStyled";

const DestinationCard = styled(ItemCard).attrs({ as: "button" })`
  width: 100%;
  text-align: left;
  color: var(--text);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  transition:
    transform 0.15s ease-out,
    border-color 0.15s ease-out;

  &:hover:not(:disabled) {
    border-color: var(--accent);
    transform: translateY(-1px);
  }

  &:active:not(:disabled) {
    transform: translateY(0);
  }

  &:disabled {
    cursor: not-allowed;
    opacity: 0.7;
  }

  p {
    margin: 0;
    font-size: 0.88rem;
  }

  strong {
    display: block;
  }
`;

const RouteDetails = styled.div`
  display: grid;
  gap: 4px;
`;

const RoutePreview = styled.p`
  color: var(--text);
  font-size: 0.88rem;
  line-height: 1.4;
`;

const RouteMeta = styled.div`
  display: grid;
  gap: 4px;
  text-align: right;
  font-size: 0.84rem;
`;

const RouteMetaTrailId = styled.span`
  color: var(--muted);
`;

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
    `${preview.baselineRideDays} day${preview.baselineRideDays === 1 ? "" : "s"} ride`,
    formatTrailTerrain(preview.routeProfile.terrain),
    formatWaterFeature(preview.routeProfile.waterFeature),
    `${formatRisk(preview.routeProfile.risk)} risk`,
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
    <Panel>
      <PanelHead>
        <h2>Travel routes</h2>
        <PanelSubtitle as="span">{destinations.length} connected</PanelSubtitle>
      </PanelHead>
      <Stack>
        {destinations.length > 0 ? (
          destinations.map(({ town, trailCount }) => {
            const preview = previews[town.id];

            return (
              <DestinationCard
                key={town.id}
                type="button"
                onClick={() => void onTravel(town.id)}
                disabled={!gameId || busy}
              >
                <RouteDetails>
                  <strong>{town.name}</strong>
                  <p>{town.id}</p>
                  <RoutePreview>
                    {preview ? formatPreviewSummary(preview) : "Loading route preview..."}
                  </RoutePreview>
                </RouteDetails>
                <RouteMeta>
                  <span>
                    {trailCount} trail{trailCount === 1 ? "" : "s"}
                  </span>
                  <RouteMetaTrailId>
                    {preview ? preview.routeProfile.trailId : "Previewing..."}
                  </RouteMetaTrailId>
                </RouteMeta>
              </DestinationCard>
            );
          })
        ) : (
          <Muted>Travel destinations derive from the current town and world trails.</Muted>
        )}
      </Stack>
    </Panel>
  );
}
