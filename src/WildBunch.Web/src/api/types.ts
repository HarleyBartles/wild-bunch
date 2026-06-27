export type GameStatus = 0 | 1 | 2;
export type TravelDifficulty = 0 | 1 | 2;
export type JourneyStatus = 0 | 1 | 2 | 3;
export const JourneyStatus = {
  Active: 0,
  Interrupted: 1,
  Completed: 2,
  Failed: 3,
} as const;
export type TravelMode = 0 | 1;

export type AvailableActionKind = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14;
export const AvailableActionKind = {
  Travel: 0,
  ViewMap: 1,
  ViewJournal: 2,
  BuySupplies: 3,
  StayAtLodging: 4,
  VisitDoctor: 5,
  SendTelegram: 6,
  ReadWantedPosters: 7,
  AdvanceTravelDay: 8,
  ResolveTravelEncounter: 9,
  InspectNoticeBoard: 10,
  CheckSheriffRecords: 11,
  FollowTelegraphLeads: 12,
  GatherLocalGossip: 13,
  LookAroundSaloon: 14,
} as const;

export type TrailRisk = 1 | 2 | 3;
export type TownServices = number;
export type AliasKind = 0 | 1 | 2 | 3 | 4;
export type ClueKind = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10;
export type SuspectStatus = 0 | 1 | 2;
export type GameLogEntryKind = 0 | 1 | 2;
export type ItemKind = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type TrailTerrain = 0 | 1 | 2 | 3;
export type WaterFeature = 0 | 1 | 2 | 3;
export type ClueRecency = 0 | 1 | 2 | 3 | 4;
export type JourneyTrailEventKind = 0 | 1;
export type JourneyTrailEventId = 0 | 1 | 2 | 3 | 4 | 5 | 6;
export type StoreVendorType = 0 | 1 | 2;
export type StoreOfferAvailability = 0 | 1;
export type CaseIdentityKind = 0 | 1 | 2 | 3 | 4;
export type CaseIdentityStatus = 0 | 1 | 2 | 3;

export interface StartGameRequest {
  playerName: string;
  travelDifficulty: TravelDifficulty;
  seedCode?: string | null;
  startingTownId?: string | null;
}

export interface TravelRequest {
  destinationTownId: string;
}

export interface ResolveJourneyEncounterRequest {
  choiceId: string;
  bulletSpend?: number | null;
  bribeAmount?: number | null;
}

export interface GameClockDto {
  day: number;
  turn: number;
  timeOfDay: string;
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
  horseState: HorseTravelStateDto | null;
  canteenState: CanteenStateDto | null;
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
  horseState: HorseTravelStateDto | null;
  canteenState: CanteenStateDto | null;
  capabilities: InventoryCapabilitiesDto;
}

export interface HorseTravelStateDto {
  hunger: number;
  thirst: number;
  exhaustion: number;
  isLame: boolean;
  isDead: boolean;
  canProvideMountedTravel: boolean;
}

export interface CanteenStateDto {
  charges: number;
  capacity: number;
  hasWater: boolean;
}

export interface TravelRouteProfileDto {
  trailId: string;
  risk: TrailRisk;
  terrain: TrailTerrain;
  waterFeature: WaterFeature;
  rideDayDistance: number;
  mountedRideDayProgress: number;
  footRideDayProgress: number;
  warnings: string[];
}

export interface TravelPreviewDto {
  originTownId: string;
  originTownName: string;
  destinationTownId: string;
  destinationTownName: string;
  travelMode: TravelMode;
  mountedTravelAvailable: boolean;
  waterSecure: boolean;
  rideDayDistance: number;
  remainingRideDayDistance: number;
  baselineRideDays: number;
  expectedDays: number;
  remainingDays: number;
  canteenChargesPerDay: number;
  requiredCanteenCharges: number;
  availableCanteenCharges: number;
  canteenReserveCharges: number;
  delayMarginDays: number;
  delayRisk: boolean;
  requiredFood: number;
  availableFood: number;
  requiredHorseFeed: number;
  availableHorseFeed: number;
  horseState: HorseTravelStateDto | null;
  warnings: string[];
  routeProfile: TravelRouteProfileDto;
}

