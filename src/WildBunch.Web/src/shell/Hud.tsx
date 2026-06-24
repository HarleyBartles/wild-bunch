import { formatGameStatus } from "../ui/formatters";
import { useGameSession } from "../state/useGameSession";

interface HudProps {
  onOpenJournal: () => void;
}

export function Hud({ onOpenJournal }: HudProps) {
  const { session, currentTown, cockpitMode } = useGameSession();

  if (!session) {
    return (
      <header className="hud" role="banner" aria-label="Game status">
        <span className="hud-metric">
          <strong>No active hunt</strong>
          <small>Status</small>
        </span>
        <span className="hud-metric">
          <strong>-</strong>
          <small>Camp</small>
        </span>
        <div className="hud-actions">
          <button type="button" className="hud-action" disabled>
            Journal
          </button>
        </div>
      </header>
    );
  }

  const cash = session.inventory.wallet.cash;
  const heat = session.pursuitState.heat;
  const statusLabel = `${formatGameStatus(session.status)}${cockpitMode === "travel" ? " | Travel" : ""}`;

  return (
    <header className="hud" role="banner" aria-label="Game status">
      <span className="hud-metric">
        <strong>{session.player.name}</strong>
        <small>Player</small>
      </span>
      <span className="hud-metric">
        <strong>{`Day ${session.clock.day}, ${session.clock.timeOfDay}`}</strong>
        <small>Clock</small>
      </span>
      <span className="hud-metric">
        <strong>{currentTown?.name ?? "On the trail"}</strong>
        <small>Location</small>
      </span>
      <span className="hud-metric">
        <strong>{session.player.health}</strong>
        <small>Health</small>
      </span>
      <span className="hud-metric">
        <strong>{`$${cash.toFixed(2)}`}</strong>
        <small>Cash</small>
      </span>
      <span className="hud-metric">
        <strong>{heat}</strong>
        <small>Lawman heat</small>
      </span>
      <span className="hud-metric">
        <strong>{statusLabel}</strong>
        <small>Status</small>
      </span>
      <div className="hud-actions">
        <button
          type="button"
          className="hud-action"
          onClick={onOpenJournal}
        >
          Journal
        </button>
      </div>
    </header>
  );
}
