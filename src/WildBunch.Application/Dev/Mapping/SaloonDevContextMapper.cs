using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class SaloonDevContextMapper
{
    public static SaloonDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // During setup phase (before GameStarted), saloon state is not available.
        // Return a minimal DTO with nulls for town-scoped fields.
        if (session.StartFlowPhase < StartFlowPhase.GameStarted)
        {
            return new SaloonDevContextDto(
                session.Id.Value,
                CurrentActionContext: session.CurrentActionContext.ToString(),
                CurrentTownId: null,
                CurrentTownName: null,
                SourceSpent: false,
                ActiveSaloonPoi: null,
                PendingDevOverride: null,
                HiddenTruth: null,
                CitizenInfo: null,
                Suspects: []);
        }

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
            // Resolve suspect name for suspect POIs
            string? activePoiName = null;
            if (activePoiId is not null)
            {
                var suspect = session.CaseFile.Suspects.FirstOrDefault(s => s.Id == activePoiId);
                activePoiName = suspect?.Name;
            }

            activePoi = new ActiveSaloonPoiDto(
                activePoiId?.Value,
                activePoiName,
                activePoiDescriptor,
                activePoiKind?.ToString(),
                townState.ActiveSaloonCitizenRole);
        }

        var suspects = session.CaseFile.Suspects.Select(s =>
        {
            session.TryGetKnownWarrantForSuspect(s.Id, out var warrant);
            return new SaloonSuspectDevDto(
                SuspectId: s.Id.Value,
                Name: s.Name,
                IsTrueCulprit: s.Id == session.CaseFile.TrueCulpritId,
                IsEligibleSaloonPoi: session.IsEligibleSaloonPersonOfInterestCandidate(s),
                IneligibilityReason: session.GetSaloonPoiIneligibilityReason(s),
                HasKnownWarrant: warrant is not null,
                PresenceState: session.TryGetWantedSuspectPresenceState(s.Id, out var presence) ? presence.ToString() : null,
                Aliases: s.Profile.Aliases.Select(a => a.Name).ToList(),
                IdentifyingFacts: s.Profile.IdentifyingFacts.Select(f => f.Language.HasForm).ToList(),
                TraitTags: s.Traits.Tags.Select(t => t.Value).ToList(),
                BountyAmount: warrant?.Terms.BountyAmount,
                WarrantDisposition: warrant is not null ? warrant.Terms.Disposition.ToString() : null,
                WarrantKnownFeatures: warrant?.Terms.KnownFeatures.ToList() ?? new List<string>(),
                WarrantSummary: warrant?.Summary);
        }).ToList();

        // Citizen info — source-backed cast of named town roles.
        // Citizen features come from the shared suspect vocabulary, not a separate
        // citizen-only feature pool. The role selector chooses the citizen role.
        var citizenInfo = new CitizenInfoDto(
            Descriptor: "a stranger with a distinguishing feature from the shared suspect vocabulary",
            HasNamedArchetypes: true,
            AvailableArchetypes: CitizenCast.Roles.Select(role =>
                new CitizenArchetypeDto(role.Key, role.DisplayName)).ToList());

        // Hidden truth with saloon loop explanation
        var killerRelease = session.CaseFile.KillerReleaseState;
        var saloonLoopExplanation = BuildSaloonLoopExplanation(session, killerRelease.IsReleased);
        HiddenTruthDevDto? hiddenTruth = null;
        if (trueCulprit is not null)
        {
            hiddenTruth = new HiddenTruthDevDto(
                trueCulprit.Id.Value,
                trueCulprit.Name,
                killerRelease.StatusText,
                killerRelease.IsReleased,
                saloonLoopExplanation);
        }

        // Resolve forced suspect name for pending override
        string? forcedSuspectName = null;
        if (devOverride?.ForcedSuspectId is not null)
        {
            forcedSuspectName = session.CaseFile.Suspects
                .FirstOrDefault(s => s.Id == devOverride.ForcedSuspectId)?.Name;
        }

        return new SaloonDevContextDto(
            session.Id.Value,
            CurrentActionContext: session.CurrentActionContext.ToString(),
            CurrentTownId: session.CurrentTown.TownId.Value,
            CurrentTownName: session.CurrentTown.TownName,
            SourceSpent: sourceSpent,
            ActiveSaloonPoi: activePoi,
            PendingDevOverride: devOverride is null ? null : new DevSaloonOverrideDto(
                devOverride.ForcedKind.ToString(),
                devOverride.ForcedSuspectId?.Value,
                forcedSuspectName,
                devOverride.ForcedCitizenRoleKey),
            HiddenTruth: hiddenTruth,
            CitizenInfo: citizenInfo,
            Suspects: suspects);
    }

    private static string BuildSaloonLoopExplanation(GameSession session, bool killerIsReleased)
    {
        var sourceSpent = session.CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround);
        var parts = new List<string>();

        parts.Add(sourceSpent
            ? "Saloon look-around source is spent for this town visit. A repeat visit or confrontation clears the active POI."
            : "Saloon look-around source is available. Call LookAroundSaloon to spot a POI.");

        parts.Add(killerIsReleased
            ? "The killer trail is released — the true culprit is now eligible to appear as a saloon POI."
            : "The killer trail is locked — the true culprit is gated out of saloon POI until the killer-release gate opens.");

        var eligibleCount = session.CaseFile.Suspects.Count(s => session.IsEligibleSaloonPersonOfInterestCandidate(s));
        var citizenCount = CitizenCast.Roles.Count;
        parts.Add($"Saloon POI pool: {eligibleCount} suspect(s) + {citizenCount} citizen role(s) + nobody. " +
                  "Any non-culprit suspect or citizen can appear in any saloon — no town presence or warrant gates.");

        return string.Join(" ", parts);
    }
}
