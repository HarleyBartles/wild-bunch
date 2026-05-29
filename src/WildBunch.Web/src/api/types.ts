export type GameStatus = 0 | 1 | 2;

export type AvailableActionKind = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7;
export const AvailableActionKind = {
  Travel: 0,
  ViewMap: 1,
  ViewJournal: 2,
  BuySupplies: 3,
  StayAtLodging: 4,
  VisitDoctor: 5,
  SendTelegram: 6,
  ReadWantedPosters: 7,
} as const;

export type TrailRisk = 1 | 2 | 3;
export type TownServices = number;
export type SuspectStatus = 0 | 1 | 2;
export type AliasKind = 0 | 1 | 2 | 3 | 4;
export type ClueKind = 0 | 1 | 2 | 3;
export type GameLogEntryKind = 0 | 1 | 2;
export type ItemKind = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type HorseCondition = 0 | 1 | 2;

export interface StartGameRequest {
  playerName: string;
}

export interface TravelRequest {
  destinationTownId: string;
}

export interface GameClockDto {
  day: number;
  turn: number;
}

export interface PursuitStateDto {
  heat: number;
}

export interface WalletDto {
  cash: number;
}

export interface InventoryItemDto {
  kind: ItemKind;
  quantity: number;
  horseCondition: HorseCondition | null;
}

export interface InventoryCapabilitiesDto {
  mountedTravelAvailable: boolean;
  horseUpkeepRequired: boolean;
  normalRouteWaterSecure: boolean;
  trailUtility: boolean;
  closeThreatAvailable: boolean;
  firearmThreatAvailable: boolean;
  gunfightCapable: boolean;
  revolverUsable: boolean;
  rifleUsable: boolean;
}

export interface InventoryDto {
  wallet: WalletDto;
  items: InventoryItemDto[];
  horseCondition: HorseCondition | null;
  capabilities: InventoryCapabilitiesDto;
}

export interface PlayerDto {
  name: string;
  currentTownId: string;
  health: number;
  money: number;
  supplies: number;
}

export interface TownDto {
  id: string;
  name: string;
  services: TownServices;
}

export interface TrailDto {
  id: string;
  fromTownId: string;
  toTownId: string;
  supplyCost: number;
  risk: TrailRisk;
}

export interface WorldDto {
  towns: TownDto[];
  trails: TrailDto[];
}

export interface SuspectTraitsDto {
  isLocal: boolean;
  isArmed: boolean;
  isDesperate: boolean;
}

export interface SuspectDto {
  id: string;
  name: string;
  profile: SuspectProfileDto;
  traits: SuspectTraitsDto;
  status: SuspectStatus;
}

export interface ClueDto {
  id: string;
  kind: ClueKind;
  description: string;
}

export interface SuspectAliasDto {
  name: string;
  kind: AliasKind;
}

export interface SuspectIdentityFactDto {
  description: string;
}

export interface SuspectProfileDto {
  aliases: SuspectAliasDto[];
  identifyingFacts: SuspectIdentityFactDto[];
}

export interface KillerReleaseStateDto {
  isReleased: boolean;
  progress: number;
  requiredPublicClues: number;
  statusText: string;
}

export interface CaseFileDto {
  accusationId: string | null;
  openingLead: string;
  killerReleaseState: KillerReleaseStateDto;
  suspects: SuspectDto[];
  knownClues: ClueDto[];
}

export interface GameLogEntryDto {
  kind: GameLogEntryKind;
  message: string;
  day: number;
  turn: number;
}

export interface GameSessionDto {
  id: string;
  status: GameStatus;
  player: PlayerDto;
  world: WorldDto;
  caseFile: CaseFileDto;
  inventory: InventoryDto;
  clock: GameClockDto;
  pursuitState: PursuitStateDto;
  logEntries: GameLogEntryDto[];
}

export interface AvailableActionDto {
  kind: AvailableActionKind;
  label: string;
}

export interface JournalTownDto {
  id: string;
  name: string;
}

export interface JournalCaseFileDto {
  accusationId: string | null;
  openingLead: string;
  killerReleaseState: KillerReleaseStateDto;
  caseSummary: string;
  suspects: SuspectDto[];
  knownClues: ClueDto[];
}

export interface JournalDto {
  id: string;
  status: GameStatus;
  clock: GameClockDto;
  currentTown: JournalTownDto;
  caseFile: JournalCaseFileDto;
  logEntries: GameLogEntryDto[];
}

export interface WantedPostersResultDto {
  success: boolean;
  message: string;
  currentJournal: JournalDto;
}

export interface GameTurnResultDto {
  success: boolean;
  message: string;
  currentSession: GameSessionDto;
}
