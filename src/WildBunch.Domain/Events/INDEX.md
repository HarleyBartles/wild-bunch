# Events

Typed domain events emitted by the GameSession aggregate and dev overrides.

## Key files

- [IDomainEvent.cs](IDomainEvent.cs) - Domain event marker contract.
- [GameStarted.cs](GameStarted.cs) - Game started event.
- [TravelDayAdvanced.cs](TravelDayAdvanced.cs) - Travel day advanced event.
- [JourneyStarted.cs](JourneyStarted.cs) - Journey started event.
- [JourneyCompleted.cs](JourneyCompleted.cs) - Journey completed event.
- [JourneyArrivalAcknowledged.cs](JourneyArrivalAcknowledged.cs) - Journey arrival acknowledged event.
- [JourneyEncounterResolved.cs](JourneyEncounterResolved.cs) - Journey encounter resolved event.
- [TrailEventApplied.cs](TrailEventApplied.cs) - Trail event applied event.
- [TownActionContextEntered.cs](TownActionContextEntered.cs) - Town action context entered event.
- [InvestigationPerformed.cs](InvestigationPerformed.cs) - Investigation performed event.
- [SaloonPersonOfInterestSpotted.cs](SaloonPersonOfInterestSpotted.cs) - Saloon POI spotted event.
- [SaloonPersonOfInterestConfronted.cs](SaloonPersonOfInterestConfronted.cs) - Saloon POI confronted event.
- [WantedSuspectConfronted.cs](WantedSuspectConfronted.cs) - Wanted suspect confronted event.
- [SheriffTurnInSettled.cs](SheriffTurnInSettled.cs) - Sheriff turn-in settled event.
- [StoreItemPurchased.cs](StoreItemPurchased.cs) - Store item purchased event.
- [DevTravelOverrideForced.cs](DevTravelOverrideForced.cs) / [DevTravelOverrideCleared.cs](DevTravelOverrideCleared.cs) / [DevTravelOverrideConsumed.cs](DevTravelOverrideConsumed.cs) - Dev travel override events.
- [DevSaloonOverrideForced.cs](DevSaloonOverrideForced.cs) / [DevSaloonOverrideCleared.cs](DevSaloonOverrideCleared.cs) / [DevSaloonOverrideConsumed.cs](DevSaloonOverrideConsumed.cs) - Dev saloon override events.

Back to [WildBunch.Domain/](../INDEX.md)
