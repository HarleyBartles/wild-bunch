export interface SessionAuditEntryDto {
  sequence: number;
  eventType: string;
  summary: string;
  occurredAtUtc: string;
}

export interface SessionAuditDto {
  sessionId: string;
  entries: SessionAuditEntryDto[];
}
