import { useCallback, useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  GameStatus,
  type GameSessionDto,
  type GameTurnResultDto,
  type SetupGameRequest,
} from "../api/types";
import {
  actionIsGatherLocalGossip,
  actionIsLookAroundSaloon,
  actionIsWantedPosters,
} from "../utils/actionTypePredicates";
import { HttpRequestError } from "../api/httpClient";
import { storageKey } from "../utils/formatting";
import {
  readPendingFirstArrivalOperation,
  readPendingSetupOperation,
  writePendingFirstArrivalOperation,
  writePendingSetupOperation,
} from "../utils/startupOperationStorage";
import { useGameSessionMutations } from "./useGameSessionMutations";
import { useGameSessionQueries } from "./useGameSessionQueries";
import { useGameSessionState } from "./useGameSessionState";
import { gameSessionQueryKey } from "../state/gameSessionQuery";

type BusyMode =
  "idle" | "booting" | "starting" | "refreshing" | "traveling" | "reading" | "investigating";

export function useCurrentGameSession() {
  const queryClient = useQueryClient();

  const state = useGameSessionState();
  const {
    setDeclaredWantedIdentityHandle,
    setError,
    setNotice,
    setResetToken,
    setStoredGameId,
    setPendingSetup,
    setWantedPosters,
  } = state;
  const gameId = state.storedGameId;
  const resetSetupDraft = useCallback(
    () => setResetToken((current) => current + 1),
    [setResetToken],
  );

  const {
    sessionQuery,
    actionsQuery,
    journalQuery,
    session,
    journal,
    actions,
    currentTown,
    cockpitMode,
  } = useGameSessionQueries(gameId);

  // Load wanted posters from session state once they're known.
  // The readWantedPosters API call adds them to KnownWarrants and the journal,
  // but we should use the session DTO to avoid requiring a separate API call
  // every time we want to declare an identity.
  useEffect(() => {
    const posters = sessionQuery.data?.wantedPosters;
    setWantedPosters(posters ?? []);
  }, [sessionQuery.data?.wantedPosters, setWantedPosters]);

  // Detect an event-backed archive from confirmed Start Over and return to setup.
  useEffect(() => {
    if (sessionQuery.data?.status === GameStatus.Archived) {
      window.localStorage.removeItem(storageKey);
      state.setStoredGameId(null);
      state.setWantedPosters([]);
      state.setDeclaredWantedIdentityHandle("");
      queryClient.removeQueries({ queryKey: gameSessionQueryKey(gameId) });
      queryClient.removeQueries({ queryKey: ["actions", gameId] });
      queryClient.removeQueries({ queryKey: ["journal", gameId] });
    }
  }, [sessionQuery.data?.status, gameId, queryClient, state]);

  const {
    setupGameMutation,
    markPrologueViewedMutation,
    startGameWithTownMutation,
    archivePlaythroughMutation,
    travelMutation,
    acknowledgeArrivalMutation,
    readWantedPostersMutation,
    gatherLocalGossipMutation,
    lookAroundSaloonMutation,
    confrontSaloonMutation,
    invalidateGameQueries,
  } = useGameSessionMutations({
    gameId,
    declaredWantedIdentityHandle: state.declaredWantedIdentityHandle,
    setStoredGameId: state.setStoredGameId,
    resetSetupDraft,
    setPendingSetup: state.setPendingSetup,
    setWantedPosters: state.setWantedPosters,
    setDeclaredWantedIdentityHandle: state.setDeclaredWantedIdentityHandle,
    setNotice: state.setNotice,
    setError: state.setError,
  });

  const setupRetryStarted = useRef(false);
  useEffect(() => {
    if (!gameId && state.pendingSetup && !setupRetryStarted.current) {
      setupRetryStarted.current = true;
      void setupGameMutation.mutateAsync(state.pendingSetup).catch(() => undefined);
    }
  }, [gameId, state.pendingSetup, setupGameMutation]);

  const investigationPending =
    gatherLocalGossipMutation.isPending ||
    lookAroundSaloonMutation.isPending ||
    confrontSaloonMutation.isPending;

  const busyMode: BusyMode = travelMutation.isPending
    ? "traveling"
    : readWantedPostersMutation.isPending
      ? "reading"
      : investigationPending
        ? "investigating"
        : sessionQuery.isFetching || actionsQuery.isFetching || journalQuery.isFetching
          ? "refreshing"
          : setupGameMutation.isPending || startGameWithTownMutation.isPending
            ? "starting"
            : "idle";

  const loading = busyMode !== "idle";

  const canReadWantedPosters = actions.some(actionIsWantedPosters);
  const canGatherLocalGossip = actions.some(actionIsGatherLocalGossip);
  const canLookAroundSaloon = actions.some(actionIsLookAroundSaloon);
  const canConfrontSaloonPersonOfInterest = Boolean(
    session?.activeSaloonPersonOfInterest && state.declaredWantedIdentityHandle,
  );

  const handleSetupGame = useCallback(
    async (request: SetupGameRequest) => {
      const pending = state.pendingSetup ?? readPendingSetupOperation();
      const operation = pending ?? {
        operationId: crypto.randomUUID(),
        request,
      };
      if (!pending) {
        setupRetryStarted.current = true;
        writePendingSetupOperation(operation);
        setPendingSetup(operation);
      }
      await setupGameMutation.mutateAsync(operation);
    },
    [setupGameMutation, state.pendingSetup, setPendingSetup],
  );

  const handleMarkPrologueViewed = useCallback(async () => {
    if (!gameId) {
      return;
    }
    await markPrologueViewedMutation.mutateAsync(gameId);
  }, [gameId, markPrologueViewedMutation]);

  const handleStartGameWithTown = useCallback(
    async (townId: string) => {
      if (!gameId) {
        return;
      }
      const pending = readPendingFirstArrivalOperation(gameId);
      if (pending && pending.townId !== townId) {
        setError(
          "A starting-town arrival is already pending. Retry the selected town to resolve it.",
        );
        return;
      }
      const operation = pending ?? {
        operationId: crypto.randomUUID(),
        sessionId: gameId,
        townId,
      };
      if (!pending) writePendingFirstArrivalOperation(operation);
      await startGameWithTownMutation.mutateAsync({
        activeGameId: gameId,
        townId: operation.townId,
        operationId: operation.operationId,
      });
    },
    [gameId, setError, startGameWithTownMutation],
  );

  const reloadCurrentGame = useCallback(
    async (activeGameId: string | null = gameId) => {
      if (!activeGameId) {
        return;
      }
      await invalidateGameQueries(activeGameId);
    },
    [gameId, invalidateGameQueries],
  );

  const handleTravelTurnResult = useCallback(
    async (result: GameTurnResultDto) => {
      const activeGameId = result.currentSession.id;
      queryClient.setQueryData(gameSessionQueryKey(activeGameId), result.currentSession);
      await invalidateGameQueries(activeGameId);
      setNotice(result.message);
    },
    [queryClient, invalidateGameQueries, setNotice],
  );

  const handleTravel = useCallback(
    async (destinationTownId: string) => {
      if (!gameId) {
        return;
      }
      await travelMutation.mutateAsync(destinationTownId);
    },
    [gameId, travelMutation],
  );

  const handleAcknowledgeArrival = useCallback(async () => {
    if (!gameId) {
      return;
    }
    await acknowledgeArrivalMutation.mutateAsync();
  }, [gameId, acknowledgeArrivalMutation]);

  const handleReadWantedPosters = useCallback(async () => {
    if (!gameId || !canReadWantedPosters) {
      return;
    }
    await readWantedPostersMutation.mutateAsync();
  }, [gameId, canReadWantedPosters, readWantedPostersMutation]);

  const handleGatherLocalGossip = useCallback(async () => {
    if (!gameId || !canGatherLocalGossip) {
      return;
    }
    await gatherLocalGossipMutation.mutateAsync();
  }, [gameId, canGatherLocalGossip, gatherLocalGossipMutation]);

  const handleLookAroundSaloon = useCallback(async () => {
    if (!gameId || !canLookAroundSaloon) {
      return;
    }
    await lookAroundSaloonMutation.mutateAsync();
  }, [gameId, canLookAroundSaloon, lookAroundSaloonMutation]);

  const handleConfrontSaloonPersonOfInterest = useCallback(async () => {
    if (!gameId || !canConfrontSaloonPersonOfInterest) {
      return;
    }
    await confrontSaloonMutation.mutateAsync();
  }, [gameId, canConfrontSaloonPersonOfInterest, confrontSaloonMutation]);

  const handleReset = useCallback(() => {
    window.localStorage.removeItem(storageKey);
    setStoredGameId(null);
    setWantedPosters([]);
    setDeclaredWantedIdentityHandle("");
    setNotice("");
    setError("");
    setResetToken((current) => current + 1);
    if (gameId) {
      queryClient.removeQueries({ queryKey: gameSessionQueryKey(gameId) });
      queryClient.removeQueries({ queryKey: ["actions", gameId] });
      queryClient.removeQueries({ queryKey: ["journal", gameId] });
    }
  }, [
    gameId,
    queryClient,
    setStoredGameId,
    setWantedPosters,
    setDeclaredWantedIdentityHandle,
    setNotice,
    setError,
    setResetToken,
  ]);

  const archivePlaythrough = useCallback(async () => {
    if (!gameId) {
      return;
    }
    await archivePlaythroughMutation.mutateAsync();
  }, [gameId, archivePlaythroughMutation]);

  const archiving = archivePlaythroughMutation.isPending;

  const sessionRestoreStatus = !gameId
    ? "none"
    : sessionQuery.isLoading || sessionQuery.data?.status === GameStatus.Archived
      ? "loading"
      : sessionQuery.isError
        ? sessionQuery.error instanceof HttpRequestError && sessionQuery.error.status === 404
          ? "missing"
          : sessionQuery.data === undefined
            ? "failed"
            : "restored"
        : "restored";

  const retrySessionRestore = useCallback(async () => {
    await sessionQuery.refetch();
  }, [sessionQuery]);

  const abandonMissingSession = useCallback(() => {
    if (sessionRestoreStatus === "missing") {
      handleReset();
    }
  }, [sessionRestoreStatus, handleReset]);

  const setSession = useCallback(
    (next: GameSessionDto | null) => {
      if (gameId) {
        if (next) {
          queryClient.setQueryData(gameSessionQueryKey(gameId), next);
        } else {
          queryClient.removeQueries({ queryKey: gameSessionQueryKey(gameId) });
        }
      }
    },
    [gameId, queryClient],
  );

  return {
    session,
    pendingSetup: state.pendingSetup,
    journal,
    wantedPosters: state.wantedPosters,
    hasReadWantedPosters: state.wantedPosters.length > 0,
    actions,
    gameId,
    currentTown,
    cockpitMode,
    busyMode,
    loading,
    sessionLoading: sessionQuery.isLoading,
    sessionRestoreStatus,
    retrySessionRestore,
    abandonMissingSession,
    notice: state.notice,
    error: state.error,
    resetToken: state.resetToken,
    canReadWantedPosters,
    canGatherLocalGossip,
    canLookAroundSaloon,
    canConfrontSaloonPersonOfInterest,
    declaredWantedIdentityHandle: state.declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle: state.setDeclaredWantedIdentityHandle,
    handleSetupGame,
    handleMarkPrologueViewed,
    handleStartGameWithTown,
    reloadCurrentGame,
    handleTravelTurnResult,
    handleTravel,
    handleAcknowledgeArrival,
    handleReadWantedPosters,
    handleGatherLocalGossip,
    handleLookAroundSaloon,
    handleConfrontSaloonPersonOfInterest,
    handleReset,
    setSession,
    setNotice: state.setNotice,
    setError: state.setError,
    archivePlaythrough,
    archiving,
  };
}
