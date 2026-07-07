namespace WildBunch.Domain.World;

/// <summary>
/// Viewing angle for building sprites in the town hub surface.
/// Determines which sprite asset to load for rendering.
/// </summary>
public enum BuildingView
{
    Front,
    Profile,
    Rear,
    FrontOblique,
    RearOblique
}
