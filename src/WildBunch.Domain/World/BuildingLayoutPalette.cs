namespace WildBunch.Domain.World;

/// <summary>
/// Tile-based building layout palette for town hub surfaces. Encodes road topology
/// (spur count, spur positions, spur direction) and placement strategy in 4 bits.
/// Used by TownLayoutGenerator to generate deterministic tile-based layouts.
/// </summary>
public enum BuildingLayoutPalette
{
    // 0 spurs
    NoSpurs_SpreadEvenly = 0,
    NoSpurs_ClusterMiddle = 1,
    NoSpurs_FavorLeft = 2,
    NoSpurs_FavorRight = 3,

    // 1 spur (at middle row)
    OneSpurLeft_SpreadEvenly = 4,
    OneSpurLeft_ClusterMiddle = 5,
    OneSpurRight_SpreadEvenly = 6,
    OneSpurRight_ClusterMiddle = 7,

    // 2 spurs (at upper and lower middle rows)
    TwoSpursLeftRight_SpreadEvenly = 8,
    TwoSpursLeftRight_ClusterMiddle = 9,
    TwoSpursRightLeft_SpreadEvenly = 10,
    TwoSpursRightLeft_ClusterMiddle = 11,

    // Reserved for future expansion
    Reserved12 = 12,
    Reserved13 = 13,
    Reserved14 = 14,
    Reserved15 = 15
}