export interface TravelPreviewResultDto {
  success: boolean;
  message: string;
  preview: TravelPreviewDto | null;
}

export interface JourneyEncounterChoiceDto {
  id: string;
  label: string;
}

export interface JourneyEncounterDto {
  kind: string;
  message: string;
  choices: JourneyEncounterChoiceDto[];
}

export interface TravelJourneyDto {
  originTownId: string;
  originTownName: string;
  destinationTownId: string;
  destinationTownName: string;
  travelMode: TravelMode;
  status: JourneyStatus;
  mountedTravelAvailable: boolean;
  waterSecure: boolean;
  rideDayDistance: number;
  remainingRideDayDistance: number;
  baselineRideDays: number;
  expectedDays: number;
  remainingDays: number;
  canteenChargesPerDay: number;
  requiredCanteenCharges: number;
  availableCanteenCharges: number;
  canteenReserveCharges: number;
  delayMarginDays: number;
  delayRisk: boolean;
  requiredFood: number;
  availableFood: number;
  requiredHorseFeed: number;
  availableHorseFeed: number;
  horseState: HorseTravelStateDto | null;
  daysTravelled: number;
  delayDays: number;
  pendingEncounter: JourneyEncounterDto | null;
  warnings: string[];
  routeProfile: TravelRouteProfileDto;
}

export interface TravelDiaryEncounterResolutionDto {
  choiceId: string;
  choiceLabel: string;
  healthDelta: number;
  walletDelta: number;
  ammoSpent: number;
  heatIncrease: number;
  horseExhaustionDelta: number;
  continuedOnFoot: boolean;
}

export interface TravelDiaryDayDto {
  dayNumber: number;
  originTownName: string;
  destinationTownName: string;
  startingTravelMode: TravelMode;
  endingTravelMode: TravelMode;
  status: JourneyStatus;
  startingRideDayDistance: number;
  remainingRideDayDistance: number;
  startingDaysRemaining: number;
  remainingDays: number;
  horseStateBefore: HorseTravelStateDto | null;
  horseStateAfter: HorseTravelStateDto | null;
  trailEvent: JourneyTrailEventDto | null;
  pendingEncounter: JourneyEncounterDto | null;
  encounterResolution: TravelDiaryEncounterResolutionDto | null;
  healthDelta: number;
  walletDelta: number;
  foodDelta: number;
  horseFeedDelta: number;
  canteenChargeDelta: number;
  ammoSpent: number;
  horseHungerDelta: number;
  horseThirstDelta: number;
  horseExhaustionDelta: number;
  delayDays: number;
  heatIncrease: number;
  currentHealth: number;
  currentWallet: number;
  currentFood: number;
  currentHorseFeed: number;
  currentCanteenCharges: number;
  currentAmmo: number;
  currentHeat: number;
  openingNarration: string | null;
  journeyBeat: string | null;
  resourceBeat: string | null;
  entries: string[];
  warnings: string[];
}

export interface TravelDiaryDto {
  days: TravelDiaryDayDto[];
}

export interface PlayerDto {
  name: string;
  currentTownId: string;
  health: number;
}

export interface TownDto {
  id: string;
  name: string;
  services: TownServices;
}

export interface StoreOfferDto {
  itemKind: ItemKind;
  displayName: string;
  price: number;
  vendorType: StoreVendorType;
  availability: StoreOfferAvailability;
  sourceNote: string;
}

export interface TownStoreOffersDto {
  townId: string;
  townName: string;
  available: boolean;
  sourceNote: string;
  offers: StoreOfferDto[];
}

export interface BuyStoreItemRequest {
  vendorType: StoreVendorType | null;
  itemKind: ItemKind | null;
  quantity: number;
}

