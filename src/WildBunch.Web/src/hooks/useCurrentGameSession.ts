import { useCallback, useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { GameStatus, type GameSessionDto, type SetupGameRequest } from "../api/types";
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
import { gameResourceQueryKeys } from "../state/gameResourceQueries";

const EMPTY_WANTED_POSTERS: NonNullable<GameSessionDto["wantedPosters"]> = [];

export function useCurrentGameSession() {
  const queryClient = useQueryClient();

  const state = useGameSessionState();
  const { setError, setNotice, setResetToken, setStoredGameId, setPendingSetup } = state;
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
    journalLoading,
    journalRefreshing,
    journalError,
    retryJournal,
    actions,
    actionsLoading,
    actionsError,
    retryActions,
    currentTown,
    cockpitMode,
  } = useGameSessionQueries(gameId);

  const wantedPosters = session?.wantedPosters ?? EMPTY_WANTED_POSTERS;

  // Detect an event-backed archive from confirmed Start Over and return to setup.
  useEffect(() => {
    if (sessionQuery.data?.status === GameStatus.Archived) {
      window.localStorage.removeItem(storageKey);
      state.setStoredGameId(null);
      queryClient.removeQueries({ queryKey: gameSessionQueryKey(gameId) });
      queryClient.removeQueries({ queryKey: gameResourceQueryKeys.actions(gameId) });
      queryClient.removeQueries({ queryKey: gameResourceQueryKeys.journal(gameId) });
    }
  }, [sessionQuery.data?.status, gameId, queryClient, state]);

  const {
    setupGameMutation,
    markPrologueViewedMutation,
    startGameWithTownMutation,
    archivePlaythroughMutation,
    invalidateGameQueries,
  } = useGameSessionMutations({
    gameId,
    setStoredGameId: state.setStoredGameId,
    resetSetupDraft,
    setPendingSetup: state.setPendingSetup,
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

  const loading =
    sessionQuery.isFetching ||
    actionsQuery.isFetching ||
    journalQuery.isFetching ||
    setupGameMutation.isPending ||
    startGameWithTownMutation.isPending;

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

  const handleReset = useCallback(() => {
    window.localStorage.removeItem(storageKey);
    setStoredGameId(null);
    setNotice("");
    setError("");
    setResetToken((current) => current + 1);
    if (gameId) {
      queryClient.removeQueries({ queryKey: gameSessionQueryKey(gameId) });
      queryClient.removeQueries({ queryKey: gameResourceQueryKeys.actions(gameId) });
      queryClient.removeQueries({ queryKey: gameResourceQueryKeys.journal(gameId) });
    }
  }, [gameId, queryClient, setStoredGameId, setNotice, setError, setResetToken]);

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

  return {
    session,
    pendingSetup: state.pendingSetup,
    journal,
    journalLoading,
    journalRefreshing,
    journalError,
    retryJournal,
    wantedPosters,
    actions,
    actionsLoading,
    actionsError,
    retryActions,
    gameId,
    currentTown,
    cockpitMode,
    loading,
    sessionLoading: sessionQuery.isLoading,
    sessionUpdatedAt: sessionQuery.dataUpdatedAt,
    sessionRestoreStatus,
    retrySessionRestore,
    abandonMissingSession,
    notice: state.notice,
    error: state.error,
    resetToken: state.resetToken,
    handleSetupGame,
    handleMarkPrologueViewed,
    handleStartGameWithTown,
    reloadCurrentGame,
    handleReset,
    setNotice: state.setNotice,
    setError: state.setError,
    archivePlaythrough,
    archiving,
  };
}
