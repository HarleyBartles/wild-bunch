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
            PendingDevOverride: devOverride is null ? null : new DevSaloonOverrideDto(
                devOverride.ForcedKind.ToString(),
                devOverride.ForcedSuspectId?.Value),
            HiddenTruth: trueCulprit is null ? null : new HiddenTruthDevDto(
                trueCulprit.Id.Value,
                trueCulprit.Name),
            Suspects: suspects);
    }
}
