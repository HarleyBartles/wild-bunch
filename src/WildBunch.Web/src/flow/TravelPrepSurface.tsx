import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import styled from "styled-components";
import type { GameSessionDto, TownDto, TravelPreviewDto } from "../api/types";
import { TravelMode } from "../api/types";
import { getWorldMap, previewTravel } from "../api/wildBunchApi";
import { useGameSession } from "../state/useGameSession";
import { InventoryPanel } from "../components/InventoryPanel";
import { PhaserMapHost } from "../components/start-flow/PhaserMapHost";
import {
  FlowSurface,
  BackButton,
  Panel,
  PanelHead,
  Stack,
  Button,
  Muted,
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

export function TravelPrepSurface() {
  const navigate = useNavigate();
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
                {preview.travelMode === TravelMode.Mounted ? " on horseback" : " on foot"}.
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

  // Destination selection screen — visual map
  const currentTownId = session.player.currentTownId;
  const selectableTownIds = destinations.map((d) => d.town.id);

  return (
    <FlowSurface $variant="travel-prep">
      <PlaceHeader>
        <BackButton type="button" onClick={() => void navigate({ to: "/town" })}>
          ← Back to town
        </BackButton>
        <h1>Hit the trail</h1>
      </PlaceHeader>
      <TravelPrepBody>
        <Stack>
          {destinations.length > 0 ? (
            <TravelMapSelection
              gameId={gameId}
              currentTownId={currentTownId}
              selectableTownIds={selectableTownIds}
              selectedDestId={selectedDestId}
              onSelectDestination={(townId) => setSelectedDestId(townId)}
            />
          ) : (
            <Muted>No trails lead out of this town.</Muted>
          )}
        </Stack>
      </TravelPrepBody>
    </FlowSurface>
  );
}

function TravelMapSelection({
  gameId,
  currentTownId,
  selectableTownIds,
  selectedDestId,
  onSelectDestination,
}: {
  gameId: string | null;
  currentTownId: string;
  selectableTownIds: string[];
  selectedDestId: string | null;
  onSelectDestination: (townId: string) => void;
}) {
  const mapQuery = useQuery({
    queryKey: ["world-map", gameId],
    queryFn: () => getWorldMap(gameId as string),
    enabled: Boolean(gameId),
    staleTime: Infinity,
    retry: false,
  });

  const mapData = mapQuery.data ?? null;

  if (mapQuery.isLoading || !mapData) {
    return <Muted>Unfolding the map…</Muted>;
  }

  return (
    <PhaserMapHost
      mapData={mapData}
      selectedTownId={selectedDestId}
      onTownSelected={onSelectDestination}
      currentTownId={currentTownId}
      selectableTownIds={selectableTownIds}
    />
  );
}
