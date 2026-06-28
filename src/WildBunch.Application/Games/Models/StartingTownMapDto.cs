using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

public sealed record StartingTownMapDto(
    IReadOnlyList<StartingTownMapTownDto> Towns,
    IReadOnlyList<StartingTownMapTrailDto> Trails);

public sealed record StartingTownMapTownDto(
    string Id,
    string Name,
    TownServices Services,
    int X,
    int Y);

public sealed record StartingTownMapTrailDto(
    string Id,
    string FromTownId,
    string ToTownId,
    decimal RideDayDistance);