export interface TrailDto {
  id: string;
  fromTownId: string;
  toTownId: string;
  risk: TrailRisk;
  terrain: TrailTerrain;
  waterFeature: WaterFeature;
  rideDayDistance: number;
}

export interface WorldDto {
  towns: TownDto[];
  trails: TrailDto[];
}

export interface ClueDto {
  id: string;
  kind: ClueKind;
  description: string;
  sourceLabel: string | null;
  context: string | null;
  anchors: ClueAnchorsDto;
}

export interface WarrantDto {
  targetName: string;
  summary: string;
  issuingSource: string;
  disposition: number;
  bountyAmount: number;
}

export interface DiscoveredSuspectDto {
  id: string;
  name: string;
  status: SuspectStatus;
}

export interface CaseBoardDto {
  namedRecords: CaseIdentityHandleDto[];
  looseLeads: CaseIdentityHandleDto[];
  evidenceItems: CaseEvidenceItemDto[];
}

export interface CaseIdentityHandleDto {
  id: string;
  displayName: string;
  kind: CaseIdentityKind;
  status: CaseIdentityStatus;
  resolvedToDisplayName: string | null;
  evidenceIds: string[];
  summaryLines: string[];
  relatedLabels: string[];
  knownAliases: string[];
  distinguishingFeatures: string[];
  warrantDisposition: number | null;
  bountyAmount: number | null;
  issuingAuthority: string | null;
  crimeSummary: string | null;
}

export interface CaseEvidenceItemDto {
  id: string;
  kindLabel: string;
  sourceLabel: string;
  summary: string;
  identityBearing: boolean;
  anchors: ClueAnchorsDto;
  handleIds: string[];
}

export interface CaseStateDto {
  statusText: string;
}

export interface ClueAnchorsDto {
  subjects: ClueSubjectAnchorDto[];
  locations: ClueLocationAnchorDto[];
  times: ClueTimeAnchorDto[];
  directions: ClueDirectionAnchorDto[];
}

export interface ClueSubjectAnchorDto {
  label: string;
  alias: string | null;
  feature: string | null;
  fact: string | null;
}

export interface ClueLocationAnchorDto {
  label: string;
  place: string | null;
  route: string | null;
}

export interface ClueTimeAnchorDto {
  recency: ClueRecency;
  day: number | null;
  turn: number | null;
}

export interface ClueDirectionAnchorDto {
  label: string;
  movement: string | null;
  route: string | null;
}

export interface CaseFileDto {
  accusationId: string | null;
  openingLead: string;
  caseState: CaseStateDto;
  discoveredSuspects: DiscoveredSuspectDto[];
  caseBoard: CaseBoardDto;
  knownClues: ClueDto[];
}

export interface GameLogEntryDto {
  kind: GameLogEntryKind;
  message: string;
  day: number;
  turn: number;
}

export interface HudProjection {
  sessionId: string;
  status: GameStatus;
  playerName: string;
  health: number;
  walletCash: number;
  currentTownId: string;
  currentTownName: string;
  inventoryItems: HudInventoryItem[];
}

export interface HudInventoryItem {
  itemKind: ItemKind;
  quantity: number;
}

export interface DiaryProjection {
  sessionId: string;
  day: number;
  turn: number;
  currentTownId: string;
  currentTownName: string;
  entries: DiaryEntry[];
}

export interface DiaryEntry {
  day: number;
  turn: number;
  summary: string;
}

export interface JourneyTrailEventDto {
  id: JourneyTrailEventId;
  kind: JourneyTrailEventKind;
  title: string;
  message: string;
  walletDelta: number;
  foodDelta: number;
  canteenChargeDelta: number;
  horseHungerDelta: number;
  horseThirstDelta: number;
  horseExhaustionDelta: number;
  delayDays: number;
  heatIncrease: number;
}

