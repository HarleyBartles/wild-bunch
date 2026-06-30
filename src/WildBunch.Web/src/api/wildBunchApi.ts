import type {
  GameEntropy,
  AvailableActionDto,
  BuyStoreItemRequest,
  InvestigationActionResultDto,
  GameSessionDto,
  GameTurnResultDto,
  JournalDto,
  PrologueDto,
  ResolveJourneyEncounterRequest,
  SetupGameRequest,
  StartGameWithTownRequest,
  StartingTownDto,
  StartingTownMapDto,
  TownStoreOffersDto,
  GameDifficulty,
  TravelRequest,
  TravelPreviewResultDto,
  SaloonPersonOfInterestConfrontationResultDto,
  WantedPostersResultDto,
  WantedSuspectConfrontationResultDto,
} from "./types";
import { requestJson } from "./httpClient";

export function setupGame(request: SetupGameRequest) {
  return requestJson<GameSessionDto>("/api/games/setup", {
    method: "POST",
    body: JSON.stringify(request satisfies SetupGameRequest),
  });
}

export function markPrologueViewed(gameId: string) {
  return requestJson<GameSessionDto>(`/api/games/${gameId}/prologue-viewed`, {
    method: "POST",
  });
}

export function startGameWithTown(gameId: string, request: StartGameWithTownRequest) {
  return requestJson<GameSessionDto>(`/api/games/${gameId}/start`, {
    method: "POST",
    body: JSON.stringify(request satisfies StartGameWithTownRequest),
  });
}

export function getGame(gameId: string) {
  return requestJson<GameSessionDto>(`/api/games/${gameId}`);
}

export function archiveGame(gameId: string) {
  return requestJson<void>(`/api/games/${gameId}/archive`, { method: "POST" });
}

export function getAvailableActions(gameId: string) {
  return requestJson<AvailableActionDto[]>(`/api/games/${gameId}/actions`);
}

export function getJournal(gameId: string) {
  return requestJson<JournalDto>(`/api/games/${gameId}/journal`);
}

export function getTownStoreOffers(gameId: string, townId: string) {
  return requestJson<TownStoreOffersDto>(`/api/games/${gameId}/towns/${townId}/store-offers`);
}

export function previewTravel(gameId: string, destinationTownId: string) {
  return requestJson<TravelPreviewResultDto>(`/api/games/${gameId}/travel/preview/${destinationTownId}`);
}

export function buyStoreItem(gameId: string, townId: string, request: BuyStoreItemRequest) {
  return requestJson<GameTurnResultDto>(`/api/games/${gameId}/towns/${townId}/store/buy`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function travel(gameId: string, destinationTownId: string) {
  return requestJson<GameTurnResultDto>(`/api/games/${gameId}/travel`, {
    method: "POST",
    body: JSON.stringify({ destinationTownId } satisfies TravelRequest),
  });
}

export function advanceTravelDay(gameId: string) {
  return requestJson<GameTurnResultDto>(`/api/games/${gameId}/travel/advance`, {
    method: "POST",
  });
}

export function acknowledgeTravelArrival(gameId: string) {
  return requestJson<GameTurnResultDto>(`/api/games/${gameId}/travel/arrival/acknowledge`, {
    method: "POST",
  });
}

export function resolveTravelEncounter(
  gameId: string,
  choiceId: string,
  options?: {
    bulletSpend?: number | null;
    bribeAmount?: number | null;
  },
) {
  return requestJson<GameTurnResultDto>(`/api/games/${gameId}/travel/encounter/resolve`, {
    method: "POST",
    body: JSON.stringify({
      choiceId,
      bulletSpend: options?.bulletSpend ?? null,
      bribeAmount: options?.bribeAmount ?? null,
    } satisfies ResolveJourneyEncounterRequest),
  });
}

export function readWantedPosters(gameId: string) {
  return requestJson<WantedPostersResultDto>(`/api/games/${gameId}/wanted-posters/read`, {
    method: "POST",
  });
}

export function inspectNoticeBoard(gameId: string) {
  return requestJson<InvestigationActionResultDto>(`/api/games/${gameId}/investigations/notice-board/inspect`, {
    method: "POST",
  });
}

export function checkLocalRecords(gameId: string) {
  return requestJson<InvestigationActionResultDto>(`/api/games/${gameId}/investigations/local-records/check`, {
    method: "POST",
  });
}

export function followTelegraphLeads(gameId: string) {
  return requestJson<InvestigationActionResultDto>(`/api/games/${gameId}/investigations/telegraph-leads/follow`, {
    method: "POST",
  });
}

export function gatherLocalGossip(gameId: string) {
  return requestJson<InvestigationActionResultDto>(`/api/games/${gameId}/investigations/local-gossip/gather`, {
    method: "POST",
  });
}

export function lookAroundSaloon(gameId: string) {
  return requestJson<InvestigationActionResultDto>(`/api/games/${gameId}/investigations/saloon/look-around`, {
    method: "POST",
  });
}

export function confrontSaloonPersonOfInterest(gameId: string, declaredWantedIdentityHandle: string) {
  return requestJson<SaloonPersonOfInterestConfrontationResultDto>(`/api/games/${gameId}/investigations/saloon/confront`, {
    method: "POST",
    body: JSON.stringify({
      declaredWantedIdentityHandle,
    }),
  });
}

export function confrontSaloonWantedSuspect(gameId: string, declaredWantedIdentityHandle: string) {
  return confrontSaloonPersonOfInterest(gameId, declaredWantedIdentityHandle) as Promise<WantedSuspectConfrontationResultDto>;
}

export function getPrologue(
  seedCode?: string | null,
  gameDifficulty?: GameDifficulty,
  gameEntropy?: GameEntropy,
) {
  const params = new URLSearchParams();
  if (seedCode) {
    params.set("seedCode", seedCode);
  }
  if (gameDifficulty != null) {
    params.set("gameDifficulty", String(gameDifficulty));
  }
  if (gameEntropy != null) {
    params.set("gameEntropy", String(gameEntropy));
  }
  const query = params.toString();
  return requestJson<PrologueDto>(`/api/games/prologue${query ? `?${query}` : ""}`);
}

export function getStartingTowns() {
  return requestJson<StartingTownDto[]>("/api/games/starting-towns");
}

export function getStartingTownMap(sessionId: string) {
  return requestJson<StartingTownMapDto>(`/api/games/${sessionId}/starting-town-map`);
}

export function getWorldMap(sessionId: string) {
  return requestJson<StartingTownMapDto>(`/api/games/${sessionId}/world-map`);
}
