import type {
  AvailableActionDto,
  BuyStoreItemRequest,
  GameSessionDto,
  GameTurnResultDto,
  JournalDto,
  ResolveJourneyEncounterRequest,
  StartGameRequest,
  TownStoreOffersDto,
  TravelRequest,
  TravelPreviewResultDto,
  WantedPostersResultDto,
} from "./types";

const defaultApiBaseUrl = "http://localhost:5275";

function getApiBaseUrl() {
  const configured = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (configured === undefined) {
    return defaultApiBaseUrl;
  }

  if (configured === "") {
    return "";
  }

  return configured.replace(/\/+$/, "");
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  const contentType = response.headers.get("content-type") ?? "";
  const body = contentType.includes("json")
    ? await response.json().catch(() => null)
    : await response.text().catch(() => "");

  if (!response.ok) {
    const message = extractErrorMessage(body) || `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return body as T;
}

function extractErrorMessage(body: unknown) {
  if (typeof body === "string") {
    return body;
  }

  if (!body || typeof body !== "object") {
    return "";
  }

  const problem = body as Record<string, unknown>;
  if (typeof problem.title === "string" && problem.title.trim()) {
    return problem.title;
  }

  const errors = problem.errors;
  if (errors && typeof errors === "object") {
    for (const value of Object.values(errors as Record<string, unknown>)) {
      if (Array.isArray(value) && value.length > 0 && typeof value[0] === "string") {
        return value[0];
      }
    }
  }

  if (typeof problem.detail === "string" && problem.detail.trim()) {
    return problem.detail;
  }

  return "";
}

export function createGame(request: StartGameRequest) {
  return requestJson<GameSessionDto>("/api/games", {
    method: "POST",
    body: JSON.stringify(request satisfies StartGameRequest),
  });
}

export function getGame(gameId: string) {
  return requestJson<GameSessionDto>(`/api/games/${gameId}`);
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

export function resolveTravelEncounter(gameId: string, choiceId: string) {
  return requestJson<GameTurnResultDto>(`/api/games/${gameId}/travel/encounter/resolve`, {
    method: "POST",
    body: JSON.stringify({ choiceId } satisfies ResolveJourneyEncounterRequest),
  });
}

export function readWantedPosters(gameId: string) {
  return requestJson<WantedPostersResultDto>(`/api/games/${gameId}/wanted-posters/read`, {
    method: "POST",
  });
}
