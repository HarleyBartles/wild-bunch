using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class GameSetupPackageBuilderTests
{
    [Fact]
    public void SameEncodedSeedProducesTheSameDurableStartingPackage()
    {
        var seed = GameSetupSeedCodec.WithOption(
            GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Hard),
            GameSetupOption.LoadoutProfile,
            (int)StartingLoadoutProfile.Stocked);
        seed = GameSetupSeedCodec.WithOption(seed, GameSetupOption.StartWithHorse, 1);

        var packageA = BuildPackage(seed);
        var packageB = BuildPackage(seed);

        Assert.Equal(BuildPackageSignature(packageA), BuildPackageSignature(packageB));
    }

    [Fact]
    public void DifferentEntropyChangesAtLeastOneSeededSetupSurface()
    {
        var firstSeed = GameSetupSeedCodec.WithOption(
            GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Normal),
            GameSetupOption.LoadoutProfile,
            (int)StartingLoadoutProfile.Standard);
        firstSeed = firstSeed with { Entropy = 1 };

        var secondSeed = firstSeed with { Entropy = 2 };

        var firstPackage = BuildPackage(firstSeed);
        var secondPackage = BuildPackage(secondSeed);

        Assert.NotEqual(BuildPackageSignature(firstPackage), BuildPackageSignature(secondPackage));
    }

    [Fact]
    public void DifferentOptionsChangeTheStartingLoadoutAndWallet()
    {
        var horseSeed = GameSetupSeedCodec.WithOption(GameSetupSeedCodec.CreateCanonicalSeed(), GameSetupOption.StartWithHorse, 1);
        var footSeed = GameSetupSeedCodec.WithOption(GameSetupSeedCodec.CreateCanonicalSeed(), GameSetupOption.StartWithHorse, 0);
        horseSeed = horseSeed with { Entropy = 8 };
        footSeed = footSeed with { Entropy = 8 };

        var horsePackage = BuildPackage(horseSeed);
        var footPackage = BuildPackage(footSeed);

        Assert.True(horsePackage.StartingInventory.HasItem(ItemKind.Horse));
        Assert.False(footPackage.StartingInventory.HasItem(ItemKind.Horse));
        Assert.True(horsePackage.StartingInventory.HasItem(ItemKind.Saddle));
        Assert.False(footPackage.StartingInventory.HasItem(ItemKind.Saddle));
        Assert.NotEqual(horsePackage.StartingWallet.Cash, footPackage.StartingWallet.Cash);
    }

    [Fact]
    public void DifferentDifficultyChangesTravelRulesAndStartingCash()
    {
        var easySeed = GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Easy);
        var hardSeed = GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Hard);
        easySeed = easySeed with { Entropy = 14 };
        hardSeed = hardSeed with { Entropy = 14 };

        var easyPackage = BuildPackage(easySeed);
        var hardPackage = BuildPackage(hardSeed);

        Assert.NotEqual(easyPackage.TravelRulesProfile, hardPackage.TravelRulesProfile);
        Assert.NotEqual(easyPackage.StartingWallet.Cash, hardPackage.StartingWallet.Cash);
    }

    [Fact]
    public void CanonicalZeroEntropyUsesTheExplicitCanonicalPlan()
    {
        var canonicalSeed = GameSetupSeedCodec.CreateCanonicalSeed();
        var plan = GameSetupGenerationPlan.Create(canonicalSeed);

        Assert.True(plan.IsCanonical);
        Assert.Equal(SeedWorldVariant.Canonical, plan.WorldVariant);

        var package = BuildPackage(canonicalSeed);
        Assert.Equal(new TownId("pinecross"), package.StartingTownId);
        Assert.Equal(25m, package.StartingWallet.Cash);
        Assert.Equal(SeedWorldVariant.Canonical, plan.WorldVariant);
    }

    private static GameSetupPackage BuildPackage(GameSetupSeed seed)
        => new GameSetupPackageBuilder().Build(seed);

    private static string BuildPackageSignature(GameSetupPackage package)
        => string.Join(
            "|",
            package.Seed.GeneratorVersion,
            package.Seed.Difficulty,
            package.Seed.Options,
            package.Seed.Entropy,
            package.TravelRulesProfile,
            package.StartingTownId.Value,
            package.StartingWallet.Cash,
            string.Join(",", package.World.Towns.OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase).Select(town => $"{town.Id.Value}:{town.Name}:{town.Services}")),
            string.Join(",", package.World.Trails.OrderBy(trail => trail.Id.Value, StringComparer.OrdinalIgnoreCase).Select(trail => $"{trail.Id.Value}:{trail.FromTownId.Value}:{trail.ToTownId.Value}:{trail.Risk}:{trail.Terrain}:{trail.WaterFeature}:{trail.RideDayDistance}")),
            string.Join(",", package.StartingInventory.Items.Select(item => $"{item.Kind}:{item.Quantity}:{item.HorseState?.Hunger ?? -1}:{item.HorseState?.Thirst ?? -1}:{item.HorseState?.Exhaustion ?? -1}:{item.CanteenState?.Charges ?? -1}:{item.CanteenState?.Capacity ?? -1}")),
            string.Join(",", package.CaseFile.Suspects.Select(suspect => $"{suspect.Id.Value}:{suspect.Name}:{suspect.Status}")),
            package.CaseFile.TrueCulpritId.Value,
            package.CaseFile.Accusation?.Value ?? string.Empty,
            package.CaseFile.OpeningLead.Description,
            string.Join(",", package.CaseFile.KnownClues.Select(clue => $"{clue.Id.Value}:{clue.Kind}:{clue.Description}:{clue.TargetKind}:{clue.Source}:{clue.Context}:{string.Join("/", clue.LinkedSuspectIds.Select(id => id.Value))}")),
            string.Join(",", package.CaseFile.PublicClues.Select(clue => $"{clue.Id.Value}:{clue.Kind}:{clue.Description}:{clue.TargetKind}:{clue.Source}:{clue.Context}:{string.Join("/", clue.LinkedSuspectIds.Select(id => id.Value))}")),
            string.Join(",", package.CaseFile.PublicWarrants.Select(warrant => $"{warrant.Id.Value}:{warrant.TargetName}:{warrant.Terms.Disposition}:{warrant.Terms.BountyAmount}:{string.Join("/", warrant.Terms.KnownAliases)}:{string.Join("/", warrant.Terms.KnownFeatures)}:{warrant.Terms.IssuingSource}:{warrant.Terms.TargetKind}:{warrant.Terms.IsGangRelevant}:{warrant.Terms.AdvancesGangPressure}:{warrant.Summary}")));
}
