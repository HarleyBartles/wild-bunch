using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using TownId = WildBunch.Domain.World.TownId;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Tests;

public sealed class TravelDayPlanGeneratorEntropyTests
{
    private const GameDifficulty FixedDifficulty = GameDifficulty.Standard;

    [Fact]
    public void GenerateProducesDifferentCategoryDistributionForDifferentEntropyAtSameDifficulty()
    {
        var seeds = Enumerable.Range(1, 120).Select(i => $"seed-entropy-{i}").ToArray();
        var boringCounts = new Dictionary<TravelDayEncounterCategory, int>();
        var classicCounts = new Dictionary<TravelDayEncounterCategory, int>();
        var adventurousCounts = new Dictionary<TravelDayEncounterCategory, int>();
        var wildCounts = new Dictionary<TravelDayEncounterCategory, int>();

        foreach (var seed in seeds)
        {
            CountCategories(CreateContext(seed, GameEntropy.Boring), boringCounts);
            CountCategories(CreateContext(seed, GameEntropy.Classic), classicCounts);
            CountCategories(CreateContext(seed, GameEntropy.Adventurous), adventurousCounts);
            CountCategories(CreateContext(seed, GameEntropy.Wild), wildCounts);
        }

        // Boring should have more Quiet than Wild
        Assert.True(boringCounts.GetValueOrDefault(TravelDayEncounterCategory.Quiet) >
                    wildCounts.GetValueOrDefault(TravelDayEncounterCategory.Quiet),
            "Boring should produce more Quiet encounters than Wild");

        // Wild should have more Lucky+Unlucky than Classic
        var wildVariance = wildCounts.GetValueOrDefault(TravelDayEncounterCategory.Lucky) +
                           wildCounts.GetValueOrDefault(TravelDayEncounterCategory.Unlucky);
        var classicVariance = classicCounts.GetValueOrDefault(TravelDayEncounterCategory.Lucky) +
                              classicCounts.GetValueOrDefault(TravelDayEncounterCategory.Unlucky);
        Assert.True(wildVariance > classicVariance,
            "Wild should produce more Lucky+Unlucky encounters than Classic");

        // Wild should have more Lucky+Unlucky than Adventurous
        var adventurousVariance = adventurousCounts.GetValueOrDefault(TravelDayEncounterCategory.Lucky) +
                                  adventurousCounts.GetValueOrDefault(TravelDayEncounterCategory.Unlucky);
        Assert.True(wildVariance > adventurousVariance,
            "Wild should produce more Lucky+Unlucky encounters than Adventurous");

        // Boring should have less Lucky+Unlucky than Classic
        var boringVariance = boringCounts.GetValueOrDefault(TravelDayEncounterCategory.Lucky) +
                             boringCounts.GetValueOrDefault(TravelDayEncounterCategory.Unlucky);
        Assert.True(boringVariance < classicVariance,
            "Boring should produce fewer Lucky+Unlucky encounters than Classic");
    }

    [Fact]
    public void GenerateProducesDifferentCategoryDistributionForDifferentDifficultyAtSameEntropy()
    {
        var seeds = Enumerable.Range(1, 120).Select(i => $"seed-diff-{i}").ToArray();
        var easyCounts = new Dictionary<TravelDayEncounterCategory, int>();
        var brutalCounts = new Dictionary<TravelDayEncounterCategory, int>();

        foreach (var seed in seeds)
        {
            CountCategories(CreateContext(seed, GameEntropy.Classic, GameDifficulty.Easy), easyCounts);
            CountCategories(CreateContext(seed, GameEntropy.Classic, GameDifficulty.Brutal), brutalCounts);
        }

        // Brutal should have more Foe than Easy (difficulty = pressure)
        Assert.True(brutalCounts.GetValueOrDefault(TravelDayEncounterCategory.Foe) >
                    easyCounts.GetValueOrDefault(TravelDayEncounterCategory.Foe),
            "Brutal should produce more Foe encounters than Easy at same entropy");
    }

    [Fact]
    public void WildEntropyDoesNotEqualBrutalDifficulty()
    {
        var seeds = Enumerable.Range(1, 120).Select(i => $"seed-wild-vs-brutal-{i}").ToArray();
        var wildStandardCounts = new Dictionary<TravelDayEncounterCategory, int>();
        var classicBrutalCounts = new Dictionary<TravelDayEncounterCategory, int>();

        foreach (var seed in seeds)
        {
            CountCategories(CreateContext(seed, GameEntropy.Wild, GameDifficulty.Standard), wildStandardCounts);
            CountCategories(CreateContext(seed, GameEntropy.Classic, GameDifficulty.Brutal), classicBrutalCounts);
        }

        // Wild+Standard should have fewer Foe than Classic+Brutal — Wild is variance, not pressure
        Assert.True(wildStandardCounts.GetValueOrDefault(TravelDayEncounterCategory.Foe) <
                    classicBrutalCounts.GetValueOrDefault(TravelDayEncounterCategory.Foe),
            "Wild+Standard should produce fewer Foe encounters than Classic+Brutal — Wild is variance, not pressure");

        // Wild+Standard should have more Lucky than Classic+Brutal — Wild increases positive variance (Lucky),
        // while Brutal does not increase Lucky (it increases Foe and Unlucky as pressure)
        Assert.True(wildStandardCounts.GetValueOrDefault(TravelDayEncounterCategory.Lucky) >
                    classicBrutalCounts.GetValueOrDefault(TravelDayEncounterCategory.Lucky),
            "Wild+Standard should produce more Lucky encounters than Classic+Brutal — Wild increases positive variance, Brutal does not");
    }

