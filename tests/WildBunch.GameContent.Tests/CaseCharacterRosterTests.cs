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

        Assert.All(CaseCharacterRoster.GangCandidatePool, candidate => Assert.True(candidate.IsGangEligible));
        Assert.All(CaseCharacterRoster.GangCandidatePool, candidate => Assert.Contains("https://en.wikipedia.org/wiki/", candidate.SourceNote));
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.DisplayName == "Butch Cassidy");
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.DisplayName == "Bill Doolin");
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.SourceAliases.Contains("Arkansas Tom Jones"));
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.SourceAliases.Contains("Bitter Creek"));
        Assert.Contains(CaseCharacterRoster.GangCandidatePool, candidate => candidate.SourceAliases.Contains("Deaf Charley Hanks"));

        Assert.All(CaseCharacterRoster.AssociatedCharacterPool, candidate => Assert.True(candidate.IsAssociatedCharacter));
        Assert.Contains(CaseCharacterRoster.AssociatedCharacterPool, candidate => candidate.DisplayName == "Ann Bassett");
        Assert.Contains(CaseCharacterRoster.AssociatedCharacterPool, candidate => candidate.DisplayName == "Etta Place");

        Assert.All(CaseCharacterRoster.UnrelatedWantedCriminalPool, warrant => Assert.False(warrant.IsGangRelevant));
        Assert.All(CaseCharacterRoster.UnrelatedWantedCriminalPool, warrant => Assert.False(warrant.AdvancesGangPressure));
        Assert.All(CaseCharacterRoster.UnrelatedWantedCriminalPool, warrant => Assert.Equal(InvestigationTargetKind.UnrelatedWantedCriminal, warrant.TargetKind));
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

        Assert.Equal(sameWarrant.TargetName, sameWarrantAgain.TargetName);

        var varyingSeed = FindVaryingSeed(RosterSignature(sameRoster), WarrantSignature(sameWarrant));
        var varyingRoster = CaseCharacterRoster.SelectGangRoster(new GameSetupDeterministicSource(varyingSeed));
        var varyingWarrant = CaseCharacterRoster.SelectUnrelatedWarrant(new GameSetupDeterministicSource(varyingSeed));

        Assert.True(
            RosterSignature(sameRoster) != RosterSignature(varyingRoster)
            || WarrantSignature(sameWarrant) != WarrantSignature(varyingWarrant),
            "Different entropy should change at least one roster or unrelated warrant surface.");
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

    private static string FindVaryingSeed(string baselineRosterSignature, string baselineWarrantSignature)
    {
        for (ulong entropy = 8; entropy < 200; entropy++)
        {
            var candidateSeed = CreateSeedCode(entropy);
            var candidateRoster = CaseCharacterRoster.SelectGangRoster(new GameSetupDeterministicSource(candidateSeed));
            var candidateWarrant = CaseCharacterRoster.SelectUnrelatedWarrant(new GameSetupDeterministicSource(candidateSeed));

            if (RosterSignature(candidateRoster) != baselineRosterSignature || WarrantSignature(candidateWarrant) != baselineWarrantSignature)
            {
                return candidateSeed;
            }
        }

        throw new InvalidOperationException("Could not find a deterministic seed that varied the roster or unrelated warrant selection.");
    }

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
            warrant.IsGangRelevant,
            warrant.AdvancesGangPressure);
}
