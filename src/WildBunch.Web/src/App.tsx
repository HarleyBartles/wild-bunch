import { useEffect, useMemo, useState } from "react";
import {
  buyStoreItem,
  createGame,
  getAvailableActions,
  getGame,
  getJournal,
  getTownStoreOffers,
  readWantedPosters,
  travel,
} from "./api/wildBunchApi";
import type {
  AvailableActionDto,
  GameSessionDto,
  GameTurnResultDto,
  JournalDto,
  TownStoreOffersDto,
} from "./api/types";
import { AvailableActionKind } from "./api/types";
import { InventoryPanel } from "./components/InventoryPanel";
import { StartGamePanel } from "./components/StartGamePanel";
import { TravelPanel } from "./components/TravelPanel";
import { TravelRoutesPanel } from "./components/TravelRoutesPanel";
import { StoreOffersPanel } from "./components/StoreOffersPanel";
import {
  formatActionKind,
  formatClueKind,
  formatGameStatus,
  formatLogKind,
  formatServices,
  formatSuspectStatus,
} from "./ui/formatters";

const storageKey = "wild-bunch.current-game-id";

type BusyMode = "idle" | "booting" | "starting" | "refreshing" | "traveling" | "reading" | "buying";
type CockpitMode = "home" | "travel";

function actionIsWantedPosters(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.ReadWantedPosters;
}

