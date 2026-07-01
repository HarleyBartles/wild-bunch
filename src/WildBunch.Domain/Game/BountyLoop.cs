using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the GameSession boundary that owns bounty-loop
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference GameSession, produce events directly,
/// enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-112 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class BountyLoop
{
    private readonly WantedSuspectPresenceLedger _presenceLedger;
    private UnrelatedCriminalLedger _unrelatedCriminalLedger;
    private DevSaloonOverride? _pendingDevSaloonOverride;

    internal BountyLoop(
        IReadOnlyList<WantedSuspectPresenceEntry>? presenceEntries,
        UnrelatedCriminalLedger unrelatedCriminalLedger)
    {
        _presenceLedger = new WantedSuspectPresenceLedger(presenceEntries);
        _unrelatedCriminalLedger = unrelatedCriminalLedger
            ?? throw new ArgumentNullException(nameof(unrelatedCriminalLedger));
    }

    internal IReadOnlyList<WantedSuspectPresenceEntry> PresenceEntries => _presenceLedger.Entries;
    internal UnrelatedCriminalLedger UnrelatedCriminalLedger => _unrelatedCriminalLedger;
    internal DevSaloonOverride? PendingDevSaloonOverride => _pendingDevSaloonOverride;

    internal WantedSuspectPresenceState GetWantedSuspectPresenceState(SuspectId suspectId)
        => _presenceLedger.GetState(suspectId);

    internal bool TryGetWantedSuspectPresenceState(SuspectId suspectId, out WantedSuspectPresenceState state)
        => _presenceLedger.TryGetState(suspectId, out state);

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    internal void RestoreUnrelatedCriminalLedger(UnrelatedCriminalLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        _unrelatedCriminalLedger = ledger;
    }

    internal void RestorePendingDevSaloonOverride(DevSaloonOverride? overrideValue)
    {
        _pendingDevSaloonOverride = overrideValue;
    }

    /// <summary>
    /// Saloon look-around decision logic. Receives narrow context, returns the
    /// investigation result plus events for GameSession to produce. Consumes
    /// the pending dev override (owned state) if present.
    /// </summary>
    internal BountyLoopResult<CaseInvestigationResult> LookAroundSaloon(SaloonLookAroundContext context)
    {
        var events = new List<IDomainEvent>();

        // Dev override: capture the pending override before producing the consumed event,
        // because GameSession's Apply(DevSaloonOverrideConsumed) will clear
        // _pendingDevSaloonOverride. The forced POI must be built from the captured value.
        // See BUNCH-90.
        var pendingDevOverride = context.PendingDevOverride;

        if (pendingDevOverride is not null)
        {
            events.Add(new DevSaloonOverrideConsumed());

            // Build the forced POI from the captured override value.
            if (pendingDevOverride.ForcedKind is DevSaloonPoiKind.Suspect)
            {
                Suspect? forcedSuspect = null;
                if (pendingDevOverride.ForcedSuspectId is not null)
                {
                    // Specific suspect was forced - validated at force time.
                    forcedSuspect = context.EligibleSuspects.FirstOrDefault(s => s.Id == pendingDevOverride.ForcedSuspectId);
                }

                // If no specific suspect or the specific suspect is not found, use normal candidate selection.
                if (forcedSuspect is null && pendingDevOverride.ForcedSuspectId is null)
                {
                    forcedSuspect = context.EligibleSuspects.FirstOrDefault();
                }

                if (forcedSuspect is not null)
                {
                    var descriptor = SaloonPersonOfInterestDescriptor.Describe(forcedSuspect, context.KnownWarrants);
                    var spotMessage = $"You look around the saloon and spot {descriptor}.";
                    events.Add(new SaloonPersonOfInterestSpotted
                    {
                        SourceKind = InvestigationSourceKind.SaloonLookAround,
                        TownId = context.TownId,
                        Message = spotMessage,
                        SuspectId = forcedSuspect.Id,
                        Descriptor = descriptor,
                        PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                        RecordLog = true
                    });
                    return new BountyLoopResult<CaseInvestigationResult>(
                        CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true), events);
                }
            }
            else if (pendingDevOverride.ForcedKind is DevSaloonPoiKind.None)
            {
                // Nobody of interest — the saloon is quiet.
                var nobodyMessage = "You look around the saloon, but nobody of interest catches your eye.";
                events.Add(new SaloonPersonOfInterestSpotted
                {
                    SourceKind = InvestigationSourceKind.SaloonLookAround,
                    TownId = context.TownId,
                    Message = nobodyMessage,
                    RecordLog = true
                });
                return new BountyLoopResult<CaseInvestigationResult>(
                    CaseInvestigationResult.Succeeded(nobodyMessage, sessionChanged: true), events);
            }
            else
            {
                // Citizen - spots a citizen from the source-backed cast.
                // The false-lead outcome comes from the normal confrontation flow
                // when the player declares a wrong wanted identity on a citizen POI.
                // Citizen features are drawn from the shared suspect feature vocabulary.
                var forcedFeatureDescriptions = context.SuspectFeatureDescriptions;
                CitizenEncounter forcedEncounter;
                if (pendingDevOverride.ForcedCitizenRoleKey is not null)
                {
                    forcedEncounter = context.CitizenSelectByRoleKey(pendingDevOverride.ForcedCitizenRoleKey, forcedFeatureDescriptions);
                }
                else
                {
                    forcedEncounter = context.CitizenSelect(context.TownId, context.Day, context.Turn, context.VisitNumber, forcedFeatureDescriptions);
                }
                var forcedCitizenDescriptor = context.CitizenDescriptorResolver(forcedEncounter);
                var forcedCitizenMessage = $"You look around the saloon and spot {forcedCitizenDescriptor}.";
                events.Add(new SaloonPersonOfInterestSpotted
                {
                    SourceKind = InvestigationSourceKind.SaloonLookAround,
                    TownId = context.TownId,
                    Message = forcedCitizenMessage,
                    Descriptor = forcedCitizenDescriptor,
                    PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
                    CitizenRole = forcedEncounter.Role.Key,
                    RecordLog = false
                });
                return new BountyLoopResult<CaseInvestigationResult>(
                    CaseInvestigationResult.Succeeded(forcedCitizenMessage, sessionChanged: true), events);
            }
        }

        // Normal path: no dev override active.
        if (context.IsSaloonSourceSpent)
        {
            var repeatMessage = "You look around the saloon again, but nobody of interest is here.";
            events.Add(new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = context.TownId,
                Message = repeatMessage,
                RecordLog = true
            });
            return new BountyLoopResult<CaseInvestigationResult>(
                CaseInvestigationResult.Succeeded(repeatMessage, sessionChanged: true), events);
        }

        // BUNCH-106: Simplified saloon POI selection.
        // The candidate pool is: each eligible non-culprit suspect + each citizen role + one "nobody" slot.
        // Any non-culprit suspect can walk into any saloon — no town presence, warrant, or poster gates.
        // The true killer is excluded until the killer-release gate opens.
        // The roll is deterministic using the salt source + town + day + turn + visit number.
        var eligibleSuspects = context.EligibleSuspects;
        var citizenRoleCount = context.CitizenRoleCount;
        var poolSize = eligibleSuspects.Count + citizenRoleCount + 1; // +1 for "nobody"
        var rollHash = StableSaloonRollHash(context.TownId, context.Day, context.Turn, context.VisitNumber, context.Salt);
        var rollIndex = rollHash % poolSize;

        // Nobody of interest.
        if (rollIndex == poolSize - 1)
        {
            var nobodyMessage = "You look around the saloon, but nobody of interest catches your eye.";
            events.Add(new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = context.TownId,
                Message = nobodyMessage,
                RecordLog = true
            });
            return new BountyLoopResult<CaseInvestigationResult>(
                CaseInvestigationResult.Succeeded(nobodyMessage, sessionChanged: true), events);
        }

        // Suspect slot.
        if (rollIndex < eligibleSuspects.Count)
        {
            var suspect = eligibleSuspects[rollIndex];
            var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, context.KnownWarrants);
            var spotMessage = $"You look around the saloon and spot {descriptor}.";
            events.Add(new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = context.TownId,
                Message = spotMessage,
                SuspectId = suspect.Id,
                Descriptor = descriptor,
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                RecordLog = true
            });
            return new BountyLoopResult<CaseInvestigationResult>(
                CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true), events);
        }

        // Citizen slot.
        var citizenFeatureDescriptions = context.SuspectFeatureDescriptions;
        var citizenEncounter = context.CitizenSelect(context.TownId, context.Day, context.Turn, context.VisitNumber, citizenFeatureDescriptions);
        var citizenDescriptor = context.CitizenDescriptorResolver(citizenEncounter);
        var citizenMessage = $"You look around the saloon and spot {citizenDescriptor}.";
        events.Add(new SaloonPersonOfInterestSpotted
        {
            SourceKind = InvestigationSourceKind.SaloonLookAround,
            TownId = context.TownId,
            Message = citizenMessage,
            Descriptor = citizenDescriptor,
            PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
            CitizenRole = citizenEncounter.Role.Key,
            RecordLog = false
        });
        return new BountyLoopResult<CaseInvestigationResult>(
            CaseInvestigationResult.Succeeded(citizenMessage, sessionChanged: true), events);
    }

    /// <summary>
    /// Stable manual hash for deterministic saloon POI rolls. Uses the salt source
    /// so different sessions get different rolls for the same town/day/turn/visit.
    /// Does NOT use <see cref="string.GetHashCode()"/> (not stable across restarts).
    /// </summary>
    private static int StableSaloonRollHash(TownId townId, int day, int turn, int visitNumber, string salt)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in salt)
            {
                hash = (hash * 31) + c;
            }
            foreach (var c in townId.Value)
            {
                hash = (hash * 31) + c;
            }
            hash = (hash * 31) + day;
            hash = (hash * 31) + turn;
            hash = (hash * 31) + visitNumber;
            return Math.Abs(hash);
        }
    }
}
