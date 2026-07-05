using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Mapping;

/// <summary>
/// Maps domain <see cref="TownLayout"/> to the <see cref="TownLayoutDto"/> read
/// model. The layout rides the existing GameSessionDto -> WorldDto -> TownDto
/// path; no separate endpoint is created.
/// </summary>
public static class TownLayoutMapper
{
    /// <summary>
    /// Maps a domain <see cref="TownLayout"/> to a <see cref="TownLayoutDto"/>.
    /// Returns null when the supplied layout is null (towns without a generated
    /// layout carry no layout on the read path).
    /// </summary>
    public static TownLayoutDto? ToDto(TownLayout? layout)
    {
        if (layout is null)
        {
            return null;
        }

        return new TownLayoutDto(
            layout.Buildings.Select(ToDto).ToArray(),
            layout.PlayerSpawnX,
            layout.PlayerSpawnY);
    }

    private static BuildingPlacementDto ToDto(BuildingPlacement placement)
        => new(
            placement.Kind,
            placement.X,
            placement.Y,
            placement.Width,
            placement.Height);
}
