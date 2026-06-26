# WildBunch.Domain.Tests

Unit tests for the Domain layer (GameSession aggregate, cases, travel, inventory, events, world).

## Subdirectories

- [Events/](Events/INDEX.md) - Typed domain event and event-sourcing tests.

## Key files

- [WildBunch.Domain.Tests.csproj](WildBunch.Domain.Tests.csproj) - Project file.
- [GameSessionAggregateRootTests.cs](GameSessionAggregateRootTests.cs) - GameSession aggregate root tests.
- [GameSessionBountyLoopCoordinatorTests.cs](GameSessionBountyLoopCoordinatorTests.cs) - Bounty loop coordinator tests.
- [GameSessionInvestigationActionsTests.cs](GameSessionInvestigationActionsTests.cs) - Investigation action tests.
- [GameSessionJourneyHistoryTests.cs](GameSessionJourneyHistoryTests.cs) - Journey history tests.
- [GameSessionPurchaseTests.cs](GameSessionPurchaseTests.cs) - Purchase tests.
- [GameSessionSaloonPersonOfInterestTests.cs](GameSessionSaloonPersonOfInterestTests.cs) - Saloon POI tests.
- [GameSessionSaloonWantedSuspectLoopTests.cs](GameSessionSaloonWantedSuspectLoopTests.cs) - Saloon wanted suspect loop tests.
- [GameSessionSheriffTurnInTests.cs](GameSessionSheriffTurnInTests.cs) - Sheriff turn-in tests.
- [GameSessionWantedPostersTests.cs](GameSessionWantedPostersTests.cs) - Wanted posters tests.
- [GameSessionWantedSuspectConfrontationTests.cs](GameSessionWantedSuspectConfrontationTests.cs) - Wanted suspect confrontation tests.
- [GameSessionWantedSuspectPresenceTests.cs](GameSessionWantedSuspectPresenceTests.cs) - Wanted suspect presence tests.
- [BountyDeclarationMatchPolicyTests.cs](BountyDeclarationMatchPolicyTests.cs) - Bounty declaration match policy tests.
- [BountySettlementPolicyTests.cs](BountySettlementPolicyTests.cs) - Bounty settlement policy tests.
- [CaseFileTests.cs](CaseFileTests.cs) / [CaseFilePeekTests.cs](CaseFilePeekTests.cs) - Case file tests.
- [CaseInvestigationFoundationTests.cs](CaseInvestigationFoundationTests.cs) - Case investigation foundation tests.
- [CaseProgressTests.cs](CaseProgressTests.cs) - Case progress tests.
- [InventoryTests.cs](InventoryTests.cs) / [InventoryCapabilityResolverTests.cs](InventoryCapabilityResolverTests.cs) - Inventory tests.
- [PlayerTests.cs](PlayerTests.cs) - Player tests.
- [ClockTurnCorrectionTests.cs](ClockTurnCorrectionTests.cs) - Clock turn correction tests.
- [TownAggregateTests.cs](TownAggregateTests.cs) - Town aggregate tests.
- [TownActionAvailabilityTests.cs](TownActionAvailabilityTests.cs) - Town action availability tests.
- [TownSourceCatalogTests.cs](TownSourceCatalogTests.cs) - Town source catalog tests.
- [TownStoreCatalogResolverTests.cs](TownStoreCatalogResolverTests.cs) - Town store catalog resolver tests.
- [TownVisitStateTests.cs](TownVisitStateTests.cs) - Town visit state tests.
- [TravelResolverTests.cs](TravelResolverTests.cs) - Travel resolver tests.
- [TravelDayPlanGeneratorTests.cs](TravelDayPlanGeneratorTests.cs) - Travel day plan generator tests.
- [TravelDiaryCharacterizationTests.cs](TravelDiaryCharacterizationTests.cs) - Travel diary characterization tests.
- [TravelDiaryDayFactoryTests.cs](TravelDiaryDayFactoryTests.cs) - Travel diary day factory tests.
- [TravelEncounterResolutionCharacterizationTests.cs](TravelEncounterResolutionCharacterizationTests.cs) - Travel encounter resolution characterization tests.
- [TravelEventApplyTests.cs](TravelEventApplyTests.cs) - Travel event apply tests.
- [TravelReplayEqualityTests.cs](TravelReplayEqualityTests.cs) - Travel replay equality tests.
- [TravelResourceTrackingCharacterizationTests.cs](TravelResourceTrackingCharacterizationTests.cs) - Travel resource tracking characterization tests.
- [TravelRulesProfileTests.cs](TravelRulesProfileTests.cs) - Travel rules profile tests.
- [TravelStateMachineCharacterizationTests.cs](TravelStateMachineCharacterizationTests.cs) - Travel state machine characterization tests.
- [JourneyUpkeepRulesTests.cs](JourneyUpkeepRulesTests.cs) - Journey upkeep rules tests.
- [JournalResolverTests.cs](JournalResolverTests.cs) - Journal resolver tests.
- [JournalLogProjectorEquivalenceTests.cs](JournalLogProjectorEquivalenceTests.cs) - Journal log projector equivalence tests.
- [InvestigationEventSourcingTests.cs](InvestigationEventSourcingTests.cs) - Investigation event sourcing tests.
- [BountySaloonEventSourcingTests.cs](BountySaloonEventSourcingTests.cs) - Bounty saloon event sourcing tests.
- [HeatSemanticGuardrailTests.cs](HeatSemanticGuardrailTests.cs) - Heat semantic guardrail tests.
- [DevSaloonOverrideTests.cs](DevSaloonOverrideTests.cs) - Dev saloon override tests.
- [DevTravelOverrideTests.cs](DevTravelOverrideTests.cs) - Dev travel override tests.
- [ActionAvailabilityResolverTests.cs](ActionAvailabilityResolverTests.cs) - Action availability resolver tests.
- [TestSessionFactory.cs](TestSessionFactory.cs) - Test session factory helper.
- [TravelTestFactory.cs](TravelTestFactory.cs) - Travel test factory helper.

Back to [tests/](../INDEX.md)
