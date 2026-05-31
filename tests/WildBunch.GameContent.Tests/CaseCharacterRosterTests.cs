using WildBunch.Domain.Cases;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class CaseCharacterRosterTests
{
    [Fact]
    public void SourceBackedPoolsStaySeparatedAndCarrySourceNotes()
    {
        Assert.NotEmpty(CaseCharacterRoster.GangCandidatePool);
        Assert.NotEmpty(CaseCharacterRoster.AssociatedCharacterPool);
        Assert.NotEmpty(CaseCharacterRoster.UnrelatedWantedCriminalPool);
        Assert.NotEmpty(CaseSuspectFeaturePool.FeaturePool);

        Assert.All(CaseCharacterRoster.GangCandidatePool, candidate => Assert.True(candidate.IsGangEligible));
        Assert.All(CaseCharacterRoster.GangCandidatePool, candidate => Assert.Contains("https://en.wikipedia.org/wiki/", candidate.SourceNote));
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.DisplayName == "Butch Cassidy");
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.DisplayName == "Bill Doolin");
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.SourceAliases.Contains("Arkansas Tom Jones"));
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.SourceAliases.Contains("Bitter Creek"));
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.SourceAliases.Contains("Deaf Charley Hanks"));
        Assert.All(CaseCharacterRoster.GangCandidatePool, candidate => Assert.Equal(new[] { OutlawGangIds.WildBunch }, candidate.GangAffiliations));

        Assert.All(CaseCharacterRoster.AssociatedCharacterPool, candidate => Assert.True(candidate.IsAssociatedCharacter));
        Assert.All(CaseCharacterRoster.AssociatedCharacterPool, candidate => Assert.Empty(candidate.GangAffiliations));
        Assert.Contains(CaseCharacterRoster.AssociatedCharacterPool, candidate => candidate.DisplayName == "Ann Bassett");
        Assert.Contains(CaseCharacterRoster.AssociatedCharacterPool, candidate => candidate.DisplayName == "Etta Place");

        Assert.All(CaseCharacterRoster.UnrelatedWantedCriminalPool, warrant => Assert.Empty(warrant.GangAffiliations));
        Assert.All(CaseCharacterRoster.UnrelatedWantedCriminalPool, warrant => Assert.Null(warrant.AdvancesGangPressureFor));
        Assert.All(CaseCharacterRoster.UnrelatedWantedCriminalPool, warrant => Assert.Equal(InvestigationTargetKind.UnrelatedWantedCriminal, warrant.TargetKind));

        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "limp-left-leg");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "limp-right-leg");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "no-left-ear");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "no-right-ear");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "scar-left-cheek");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "scar-right-cheek");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "no-eyebrows");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "distinctive-left-earring");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "distinctive-right-earring");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "eyepatch-left");
        Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Key == "eyepatch-right");
        Assert.All(
            CaseSuspectFeaturePool.FeaturePool.Where(feature => feature.SupportsOpeningLead),
            feature => Assert.False(string.IsNullOrWhiteSpace(feature.OpeningLeadText)));

        var noLeftEar = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "no-left-ear");
        var noRightLeg = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "limp-right-leg");
        var noLeftLeg = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "limp-left-leg");
        var noRightEar = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "no-right-ear");
        var scarLeft = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "scar-left-cheek");
        var scarRight = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "scar-right-cheek");
        var leftEarring = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "distinctive-left-earring");
        var rightEarring = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "distinctive-right-earring");
        var leftEyepatch = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "eyepatch-left");
        var rightEyepatch = CaseSuspectFeaturePool.FeaturePool.Single(feature => feature.Key == "eyepatch-right");
        Assert.False(CaseSuspectFeaturePool.AreCompatible(noLeftEar, leftEarring));
        Assert.False(CaseSuspectFeaturePool.AreCompatible(noRightEar, rightEarring));
        Assert.False(CaseSuspectFeaturePool.AreCompatible(noLeftLeg, noRightLeg));
        Assert.False(CaseSuspectFeaturePool.AreCompatible(noLeftEar, noRightEar));
        Assert.False(CaseSuspectFeaturePool.AreCompatible(scarLeft, scarRight));
        Assert.False(CaseSuspectFeaturePool.AreCompatible(leftEarring, rightEarring));
        Assert.False(CaseSuspectFeaturePool.AreCompatible(leftEyepatch, rightEyepatch));
    }

    [Fact]
    public void DeterministicSelectionStaysStableForTheSameSeedAndCanVaryForDifferentSeeds()
    {
        var sameSeed = CreateSeedCode(7);
        var anotherSameSeed = CreateSeedCode(7);

        var sameRoster = CaseCharacterRoster.SelectGangRoster(new GameSetupDeterministicSource(sameSeed));
        var sameRosterAgain = CaseCharacterRoster.SelectGangRoster(new GameSetupDeterministicSource(anotherSameSeed));

        Assert.Equal(RosterSignature(sameRoster), RosterSignature(sameRosterAgain));

        var sameWarrant = CaseCharacterRoster.SelectUnrelatedWarrant(new GameSetupDeterministicSource(sameSeed));
        var sameWarrantAgain = CaseCharacterRoster.SelectUnrelatedWarrant(new GameSetupDeterministicSource(anotherSameSeed));
        var sameFeatures = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(sameSeed));
        var sameFeaturesAgain = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(anotherSameSeed));

        Assert.Equal(sameWarrant.TargetName, sameWarrantAgain.TargetName);
        Assert.Equal(FeatureSignature(sameFeatures), FeatureSignature(sameFeaturesAgain));

        var varyingSeed = FindVaryingSeed(RosterSignature(sameRoster), WarrantSignature(sameWarrant), FeatureSignature(sameFeatures));
        var varyingRoster = CaseCharacterRoster.SelectGangRoster(new GameSetupDeterministicSource(varyingSeed));
        var varyingWarrant = CaseCharacterRoster.SelectUnrelatedWarrant(new GameSetupDeterministicSource(varyingSeed));
        var varyingFeatures = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(varyingSeed));

        Assert.True(
            RosterSignature(sameRoster) != RosterSignature(varyingRoster)
            || WarrantSignature(sameWarrant) != WarrantSignature(varyingWarrant)
            || FeatureSignature(sameFeatures) != FeatureSignature(varyingFeatures),
            "Different entropy should change at least one roster, feature, or unrelated warrant surface.");
    }

    [Fact]
    public void FeatureAssignmentsCanIncludeMultipleCompatibleFeaturesAndStayWithinClassicNodBudget()
    {
        var seed = FindFeatureRichSeed();
        var features = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(seed));
        var featuresAgain = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(seed));

        Assert.Equal(FeatureSignature(features), FeatureSignature(featuresAgain));
        Assert.Contains(features, assignment => assignment.AdditionalFeatures.Count > 0);
        Assert.All(features, assignment => Assert.True(assignment.AllFeatures.Count(feature => feature.IsClassicNod) <= 2));
        Assert.All(features, assignment => Assert.All(assignment.AdditionalFeatures, additional => Assert.True(assignment.PrimaryFeature.IsCompatibleWith(additional))));
        Assert.All(
            features,
            assignment => Assert.All(
                assignment.AdditionalFeatures,
                additional => Assert.All(
                    assignment.AdditionalFeatures.Where(other => !ReferenceEquals(other, additional)),
                    other => Assert.True(additional.IsCompatibleWith(other)))));
    }

    private static string CreateSeedCode(ulong entropy)
        => GameSetupSeedCodec.Encode(
            new GameSetupSeed(
                GameSetupSeedCodec.CurrentGeneratorVersion,
                TravelDifficulty.Normal,
                GameSetupOptionsV1.Default,
                entropy));

    private static string RosterSignature(IReadOnlyList<CaseCharacterProfile> roster)
        => string.Join("|", roster.Select(candidate => $"{candidate.Key}:{candidate.DisplayName}:{string.Join(",", candidate.SourceAliases)}"));

    private static string FindVaryingSeed(string baselineRosterSignature, string baselineWarrantSignature, string baselineFeatureSignature)
    {
        for (ulong entropy = 8; entropy < 200; entropy++)
        {
            var candidateSeed = CreateSeedCode(entropy);
            var candidateRoster = CaseCharacterRoster.SelectGangRoster(new GameSetupDeterministicSource(candidateSeed));
            var candidateWarrant = CaseCharacterRoster.SelectUnrelatedWarrant(new GameSetupDeterministicSource(candidateSeed));
            var candidateFeatures = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(candidateSeed));

            if (RosterSignature(candidateRoster) != baselineRosterSignature || WarrantSignature(candidateWarrant) != baselineWarrantSignature || FeatureSignature(candidateFeatures) != baselineFeatureSignature)
            {
                return candidateSeed;
            }
        }

        throw new InvalidOperationException("Could not find a deterministic seed that varied the roster or unrelated warrant selection.");
    }

    private static string FindFeatureRichSeed()
    {
        for (ulong entropy = 7; entropy < 400; entropy++)
        {
            var candidateSeed = CreateSeedCode(entropy);
            var candidateFeatures = CaseSuspectFeaturePool.SelectAssignedFeatures(new GameSetupDeterministicSource(candidateSeed));

            if (candidateFeatures.Any(assignment => assignment.AdditionalFeatures.Count > 0))
            {
                return candidateSeed;
            }
        }

        throw new InvalidOperationException("Could not find a deterministic seed that produced multiple compatible suspect features.");
    }

    private static string FeatureSignature(IReadOnlyList<CaseSuspectFeatureAssignment> features)
        => string.Join("|", features.Select(feature => $"{feature.PrimaryFeature.Key}:{string.Join(",", feature.AdditionalFeatures.Select(additional => additional.Key))}"));

    private static string WarrantSignature(OutlawWarrantProfile warrant)
        => string.Join(
            ":",
            warrant.Key,
            warrant.TargetName,
            string.Join(",", warrant.KnownAliases),
            string.Join(",", warrant.KnownFeatures),
            warrant.IssuingSource,
            warrant.Disposition,
            warrant.BountyAmount,
            warrant.TargetKind,
            string.Join("/", warrant.GangAffiliations.Select(gang => gang.Value)),
            warrant.AdvancesGangPressureFor?.Value ?? string.Empty);
}
