import { requestJson } from "../api/httpClient";
import type { ForceTravelOverrideRequestDto, SessionAuditDto, TravelDevContextDto } from "./types";

export function getSessionAudit(gameId: string) {
  return requestJson<SessionAuditDto>(`/api/dev/sessions/${gameId}/audit`);
}

export function getTravelDevContext(gameId: string) {
  return requestJson<TravelDevContextDto>(`/api/dev/sessions/${gameId}/travel-context`);
}

export function forceTravelOverride(gameId: string, request: ForceTravelOverrideRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/travel/force-override`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function clearTravelOverride(gameId: string) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/travel/clear-override`, {
    method: "POST",
  });
}
