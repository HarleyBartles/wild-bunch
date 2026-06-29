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

export interface HiddenTruthDevDto {
  trueCulpritId: string;
  trueCulpritName: string;
  killerReleaseStatus: string;
  killerIsReleased: boolean;
  saloonLoopExplanation: string;
}

export interface CitizenArchetypeDto {
  roleKey: string;
  displayName: string;
}

export interface CitizenInfoDto {
  descriptor: string;
  hasNamedArchetypes: boolean;
  availableArchetypes: CitizenArchetypeDto[];
}

export interface SaloonSuspectDevDto {
  suspectId: string;
  name: string;
  isTrueCulprit: boolean;
  isEligibleSaloonPoi: boolean;
  ineligibilityReason: string | null;
  hasKnownWarrant: boolean;
  presenceState: string | null;
  aliases: string[];
  identifyingFacts: string[];
  traitTags: string[];
  bountyAmount: number | null;
  warrantDisposition: string | null;
  warrantKnownFeatures: string[];
  warrantSummary: string | null;
}

export interface DevSaloonOverrideDto {
  forcedKind: string;
  forcedSuspectId: string | null;
  forcedSuspectName: string | null;
  forcedCitizenRoleKey: string | null;
}

export interface ActiveSaloonPoiDto {
  suspectId: string | null;
  suspectName: string | null;
  descriptor: string | null;
  personOfInterestKind: string | null;
  citizenRole: string | null;
}

export interface SaloonDevContextDto {
  sessionId: string;
  currentActionContext: string | null;
  currentTownId: string | null;
  currentTownName: string | null;
  sourceSpent: boolean;
  activeSaloonPoi: ActiveSaloonPoiDto | null;
  pendingDevOverride: DevSaloonOverrideDto | null;
  hiddenTruth: HiddenTruthDevDto | null;
  citizenInfo: CitizenInfoDto | null;
  suspects: SaloonSuspectDevDto[];
}

export interface ForceSaloonOverrideRequestDto {
  forcedKind: string;
  forcedSuspectId?: string | null;
  forcedCitizenRoleKey?: string | null;
}

export interface SaltPostureDevDto {
  mode: string;
  salt: string | null;
}

export interface ClockDevDto {
  day: number;
  turn: number;
  timeOfDay: string;
}

export interface SessionDevContextDto {
  sessionId: string;
  status: string;
  gameDifficulty: string;
  gameEntropy: string;
  saltPosture: SaltPostureDevDto;
  clock: ClockDevDto;
  currentTownId: string | null;
  currentTownName: string | null;
  currentActionContext: string;
  hasActiveJourney: boolean;
  seedCodeRetained: boolean;
  seedCodeText: string | null;
  travelRules: TravelRulesDevDto | null;
}

export interface TravelRulesDevDto {
  canteenCapacity: number;
  mountedRideDayProgress: number;
  footRideDayProgress: number;
  encounterFightAmmoHealthLoss: number;
  encounterFightUnarmedHealthLoss: number;
  encounterRunFootHealthLoss: number;
}

export interface LockRngRequestDto {
  salt?: string | null;
}

export interface ForceDevDifficultyRequestDto {
  difficulty: string;
}
