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

export interface FoeProfileDevDto {
  speed: number;
  fightStrength: number;
  minimumBribe: number;
  speedBand: string;
  fightBand: string;
  bribeBand: string;
}

export interface DevOverrideDto {
  forcedCategory: string;
  foeProfile: FoeProfileDevDto | null;
  encounterMessage: string | null;
}

export interface TravelDevContextDto {
  sessionId: string;
  hasActiveJourney: boolean;
  journeyStatus: string | null;
  daysTravelled: number | null;
  remainingDays: number | null;
  pendingEncounterKind: string | null;
  pendingEncounterMessage: string | null;
  pendingFoeProfile: FoeProfileDevDto | null;
  pendingDevOverride: DevOverrideDto | null;
}

export interface ForceTravelOverrideRequestDto {
  forcedCategory: string;
  foeSpeed?: number | null;
  foeFightStrength?: number | null;
  foeMinimumBribe?: number | null;
  encounterMessage?: string | null;
}