    [Fact]
    public void BoringEntropyDampensRareCategories()
    {
        var seeds = Enumerable.Range(1, 120).Select(i => $"seed-boring-rare-{i}").ToArray();
        var boringCounts = new Dictionary<TravelDayEncounterCategory, int>();
        var classicCounts = new Dictionary<TravelDayEncounterCategory, int>();

        foreach (var seed in seeds)
        {
            CountCategories(CreateContext(seed, GameEntropy.Boring), boringCounts);
            CountCategories(CreateContext(seed, GameEntropy.Classic), classicCounts);
        }

        // Boring should have fewer Environmental+Npc+HorseTrouble than Classic
        var boringRare = boringCounts.GetValueOrDefault(TravelDayEncounterCategory.Environmental) +
                         boringCounts.GetValueOrDefault(TravelDayEncounterCategory.Npc) +
                         boringCounts.GetValueOrDefault(TravelDayEncounterCategory.HorseTrouble);
        var classicRare = classicCounts.GetValueOrDefault(TravelDayEncounterCategory.Environmental) +
                          classicCounts.GetValueOrDefault(TravelDayEncounterCategory.Npc) +
                          classicCounts.GetValueOrDefault(TravelDayEncounterCategory.HorseTrouble);
        Assert.True(boringRare < classicRare,
            "Boring should produce fewer rare-category encounters than Classic");
    }

    [Fact]
    public void EntropyAndDifficultyAreIndependent()
    {
        var seeds = Enumerable.Range(1, 120).Select(i => $"seed-indep-{i}").ToArray();

        // Changing entropy at fixed difficulty changes the distribution
        var classicAtStandard = new Dictionary<TravelDayEncounterCategory, int>();
        var wildAtStandard = new Dictionary<TravelDayEncounterCategory, int>();
        // Changing difficulty at fixed entropy changes the distribution
        var classicAtBrutal = new Dictionary<TravelDayEncounterCategory, int>();

        foreach (var seed in seeds)
        {
            CountCategories(CreateContext(seed, GameEntropy.Classic, GameDifficulty.Standard), classicAtStandard);
            CountCategories(CreateContext(seed, GameEntropy.Wild, GameDifficulty.Standard), wildAtStandard);
            CountCategories(CreateContext(seed, GameEntropy.Classic, GameDifficulty.Brutal), classicAtBrutal);
        }

        // Entropy change and difficulty change produce different patterns
        var classicStdFoe = classicAtStandard.GetValueOrDefault(TravelDayEncounterCategory.Foe);
        var wildStdFoe = wildAtStandard.GetValueOrDefault(TravelDayEncounterCategory.Foe);
        var classicBrutalFoe = classicAtBrutal.GetValueOrDefault(TravelDayEncounterCategory.Foe);

        // Difficulty change increases Foe; entropy change (Wild) does not increase Foe
        Assert.True(classicBrutalFoe > classicStdFoe,
            "Difficulty change (Standard->Brutal) should increase Foe");
        Assert.True(wildStdFoe <= classicBrutalFoe,
            "Wild entropy should not increase Foe beyond what Brutal difficulty does");
    }

    private static void CountCategories(TravelDayGenerationContext context, Dictionary<TravelDayEncounterCategory, int> counts)
    {
        var plan = TravelDayPlanGenerator.Generate(context);
        foreach (var encounter in plan.Encounters)
        {
            counts[encounter.Category] = counts.GetValueOrDefault(encounter.Category) + 1;
        }
    }

    private static TravelDayGenerationContext CreateContext(string seed, GameEntropy entropy, GameDifficulty difficulty = FixedDifficulty)
        => new(
            TravelDayPlanGenerator.CurrentVersion,
            seed,
            "profile-entropy",
            "trail-entropy",
            new TownId("pinecross"),
            new TownId("dryfork"),
            1,
            TravelMode.Mounted,
            TrailRisk.Moderate,
            TrailTerrain.OpenRange,
            WaterFeature.Creek,
            difficulty,
            3,
            3m,
            TravelPressureBand.None,
            TravelPressureBand.None,
            TravelPressureBand.None,
            HorseConditionBand.Sound,
            WalletBand.Steady,
            Array.Empty<JourneyTrailEventKind>(),
            Array.Empty<JourneyTrailEventId>(),
            Array.Empty<TravelDayEncounterCategory>(),
            HasHorse: true,
            SaltSourceMode.Fixed,
            seed,
            entropy);
}
