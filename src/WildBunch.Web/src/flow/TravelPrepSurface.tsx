import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import styled from "styled-components";
import type { GameSessionDto, GameTurnResultDto, TownDto } from "../api/types";
import { StartFlowPhase, TravelMode } from "../api/types";
import { getWorldMap, travel } from "../api/wildBunchApi";
import { useGameSession } from "../state/useGameSession";
import { gameSessionQueryKey } from "../state/gameSessionQuery";
import { refreshGameResources } from "../state/gameResourceQueries";
import { travelPreviewQueryOptions } from "../state/travelPreviewQuery";
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

interface ConnectedDestination {
  town: TownDto;
  trailCount: number;
}

interface DestinationSelection {
  gameId: string;
  originTownId: string;
  destinationTownId: string;
}

function connectedDestinations(session: GameSessionDto) {
  const currentTownId = session.player.currentTownId;
  if (currentTownId === null) {
    return [];
  }
  const townMap = new Map(session.world.towns.map((town) => [town.id, town]));
  const destinations = new Map<string, ConnectedDestination>();

  for (const trail of session.world.trails) {
    if (trail.fromTownId !== currentTownId && trail.toTownId !== currentTownId) {
      continue;
    }

    const destinationTownId =
      trail.fromTownId === currentTownId ? trail.toTownId : trail.fromTownId;
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
  const queryClient = useQueryClient();
  const { session, gameId, sessionUpdatedAt, loading } = useGameSession();
  const [selection, setSelection] = useState<DestinationSelection | null>(null);
  const departureMutation = useMutation<
    GameTurnResultDto,
    Error,
    { gameId: string; destinationTownId: string }
  >({
    mutationFn: ({ gameId: submittedGameId, destinationTownId }) =>
      travel(submittedGameId, destinationTownId),
    onSuccess: async (result, variables) => {
      const updatedGameId = variables.gameId;
      queryClient.setQueryData(gameSessionQueryKey(updatedGameId), result.currentSession);
      await refreshGameResources(queryClient, updatedGameId);
    },
  });
  const departureBelongsToCurrentGame = departureMutation.variables?.gameId === gameId;
  const departurePendingForCurrentGame =
    departureMutation.isPending && departureBelongsToCurrentGame;

  const destinations = useMemo(() => (session ? connectedDestinations(session) : []), [session]);
  const currentTownId = session?.player.currentTownId ?? null;
  const selectionMatchesCurrentContext = Boolean(
    selection &&
    selection.gameId === gameId &&
    selection.originTownId === currentTownId &&
    destinations.some((destination) => destination.town.id === selection.destinationTownId),
  );
  const selectedDestinationId = selectionMatchesCurrentContext
    ? (selection?.destinationTownId ?? null)
    : null;

  const previewQuery = useQuery(
    travelPreviewQueryOptions(
      selectedDestinationId ? gameId : null,
      selectedDestinationId ? currentTownId : null,
      selectedDestinationId,
      sessionUpdatedAt,
    ),
  );

  useEffect(() => {
    if (selection && !selectionMatchesCurrentContext) {
      setSelection(null);
    }
  }, [selection, selectionMatchesCurrentContext]);

  if (
    !session ||
    session.startFlowPhase !== StartFlowPhase.GameStarted ||
    session.player.currentTownId === null
  ) {
    return null;
  }
  const activeCurrentTownId = session.player.currentTownId;

  const previewResult = previewQuery.data ?? null;
  const preview = previewResult?.success ? previewResult.preview : null;
  const previewMatchesCurrentSelection = Boolean(
    selectionMatchesCurrentContext &&
    preview &&
    preview.originTownId === currentTownId &&
    preview.destinationTownId === selectedDestinationId,
  );

  // Keep the selected ride context visible while its current preview loads or recovers.
  if (selectionMatchesCurrentContext && selectedDestinationId) {
    const destination = destinations.find((item) => item.town.id === selectedDestinationId);
    const previewError =
      previewResult && (!previewResult.success || !previewResult.preview)
        ? previewResult.message || "This ride could not be previewed."
        : null;
    const mismatchedPreview = Boolean(preview && !previewMatchesCurrentSelection);

    return (
      <FlowSurface $variant="travel-prep">
        <PlaceHeader>
          <BackButton type="button" onClick={() => setSelection(null)}>
            ← Pick another destination
          </BackButton>
          <h1>Prepare to ride</h1>
        </PlaceHeader>
        <TravelPrepBody>
          <Panel>
            <PanelHead>
              <h2>{destination?.town.name ?? selectedDestinationId}</h2>
            </PanelHead>
            <Stack>
              {previewMatchesCurrentSelection && preview ? (
                <>
                  <TravelPrepRide>
                    That's a <strong>{preview.baselineRideDays}-day ride</strong>
                    {preview.travelMode === TravelMode.Mounted ? " on horseback" : " on foot"}.
                  </TravelPrepRide>
                  <InventoryPanel inventory={session.inventory} />
                  <TravelPrepActions>
                    <Button
                      type="button"
                      $variant="secondary"
                      onClick={() => setSelection(null)}
                      disabled={loading}
                    >
                      Back to town
                    </Button>
                    <Button
                      type="button"
                      $variant="primary"
                      onClick={() => {
                        if (gameId) {
                          departureMutation.mutate({
                            gameId,
                            destinationTownId: selectedDestinationId,
                          });
                        }
                      }}
                      disabled={loading || departurePendingForCurrentGame}
                    >
                      {departurePendingForCurrentGame ? "Setting out..." : "Start the ride"}
                    </Button>
                  </TravelPrepActions>
                </>
              ) : previewQuery.isError ? (
                <>
                  <FlowError>
                    {previewQuery.error instanceof Error
                      ? previewQuery.error.message
                      : "Unable to calculate this ride."}
                  </FlowError>
                  <TravelPrepActions>
                    <Button type="button" $variant="secondary" onClick={() => setSelection(null)}>
                      Pick another destination
                    </Button>
                    <Button
                      type="button"
                      $variant="primary"
                      onClick={() => void previewQuery.refetch()}
                    >
                      Try again
                    </Button>
                  </TravelPrepActions>
                </>
              ) : previewError || mismatchedPreview ? (
                <>
                  <FlowError>
                    {previewError ??
                      "The ride preview no longer matches this route. Pick a destination again."}
                  </FlowError>
                  <Button type="button" $variant="secondary" onClick={() => setSelection(null)}>
                    Pick another destination
                  </Button>
                </>
              ) : (
                <>
                  <Muted>Calculating the ride...</Muted>
                  <Button type="button" $variant="secondary" onClick={() => setSelection(null)}>
                    Pick another destination
                  </Button>
                </>
              )}
            </Stack>
          </Panel>
        </TravelPrepBody>
        {departureBelongsToCurrentGame && departureMutation.data?.message ? (
          <FlowNotice>{departureMutation.data.message}</FlowNotice>
        ) : null}
        {departureBelongsToCurrentGame && departureMutation.error ? (
          <FlowError>{departureMutation.error.message}</FlowError>
        ) : null}
      </FlowSurface>
    );
  }

  // Destination selection screen — visual map
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
              currentTownId={activeCurrentTownId}
              selectableTownIds={selectableTownIds}
              selectedDestId={selectedDestinationId}
              onSelectDestination={(townId) =>
                setSelection({
                  gameId: gameId ?? session.id,
                  originTownId: activeCurrentTownId,
                  destinationTownId: townId,
                })
              }
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
