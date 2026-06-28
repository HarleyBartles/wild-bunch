using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Game;

/// <summary>
/// The kind of saloon POI that a dev override can force.
/// Suspect: spot a specific (or first eligible) wanted suspect.
/// Citizen: spot a town citizen from the source-backed cast.
/// None: spot nobody of interest (the saloon is quiet).
/// The false-lead outcome is not a separate override kind — it comes from the
/// normal confrontation flow when the player declares a wrong wanted identity
/// on a citizen POI. To test the false-lead path, force a Citizen override and
/// then make a wrong declaration during confrontation.
/// </summary>
public enum DevSaloonPoiKind
{
    Suspect = 0,
    Citizen = 1,
    None = 2
}

/// <summary>
/// Pending dev override for the next saloon look-around.
/// When present, LookAroundSaloon uses this instead of the normal roll.
/// Consumed once by the next look-around, then cleared from aggregate state.
/// This is dev-only session state, not player-facing. See BUNCH-90.
/// </summary>
public sealed record DevSaloonOverride(
    DevSaloonPoiKind ForcedKind,
    SuspectId? ForcedSuspectId,
    string? ForcedCitizenRoleKey)
{
    /// <summary>
    /// Force the next look-around to spot a specific suspect by ID.
    /// The suspect must exist in the case file and must not be the unreleased
    /// true killer. The aggregate command method validates these rules at force
    /// time. See BUNCH-90.
    /// </summary>
    public static DevSaloonOverride ForSuspect(SuspectId suspectId)
        => new(DevSaloonPoiKind.Suspect, suspectId, null);

    /// <summary>
    /// Force the next look-around to spot the first eligible suspect.
    /// No suspect-specific validation needed at force time - the consume
    /// path uses normal candidate selection, which already enforces eligibility.
    /// </summary>
    public static DevSaloonOverride ForAnySuspect()
        => new(DevSaloonPoiKind.Suspect, null, null);

    /// <summary>
    /// Force the next look-around to spot a town citizen from the source-backed cast.
    /// To test the false-lead confrontation path, force a Citizen override and
    /// then make a wrong wanted declaration during confrontation.
    /// </summary>
    public static DevSaloonOverride ForCitizen()
        => new(DevSaloonPoiKind.Citizen, null, null);

    /// <summary>
    /// Force the next look-around to spot a specific citizen role from the source-backed cast.
    /// The role key must exist in <see cref="CitizenCast.Roles"/>. The aggregate command
    /// method validates the role key at force time. The distinguishing feature is still
    /// drawn from the shared suspect feature vocabulary at lookaround time.
    /// </summary>
    public static DevSaloonOverride ForCitizen(string roleKey)
        => new(DevSaloonPoiKind.Citizen, null, roleKey);

    /// <summary>
    /// Force the next look-around to find nobody of interest in the saloon.
    /// </summary>
    public static DevSaloonOverride ForNone()
        => new(DevSaloonPoiKind.None, null, null);
}