export interface GameSessionDto {
  id: string;
  status: GameStatus;
  travelDifficulty: TravelDifficulty;
  player: PlayerDto;
  world: WorldDto;
  caseFile: CaseFileDto;
  inventory: InventoryDto;
  clock: GameClockDto;
  pursuitState: PursuitStateDto;
  journey: TravelJourneyDto | null;
  travelDiary: TravelDiaryDto | null;
  logEntries: GameLogEntryDto[];
  activeSaloonPersonOfInterest: ActiveSaloonPersonOfInterestDto | null;
  activeSaloonWantedSuspect?: ActiveSaloonWantedSuspectDto | null;
  hudProjection?: HudProjection | null;
  diaryProjection?: DiaryProjection | null;
}

export interface ActiveSaloonPersonOfInterestDto {
  descriptor: string;
  kind: SaloonPersonOfInterestKind;
}

export interface ActiveSaloonWantedSuspectDto {
  descriptor: string;
}

export enum SaloonPersonOfInterestKind {
  Citizen = 0,
  WantedSuspect = 1,
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
  caseState: CaseStateDto;
  caseSummary: string;
  discoveredSuspects: DiscoveredSuspectDto[];
  caseBoard: CaseBoardDto;
  knownClues: ClueDto[];
  knownWarrants: WarrantDto[];
  wantedPosters: WantedPosterDto[];
}

export interface JournalDto {
  id: string;
  status: GameStatus;
  clock: GameClockDto;
  currentTown: JournalTownDto;
  caseFile: JournalCaseFileDto;
  logEntries: GameLogEntryDto[];
}


export interface WantedPosterDto {
  posterId: string;
  targetDisplayName: string;
  aliases: string[];
  legalTerms: WantedPosterLegalTermsDto;
  quickView: WantedPosterQuickViewDto;
  details: WantedPosterDetailsDto;
  publicSafeClassification: string | null;
}

export interface WantedPosterLegalTermsDto {
  disposition: number;
  bountyAmount: number;
  issuingAuthority: string;
}

export interface WantedPosterQuickViewDto {
  headlineNameOrAlias: string;
  headlineFeatureOrDescriptor: string;
  pocketCheckDescriptor: string;
}

export interface WantedPosterDetailsDto {
  summary: string;
  publicOrigin: string;
  features: WantedPosterFeatureDto[];
}

export interface WantedPosterFeatureDto {
  text: string;
  salience: WantedPosterFeatureSalience;
  renderMode: WantedPosterFeatureRenderMode;
}

export type WantedPosterFeatureSalience = 0 | 1 | 2;
export const WantedPosterFeatureSalience = {
  Headline: 0,
  Supporting: 1,
  Buried: 2,
} as const;

export type WantedPosterFeatureRenderMode = 0 | 1;
export const WantedPosterFeatureRenderMode = {
  PortraitRenderable: 0,
  TextOnly: 1,
} as const;

export interface WantedPostersResultDto {
  success: boolean;
  message: string;
  currentJournal: JournalDto;
  wantedPosters: WantedPosterDto[];
}

export interface InvestigationActionResultDto {
  success: boolean;
  message: string;
  currentJournal: JournalDto;
}

export interface GameTurnResultDto {
  success: boolean;
  message: string;
  currentSession: GameSessionDto;
  journeyStatus?: JourneyStatus | null;
  journey?: TravelJourneyDto | null;
  trailEvent?: JourneyTrailEventDto | null;
  travelDiary?: TravelDiaryDto | null;
}

export interface SaloonPersonOfInterestConfrontationResultDto {
  success: boolean;
  message: string;
  outcome: number;
  currentSession: GameSessionDto;
  declaredWantedIdentityHandle: string | null;
  targetName: string | null;
  disposition: number | null;
  isAlive: boolean | null;
  isSecured: boolean | null;
  isCitizen: boolean | null;
  fineAmount: number | null;
  walletBefore: number | null;
  walletAfter: number | null;
  sessionChanged: boolean;
  personOfInterestKind?: SaloonPersonOfInterestKind | null;
}

export interface WantedSuspectConfrontationResultDto extends SaloonPersonOfInterestConfrontationResultDto {}

export interface PrologueDto {
  heading: string;
  body: string;
  primaryAction: string;
  variantId: string;
}
