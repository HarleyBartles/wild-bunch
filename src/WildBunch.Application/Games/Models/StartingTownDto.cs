using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

public sealed record StartingTownDto(
    string Id,
    string Name,
    TownServices Services);
