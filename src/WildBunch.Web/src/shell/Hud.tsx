import { useGameSession } from "../state/GameSessionContext";
import { formatGameStatus } from "../ui/formatters";

interface HudStat {
  label: string;
  value: string;
}

export function Hud() {
  const { session, currentTown, cockpitMode, loading } = useGameSession();

  const stats: HudStat[] = session
    ? [
        { label: "Player", value: session.player.name },
        { label: "Status", value: `${formatGameStatus(session.status)}${cockpitMode === "travel" ? " · Trail" : ""}` },
        { label: "Day / Turn", value: `${session.clock.day} / ${session.clock.turn}` },
        { label: "Town", value: currentTown?.name ?? session.player.currentTownId },
        { label: "Health", value: session.player.health.toLocaleString() },
        { label: "Cash", value: `$${session.inventory.wallet.cash.toFixed(2)}` },
        { label: "Heat", value: `${session.pursuitState.heat}` },
      ]
    : [{ label: "Status", value: loading ? "Loading…" : "No active hunt" }];

  return (
    <header className="hud" aria-label="Game status">
      <div className="hud__brand">
        <span className="hud__brand-mark">Wild Bunch</span>
        <span className="hud__brand-tag">UI v1</span>
      </div>
      <dl className="hud__stats">
        {stats.map((stat) => (
          <div key={stat.label} className="hud__stat">
            <dt>{stat.label}</dt>
            <dd>{stat.value}</dd>
          </div>
        ))}
      </dl>
    </header>
  );
}
