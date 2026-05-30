import { useEffect, useMemo, useState } from "react";
import {
  createGame,
  getAvailableActions,
  getGame,
  getJournal,
  readWantedPosters,
  travel,
} from "../api/wildBunchApi";
import { AvailableActionKind } from "../api/types";
import type { AvailableActionDto, GameSessionDto, GameTurnResultDto, JournalDto, StartGameRequest } from "../api/types";

const storageKey = "wild-bunch.current-game-id";

type BusyMode = "idle" | "booting" | "starting" | "refreshing" | "traveling" | "reading";
type CockpitMode = "home" | "travel";

function actionIsWantedPosters(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.ReadWantedPosters;
}

export function useCurrentGameSession() {
  const [session, setSession] = useState<GameSessionDto | null>(null);
  const [journal, setJournal] = useState<JournalDto | null>(null);
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
      await reloadCurrentGame(gameId);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to read wanted posters.");
    } finally {
      setBusyMode("idle");
    }
  }

  function handleReset() {
    window.localStorage.removeItem(storageKey);
    setSession(null);
    setJournal(null);
    setActions([]);
    setNotice("");
    setError("");
    setBusyMode("idle");
    setCockpitMode("home");
    setResetToken((current) => current + 1);
  }

  const loading = busyMode !== "idle";
  const canReadWantedPosters = actions.some(actionIsWantedPosters);

  return {
    session,
    journal,
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
    startNewGame,
    reloadCurrentGame,
    handleTravelTurnResult,
    handleTravel,
    handleReadWantedPosters,
    handleReset,
    setSession,
    setNotice,
    setError,
  };
}
