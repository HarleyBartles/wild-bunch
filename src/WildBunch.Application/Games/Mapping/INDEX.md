# Mapping

Domain-to-DTO mappers and the game turn result factory.

## Key files

- [GameSessionMapper.cs](GameSessionMapper.cs) - Maps GameSession to the primary game DTO (delegates travel shape to TravelMapper).
- [TravelMapper.cs](TravelMapper.cs) - Maps travel, journey, and encounter state to DTOs.
- [TravelDiaryMapper.cs](TravelDiaryMapper.cs) - Maps travel diary day records to DTOs.
- [TravelDiaryTextRenderer.cs](TravelDiaryTextRenderer.cs) - Renders travel diary text.
- [JournalMapper.cs](JournalMapper.cs) - Maps journal entries to DTOs.
- [CaseReadMapper.cs](CaseReadMapper.cs) - Maps case file state to read DTOs.
- [CaseBoardMapper.cs](CaseBoardMapper.cs) - Maps case board state to DTOs.
- [WantedPosterMapper.cs](WantedPosterMapper.cs) - Maps wanted poster state to DTOs.
- [InventoryMapper.cs](InventoryMapper.cs) - Maps inventory state to DTOs.
- [StoreCatalogMapper.cs](StoreCatalogMapper.cs) - Maps town store catalog to DTOs.
- [AvailableActionMapper.cs](AvailableActionMapper.cs) - Maps available actions to DTOs.
- [GameTurnResultFactory.cs](GameTurnResultFactory.cs) - Builds the composite game turn result.
- [GameSessionLogProjection.cs](GameSessionLogProjection.cs) - Legacy game log projection helper.

Back to [Games/](../INDEX.md)
