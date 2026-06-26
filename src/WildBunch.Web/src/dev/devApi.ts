import { requestJson } from "../api/httpClient";
import type { ForceSaloonOverrideRequestDto, ForceTravelOverrideRequestDto, SaloonDevContextDto, SessionAuditDto, TravelDevContextDto } from "./types";

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

export function getSaloonDevContext(gameId: string) {
  return requestJson<SaloonDevContextDto>(`/api/dev/sessions/${gameId}/saloon-context`);
}

export function forceSaloonOverride(gameId: string, request: ForceSaloonOverrideRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/saloon/force-override`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function clearSaloonOverride(gameId: string) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/saloon/clear-override`, {
    method: "POST",
  });
}
