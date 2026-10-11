import type { GameSessionDto, TownDto } from "../api/types";
import { StartFlowPhase } from "../api/types";
import { InventoryPanel } from "./InventoryPanel";
import { StoreOffersPanel } from "./StoreOffersPanel";
import { TravelPanel } from "./TravelPanel";
import { Grid, StatusCard, StatusCardHeading, StatList } from "./ui/sharedStyled";

interface FieldReportPanelProps {
  session: GameSessionDto;
  currentTown: TownDto | null;
  storeOffers: Parameters<typeof StoreOffersPanel>[0]["storeOffers"];
  storeOffersLoading: boolean;
  busy: boolean;
  gameId: string;
  onBuyOffer: Parameters<typeof StoreOffersPanel>[0]["onBuyOffer"];
}

export function FieldReportPanel({
  session,
  currentTown,
  storeOffers,
  storeOffersLoading,
  busy,
  gameId,
  onBuyOffer,
}: FieldReportPanelProps) {
  if (
    session.startFlowPhase !== StartFlowPhase.GameStarted ||
    session.inventory === null ||
    session.player.health === null
  ) {
    return null;
  }

  return (
    <Grid>
      <StatusCard>
        <StatusCardHeading>Field report</StatusCardHeading>
        <StatList>
          <div>
            <dt>Player</dt>
            <dd>{session.player.name}</dd>
          </div>
          <div>
            <dt>Town</dt>
            <dd>
              {currentTown
                ? `${currentTown.name} (${currentTown.id})`
                : session.player.currentTownId}
            </dd>
          </div>
          <div>
            <dt>Current health</dt>
            <dd>{session.player.health.toLocaleString()}</dd>
          </div>
          <div>
            <dt>Lawman heat</dt>
            <dd>{session.pursuitState.heat}</dd>
          </div>
        </StatList>
      </StatusCard>

      <StatusCard>
        <StatusCardHeading>Town details</StatusCardHeading>
        <StatList>
          <div>
            <dt>Current town</dt>
            <dd>{currentTown?.name ?? "Unknown"}</dd>
          </div>
          <div>
            <dt>Town id</dt>
            <dd>{currentTown?.id ?? session.player.currentTownId}</dd>
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
        </StatList>
      </StatusCard>
      <InventoryPanel inventory={session.inventory} />
      <StoreOffersPanel
        storeOffers={storeOffers}
        loading={storeOffersLoading}
        busy={busy}
        onBuyOffer={onBuyOffer}
      />
      {session.journey ? <TravelPanel gameId={gameId} session={session} busy={busy} /> : null}
    </Grid>
  );
}