export default function App() {
  const [session, setSession] = useState<GameSessionDto | null>(null);
  const [journal, setJournal] = useState<JournalDto | null>(null);
  const [actions, setActions] = useState<AvailableActionDto[]>([]);
  const [storeOffers, setStoreOffers] = useState<TownStoreOffersDto | null>(null);
  const [storeOffersLoading, setStoreOffersLoading] = useState(false);
  const [cockpitMode, setCockpitMode] = useState<CockpitMode>("home");
  const [busyMode, setBusyMode] = useState<BusyMode>("booting");
  const [notice, setNotice] = useState<string>("");
  const [error, setError] = useState<string>("");
  const [resetToken, setResetToken] = useState(0);

  const gameId = session?.id ?? journal?.id ?? null;
  const currentTown = useMemo(() => {
    if (!session) {
      return null;
    }

    return session.world.towns.find((town) => town.id === session.player.currentTownId) ?? null;
  }, [session]);
  const canReadWantedPosters = actions.some(actionIsWantedPosters);

  useEffect(() => {
    setCockpitMode(session?.journey ? "travel" : "home");
  }, [session?.journey]);

  async function loadTownStoreOffers(activeGameId: string, activeTownId: string) {
    setStoreOffersLoading(true);

    try {
      const offers = await getTownStoreOffers(activeGameId, activeTownId);
      setStoreOffers(offers);
    } catch {
      setStoreOffers(null);
    } finally {
      setStoreOffersLoading(false);
    }
  }

  useEffect(() => {
    if (!gameId || !currentTown?.id) {
      setStoreOffers(null);
      setStoreOffersLoading(false);
      return;
    }

    const activeGameId = gameId;
    const activeTownId = currentTown.id;
    let cancelled = false;

    void (async () => {
      setStoreOffersLoading(true);

      try {
        const offers = await getTownStoreOffers(activeGameId, activeTownId);
        if (!cancelled) {
          setStoreOffers(offers);
        }
      } catch {
        if (!cancelled) {
          setStoreOffers(null);
        }
      } finally {
        if (!cancelled) {
          setStoreOffersLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [currentTown?.id, gameId]);

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
    setStoreOffers(null);
    setStoreOffersLoading(false);

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

  async function startNewGame(request: Parameters<typeof createGame>[0]) {
    setBusyMode("starting");
    setError("");
    setStoreOffers(null);
    setStoreOffersLoading(false);

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

  async function handleTravelTurnResult(result: GameTurnResultDto) {
    setSession(result.currentSession);

    try {
      await reloadCurrentGame(result.currentSession.id);
      setNotice(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to refresh the current hunt.");
    }
  }

  async function reloadCurrentGame(activeGameId = gameId) {
    if (!activeGameId) {
      return;
    }

    await hydrateGame(activeGameId);
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

  async function handleBuyOffer(offer: TownStoreOffersDto["offers"][number], quantity: number) {
    if (!gameId || !currentTown?.id) {
      return;
    }

    setBusyMode("buying");
    setNotice("");
    setError("");

    try {
      const result = await buyStoreItem(gameId, currentTown.id, {
        vendorType: offer.vendorType,
        itemKind: offer.itemKind,
        quantity,
      });

      setSession(result.currentSession);

      if (result.success) {
        await reloadCurrentGame(gameId);
        await loadTownStoreOffers(gameId, currentTown.id);
        setNotice(result.message);
        setError("");
        return;
      }

      setNotice("");
      setError(result.message);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Unable to buy the selected item.");
    } finally {
      setBusyMode("idle");
    }
  }

  function handleReset() {
    window.localStorage.removeItem(storageKey);
    setSession(null);
    setJournal(null);
    setActions([]);
    setStoreOffers(null);
    setStoreOffersLoading(false);
    setNotice("");
    setError("");
    setBusyMode("idle");
    setCockpitMode("home");
    setResetToken((current) => current + 1);
  }

  const loading = busyMode !== "idle";

  return (
    <div className="app-shell">
      <header className="hero">
        <div>
          <p className="eyebrow">Wild Bunch</p>
          <h1>Field cockpit</h1>
          <p className="hero-copy">
            A thin command surface over the existing game loop: start a hunt, travel
            between towns, read the board, and keep the case file in view.
          </p>
        </div>
        <div className="hero-metrics">
          <span className="metric">
            <strong>{session ? session.player.name : "No active hunt"}</strong>
            <small>Player</small>
          </span>
          <span className="metric">
            <strong>
              {session ? formatGameStatus(session.status) : "Idle"} {session ? `| ${cockpitMode === "travel" ? "Travel diary" : "Cockpit"}` : ""}
            </strong>
            <small>Status</small>
          </span>
          <span className="metric">
            <strong>{session ? `Day ${session.clock.day}, Turn ${session.clock.turn}` : "-"}</strong>
            <small>Clock</small>
          </span>
        </div>
      </header>

      <main className="layout">
        <section className="panel panel--wide">
          <div className="panel-head">
            <h2>{gameId ? "Current session" : "Start a new hunt"}</h2>
            <div className="panel-actions">
              <button type="button" className="button button--ghost" onClick={handleReset}>
                Reset
              </button>
            </div>
          </div>

          <StartGamePanel
            session={session}
            busy={loading}
            gameId={gameId}
            resetToken={resetToken}
            onStartGame={startNewGame}
            onRefresh={async () => {
              await reloadCurrentGame();
            }}
          />

          {notice ? <div className="notice">{notice}</div> : null}
          {error ? <div className="error">{error}</div> : null}

          {session ? (
            <div className="session-grid">
              <article className="status-card">
                <h3>Field report</h3>
                <dl className="stat-list">
                  <div>
                    <dt>Player</dt>
                    <dd>{session.player.name}</dd>
                  </div>
                  <div>
                    <dt>Town</dt>
                    <dd>{currentTown ? `${currentTown.name} (${currentTown.id})` : session.player.currentTownId}</dd>
                  </div>
                  <div>
                    <dt>Health</dt>
                    <dd>{session.player.health}</dd>
                  </div>
                  <div>
                    <dt>Heat</dt>
                    <dd>{session.pursuitState.heat}</dd>
                  </div>
                </dl>
              </article>

              <article className="status-card">
                <h3>Town details</h3>
                <dl className="stat-list">
                  <div>
                    <dt>Current town</dt>
                    <dd>{currentTown?.name ?? "Unknown"}</dd>
                  </div>
                  <div>
                    <dt>Town id</dt>
                    <dd>{currentTown?.id ?? session.player.currentTownId}</dd>
                  </div>
                  <div>
                    <dt>Services</dt>
                    <dd>{currentTown ? formatServices(currentTown.services) : "Unknown"}</dd>
                  </div>
                  <div>
                    <dt>World towns</dt>
                    <dd>{session.world.towns.length}</dd>
                  </div>
                  <div>
                    <dt>Trails</dt>
                    <dd>{session.world.trails.length}</dd>
                  </div>
                  <div>
                    <dt>Log entries</dt>
                    <dd>{session.logEntries.length}</dd>
                  </div>
                </dl>
              </article>
              <InventoryPanel inventory={session.inventory} />
              <StoreOffersPanel
                storeOffers={storeOffers}
                loading={storeOffersLoading}
                busy={loading}
                onBuyOffer={handleBuyOffer}
              />
              {session?.journey ? (
                <TravelPanel
                  gameId={gameId ?? session.id}
                  session={session}
                  busy={loading}
                  onTurnResult={handleTravelTurnResult}
                />
              ) : null}
            </div>
          ) : null}
        </section>

        <section className="panel">
          <div className="panel-head">
            <h2>Available actions</h2>
            <span className="panel-subtitle">{actions.length} fetched</span>
          </div>
          <div className="stack">
            {actions.length > 0 ? (
              actions.map((action) => (
                <div key={`${action.kind}-${action.label}`} className="action-row">
                  <div>
                    <strong>{action.label}</strong>
                    <p>{formatActionKind(action.kind)}</p>
                  </div>
                  {actionIsWantedPosters(action) ? (
                    <button
                      type="button"
                      className="button"
                      onClick={handleReadWantedPosters}
                      disabled={!gameId || loading}
                    >
                      {busyMode === "reading" ? "Reading..." : "Read wanted posters"}
                    </button>
                  ) : null}
                </div>
              ))
            ) : (
              <p className="muted">Actions will appear here after a game loads.</p>
            )}
          </div>
        </section>

        <TravelRoutesPanel gameId={gameId ?? session?.id ?? null} session={session} busy={loading} onTravel={handleTravel} />

        <section className="panel panel--wide">
          <div className="panel-head">
            <h2>Case file</h2>
            <span className="panel-subtitle">{journal ? `Updated day ${journal.clock.day}, turn ${journal.clock.turn}` : "No journal loaded"}</span>
          </div>

          {journal ? (
            <div className="case-grid">
              <article className="status-card">
                <h3>Summary</h3>
                <p className="case-summary">{journal.caseFile.caseSummary}</p>
                <p className="case-lead">
                  <strong>Opening lead:</strong> {journal.caseFile.openingLead}
                </p>
                <dl className="stat-list">
                  <div>
                    <dt>Status</dt>
                    <dd>{formatGameStatus(journal.status)}</dd>
                  </div>
                  <div>
                    <dt>Town</dt>
                    <dd>{journal.currentTown.name}</dd>
                  </div>
                  <div>
                    <dt>Accusation</dt>
                    <dd>{journal.caseFile.accusationId ?? "None"}</dd>
                  </div>
                  <div>
                    <dt>Release</dt>
                    <dd>
                      {journal.caseFile.killerReleaseState.progress}/{journal.caseFile.killerReleaseState.requiredPublicClues}
                    </dd>
                  </div>
                </dl>
                <p className="case-release">{journal.caseFile.killerReleaseState.statusText}</p>
              </article>

              <article className="status-card">
                <h3>Discovered suspects</h3>
                <div className="stack">
                  {journal.caseFile.discoveredSuspects.length > 0 ? (
                    journal.caseFile.discoveredSuspects.map((suspect) => (
                      <div key={suspect.id} className="compact-item">
                        <strong>{suspect.name}</strong>
                        <p>
                          {suspect.id} - {formatSuspectStatus(suspect.status)}
                        </p>
                      </div>
                    ))
                  ) : (
                    <p className="muted">No suspects have been discovered yet.</p>
                  )}
                </div>
              </article>

              <article className="status-card">
                <h3>Known clues</h3>
                <div className="stack">
                  {journal.caseFile.knownClues.length > 0 ? (
                    journal.caseFile.knownClues.map((clue) => (
                      <div key={clue.id} className="compact-item">
                        <strong>{clue.description}</strong>
                        <p>
                          {clue.id} - {formatClueKind(clue.kind)}
                        </p>
                      </div>
                    ))
                  ) : (
                    <p className="muted">No clues recorded yet.</p>
                  )}
                </div>
              </article>

            </div>
          ) : (
            <p className="muted">Load a game to inspect the case file.</p>
          )}
        </section>

        <section className="panel panel--wide">
          <div className="panel-head">
            <h2>Log</h2>
            <span className="panel-subtitle">{session?.logEntries.length ?? 0} entries</span>
          </div>
          <div className="log-list">
            {(journal?.logEntries ?? session?.logEntries ?? []).length > 0 ? (
              (journal?.logEntries ?? session?.logEntries ?? []).map((entry, index) => (
                <article key={`${entry.day}-${entry.turn}-${index}`} className="log-entry">
                  <div className="log-entry__meta">
                    <strong>{formatLogKind(entry.kind)}</strong>
                    <span>
                      Day {entry.day}, Turn {entry.turn}
                    </span>
                  </div>
                  <p>{entry.message}</p>
                </article>
              ))
            ) : (
              <p className="muted">Log entries will appear here as the hunt unfolds.</p>
            )}
          </div>
        </section>
      </main>
    </div>
  );
}
