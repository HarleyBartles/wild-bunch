using System.Linq;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class MapGeneratorLayoutSaltsTests
{
    [Fact]
    public void Generate_DerivesAndRetainsConcernSaltsForEveryTown()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString());

        var world = MapGenerator.Generate(
            seedWorld,
            source,
            GameEntropy.Classic,
            null);

        Assert.NotNull(world);
        Assert.NotNull(world.Towns);

        foreach (var town in world.Towns)
        {
            Assert.NotNull(town.Layout);
            Assert.NotNull(town.Layout.LayoutSalts);
            Assert.NotNull(town.Layout.LayoutSalts.BuildingsSalt);
            Assert.NotNull(town.Layout.LayoutSalts.RoadsSalt);
            Assert.NotNull(town.Layout.LayoutSalts.DirtSalt);
            Assert.NotNull(town.Layout.LayoutSalts.PropsSalt);
        }
    }

    [Fact]
    public void Generate_SameSeedEntropyTownSalts_ReproducesSameLayout()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString();

        var world1 = MapGenerator.Generate(
            seedWorld,
            new GameSetupDeterministicSource(seedCode),
            GameEntropy.Classic,
            null);

        var world2 = MapGenerator.Generate(
            seedWorld,
            new GameSetupDeterministicSource(seedCode),
            GameEntropy.Classic,
            null);

        Assert.Equal(world1.Towns.Count, world2.Towns.Count);

        for (var i = 0; i < world1.Towns.Count; i++)
        {
            var layout1 = world1.Towns.ElementAt(i).Layout;
            var layout2 = world2.Towns.ElementAt(i).Layout;

            Assert.Equal(layout1!.Buildings, layout2!.Buildings);
            Assert.Equal(layout1!.LayoutSalts, layout2!.LayoutSalts);
        }
    }

    [Fact]
    public void Generate_DerivedPath_ExposesActualBundleUsed()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString();

        var world = MapGenerator.Generate(
            seedWorld,
            new GameSetupDeterministicSource(seedCode),
            GameEntropy.Classic,
            null);

        var layout = world.Towns.ElementAt(0).Layout;

        // Layout should have the derived salts, not null
        Assert.NotNull(layout!.LayoutSalts!);
        Assert.NotNull(layout!.LayoutSalts!.BuildingsSalt);
        Assert.NotNull(layout!.LayoutSalts!.RoadsSalt);
        Assert.NotNull(layout!.LayoutSalts!.DirtSalt);
        Assert.NotNull(layout!.LayoutSalts!.PropsSalt);

        // Salts should be different for different towns (deterministic partitioning)
        if (world.Towns.Count > 1)
        {
            var layout2 = world.Towns.ElementAt(1).Layout;
            Assert.NotEqual(layout!.LayoutSalts!.BuildingsSalt, layout2!.LayoutSalts!.BuildingsSalt);
        }
    }
}
