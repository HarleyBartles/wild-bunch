namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO for generated town layout salts in the game map contract. Includes resolver version and the
/// four split salts for buildings, roads, dirt, and props. Salts are nullable
/// to represent whether a generated layout carries those values.
/// </summary>
public sealed record TownLayoutSaltsDto(
    string? ResolverVersion,
    string? BuildingsSalt,
    string? RoadsSalt,
    string? DirtSalt,
    string? PropsSalt);
