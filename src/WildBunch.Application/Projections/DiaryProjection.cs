using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Projections;

/// <summary>
/// Diary projection: the player's travel diary derived from domain events.
/// This is a read-only projection — it does not mutate aggregate state.
/// See ADR-0028.
/// </summary>
public sealed record DiaryProjection(
    Guid SessionId,
    int Day,
    int Turn,
    TownId CurrentTownId,
    string CurrentTownName,
    IReadOnlyList<DiaryEntry> Entries) : IProjectionResult;

public sealed record DiaryEntry(
    int Day,
    int Turn,
    string Summary);
