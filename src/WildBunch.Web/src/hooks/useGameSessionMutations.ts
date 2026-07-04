import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  acknowledgeTravelArrival,
  archiveGame,
  checkLocalRecords,
  confrontSaloonPersonOfInterest,
  followTelegraphLeads,
  gatherLocalGossip,
  inspectNoticeBoard,
  lookAroundSaloon,
  markPrologueViewed,
  readWantedPosters,
  setupGame,
  startGameWithTown,
  travel,
} from "../api/wildBunchApi";
import type {
  SetupGameRequest,
  WantedPosterDto,
} from "../api/types";
import { formatMoney, storageKey } from "../utils/formatting";
import { formatInvestigationNotice } from "../ui/beatFormatters";

type UseGameSessionMutationsArgs = {
  gameId: string | null;
  declaredWantedIdentityHandle: string;
  setStoredGameId: (id: string | null) => void;
  setWantedPosters: (posters: WantedPosterDto[]) => void;
  setDeclaredWantedIdentityHandle: (handle: string) => void;
  setNotice: (notice: string) => void;
  setError: (error: string) => void;
};

export function useGameSessionMutations({
  gameId,
  declaredWantedIdentityHandle,
  setStoredGameId,
  setWantedPosters,
  setDeclaredWantedIdentityHandle,
  setNotice,
  setError,
}: UseGameSessionMutationsArgs) {
  const queryClient = useQueryClient();

  const invalidateGameQueries = useCallback(
    (activeGameId: string) =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["session", activeGameId] }),
        queryClient.invalidateQueries({ queryKey: ["actions", activeGameId] }),
        queryClient.invalidateQueries({ queryKey: ["journal", activeGameId] }),
      ]),
    [queryClient],
  );

  const setupGameMutation = useMutation({
    mutationFn: (request: SetupGameRequest) => setupGame(request),
    onSuccess: async (createdSession) => {
      window.localStorage.setItem(storageKey, createdSession.id);
      setStoredGameId(createdSession.id);
      setNotice("");
      setError("");
      await invalidateGameQueries(createdSession.id);
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to complete setup.");
    },
  });

  const markPrologueViewedMutation = useMutation({
    mutationFn: (activeGameId: string) => markPrologueViewed(activeGameId),
    onSuccess: async (updatedSession) => {
      queryClient.setQueryData(["session", updatedSession.id], updatedSession);
      await invalidateGameQueries(updatedSession.id);
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to mark prologue as viewed.");
    },
  });

  const startGameWithTownMutation = useMutation({
    mutationFn: ({ activeGameId, townId }: { activeGameId: string; townId: string }) =>
      startGameWithTown(activeGameId, { startingTownId: townId }),
    onSuccess: async (updatedSession) => {
      queryClient.setQueryData(["session", updatedSession.id], updatedSession);
      await invalidateGameQueries(updatedSession.id);
      setNotice(`New game started for ${updatedSession.player.name}.`);
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to start the game.");
    },
  });

  const archivePlaythroughMutation = useMutation({
    mutationFn: () => archiveGame(gameId as string),
    onSuccess: async () => {
      const archivedGameId = gameId;
      window.localStorage.removeItem(storageKey);
      setStoredGameId(null);
      setWantedPosters([]);
      setDeclaredWantedIdentityHandle("");
      setError("");
      if (archivedGameId) {
        queryClient.removeQueries({ queryKey: ["session", archivedGameId] });
        queryClient.removeQueries({ queryKey: ["actions", archivedGameId] });
        queryClient.removeQueries({ queryKey: ["journal", archivedGameId] });
      }
      setNotice("Your old playthrough has been archived. Start a new one when you are ready.");
    },
    onError: (exception: unknown) => {
      // If the session is already archived (409 Conflict), clear local state
      // and return to setup instead of stranding the player on a dead session.
      const message = exception instanceof Error ? exception.message : "";
      if (message.includes("already archived") || message.includes("409")) {
        const archivedGameId = gameId;
        window.localStorage.removeItem(storageKey);
        setStoredGameId(null);
        setWantedPosters([]);
        setDeclaredWantedIdentityHandle("");
        setError("");
        if (archivedGameId) {
          queryClient.removeQueries({ queryKey: ["session", archivedGameId] });
          queryClient.removeQueries({ queryKey: ["actions", archivedGameId] });
          queryClient.removeQueries({ queryKey: ["journal", archivedGameId] });
        }
        setNotice("Your old playthrough has been archived. Start a new one when you are ready.");
      } else {
        setError(message || "Unable to archive the playthrough.");
      }
    },
  });

  const travelMutation = useMutation({
    mutationFn: (destinationTownId: string) => travel(gameId as string, destinationTownId),
    onSuccess: async (result) => {
      queryClient.setQueryData(["session", gameId], result.currentSession);
      await invalidateGameQueries(gameId as string);
      setNotice(result.message);
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to travel.");
    },
  });

  const acknowledgeArrivalMutation = useMutation({
    mutationFn: () => acknowledgeTravelArrival(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["session", gameId], result.currentSession);
      await invalidateGameQueries(gameId as string);
      setNotice(result.message);
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to acknowledge arrival.");
    },
  });

  const readWantedPostersMutation = useMutation({
    mutationFn: () => readWantedPosters(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["journal", gameId], result.currentJournal);
      setWantedPosters(result.wantedPosters);
      await invalidateGameQueries(gameId as string);
      setNotice(result.message);
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to read wanted posters.");
    },
  });

  const inspectNoticeBoardMutation = useMutation({
    mutationFn: () => inspectNoticeBoard(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["journal", gameId], result.currentJournal);
      await invalidateGameQueries(gameId as string);
      setNotice(formatInvestigationNotice(result.beatNarration ?? null, result.message));
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to inspect the notice board.");
    },
  });

  const checkLocalRecordsMutation = useMutation({
    mutationFn: () => checkLocalRecords(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["journal", gameId], result.currentJournal);
      await invalidateGameQueries(gameId as string);
      setNotice(formatInvestigationNotice(result.beatNarration ?? null, result.message));
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to check local records.");
    },
  });

  const followTelegraphLeadsMutation = useMutation({
    mutationFn: () => followTelegraphLeads(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["journal", gameId], result.currentJournal);
      await invalidateGameQueries(gameId as string);
      setNotice(formatInvestigationNotice(result.beatNarration ?? null, result.message));
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to follow telegraph leads.");
    },
  });

  const gatherLocalGossipMutation = useMutation({
    mutationFn: () => gatherLocalGossip(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["journal", gameId], result.currentJournal);
      await invalidateGameQueries(gameId as string);
      setNotice(result.message);
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to gather local gossip.");
    },
  });

  const lookAroundSaloonMutation = useMutation({
    mutationFn: () => lookAroundSaloon(gameId as string),
    onSuccess: async (result) => {
      queryClient.setQueryData(["journal", gameId], result.currentJournal);
      await invalidateGameQueries(gameId as string);
      setNotice(result.message);
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to look around the saloon.");
    },
  });

  const confrontSaloonMutation = useMutation({
    mutationFn: () => confrontSaloonPersonOfInterest(gameId as string, declaredWantedIdentityHandle),
    onSuccess: async (result) => {
      queryClient.setQueryData(["session", gameId], result.currentSession);
      await invalidateGameQueries(gameId as string);
      const noticeText =
        result.fineAmount !== null && result.walletBefore !== null && result.walletAfter !== null
          ? `${result.message} Wallet ${formatMoney(result.walletBefore)} -> ${formatMoney(result.walletAfter)}.`
          : result.message;
      setNotice(noticeText);
      setError("");
    },
    onError: (exception: unknown) => {
      setError(exception instanceof Error ? exception.message : "Unable to confront the person in the saloon.");
    },
  });

  return {
    setupGameMutation,
    markPrologueViewedMutation,
    startGameWithTownMutation,
    archivePlaythroughMutation,
    travelMutation,
    acknowledgeArrivalMutation,
    readWantedPostersMutation,
    inspectNoticeBoardMutation,
    checkLocalRecordsMutation,
    followTelegraphLeadsMutation,
    gatherLocalGossipMutation,
    lookAroundSaloonMutation,
    confrontSaloonMutation,
    invalidateGameQueries,
  };
}
