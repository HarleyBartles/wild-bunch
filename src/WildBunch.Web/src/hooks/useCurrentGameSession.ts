import { useCallback, useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acknowledgeTravelArrival,
  archiveGame,
  checkLocalRecords,
  followTelegraphLeads,
  getAvailableActions,
  getGame,
  getJournal,
  inspectNoticeBoard,
  gatherLocalGossip,
  confrontSaloonPersonOfInterest,
  lookAroundSaloon,
  markPrologueViewed,
  readWantedPosters,
  setupGame,
  startGameWithTown,
  travel,
} from "../api/wildBunchApi";
import { AvailableActionKind } from "../api/types";
import type {
  AvailableActionDto,
  GameSessionDto,
  GameTurnResultDto,
  JournalDto,
  SetupGameRequest,
  WantedPosterDto,
} from "../api/types";

const storageKey = "wild-bunch.current-game-id";

type BusyMode = "idle" | "booting" | "starting" | "refreshing" | "traveling" | "reading" | "investigating";
type CockpitMode = "home" | "travel";

function actionIsWantedPosters(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.ReadWantedPosters;
}

function actionIsInspectNoticeBoard(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.InspectNoticeBoard;
}

function actionIsCheckLocalRecords(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.CheckSheriffRecords;
}

function actionIsFollowTelegraphLeads(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.FollowTelegraphLeads;
}

function actionIsGatherLocalGossip(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.GatherLocalGossip;
}

function actionIsLookAroundSaloon(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.LookAroundSaloon;
}

function formatMoney(value: number) {
  return `$${value.toFixed(2)}`;
}

function readStoredGameId() {
  return window.localStorage.getItem(storageKey);
}

export function useCurrentGameSession() {
  const queryClient = useQueryClient();
  const [storedGameId, setStoredGameId] = useState<string | null>(readStoredGameId);
  const [wantedPosters, setWantedPosters] = useState<WantedPosterDto[]>([]);
  const [declaredWantedIdentityHandle, setDeclaredWantedIdentityHandle] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [resetToken, setResetToken] = useState(0);

  const gameId = storedGameId;

  const sessionQuery = useQuery({
    queryKey: ["session", gameId],
    queryFn: () => getGame(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  // Load wanted posters from session state once they're known.
  // The readWantedPosters API call adds them to KnownWarrants and the journal,
  // but we should use the session DTO to avoid requiring a separate API call
  // every time we want to declare an identity.
  useEffect(() => {
    const posters = sessionQuery.data?.wantedPosters;
    setWantedPosters(posters ?? []);
  }, [sessionQuery.data?.wantedPosters]);

  const actionsQuery = useQuery({
    queryKey: ["actions", gameId],
    queryFn: () => getAvailableActions(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const journalQuery = useQuery({
    queryKey: ["journal", gameId],
    queryFn: () => getJournal(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const session = sessionQuery.data ?? null;
  const journal = journalQuery.data ?? null;
  const actions = actionsQuery.data ?? [];

  const currentTown = useMemo(() => {
    if (!session) {
      return null;
    }
    return session.world.towns.find((town) => town.id === session.player.currentTownId) ?? null;
  }, [session]);

  const cockpitMode: CockpitMode = session?.journey ? "travel" : "home";

  useEffect(() => {
    if (wantedPosters.length === 0) {
      setDeclaredWantedIdentityHandle("");
      return;
    }
    setDeclaredWantedIdentityHandle((current) =>
      wantedPosters.some((poster) => poster.posterId === current) ? current : wantedPosters[0].posterId,
    );
  }, [wantedPosters]);

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
      setError(exception instanceof Error ? exception.message : "Unable to archive the playthrough.");
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
      setNotice(result.message);
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
      setNotice(result.message);
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
      setNotice(result.message);
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
  const canConfrontSaloonPersonOfInterest = Boolean(session?.activeSaloonPersonOfInterest && declaredWantedIdentityHandle);

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
      setNotice(result.message);
    },
    [queryClient, invalidateGameQueries],
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
    setStoredGameId(null);
    setWantedPosters([]);
    setDeclaredWantedIdentityHandle("");
    setNotice("");
    setError("");
    setResetToken((current) => current + 1);
    if (gameId) {
      queryClient.removeQueries({ queryKey: ["session", gameId] });
      queryClient.removeQueries({ queryKey: ["actions", gameId] });
      queryClient.removeQueries({ queryKey: ["journal", gameId] });
    }
  }, [gameId, queryClient]);

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
    wantedPosters,
    hasReadWantedPosters: wantedPosters.length > 0,
    actions,
    gameId,
    currentTown,
    cockpitMode,
    busyMode,
    loading,
    notice,
    error,
    resetToken,
    canReadWantedPosters,
    canInspectNoticeBoard,
    canCheckLocalRecords,
    canFollowTelegraphLeads,
    canGatherLocalGossip,
    canLookAroundSaloon,
    canConfrontSaloonPersonOfInterest,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
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
    setNotice,
    setError,
    archivePlaythrough,
    archiving,
  };
}
