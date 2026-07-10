namespace WildBunch.Application.Dev.Models;

/// <summary>
/// DTO for town layout salts in dev API. Includes resolver version and the
/// four split salts for buildings, roads, dirt, and props.
/// </summary>
public sealed record TownLayoutSaltsDto(
    string ResolverVersion,
    string BuildingsSalt,
    string RoadsSalt,
    string DirtSalt,
    string PropsSalt);
