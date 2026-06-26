# WildBunch.Application.Tests

Unit tests for the Application layer (commands, queries, mappers, projections, dev controls).

## Subdirectories

- [Dev/](Dev/INDEX.md) - Dev override handler tests.
- [Execution/](Execution/INDEX.md) - Command execution pipeline tests.
- [Projections/](Projections/INDEX.md) - Projection and projector tests.
- [TestDoubles/](TestDoubles/INDEX.md) - In-memory repository and stub factory test doubles.

## Key files

- [WildBunch.Application.Tests.csproj](WildBunch.Application.Tests.csproj) - Project file.
- [StartNewGameHandlerTests.cs](StartNewGameHandlerTests.cs) - Start new game handler tests.
- [AdvanceTravelDayHandlerTests.cs](AdvanceTravelDayHandlerTests.cs) - Advance travel day handler tests.
- [TravelToTownHandlerTests.cs](TravelToTownHandlerTests.cs) - Travel to town handler tests.
- [ResolveJourneyEncounterHandlerTests.cs](ResolveJourneyEncounterHandlerTests.cs) - Journey encounter resolution tests.
- [PurchaseStoreItemHandlerTests.cs](PurchaseStoreItemHandlerTests.cs) - Store purchase handler tests.
- [GetAvailableActionsHandlerTests.cs](GetAvailableActionsHandlerTests.cs) - Available actions query tests.
- [GetGameSessionHandlerTests.cs](GetGameSessionHandlerTests.cs) - Game session query tests.
- [GetJournalHandlerTests.cs](GetJournalHandlerTests.cs) - Journal query tests.
- [GetTownStoreOffersHandlerTests.cs](GetTownStoreOffersHandlerTests.cs) - Town store offers query tests.
- [PreviewTravelHandlerTests.cs](PreviewTravelHandlerTests.cs) - Travel preview query tests.
- [CheckSheriffRecordsHandlerTests.cs](CheckSheriffRecordsHandlerTests.cs) - Check sheriff records handler tests.
- [ConfrontSaloonWantedSuspectHandlerTests.cs](ConfrontSaloonWantedSuspectHandlerTests.cs) - Confront saloon wanted suspect tests.
- [ConfrontWantedSuspectHandlerTests.cs](ConfrontWantedSuspectHandlerTests.cs) - Confront wanted suspect tests.
- [InspectNoticeBoardHandlerTests.cs](InspectNoticeBoardHandlerTests.cs) - Inspect notice board tests.
- [InvestigationSourceHandlerTests.cs](InvestigationSourceHandlerTests.cs) - Investigation source handler tests.
- [ReadWantedPostersHandlerTests.cs](ReadWantedPostersHandlerTests.cs) - Read wanted posters tests.
- [TurnInToSheriffHandlerTests.cs](TurnInToSheriffHandlerTests.cs) - Turn in to sheriff tests.
- [CaseBoardMapperTests.cs](CaseBoardMapperTests.cs) - Case board mapper tests.
- [JournalMapperTests.cs](JournalMapperTests.cs) - Journal mapper tests.
- [TravelDiaryMapperTests.cs](TravelDiaryMapperTests.cs) - Travel diary mapper tests.
- [TravelDiaryTextRendererTests.cs](TravelDiaryTextRendererTests.cs) - Travel diary text renderer tests.
- [WantedPosterMapperTests.cs](WantedPosterMapperTests.cs) - Wanted poster mapper tests.
- [GameSessionDtoProjectionFieldsTests.cs](GameSessionDtoProjectionFieldsTests.cs) - DTO projection field tests.
- [QueryHandlersAreReadOnlyTests.cs](QueryHandlersAreReadOnlyTests.cs) - Guardrail: query handlers are read-only.
- [AddLogEntryGuardrailTests.cs](AddLogEntryGuardrailTests.cs) - Guardrail: no direct log entry additions.
- [ReadStoreLoaderJournalProjectionGuardrailTests.cs](ReadStoreLoaderJournalProjectionGuardrailTests.cs) - Guardrail: read-store journal projection.
- [SaloonPersonOfInterestDescriptorParityTests.cs](SaloonPersonOfInterestDescriptorParityTests.cs) - Saloon POI descriptor parity tests.

Back to [tests/](../INDEX.md)
