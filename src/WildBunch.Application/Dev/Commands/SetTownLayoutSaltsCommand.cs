namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Command to set town layout salts for a game session. Used by dev overlay
/// to control layout generation at setup time.
/// </summary>
public sealed record SetTownLayoutSaltsCommand(
    Guid GameId,
    string BuildingsSalt,
    string RoadsSalt,
    string DirtSalt,
    string PropsSalt);
