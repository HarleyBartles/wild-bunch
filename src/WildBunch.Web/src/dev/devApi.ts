import { requestJson } from "../api/httpClient";
import type { SessionAuditDto } from "./types";

export function getSessionAudit(gameId: string) {
  return requestJson<SessionAuditDto>(`/api/dev/sessions/${gameId}/audit`);
}
