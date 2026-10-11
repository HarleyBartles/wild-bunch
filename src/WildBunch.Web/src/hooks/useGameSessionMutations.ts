import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  archiveGame,
  getGame,
  markPrologueViewed,
  setupGame,
  startGameWithTown,
} from "../api/wildBunchApi";
import { GameStatus } from "../api/types";
import { storageKey } from "../utils/formatting";
import { HttpRequestError } from "../api/httpClient";
import {
  clearPendingFirstArrivalOperation,
  clearPendingSetupOperation,
  type PendingSetupOperation,
} from "../utils/startupOperationStorage";
import { gameSessionQueryKey } from "../state/gameSessionQuery";
import { refreshGameResources } from "../state/gameResourceQueries";
import { gameResourceQueryKeys } from "../state/gameResourceQueries";

type UseGameSessionMutationsArgs = {
  gameId: string | null;
  setStoredGameId: (id: string | null) => void;
  resetSetupDraft: () => void;
  setPendingSetup: (pending: PendingSetupOperation | null) => void;
  setNotice: (notice: string) => void;
  setError: (error: string) => void;
};

export function useGameSessionMutations({
  gameId,
  setStoredGameId,
  resetSetupDraft,
  setPendingSetup,
  setNotice,
  setError,
}: UseGameSessionMutationsArgs) {
  const queryClient = useQueryClient();

  const invalidateGameQueries = useCallback(
    (activeGameId: string) => refreshGameResources(queryClient, activeGameId),
    [queryClient],
  );

  const setupGameMutation = useMutation({
    mutationFn: (operation: PendingSetupOperation) =>
      setupGame(operation.request, operation.operationId),
    onSuccess: async (createdSession) => {
      clearPendingSetupOperation();
      setPendingSetup(null);
      window.localStorage.setItem(storageKey, createdSession.id);
      setStoredGameId(createdSession.id);
      setNotice("");
      setError("");
      await invalidateGameQueries(createdSession.id);
    },
    onError: (exception: unknown) => {
      if (
        exception instanceof HttpRequestError &&
        exception.status >= 400 &&
        exception.status < 500
      ) {
        clearPendingSetupOperation();
        setPendingSetup(null);
      }
      setError(exception instanceof Error ? exception.message : "Unable to complete setup.");
    },
  });

  const markPrologueViewedMutation = useMutation({
    mutationFn: (activeGameId: string) => markPrologueViewed(activeGameId),
    onSuccess: async (updatedSession) => {
      queryClient.setQueryData(gameSessionQueryKey(updatedSession.id), updatedSession);
      await invalidateGameQueries(updatedSession.id);
    },
    onError: (exception: unknown) => {
      setError(
        exception instanceof Error ? exception.message : "Unable to mark prologue as viewed.",
      );
    },
  });

  const startGameWithTownMutation = useMutation({
    mutationFn: async ({
      activeGameId,
      townId,
      operationId,
    }: {
      activeGameId: string;
      townId: string;
      operationId: string;
    }) => {
      try {
        return await startGameWithTown(activeGameId, { startingTownId: townId }, operationId);
      } catch (exception) {
        if (
          exception instanceof HttpRequestError &&
          exception.status >= 400 &&
          exception.status < 500
        ) {
          clearPendingFirstArrivalOperation(activeGameId);
        }
        throw exception;
      }
    },
    onSuccess: async (updatedSession) => {
      clearPendingFirstArrivalOperation(updatedSession.id);
      queryClient.setQueryData(gameSessionQueryKey(updatedSession.id), updatedSession);
      await invalidateGameQueries(updatedSession.id);
      setNotice(`New game started for ${updatedSession.player.name}.`);
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to start the game.");
    },
  });

  const archivePlaythroughMutation = useMutation({
    mutationFn: async () => {
      const archivedGameId = gameId as string;
      try {
        await archiveGame(archivedGameId);
      } catch {
        let currentSession;
        try {
          currentSession = await getGame(archivedGameId);
        } catch {
          throw new Error(
            "Unable to confirm whether this playthrough was archived. Your saved game remains available. Try Start Over again.",
          );
        }

        if (currentSession?.status !== GameStatus.Archived) {
          throw new Error(
            "The playthrough is still active. Start Over did not complete. Try again.",
          );
        }
      }
    },
    onMutate: () => {
      setError("");
    },
    onSuccess: async () => {
      const archivedGameId = gameId;
      window.localStorage.removeItem(storageKey);
      setStoredGameId(null);
      resetSetupDraft();
      setError("");
      if (archivedGameId) {
        queryClient.removeQueries({ queryKey: gameSessionQueryKey(archivedGameId) });
        queryClient.removeQueries({ queryKey: gameResourceQueryKeys.actions(archivedGameId) });
        queryClient.removeQueries({ queryKey: gameResourceQueryKeys.journal(archivedGameId) });
      }
      setNotice("Your old playthrough has been archived. Start a new one when you are ready.");
    },
    onError: (exception: unknown) => {
      const message = exception instanceof Error ? exception.message : "";
      setError(message || "Unable to archive the playthrough.");
    },
  });

  return {
    setupGameMutation,
    markPrologueViewedMutation,
    startGameWithTownMutation,
    archivePlaythroughMutation,
    invalidateGameQueries,
  };
}
