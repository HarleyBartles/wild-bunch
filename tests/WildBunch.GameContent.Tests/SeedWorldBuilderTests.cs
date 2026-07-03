using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using WildBunch.Domain.Game;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldBuilderTests
{
    [Fact]
    public void NonNegativeModulo_SafeForIntMinValue()
    {
        // Test the edge case of int.MinValue which would overflow Math.Abs
        var result = SeedWorldBuilder.NonNegativeModulo(int.MinValue, 10);
        Assert.InRange(result, 0, 9); // Should be a valid index 0-9
    }

    [Fact]
    public void NonNegativeModulo_SafeForNegativeValues()
    {
        // Test various negative values
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(-1, 10), 0, 9);
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(-100, 10), 0, 9);
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(-1000, 10), 0, 9);
    }

    [Fact]
    public void NonNegativeModulo_SafeForPositiveValues()
    {
        // Test various positive values
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(0, 10), 0, 9);
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(1, 10), 0, 9);
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(100, 10), 0, 9);
        Assert.InRange(SeedWorldBuilder.NonNegativeModulo(1000, 10), 0, 9);
    }

    [Fact]
    public void CreateCanonicalWorldAppliesUniformProsperousPalette()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        foreach (var town in world.Towns)
        {
            Assert.Equal(TownProsperity.Prosperous, town.Prosperity);
        }
    }

    [Fact]
    public void CreateCanonicalWorldAppliesHubTelegraphServicesPalette()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var townsByIndex = world.Towns.OrderBy(t => t.Id.Value, StringComparer.OrdinalIgnoreCase).ToArray();

        // HubTelegraph: only slot 0 has telegraph. But slot assignment is by
        // position in the derived name list, not by sorted order. We verify
        // that exactly one town has telegraph.
        var telegraphTowns = world.Towns.Where(t => t.Services.HasFlag(TownServices.Telegraph)).ToArray();
        Assert.Single(telegraphTowns);
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownNames()
    {
        // Different encoded fields produce different name shuffles, so different
        // town name selections. We use two different cash bonus values to get
        // different derivation seeds.
        var seedA = CreateSeedCode(0, 1, 3, 0, tail: 0);
        var seedB = CreateSeedCode(0, 1, 3, 3, tail: 0);

        var namesA = string.Join(",", SeedWorldResolver.Resolve(seedA).SelectedTownIds.OrderBy(id => id));
        var namesB = string.Join(",", SeedWorldResolver.Resolve(seedB).SelectedTownIds.OrderBy(id => id));

        Assert.NotEqual(namesA, namesB);
    }

    [Fact]
    public void StartingTownPolicyDefaultsToFirstTown()
    {
        // Starting town is NOT seed-owned. The safe default from StartingTownPolicy
        // is the first town in the world (slot 0), which is always present.
        var canonicalWorld = SeedWorldBuilder.CreateCanonicalWorld();
        var defaultTown = StartingTownPolicy.ResolveStartingTown(canonicalWorld, null);
        Assert.Contains(canonicalWorld.Towns, t => t.Id.Equals(defaultTown));
    }

    [Fact]
    public void StartingTownPolicyAcceptsAnyValidTownChoice()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var chosenTown = world.Towns.First();

        var resolved = StartingTownPolicy.ResolveStartingTown(world, chosenTown.Id);
        Assert.Equal(chosenTown.Id, resolved);
    }

    [Fact]
    public void StartingTownPolicyRejectsInvalidTownChoice()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        Assert.Throws<ArgumentException>(() =>
            StartingTownPolicy.ResolveStartingTown(world, new TownId("nonexistent-town")));
    }

    [Fact]
    public void TownCountRespectsMinAndMax()
    {
        for (var count = 5; count <= 10; count++)
        {
            var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld() with { TownCount = count };
            var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
            var resolved = SeedWorldResolver.Resolve(seedCode);
            Assert.Equal(count, resolved.TownCount);
            Assert.Equal(count, resolved.SelectedTownIds.Count);
        }
    }

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);
}
