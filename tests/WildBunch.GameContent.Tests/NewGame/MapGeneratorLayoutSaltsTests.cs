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
    public void Generate_AllLegacyServiceBitPatternsKeepCoreBuildingsAndDeterministicWorld()
    {
        for (var legacyBits = 0; legacyBits < 8; legacyBits++)
        {
            var seedCode = SeedCodeWithLegacyServiceBits(legacyBits);
            var seedWorld = SeedWorldResolver.Resolve(seedCode);

            var first = MapGenerator.Generate(
                seedWorld,
                new GameSetupDeterministicSource(seedCode.ToString()),
                GameEntropy.Boring,
                null);
            var repeated = MapGenerator.Generate(
                seedWorld,
                new GameSetupDeterministicSource(seedCode.ToString()),
                GameEntropy.Boring,
                null);
            var firstTowns = first.Towns.ToArray();
            var repeatedTowns = repeated.Towns.ToArray();

            Assert.Equal(firstTowns.Length, repeatedTowns.Length);
            for (var i = 0; i < firstTowns.Length; i++)
            {
                var town = firstTowns[i];
                var sameTown = repeatedTowns[i];

                Assert.Equal(town.Id, sameTown.Id);
                Assert.Equal(town.Name, sameTown.Name);
                Assert.Equal((town.MapX, town.MapY), (sameTown.MapX, sameTown.MapY));
                Assert.Equal(town.Prosperity, sameTown.Prosperity);
                Assert.Equal(town.Layout!.Buildings, sameTown.Layout!.Buildings);

                foreach (var requiredKind in new[]
                    { BuildingKind.Saloon, BuildingKind.Sheriff, BuildingKind.Store, BuildingKind.Telegraph })
                {
                    Assert.Equal(1, town.Layout.Buildings.Count(building => building.Kind == requiredKind));
                }
            }
        }
    }

    private static Guid SeedCodeWithLegacyServiceBits(int legacyBits)
    {
        var bytes = SeedWorldResolver.CreateCanonicalSeedCode().ToByteArray();
        var low = BitConverter.ToUInt64(bytes, 0);
        low = (low & ~(0x7UL << 21)) | ((ulong)legacyBits << 21);
        BitConverter.GetBytes(low).CopyTo(bytes, 0);
        return new Guid(bytes);
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
