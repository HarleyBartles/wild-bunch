# WildBunch.Domain

Domain layer: GameSession aggregate root and the pure domain model (cases, travel, inventory, economy, events, world).

## Subdirectories

- [Actions/](Actions/INDEX.md) - Available-action kinds and resolution.
- [Cases/](Cases/INDEX.md) - Case files, suspects, warrants, bounty, and confrontation models.
- [Economy/](Economy/INDEX.md) - Wallet, store catalog, and purchase results.
- [Events/](Events/INDEX.md) - Typed domain events.
- [Game/](Game/INDEX.md) - GameSession aggregate root, player, clock, dev overrides, and town aggregates.
- [Inventory/](Inventory/INDEX.md) - Inventory, canteen, horse, and item kinds.
- [Journal/](Journal/INDEX.md) - Journal resolver and snapshot.
- [Properties/](Properties/INDEX.md) - Assembly info.
- [Travel/](Travel/INDEX.md) - Travel journeys, encounters, trail events, diary, and rules.
- [WantedPosters/](WantedPosters/INDEX.md) - Wanted poster read results.
- [World/](World/INDEX.md) - World and town source models.

## Key files

- [WildBunch.Domain.csproj](WildBunch.Domain.csproj) - Project file.
- [IAggregateRoot.cs](IAggregateRoot.cs) - Marker interface for aggregate roots.

Back to [src/](../INDEX.md)
