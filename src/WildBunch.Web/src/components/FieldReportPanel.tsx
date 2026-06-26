import type { GameSessionDto, TownDto } from "../api/types";
import { formatServices, formatGameStatus } from "../ui/formatters";
import { InventoryPanel } from "./InventoryPanel";
import { StoreOffersPanel } from "./StoreOffersPanel";
import { TravelPanel } from "./TravelPanel";
import { Grid, StatusCard, StatList } from "./ui/sharedStyled";

interface FieldReportPanelProps {
  session: GameSessionDto;
  currentTown: TownDto | null;
  storeOffers: Parameters<typeof StoreOffersPanel>[0]["storeOffers"];
  storeOffersLoading: boolean;
  busy: boolean;
  gameId: string;
  onBuyOffer: Parameters<typeof StoreOffersPanel>[0]["onBuyOffer"];
  onTurnResult: Parameters<typeof TravelPanel>[0]["onTurnResult"];
}

export function FieldReportPanel({
  session,
  currentTown,
  storeOffers,
  storeOffersLoading,
  busy,
  gameId,
  onBuyOffer,
  onTurnResult,
}: FieldReportPanelProps) {
  return (
    <Grid>
      <StatusCard>
        <h3>Field report</h3>
        <StatList>
          <div>
            <dt>Player</dt>
            <dd>{session.player.name}</dd>
          </div>
          <div>
            <dt>Town</dt>
            <dd>{currentTown ? `${currentTown.name} (${currentTown.id})` : session.player.currentTownId}</dd>
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
        <h3>Town details</h3>
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
        </StatList>
      </StatusCard>
      <InventoryPanel inventory={session.inventory} />
      <StoreOffersPanel
        storeOffers={storeOffers}
        loading={storeOffersLoading}
        busy={busy}
        onBuyOffer={onBuyOffer}
      />
      {session.journey ? (
        <TravelPanel
          gameId={gameId}
          session={session}
          busy={busy}
          onTurnResult={onTurnResult}
        />
      ) : null}
    </Grid>
  );
}
