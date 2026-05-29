import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  createGame,
  getAvailableActions,
  getGame,
  getJournal,
  readWantedPosters,
  travel,
} from "./api/wildBunchApi";
import type {
  AvailableActionDto,
  GameSessionDto,
  JournalDto,
  InventoryCapabilitiesDto,
  InventoryItemDto,
  TrailDto,
  TownDto,
} from "./api/types";
import { AvailableActionKind } from "./api/types";

const storageKey = "wild-bunch.current-game-id";

type BusyMode = "idle" | "booting" | "starting" | "refreshing" | "traveling" | "reading";

function formatGameStatus(status: number) {
  switch (status) {
    case 0:
      return "Active";
    case 1:
      return "Completed";
    case 2:
      return "Failed";
    default:
      return `Status ${status}`;
  }
}

function formatActionKind(kind: number) {
  switch (kind) {
    case 0:
      return "Travel";
    case 1:
      return "View map";
    case 2:
      return "View journal";
    case 3:
      return "Buy supplies";
    case 4:
      return "Stay at lodging";
    case 5:
      return "Visit doctor";
    case 6:
      return "Send telegram";
    case 7:
      return "Read wanted posters";
    default:
      return `Action ${kind}`;
  }
}

function formatRisk(risk: number) {
  switch (risk) {
    case 1:
      return "Low";
    case 2:
      return "Moderate";
    case 3:
      return "High";
    default:
      return `Risk ${risk}`;
  }
}

function formatServices(services: number) {
  const labels: string[] = [];
  if (services & 1) labels.push("Supplies");
  if (services & 2) labels.push("Lodging");
  if (services & 4) labels.push("Doctor");
  if (services & 8) labels.push("Telegraph");
  if (services & 16) labels.push("Notice board");
  return labels.length > 0 ? labels.join(", ") : "None";
}

function formatSuspectStatus(status: number) {
  switch (status) {
    case 0:
      return "At large";
    case 1:
      return "Captured";
    case 2:
      return "Exonerated";
    default:
      return `Status ${status}`;
  }
}

function formatClueKind(kind: number) {
  switch (kind) {
    case 0:
      return "Physical";
    case 1:
      return "Witness";
    case 2:
      return "Record";
    case 3:
      return "Rumor";
    default:
      return `Clue ${kind}`;
  }
}

function formatItemKind(kind: number) {
  switch (kind) {
    case 0:
      return "Food";
    case 1:
      return "Horse feed";
    case 2:
      return "Canteen";
    case 3:
      return "Horse";
    case 4:
      return "Saddle";
    case 5:
      return "Knife";
    case 6:
      return "Revolver";
    case 7:
      return "Revolver ammo";
    case 8:
      return "Rifle";
    case 9:
      return "Rifle ammo";
    default:
      return `Item ${kind}`;
  }
}

function formatHorseCondition(condition: number) {
  switch (condition) {
    case 0:
      return "Healthy";
    case 1:
      return "Lame";
    case 2:
      return "Dead";
    default:
      return `Condition ${condition}`;
  }
}

function formatCapabilityLabel(label: keyof InventoryCapabilitiesDto) {
  switch (label) {
    case "mountedTravelAvailable":
      return "Mounted travel";
    case "horseUpkeepRequired":
      return "Horse upkeep";
    case "normalRouteWaterSecure":
      return "Water secure";
    case "trailUtility":
      return "Trail utility";
    case "closeThreatAvailable":
      return "Close threat";
    case "firearmThreatAvailable":
      return "Firearm threat";
    case "gunfightCapable":
      return "Gunfight capable";
    case "revolverUsable":
      return "Revolver usable";
    case "rifleUsable":
      return "Rifle usable";
    default:
      return label;
  }
}

function formatLogKind(kind: number) {
  switch (kind) {
    case 0:
      return "Opening";
    case 1:
      return "Travel";
    case 2:
      return "Case update";
    default:
      return `Log ${kind}`;
  }
}

function formatAliasKind(kind: number) {
  switch (kind) {
    case 0:
      return "Nickname";
    case 1:
      return "Former name";
    case 2:
      return "Street name";
    case 3:
      return "Known as";
    case 4:
      return "Cover identity";
    default:
      return `Alias ${kind}`;
  }
}

