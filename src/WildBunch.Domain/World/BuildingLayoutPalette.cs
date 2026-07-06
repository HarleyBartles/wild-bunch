namespace WildBunch.Domain.World;

/// <summary>
/// Canonical building layout patterns for town hub surfaces. Each palette
/// defines a deterministic arrangement of buildings, views, spur count, and
/// spur positions. Encoded in the seed (4 bits at positions 29-32) to
/// ensure the same town always produces the same layout.
/// </summary>
public enum BuildingLayoutPalette
{
    HubAndSpoke = 0,
    LinearChain = 1,
    DoubleLine = 2,
    Tree = 3,
    Star = 4,
    XShaped = 5,
    Cluster = 6,
    Grid = 7
}
