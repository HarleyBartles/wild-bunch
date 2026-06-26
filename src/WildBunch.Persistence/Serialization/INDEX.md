# Serialization

JSON snapshot codecs and the session rehydrator, split by coherent domain area.

## Key files

- [GameSessionJsonSerializer.cs](GameSessionJsonSerializer.cs) - Core JSON serializer for session snapshots.
- [GameSessionJsonSerializer.SessionSnapshot.cs](GameSessionJsonSerializer.SessionSnapshot.cs) - Session snapshot partial.
- [GameSessionJsonSerializer.Setup.cs](GameSessionJsonSerializer.Setup.cs) - Setup partial.
- [GameSessionJsonSerializer.Components.cs](GameSessionJsonSerializer.Components.cs) - Components partial.
- [GameSessionJsonSerializer.Events.cs](GameSessionJsonSerializer.Events.cs) - Events partial.
- [GameSessionJsonSerializer.Travel.cs](GameSessionJsonSerializer.Travel.cs) - Travel partial.
- [GameSessionJsonSerializer.WantedSuspectPresence.cs](GameSessionJsonSerializer.WantedSuspectPresence.cs) - Wanted suspect presence partial.
- [GameSessionJsonSerializer.Log.cs](GameSessionJsonSerializer.Log.cs) - Legacy log partial.
- [GameSessionJsonSerializer.Rehydration.cs](GameSessionJsonSerializer.Rehydration.cs) - Rehydration partial.
- [GameSessionRehydrator.cs](GameSessionRehydrator.cs) - Rehydrates a GameSession from stored state.

Back to [WildBunch.Persistence/](../INDEX.md)