function townById(towns: TownDto[], townId: string) {
  return towns.find((town) => town.id === townId);
}

function connectedDestinations(session: GameSessionDto) {
  const currentTownId = session.player.currentTownId;
  const townMap = new Map(session.world.towns.map((town) => [town.id, town]));
  const destinations = new Map<
    string,
    { town: TownDto; trails: TrailDto[] }
  >();

  for (const trail of session.world.trails) {
    if (trail.fromTownId !== currentTownId && trail.toTownId !== currentTownId) {
      continue;
    }

    const destinationTownId = trail.fromTownId === currentTownId ? trail.toTownId : trail.fromTownId;
    const town = townMap.get(destinationTownId);
    if (!town) {
      continue;
    }

    const entry = destinations.get(destinationTownId);
    if (entry) {
      entry.trails.push(trail);
      continue;
    }

    destinations.set(destinationTownId, { town, trails: [trail] });
  }

  return Array.from(destinations.values()).sort((left, right) =>
    left.town.name.localeCompare(right.town.name),
  );
}

function actionIsWantedPosters(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.ReadWantedPosters;
}

export default function App() {
  const [playerName, setPlayerName] = useState("");
  const [session, setSession] = useState<GameSessionDto | null>(null);
  const [journal, setJournal] = useState<JournalDto | null>(null);
  const [actions, setActions] = useState<AvailableActionDto[]>([]);
  const [busyMode, setBusyMode] = useState<BusyMode>("booting");
  const [notice, setNotice] = useState<string>("");
  const [error, setError] = useState<string>("");

  const gameId = session?.id ?? journal?.id ?? null;
  const currentTown = useMemo(() => {
    if (!session) {
      return null;
    }

    return townById(session.world.towns, session.player.currentTownId) ?? null;
  }, [session]);
  const destinations = useMemo(() => (session ? connectedDestinations(session) : []), [session]);
  const canReadWantedPosters = actions.some(actionIsWantedPosters);

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
      setPlayerName(sessionResult.player.name);
      setNotice("");
      window.localStorage.setItem(storageKey, activeGameId);
    } catch (exception) {
      window.localStorage.removeItem(storageKey);
      setSession(null);
      setJournal(null);
      setActions([]);
      setError(exception instanceof Error ? exception.message : "Unable to load the saved game.");
    } finally {
      setBusyMode("idle");
    }
  }

  async function startNewGame(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmedName = playerName.trim();
    if (!trimmedName) {
      setError("Enter a player name to start.");
      return;
    }

    setBusyMode("starting");
    setError("");

    try {
      const createdSession = await createGame(trimmedName);
      setSession(createdSession);
      setPlayerName(createdSession.player.name);
      window.localStorage.setItem(storageKey, createdSession.id);
      await hydrateGame(createdSession.id);
      setNotice(`New game started for ${trimmedName}.`);
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

  async function handleTravel(destinationTownId: string) {
    if (!gameId) {
      return;
    }

    setBusyMode("traveling");
    setError("");

    try {
      const result = await travel(gameId, destinationTownId);
      setSession(result.currentSession);
      await reloadCurrentGame(gameId);
      setNotice(result.message);
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
    setPlayerName("");
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
            <strong>{session ? formatGameStatus(session.status) : "Idle"}</strong>
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

          <form className="start-form" onSubmit={startNewGame}>
            <label className="field">
              <span>Player name</span>
              <input
                type="text"
                value={playerName}
                onChange={(event) => setPlayerName(event.target.value)}
                placeholder="Enter a rider name"
                autoComplete="off"
              />
            </label>
            <div className="button-row">
              <button type="submit" className="button" disabled={loading}>
                {busyMode === "starting" ? "Starting..." : "Start new game"}
              </button>
              <button
                type="button"
                className="button button--ghost"
                onClick={() => reloadCurrentGame()}
                disabled={!gameId || loading}
              >
                {busyMode === "refreshing" ? "Refreshing..." : "Refresh"}
              </button>
            </div>
          </form>

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
                    <dt>Money</dt>
                    <dd>${session.player.money.toFixed(2)}</dd>
                  </div>
                  <div>
                    <dt>Supplies</dt>
                    <dd>{session.player.supplies}</dd>
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

              <article className="status-card">
                <h3>Inventory</h3>
                <dl className="stat-list">
                  <div>
                    <dt>Cash</dt>
                    <dd>${session.inventory.wallet.cash.toFixed(2)}</dd>
                  </div>
                  <div>
                    <dt>Horse condition</dt>
                    <dd>{session.inventory.horseCondition === null ? "None" : formatHorseCondition(session.inventory.horseCondition)}</dd>
                  </div>
                  <div>
                    <dt>Loadout items</dt>
                    <dd>{session.inventory.items.length}</dd>
                  </div>
                  <div>
                    <dt>Capabilities</dt>
                    <dd>{Object.values(session.inventory.capabilities).filter(Boolean).length}</dd>
                  </div>
                </dl>
                <div className="stack">
                  {session.inventory.items.map((item: InventoryItemDto) => (
                    <div key={`${item.kind}-${item.horseCondition ?? "none"}`} className="compact-item">
                      <strong>
                        {formatItemKind(item.kind)} x {item.quantity}
                      </strong>
                      <p>
                        {item.horseCondition === null ? "No condition" : `Condition: ${formatHorseCondition(item.horseCondition)}`}
                      </p>
                    </div>
                  ))}
                </div>
                <div className="tag-row">
                  {Object.entries(session.inventory.capabilities).map(([key, enabled]) => (
                    <span key={key} className="tag">
                      {formatCapabilityLabel(key as keyof InventoryCapabilitiesDto)}: {enabled ? "Yes" : "No"}
                    </span>
                  ))}
                </div>
              </article>
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

        <section className="panel">
          <div className="panel-head">
            <h2>Travel routes</h2>
            <span className="panel-subtitle">{destinations.length} connected</span>
          </div>
          <div className="stack">
            {destinations.length > 0 ? (
              destinations.map(({ town, trails }) => (
                <button
                  key={town.id}
                  type="button"
                  className="destination-card"
                  onClick={() => handleTravel(town.id)}
                  disabled={!gameId || loading}
                >
                  <div>
                    <strong>{town.name}</strong>
                    <p>{town.id}</p>
                  </div>
                  <div className="destination-meta">
                    <span>{trails.length} trail{trails.length === 1 ? "" : "s"}</span>
                    <span>{Math.min(...trails.map((trail) => trail.supplyCost))} min supplies</span>
                    <span>{trails.map((trail) => formatRisk(trail.risk)).join(", ")}</span>
                  </div>
                </button>
              ))
            ) : (
              <p className="muted">Travel destinations derive from the current town and world trails.</p>
            )}
          </div>
        </section>

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

              <article className="status-card">
                <h3>Suspects</h3>
                <div className="stack">
                  {journal.caseFile.suspects.map((suspect) => (
                    <div key={suspect.id} className="compact-item">
                      <strong>{suspect.name}</strong>
                      <p>
                        {suspect.id} - {formatSuspectStatus(suspect.status)}
                      </p>
                      <p className="muted">
                        Aliases:{" "}
                        {suspect.profile.aliases.length > 0
                          ? suspect.profile.aliases
                              .map((alias) => `${alias.name} (${formatAliasKind(alias.kind)})`)
                              .join(", ")
                          : "None"}
                      </p>
                      <p className="muted">
                        Identifying facts:{" "}
                        {suspect.profile.identifyingFacts.length > 0
                          ? suspect.profile.identifyingFacts.map((fact) => fact.description).join("; ")
                          : "None"}
                      </p>
                      <span className="tag-row">
                        <span className="tag">{suspect.traits.isLocal ? "Local" : "Not local"}</span>
                        <span className="tag">{suspect.traits.isArmed ? "Armed" : "Unarmed"}</span>
                        <span className="tag">{suspect.traits.isDesperate ? "Desperate" : "Calm"}</span>
                      </span>
                    </div>
                  ))}
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
