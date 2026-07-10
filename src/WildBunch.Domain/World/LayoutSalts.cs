namespace WildBunch.Domain.World;

/// <summary>
/// Split layout salts for town hub layout generation. Each salt controls
/// a distinct layout concern: buildings, roads, dirt, and props. Salts are
/// derived from seed + entropy policy and used deterministically in layout
/// resolution. Same seed + same entropy policy = same derived salts = same layout.
/// </summary>
public sealed record LayoutSalts(
    string BuildingsSalt,
    string RoadsSalt,
    string DirtSalt,
    string PropsSalt);
