using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Tests for the GameSetupResolver pipeline.
/// Replaces the former GameSetupPackageBuilderTests.
/// </summary>
public sealed class GameSetupResolverTests
{
    [Fact]
    public void SameTemplateProducesTheSameDurableStartingPackage()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);

        var setupA = BuildSetup(seedWorld, difficulty, entropy);
        var setupB = BuildSetup(seedWorld, difficulty, entropy);

        Assert.Equal(BuildSetupSignature(setupA), BuildSetupSignature(setupB));
    }

    [Fact]
    public void DifferentSeedCodesCanChangeAtLeastOneSetupSurface()
    {
        var seedWorldA = SeedWorldResolver.Resolve(CreateSeedCode(0, 1, 3, 0, tail: 11));
        var seedWorldB = SeedWorldResolver.Resolve(CreateSeedCode(1, 2, 4, 6, tail: 42));
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);

        var setupA = BuildSetup(seedWorldA, difficulty, entropy);
        var setupB = BuildSetup(seedWorldB, difficulty, entropy);

        Assert.NotEqual(BuildSetupSignature(setupA), BuildSetupSignature(setupB));
    }

    [Fact]
    public void DifferentDifficultyChangesTravelRulesAndStartingCash()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var easyDifficulty = DifficultyEnvelope.For(GameDifficulty.Easy);
        var hardDifficulty = DifficultyEnvelope.For(GameDifficulty.Challenging);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);

        var easySetup = BuildSetup(seedWorld, easyDifficulty, entropy);
        var hardSetup = BuildSetup(seedWorld, hardDifficulty, entropy);

        Assert.NotEqual(easySetup.TravelRulesProfile, hardSetup.TravelRulesProfile);
        Assert.Equal(30m, easySetup.StartingWallet.Cash);
        Assert.Equal(20m, hardSetup.StartingWallet.Cash);
    }

    [Fact]
    public void CanonicalTemplateUsesTheExplicitCanonicalPlan()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);

        var setup = BuildSetup(seedWorld, difficulty, entropy);

        // Starting town is the first town in the generated world (slot 0).
        var expectedStartingTown = SeedWorldBuilder.CreateWorld(seedWorld, new GameSetupDeterministicSource(seedWorld.SeedCodeText)).Towns.First().Id;
        Assert.Equal(expectedStartingTown, setup.StartingTownId);
        Assert.Equal(25m, setup.StartingWallet.Cash);
        Assert.Equal(7, setup.CaseFile.Suspects.Count);
        Assert.Single(setup.CaseFile.Suspects, suspect => suspect.Id.Equals(setup.CaseFile.TrueCulpritId));
        Assert.Equal(5, setup.CaseFile.KillerReleaseThreshold);
        Assert.Equal("The culprit has a scar on the left cheek.", setup.CaseFile.OpeningLead.Description);
        // 6 base surface-tagged clues; 7 gang + 21 unrelated = 28 warrants.
        Assert.Equal(6, setup.CaseFile.PublicClues.Count);
        Assert.Equal(28, setup.CaseFile.PublicWarrants.Count);
        Assert.Equal(7, setup.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.GangMember || w.Terms.TargetKind == InvestigationTargetKind.TrueCulprit));
        Assert.Equal(21, setup.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal));
        Assert.Equal("Butch Cassidy", setup.CaseFile.PublicWarrants[0].TargetName);
        Assert.Equal(InvestigationTargetKind.GangMember, setup.CaseFile.PublicWarrants[0].Terms.TargetKind);
        // The true culprit's warrant is in the pool (gated behind the killer release gate at runtime).
        Assert.Contains(setup.CaseFile.PublicWarrants, warrant => warrant.TargetName == setup.CaseFile.Suspects[3].Name
            && warrant.Terms.TargetKind == InvestigationTargetKind.TrueCulprit);
        Assert.DoesNotContain(setup.CaseFile.PublicWarrants[0].Terms.KnownFeatures, feature => feature.Contains("scar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setup.CaseFile.KnownClues, clue =>
            clue.Kind == ClueKind.CulpritTrail
            && clue.TargetKind == InvestigationTargetKind.TrueCulprit);
        Assert.All(setup.CaseFile.KnownClues.Concat(setup.CaseFile.PublicClues), clue => Assert.True(clue.Anchors.HasAnchors));
        var localGossipClue = Assert.Single(setup.CaseFile.PublicClues, clue => clue.Description.StartsWith("Local gossip out of ", StringComparison.Ordinal));
        Assert.NotEmpty(localGossipClue.Anchors.Locations);
        Assert.NotEmpty(localGossipClue.Anchors.Times);
        Assert.NotEmpty(localGossipClue.Anchors.Directions);
        Assert.Equal(7, setup.CaseFile.SuspectTurfAssignments.Count);
        Assert.All(setup.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(setup.World.Towns, town => town.Id.Equals(assignment.TurfTownId)));
        Assert.All(setup.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(setup.CaseFile.Suspects, suspect => suspect.Id.Equals(assignment.SuspectId)));
    }

    [Fact]
    public void DifferentSeedCodesCanChangeSuspectTurfAssignments()
    {
        // With direct bit encoding, different case fields produce different UUIDs,
        // which changes the GameSetupDeterministicSource hash that drives turf
        // assignment.
        var seedWorldA = SeedWorldResolver.Resolve(CreateSeedCode(0, 1, 3, 0, tail: 0));
        var seedWorldB = SeedWorldResolver.Resolve(CreateSeedCode(0, 2, 4, 5, tail: 0));
        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var entropy = EntropyPolicy.For(GameEntropy.Classic);

        var setupA = BuildSetup(seedWorldA, difficulty, entropy);
        var setupB = BuildSetup(seedWorldB, difficulty, entropy);

        Assert.NotEqual(TurfSignature(setupA), TurfSignature(setupB));
    }

    [Fact]
    public void MysteryTruthResolverPassesThroughTemplateDefaultsForAllEntropyModes()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        foreach (var entropyMode in Enum.GetValues<GameEntropy>())
        {
            var entropy = EntropyPolicy.For(entropyMode);
            var setup = BuildSetup(seedWorld, DifficultyEnvelope.For(GameDifficulty.Standard), entropy);

            // Transitional: all entropy modes use the template default culprit index.
            Assert.Equal(seedWorld.DefaultCulpritIndex, 3);
            Assert.Equal(seedWorld.DefaultCulpritIndex,
                setup.CaseFile.Suspects.ToList().IndexOf(setup.CaseFile.Suspects.First(s => s.Id == setup.CaseFile.TrueCulpritId)));
        }
    }

    [Fact]
    public void BoringEntropyUsesFixedSaltAndOthersUseRuntimeSalt()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        var boringSetup = BuildSetup(seedWorld, DifficultyEnvelope.For(GameDifficulty.Standard), EntropyPolicy.For(GameEntropy.Boring));
        Assert.Equal(SaltSourceMode.Fixed, boringSetup.SaltSource.Mode);

        var classicSetup = BuildSetup(seedWorld, DifficultyEnvelope.For(GameDifficulty.Standard), EntropyPolicy.For(GameEntropy.Classic));
        Assert.Equal(SaltSourceMode.Runtime, classicSetup.SaltSource.Mode);
    }

    [Fact]
    public void CashBonusIsCappedByEntropyPolicy()
    {
        // Template with cash bonus 5, Classic cap is 2 → applied bonus should be 2.
        var seedWorld = CreateCanonicalSeedWorldWithCashBonus(5);

        var difficulty = DifficultyEnvelope.For(GameDifficulty.Standard);
        var classicEntropy = EntropyPolicy.For(GameEntropy.Classic);
        var setup = BuildSetup(seedWorld, difficulty, classicEntropy);

        // Standard base cash = 25, Classic cap = 2, template bonus = 5 → applied = min(5, 2) = 2.
        Assert.Equal(27m, setup.StartingWallet.Cash);

        // Wild cap = 8 → applied = min(5, 8) = 5.
        var wildEntropy = EntropyPolicy.For(GameEntropy.Wild);
        var wildSetup = BuildSetup(seedWorld, difficulty, wildEntropy);
        Assert.Equal(30m, wildSetup.StartingWallet.Cash);
    }

    private static ResolvedGameSetup BuildSetup(SeedWorld seedWorld, DifficultyEnvelope difficulty, EntropyPolicy entropy)
        => new GameSetupResolver().Resolve(seedWorld, difficulty, entropy);

    private static string BuildSetupSignature(ResolvedGameSetup setup)
        => string.Join(
            "|",
            setup.SeedWorld.WorldVariant,
            setup.GameDifficulty,
            setup.GameEntropy,
            setup.TravelRulesProfile,
            setup.StartingTownId.Value,
            setup.StartingWallet.Cash,
            string.Join(",", setup.World.Towns.OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase).Select(town => $"{town.Id.Value}:{town.Name}:{town.Services}")),
            string.Join(",", setup.World.Trails.OrderBy(trail => trail.Id.Value, StringComparer.OrdinalIgnoreCase).Select(trail => $"{trail.Id.Value}:{trail.FromTownId.Value}:{trail.ToTownId.Value}:{trail.Risk}:{trail.Terrain}:{trail.WaterFeature}:{trail.RideDayDistance}")),
            string.Join(",", setup.StartingInventory.Items.Select(item => $"{item.Kind}:{item.Quantity}:{item.HorseState?.Hunger ?? -1}:{item.HorseState?.Thirst ?? -1}:{item.HorseState?.Exhaustion ?? -1}:{item.CanteenState?.Charges ?? -1}:{item.CanteenState?.Capacity ?? -1}")),
            string.Join(",", setup.CaseFile.Suspects.Select(suspect => $"{suspect.Id.Value}:{suspect.Name}:{suspect.Status}")),
            setup.CaseFile.TrueCulpritId.Value,
            setup.CaseFile.Accusation?.Value ?? string.Empty,
            setup.CaseFile.OpeningLead.Description,
            string.Join(",", setup.CaseFile.KnownClues.Select(DescribeClue)),
            string.Join(",", setup.CaseFile.PublicClues.Select(DescribeClue)),
            string.Join(",", setup.CaseFile.PublicWarrants.Select(warrant => $"{warrant.Id.Value}:{warrant.TargetName}:{warrant.Terms.Disposition}:{warrant.Terms.BountyAmount}:{string.Join("/", warrant.Terms.KnownAliases)}:{string.Join("/", warrant.Terms.KnownFeatures)}:{warrant.Terms.IssuingSource}:{warrant.Terms.TargetKind}:{string.Join("/", warrant.Terms.GangAffiliations.Select(gang => gang.Value))}:{warrant.Terms.AdvancesGangPressureFor?.Value ?? string.Empty}:{warrant.Summary}")),
            TurfSignature(setup));

    private static string TurfSignature(ResolvedGameSetup setup)
        => string.Join("|", setup.CaseFile.SuspectTurfAssignments.Select(assignment => $"{assignment.SuspectId.Value}:{assignment.TurfTownId.Value}"));

    private static string DescribeClue(Clue clue)
        => $"{clue.Id.Value}:{clue.Kind}:{clue.Description}:{clue.TargetKind}:{clue.Source}:{clue.Context}:{string.Join("/", clue.LinkedSuspectIds.Select(id => id.Value))}";

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);

    private static SeedWorld CreateCanonicalSeedWorldWithCashBonus(int cashBonus)
    {
        var canonical = SeedWorldResolver.CreateCanonicalSeedWorld();
        return canonical with { CashBonus = cashBonus };
    }
}
