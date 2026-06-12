import { useEffect, useMemo, useState } from "react";
import {
  createGame,
  checkLocalRecords,
  followTelegraphLeads,
  getAvailableActions,
  getGame,
  getJournal,
  inspectNoticeBoard,
  gatherLocalGossip,
  lookAroundSaloon,
  readWantedPosters,
  travel,
} from "../api/wildBunchApi";
import { AvailableActionKind } from "../api/types";
import type {
  AvailableActionDto,
  GameSessionDto,
  GameTurnResultDto,
  JournalDto,
  StartGameRequest,
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

export function useCurrentGameSession() {
  const [session, setSession] = useState<GameSessionDto | null>(null);
  const [journal, setJournal] = useState<JournalDto | null>(null);
  const [wantedPosters, setWantedPosters] = useState<WantedPosterDto[]>([]);
  const [hasReadWantedPosters, setHasReadWantedPosters] = useState(false);
  const [actions, setActions] = useState<AvailableActionDto[]>([]);
  const [cockpitMode, setCockpitMode] = useState<CockpitMode>("home");
  const [busyMode, setBusyMode] = useState<BusyMode>("booting");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [resetToken, setResetToken] = useState(0);

  const gameId = session?.id ?? journal?.id ?? null;
  const currentTown = useMemo(() => {
    if (!session) {
      return null;
    }

    return session.world.towns.find((town) => town.id === session.player.currentTownId) ?? null;
  }, [session]);

  useEffect(() => {
    setCockpitMode(session?.journey ? "travel" : "home");
  }, [session?.journey]);

  useEffect(() => {
    const storedGameId = window.localStorage.getItem(storageKey);
    if (!storedGameId) {
      setBusyMode("idle");
      return;
    }

    void hydrateGame(storedGameId);
  }, []);

  async function hydrateGame(activeGameId: string) {
    setBusyMode((current) => (current === "booting" ? "booting" : "refreshing"));
    setError("");

    try {
      const [sessionResult, actionsResult, journalResult] = await Promise.all([
        getGame(activeGameId),
        getAvailableActions(activeGameId),
        getJournal(activeGameId),
      ]);
      setSession(sessionResult);
      setActions(actionsResult);
      setJournal(journalResult);
      setNotice("");
      window.localStorage.setItem(storageKey, activeGameId);
    } catch (exception) {
      window.localStorage.removeItem(storageKey);
      setSession(null);
      setJournal(null);
      setWantedPosters([]);
      setHasReadWantedPosters(false);
      setActions([]);
      setCockpitMode("home");
      setError(exception instanceof Error ? exception.message : "Unable to load the saved game.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function startNewGame(request: StartGameRequest) {
    setBusyMode("starting");
    setError("");
    setWantedPosters([]);
    setHasReadWantedPosters(false);

    try {
      const createdSession = await createGame(request);
      setSession(createdSession);
      window.localStorage.setItem(storageKey, createdSession.id);
      await hydrateGame(createdSession.id);
      setNotice(`New game started for ${createdSession.player.name}.`);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to start a new game.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function reloadCurrentGame(activeGameId = gameId) {
    if (!activeGameId) {
      return;
    }

    await hydrateGame(activeGameId);
  }

  async function handleTravelTurnResult(result: GameTurnResultDto) {
    setSession(result.currentSession);

    try {
      await reloadCurrentGame(result.currentSession.id);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to refresh the current hunt.");
    }
  }

  async function handleTravel(destinationTownId: string) {
    if (!gameId) {
      return;
    }

    setBusyMode("traveling");
    setError("");

    try {
      const result = await travel(gameId, destinationTownId);
      await handleTravelTurnResult(result);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to travel.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function handleReadWantedPosters() {
    if (!gameId || !canReadWantedPosters) {
      return;
    }

    setBusyMode("reading");
    setError("");

    try {
      const result = await readWantedPosters(gameId);
      setJournal(result.currentJournal);
      setWantedPosters(result.wantedPosters);
      setHasReadWantedPosters(true);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to read wanted posters.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function handleInspectNoticeBoard() {
    if (!gameId || !canInspectNoticeBoard) {
      return;
    }

    setBusyMode("investigating");
    setError("");

    try {
      const result = await inspectNoticeBoard(gameId);
      setJournal(result.currentJournal);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to inspect the notice board.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function handleCheckLocalRecords() {
    if (!gameId || !canCheckLocalRecords) {
      return;
    }

    setBusyMode("investigating");
    setError("");

    try {
      const result = await checkLocalRecords(gameId);
      setJournal(result.currentJournal);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to check local records.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function handleFollowTelegraphLeads() {
    if (!gameId || !canFollowTelegraphLeads) {
      return;
    }

    setBusyMode("investigating");
    setError("");

    try {
      const result = await followTelegraphLeads(gameId);
      setJournal(result.currentJournal);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to follow telegraph leads.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function handleGatherLocalGossip() {
    if (!gameId || !canGatherLocalGossip) {
      return;
    }

    setBusyMode("investigating");
    setError("");

    try {
      const result = await gatherLocalGossip(gameId);
      setJournal(result.currentJournal);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to gather local gossip.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function handleLookAroundSaloon() {
    if (!gameId || !canLookAroundSaloon) {
      return;
    }

    setBusyMode("investigating");
    setError("");

    try {
      const result = await lookAroundSaloon(gameId);
      setJournal(result.currentJournal);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to look around the saloon.");
    } finally {
      setBusyMode("idle");
    }
  }

  function handleReset() {
    window.localStorage.removeItem(storageKey);
    setSession(null);
    setJournal(null);
    setWantedPosters([]);
    setHasReadWantedPosters(false);
    setActions([]);
    setNotice("");
    setError("");
    setBusyMode("idle");
    setCockpitMode("home");
    setResetToken((current) => current + 1);
  }

  const loading = busyMode !== "idle";
  const canReadWantedPosters = actions.some(actionIsWantedPosters);
  const canInspectNoticeBoard = actions.some(actionIsInspectNoticeBoard);
  const canCheckLocalRecords = actions.some(actionIsCheckLocalRecords);
  const canFollowTelegraphLeads = actions.some(actionIsFollowTelegraphLeads);
  const canGatherLocalGossip = actions.some(actionIsGatherLocalGossip);
  const canLookAroundSaloon = actions.some(actionIsLookAroundSaloon);

  return {
    session,
    journal,
    wantedPosters,
    hasReadWantedPosters,
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
    startNewGame,
    reloadCurrentGame,
    handleTravelTurnResult,
    handleTravel,
    handleReadWantedPosters,
    handleInspectNoticeBoard,
    handleCheckLocalRecords,
    handleFollowTelegraphLeads,
    handleGatherLocalGossip,
    handleLookAroundSaloon,
    handleReset,
    setSession,
    setNotice,
    setError,
  };
}
