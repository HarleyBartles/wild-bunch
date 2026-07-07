using System;
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

        // Convert 2D int array to 2D array for DTO
        var tileGrid = new int[layout.TileGrid.GetLength(0)][];
        for (var row = 0; row < layout.TileGrid.GetLength(0); row++)
        {
            tileGrid[row] = new int[layout.TileGrid.GetLength(1)];
            for (var col = 0; col < layout.TileGrid.GetLength(1); col++)
            {
                tileGrid[row][col] = layout.TileGrid[row, col];
            }
        }

        return new TownLayoutDto(
            layout.Buildings.Select(ToDto).ToArray(),
            layout.PlayerSpawnX,
            layout.PlayerSpawnY,
            layout.Prosperity,
            layout.Paths.Select(ToDto).ToArray(),
            tileGrid);
    }

    private static BuildingPlacementDto ToDto(BuildingPlacement placement)
        => new(
            placement.Kind,
            placement.X,
            placement.Y,
            placement.View,
            placement.Width,
            placement.Height);

    private static PathSegmentDto ToDto(PathSegment path)
        => new(path.StartX, path.StartY, path.EndX, path.EndY);
}
