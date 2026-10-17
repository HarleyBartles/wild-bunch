namespace WildBunch.Application.Games.Models;

public sealed record WorldMapDto(
    IReadOnlyList<WorldMapTownDto> Towns,
    IReadOnlyList<WorldMapTrailDto> Trails);

public sealed record WorldMapTownDto(
    string Id,
    string Name,
    int X,
    int Y);

public sealed record WorldMapTrailDto(
    string Id,
    string FromTownId,
    string ToTownId,
    decimal RideDayDistance);
