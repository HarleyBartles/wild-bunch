using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class SaloonDevContextMapper
{
    public static SaloonDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var devOverride = session.PendingDevSaloonOverride;
        var trueCulprit = session.CaseFile.Suspects.FirstOrDefault(s => s.Id == session.CaseFile.TrueCulpritId);
        var sourceSpent = session.CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround);

        // Map active saloon POI directly from the current town visit state.
        // This is the contextual encounter state after LookAroundSaloon(), not
        // recomputed from suspects. See BUNCH-90 and ADR-0032.
        var townState = session.CurrentTownVisit.CurrentTownState;
        var activePoiId = townState.ActiveSaloonPersonOfInterestId;
        var activePoiDescriptor = townState.ActiveSaloonPersonOfInterestDescriptor;
        var activePoiKind = townState.ResolveActiveSaloonPersonOfInterestKind();
        ActiveSaloonPoiDto? activePoi = null;
        if (activePoiId is not null || activePoiDescriptor is not null)
        {
            activePoi = new ActiveSaloonPoiDto(
                activePoiId?.Value,
                activePoiDescriptor,
                activePoiKind?.ToString());
        }

        var suspects = session.CaseFile.Suspects.Select(s => new SaloonSuspectDevDto(
            s.Id.Value,
            s.Name,
            IsTrueCulprit: s.Id == session.CaseFile.TrueCulpritId,
            IsEligibleSaloonPoi: session.IsEligibleSaloonPersonOfInterestCandidate(s),
            IneligibilityReason: session.GetSaloonPoiIneligibilityReason(s),
            HasKnownWarrant: session.TryGetKnownWarrantForSuspect(s.Id, out _),
            PresenceState: session.TryGetWantedSuspectPresenceState(s.Id, out var presence) ? presence.ToString() : null
        )).ToList();

        return new SaloonDevContextDto(
            session.Id.Value,
            CurrentActionContext: session.CurrentActionContext.ToString(),
            CurrentTownId: session.CurrentTown.TownId.Value,
            CurrentTownName: session.CurrentTown.TownName,
            SourceSpent: sourceSpent,
            ActiveSaloonPoi: activePoi,
            PendingDevOverride: devOverride is null ? null : new DevSaloonOverrideDto(
                devOverride.ForcedKind.ToString(),
                devOverride.ForcedSuspectId?.Value),
            HiddenTruth: trueCulprit is null ? null : new HiddenTruthDevDto(
                trueCulprit.Id.Value,
                trueCulprit.Name),
            Suspects: suspects);
    }
}
