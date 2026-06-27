import { useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import type { GameSessionDto, TownDto, TravelPreviewDto } from "../api/types";
import { previewTravel } from "../api/wildBunchApi";
import { useGameSession } from "../state/useGameSession";
import { InventoryPanel } from "../components/InventoryPanel";
import {
  FlowSurface,
  BackButton,
  Panel,
  PanelHead,
  Stack,
  Button,
  Muted,
  ItemCard,
  FlowNotice,
  FlowError,
} from "../components/ui/sharedStyled";

const PlaceHeader = styled.header`
  display: grid;
  gap: 12px;
  padding: 24px 0 4px;

  h1 {
    margin: 0;
  }
`;

const TravelPrepBody = styled.div`
  display: grid;
  gap: 20px;
`;

const TravelPrepRide = styled.p`
  font-size: 1.15rem;
  margin: 0;
`;

const TravelPrepActions = styled.div`
  display: flex;
  gap: 12px;
  margin-top: 12px;
`;

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
  text-align: right;
  font-size: 0.84rem;
`;

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

  return Array.from(destinations.values()).sort((left, right) =>
    left.town.name.localeCompare(right.town.name),
  );
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
      <FlowSurface $variant="travel-prep">
        <PlaceHeader>
          <BackButton type="button" onClick={() => setSelectedDestId(null)}>
            ← Pick another destination
          </BackButton>
          <h1>Prepare to ride</h1>
        </PlaceHeader>
        <TravelPrepBody>
          <Panel>
            <PanelHead>
              <h2>{destination?.town.name ?? selectedDestId}</h2>
            </PanelHead>
            <Stack>
              <TravelPrepRide>
                That's a <strong>{rideDays}-day ride</strong>
                {preview.travelMode === 1 ? " on horseback" : " on foot"}.
              </TravelPrepRide>
              <InventoryPanel inventory={session.inventory} />
              <TravelPrepActions>
                <Button
                  type="button"
                  $variant="secondary"
                  onClick={() => setSelectedDestId(null)}
                  disabled={loading}
                >
                  Back to town
                </Button>
                <Button
                  type="button"
                  $variant="primary"
                  onClick={() => void handleTravel(selectedDestId)}
                  disabled={loading}
                >
                  {loading ? "Setting out..." : "Start the ride"}
                </Button>
              </TravelPrepActions>
            </Stack>
          </Panel>
        </TravelPrepBody>
        {notice ? <FlowNotice>{notice}</FlowNotice> : null}
        {error ? <FlowError>{error}</FlowError> : null}
      </FlowSurface>
    );
  }

  // Destination selection screen
  return (
    <FlowSurface $variant="travel-prep">
      <PlaceHeader>
        <BackButton type="button" onClick={onBack}>
          ← Back to town
        </BackButton>
        <h1>Hit the trail</h1>
      </PlaceHeader>
      <TravelPrepBody>
        <Stack>
          {destinations.length > 0 ? (
            destinations.map(({ town, trailCount }) => (
              <DestinationCard
                key={town.id}
                type="button"
                onClick={() => setSelectedDestId(town.id)}
                disabled={!gameId || loading}
              >
                <RouteDetails>
                  <strong>{town.name}</strong>
                  <RoutePreview>
                    {previewLoading && selectedDestId === town.id
                      ? "Checking the route..."
                      : "Click to check the ride"}
                  </RoutePreview>
                </RouteDetails>
                <RouteMeta>
                  <span>
                    {trailCount} trail{trailCount === 1 ? "" : "s"}
                  </span>
                </RouteMeta>
              </DestinationCard>
            ))
          ) : (
            <Muted>No trails lead out of this town.</Muted>
          )}
        </Stack>
      </TravelPrepBody>
    </FlowSurface>
  );
}
