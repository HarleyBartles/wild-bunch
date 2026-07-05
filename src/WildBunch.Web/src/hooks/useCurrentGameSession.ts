import { useCallback, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  GameStatus,
  type GameSessionDto,
  type GameTurnResultDto,
  type SetupGameRequest,
} from "../api/types";
import {
  actionIsCheckLocalRecords,
  actionIsFollowTelegraphLeads,
  actionIsGatherLocalGossip,
  actionIsInspectNoticeBoard,
  actionIsLookAroundSaloon,
  actionIsWantedPosters,
} from "../utils/actionTypePredicates";
import { storageKey } from "../utils/formatting";
import { useGameSessionMutations } from "./useGameSessionMutations";
import { useGameSessionQueries } from "./useGameSessionQueries";
import { useGameSessionState } from "./useGameSessionState";

type BusyMode = "idle" | "booting" | "starting" | "refreshing" | "traveling" | "reading" | "investigating";

export function useCurrentGameSession() {
  const queryClient = useQueryClient();

  const state = useGameSessionState();
  const gameId = state.storedGameId;

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
  const setWantedPosters = state.setWantedPosters;
  useEffect(() => {
    const posters = sessionQuery.data?.wantedPosters;
    setWantedPosters(posters ?? []);
  }, [sessionQuery.data?.wantedPosters, setWantedPosters]);

  // Detect archived sessions and clear local state so the player returns to
  // the setup screen. A session can be archived by the player (Start Over) or
  // by the backend (superseded by a new playthrough). Without this, the
  // frontend keeps showing the archived session as playable, and the archive
  // endpoint returns 409 on repeated attempts.
  useEffect(() => {
    if (sessionQuery.data?.status === GameStatus.Archived) {
      window.localStorage.removeItem(storageKey);
      state.setStoredGameId(null);
      state.setWantedPosters([]);
      state.setDeclaredWantedIdentityHandle("");
      queryClient.removeQueries({ queryKey: ["session", gameId] });
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
    inspectNoticeBoardMutation,
    checkLocalRecordsMutation,
    followTelegraphLeadsMutation,
    gatherLocalGossipMutation,
    lookAroundSaloonMutation,
    confrontSaloonMutation,
    invalidateGameQueries,
  } = useGameSessionMutations({
    gameId,
    declaredWantedIdentityHandle: state.declaredWantedIdentityHandle,
    setStoredGameId: state.setStoredGameId,
    setWantedPosters: state.setWantedPosters,
    setDeclaredWantedIdentityHandle: state.setDeclaredWantedIdentityHandle,
    setNotice: state.setNotice,
    setError: state.setError,
  });

  const investigationPending =
    inspectNoticeBoardMutation.isPending ||
    checkLocalRecordsMutation.isPending ||
    followTelegraphLeadsMutation.isPending ||
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
          : "idle";

  const loading = busyMode !== "idle";

  const canReadWantedPosters = actions.some(actionIsWantedPosters);
  const canInspectNoticeBoard = actions.some(actionIsInspectNoticeBoard);
  const canCheckLocalRecords = actions.some(actionIsCheckLocalRecords);
  const canFollowTelegraphLeads = actions.some(actionIsFollowTelegraphLeads);
  const canGatherLocalGossip = actions.some(actionIsGatherLocalGossip);
  const canLookAroundSaloon = actions.some(actionIsLookAroundSaloon);
  const canConfrontSaloonPersonOfInterest = Boolean(session?.activeSaloonPersonOfInterest && state.declaredWantedIdentityHandle);

  const handleSetupGame = useCallback(
    async (request: SetupGameRequest) => {
      await setupGameMutation.mutateAsync(request);
    },
    [setupGameMutation],
  );

  const handleMarkPrologueViewed = useCallback(
    async () => {
      if (!gameId) {
        return;
      }
      await markPrologueViewedMutation.mutateAsync(gameId);
    },
    [gameId, markPrologueViewedMutation],
  );

  const handleStartGameWithTown = useCallback(
    async (townId: string) => {
      if (!gameId) {
        return;
      }
      await startGameWithTownMutation.mutateAsync({ activeGameId: gameId, townId });
    },
    [gameId, startGameWithTownMutation],
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
      queryClient.setQueryData(["session", activeGameId], result.currentSession);
      await invalidateGameQueries(activeGameId);
      state.setNotice(result.message);
    },
    [queryClient, invalidateGameQueries, state.setNotice],
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

  const handleInspectNoticeBoard = useCallback(async () => {
    if (!gameId || !canInspectNoticeBoard) {
      return;
    }
    await inspectNoticeBoardMutation.mutateAsync();
  }, [gameId, canInspectNoticeBoard, inspectNoticeBoardMutation]);

  const handleCheckLocalRecords = useCallback(async () => {
    if (!gameId || !canCheckLocalRecords) {
      return;
    }
    await checkLocalRecordsMutation.mutateAsync();
  }, [gameId, canCheckLocalRecords, checkLocalRecordsMutation]);

  const handleFollowTelegraphLeads = useCallback(async () => {
    if (!gameId || !canFollowTelegraphLeads) {
      return;
    }
    await followTelegraphLeadsMutation.mutateAsync();
  }, [gameId, canFollowTelegraphLeads, followTelegraphLeadsMutation]);

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
    state.setStoredGameId(null);
    state.setWantedPosters([]);
    state.setDeclaredWantedIdentityHandle("");
    state.setNotice("");
    state.setError("");
    state.setResetToken((current) => current + 1);
    if (gameId) {
      queryClient.removeQueries({ queryKey: ["session", gameId] });
      queryClient.removeQueries({ queryKey: ["actions", gameId] });
      queryClient.removeQueries({ queryKey: ["journal", gameId] });
    }
  }, [
    gameId,
    queryClient,
    state.setStoredGameId,
    state.setWantedPosters,
    state.setDeclaredWantedIdentityHandle,
    state.setNotice,
    state.setError,
    state.setResetToken,
  ]);

  const archivePlaythrough = useCallback(async () => {
    if (!gameId) {
      return;
    }
    await archivePlaythroughMutation.mutateAsync();
  }, [gameId, archivePlaythroughMutation]);

  const archiving = archivePlaythroughMutation.isPending;

  const setSession = useCallback(
    (next: GameSessionDto | null) => {
      if (gameId) {
        if (next) {
          queryClient.setQueryData(["session", gameId], next);
        } else {
          queryClient.removeQueries({ queryKey: ["session", gameId] });
        }
      }
    },
    [gameId, queryClient],
  );

  return {
    session,
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
    notice: state.notice,
    error: state.error,
    resetToken: state.resetToken,
    canReadWantedPosters,
    canInspectNoticeBoard,
    canCheckLocalRecords,
    canFollowTelegraphLeads,
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
    handleInspectNoticeBoard,
    handleCheckLocalRecords,
    handleFollowTelegraphLeads,
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
