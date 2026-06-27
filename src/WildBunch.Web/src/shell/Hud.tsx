import styled from "styled-components";
import { formatGameStatus } from "../ui/formatters";
import { useGameSession } from "../state/useGameSession";

interface HudProps {
  onOpenJournal: () => void;
  onOpenGameSettings: () => void;
}

export function Hud({ onOpenJournal, onOpenGameSettings }: HudProps) {
  const { session, currentTown, cockpitMode } = useGameSession();

  if (!session) {
    return (
      <HudBar role="banner" aria-label="Game status">
        <Metric>
          <strong>No active hunt</strong>
          <small>Status</small>
        </Metric>
        <Metric>
          <strong>-</strong>
          <small>Camp</small>
        </Metric>
        <HudActions>
          <HudButton type="button" disabled>
            Journal
          </HudButton>
          <HudButton type="button" disabled>
            Game Settings
          </HudButton>
        </HudActions>
      </HudBar>
    );
  }

  const cash = session.inventory.wallet.cash;
  const heat = session.pursuitState.heat;
  const statusLabel = `${formatGameStatus(session.status)}${cockpitMode === "travel" ? " | Travel" : ""}`;

  return (
    <HudBar role="banner" aria-label="Game status">
      <Metric>
        <strong>{session.player.name}</strong>
        <small>Player</small>
      </Metric>
      <Metric>
        <strong>{`Day ${session.clock.day}, ${session.clock.timeOfDay}`}</strong>
        <small>Clock</small>
      </Metric>
      <Metric>
        <strong>{currentTown?.name ?? "On the trail"}</strong>
        <small>Location</small>
      </Metric>
      <Metric>
        <strong>{session.player.health}</strong>
        <small>Health</small>
      </Metric>
      <Metric>
        <strong>{`$${cash.toFixed(2)}`}</strong>
        <small>Cash</small>
      </Metric>
      <Metric>
        <strong>{heat}</strong>
        <small>Lawman heat</small>
      </Metric>
      <Metric>
        <strong>{statusLabel}</strong>
        <small>Status</small>
      </Metric>
      <HudActions>
        <HudButton type="button" onClick={onOpenJournal}>
          Journal
        </HudButton>
        <HudButton type="button" onClick={onOpenGameSettings}>
          Game Settings
        </HudButton>
      </HudActions>
    </HudBar>
  );
}

const HudBar = styled.header`
  display: flex;
  flex-wrap: wrap;
  gap: 8px 18px;
  align-items: center;
  padding: 10px 24px;
  background: var(--bg-elevated);
  border-bottom: 1px solid var(--border);
  backdrop-filter: blur(18px);

  @media (max-width: 640px) {
    padding: 8px 14px;
    gap: 6px 12px;
  }
`;

const Metric = styled.span`
  display: grid;
  gap: 2px;
  min-width: 0;

  strong {
    font-size: 0.92rem;
    line-height: 1.2;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  small {
    font-size: 0.66rem;
    text-transform: uppercase;
    letter-spacing: 0.18em;
    color: var(--muted);
  }
`;

const HudActions = styled.div`
  margin-left: auto;
  display: flex;
  align-items: center;

  @media (max-width: 640px) {
    width: 100%;
    margin-left: 0;
  }
`;

const HudButton = styled.button`
  border: 1px solid var(--border-strong);
  background: rgba(255, 255, 255, 0.04);
  color: var(--text);
  border-radius: 999px;
  padding: 8px 14px;
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  transition: transform 150ms ease-out, border-color 150ms ease-out, background-color 150ms ease-out;

  &:hover:not(:disabled),
  &:focus-visible:not(:disabled) {
    border-color: var(--accent);
    background: rgba(223, 159, 79, 0.1);
  }

  &:active:not(:disabled) {
    transform: translateY(1px);
  }

  &:disabled {
    opacity: 0.45;
    cursor: not-allowed;
  }

  @media (max-width: 640px) {
    width: 100%;
  }
`;
