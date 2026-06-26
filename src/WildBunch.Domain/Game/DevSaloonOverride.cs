using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Game;

/// <summary>
/// The kind of saloon POI that a dev override can force.
/// Suspect: spot a specific (or first eligible) wanted suspect.
/// Citizen: spot a generic town citizen.
/// FalseLead: spot a citizen (the false-lead outcome comes from the
/// normal confrontation flow when the player declares a wrong wanted
/// identity on a citizen POI).
/// </summary>
public enum DevSaloonPoiKind
{
    Suspect = 0,
    Citizen = 1,
    FalseLead = 2
}

/// <summary>
/// Pending dev override for the next saloon look-around.
/// When present, LookAroundSaloon uses this instead of calling
/// TryGetConfrontableSaloonPersonOfInterestCandidateInTown.
/// Consumed once by the next look-around, then cleared from aggregate state.
/// This is dev-only session state, not player-facing. See BUNCH-90.
/// </summary>
public sealed record DevSaloonOverride(
    DevSaloonPoiKind ForcedKind,
    SuspectId? ForcedSuspectId)
{
    /// <summary>
    /// Force the next look-around to spot a specific suspect by ID.
    /// The suspect must exist in the case file AND must be a valid saloon
    /// POI candidate under the current domain rules: not the true culprit,
    /// and if the suspect has a known warrant, their presence state must be
    /// AvailableInTown or GoneToGround. The aggregate command method
    /// validates these rules at force time and rejects ineligible suspects.
    /// Dev inspection can show why a suspect is ineligible, but dev force
    /// must not break core saloon/culprit invariants. See BUNCH-90.
    /// </summary>
    public static DevSaloonOverride ForSuspect(SuspectId suspectId)
        => new(DevSaloonPoiKind.Suspect, suspectId);

    /// <summary>
    /// Force the next look-around to spot the first eligible suspect.
    /// No suspect-specific validation needed at force time - the consume
    /// path uses normal candidate selection, which already enforces eligibility.
    /// </summary>
    public static DevSaloonOverride ForAnySuspect()
        => new(DevSaloonPoiKind.Suspect, null);

    /// <summary>
    /// Force the next look-around to spot a generic town citizen.
    /// </summary>
    public static DevSaloonOverride ForCitizen()
        => new(DevSaloonPoiKind.Citizen, null);

    /// <summary>
    /// Force the next look-around to spot a citizen (false-lead path).
    /// Semantically identical to Citizen - the false-lead outcome comes
    /// from the normal confrontation flow when the player declares a wrong
    /// wanted identity on a citizen POI.
    /// </summary>
    public static DevSaloonOverride ForFalseLead()
        => new(DevSaloonPoiKind.FalseLead, null);
}
