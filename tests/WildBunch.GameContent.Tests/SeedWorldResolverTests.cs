using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldResolverTests
{
    [Fact]
    public void CanonicalSeedWorldRoundTripsThroughAUuidShapedSeedCode()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();

        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
        var resolved = SeedWorldResolver.Resolve(seedCode);

        Assert.Equal(seedWorld.WorldVariant, resolved.WorldVariant);
        Assert.Equal(seedWorld.TownCount, resolved.TownCount);
        Assert.Equal(seedWorld.ServicesPalette, resolved.ServicesPalette);
        Assert.Equal(seedWorld.ProsperityPalette, resolved.ProsperityPalette);
        Assert.Equal(seedWorld.ClusterCount, resolved.ClusterCount);
        Assert.Equal(seedWorld.GraphDensity, resolved.GraphDensity);
        Assert.Equal(seedWorld.SelectedTownIds, resolved.SelectedTownIds);
        Assert.Equal(seedWorld.AccusationIndex, resolved.AccusationIndex);
        Assert.Equal(seedWorld.DefaultCulpritIndex, resolved.DefaultCulpritIndex);
        Assert.Equal(seedWorld.CashBonus, resolved.CashBonus);
        Assert.Equal(seedCode, SeedWorldResolver.CreateRepresentativeSeedCode(resolved));
    }

    [Fact]
    public void MultipleUuidSeedsCanResolveToTheSameSeedWorld()
    {
        var seedA = CreateSeedCode(0, 1, 3, 0, tail: 1);
        var seedB = CreateSeedCode(0, 1, 3, 0, tail: 2);

        var seedWorldA = SeedWorldResolver.Resolve(seedA);
        var seedWorldB = SeedWorldResolver.Resolve(seedB);

        Assert.Equal(seedWorldA.WorldVariant, seedWorldB.WorldVariant);
        Assert.Equal(seedWorldA.TownCount, seedWorldB.TownCount);
        Assert.Equal(seedWorldA.SelectedTownIds, seedWorldB.SelectedTownIds);
        Assert.Equal(seedWorldA.AccusationIndex, seedWorldB.AccusationIndex);
        Assert.Equal(seedWorldA.DefaultCulpritIndex, seedWorldB.DefaultCulpritIndex);
        Assert.Equal(seedWorldA.CashBonus, seedWorldB.CashBonus);
    }

    [Fact]
    public void SeedWorldResolutionIsIndependentOfDifficultyAndEntropy()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());

        var seedWorld = SeedWorldResolver.Resolve(Guid.Parse(seedCode));

        Assert.True(Enum.IsDefined(typeof(SeedWorldVariant), seedWorld.WorldVariant));
        Assert.InRange(seedWorld.AccusationIndex, 0, 6);
        Assert.InRange(seedWorld.DefaultCulpritIndex, 0, 6);
        Assert.InRange(seedWorld.CashBonus, 0, 8);
    }

    [Fact]
    public void SeedWorldValidationRejectsImpossibleManualEdits()
    {
        var valid = SeedWorldResolver.CreateCanonicalSeedWorld();

        var invalidAccusation = valid with { AccusationIndex = 42 };
        var invalidCulprit = valid with { DefaultCulpritIndex = 42 };
        var invalidCashBonus = valid with { CashBonus = 42 };
        var invalidTownCount = valid with { TownCount = 99 };
        var invalidProsperity = valid with { ProsperityPalette = (ProsperityPalette)99 };
        var invalidServices = valid with { ServicesPalette = (ServicesPalette)99 };
        var invalidClusterCount = valid with { ClusterCount = 99 };
        var invalidGraphDensity = valid with { GraphDensity = (GraphDensity)99 };

        Assert.False(SeedWorldResolver.Validate(invalidAccusation).Success);
        Assert.False(SeedWorldResolver.Validate(invalidCulprit).Success);
        Assert.False(SeedWorldResolver.Validate(invalidCashBonus).Success);
        Assert.False(SeedWorldResolver.Validate(invalidTownCount).Success);
        Assert.False(SeedWorldResolver.Validate(invalidProsperity).Success);
        Assert.False(SeedWorldResolver.Validate(invalidServices).Success);
        Assert.False(SeedWorldResolver.Validate(invalidClusterCount).Success);
        Assert.False(SeedWorldResolver.Validate(invalidGraphDensity).Success);
    }

    [Fact]
    public void AnyValidUuidResolvesToLegalSeedWorldValues()
    {
        for (var variant = 0; variant < 3; variant++)
        {
            for (var accusation = 0; accusation < 7; accusation++)
            {
                for (var culprit = 0; culprit < 7; culprit++)
                {
                    for (var cash = 0; cash <= 8; cash++)
                    {
                        var seed = CreateSeedCode((byte)variant, (byte)accusation, (byte)culprit, (byte)cash, tail: 0);
                        var seedWorld = SeedWorldResolver.Resolve(seed);
                        var validation = SeedWorldResolver.Validate(seedWorld);

                        Assert.True(validation.Success, validation.ErrorMessage);
                        Assert.Contains(seedWorld.WorldVariant, Enum.GetValues<SeedWorldVariant>());
                        Assert.InRange(seedWorld.AccusationIndex, 0, 6);
                        Assert.InRange(seedWorld.DefaultCulpritIndex, 0, 6);
                        Assert.InRange(seedWorld.CashBonus, 0, 8);
                        Assert.InRange(seedWorld.TownCount, SeedWorldResolver.MinTownCount, SeedWorldResolver.MaxTownCount);
                    }
                }
            }
        }
    }

    [Fact]
    public void DifferentUuidBitPositionsChangeDifferentSeedWorldFields()
    {
        var baseSeed = SeedWorldResolver.CreateCanonicalSeedCode();
        var baseWorld = SeedWorldResolver.Resolve(baseSeed);

        var bytes = baseSeed.ToByteArray();
        var low = BitConverter.ToUInt64(bytes, 0);

        // Change variant (bits 0-1): flip to a different variant.
        var newVariant = ((int)baseWorld.WorldVariant + 1) % 3;
        var variantLow = (low & ~0x3UL) | (ulong)(uint)newVariant;
        var variantBytes = new byte[16];
        BitConverter.TryWriteBytes(variantBytes.AsSpan(0), variantLow);
        BitConverter.TryWriteBytes(variantBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var variantWorld = SeedWorldResolver.Resolve(new Guid(variantBytes));
        Assert.NotEqual(baseWorld.WorldVariant, variantWorld.WorldVariant);

        // Change accusation (bits 2-5): increment by 1.
        var newAccusation = (baseWorld.AccusationIndex + 1) % 7;
        var accusationLow = (low & ~(0xFUL << 2)) | ((ulong)newAccusation << 2);
        var accusationBytes = new byte[16];
        BitConverter.TryWriteBytes(accusationBytes.AsSpan(0), accusationLow);
        BitConverter.TryWriteBytes(accusationBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var accusationWorld = SeedWorldResolver.Resolve(new Guid(accusationBytes));
        Assert.NotEqual(baseWorld.AccusationIndex, accusationWorld.AccusationIndex);

        // Change townCount (bits 14-17): encoded value 3 → town count 8 (3+5 offset).
        var newCountEncoded = 3;
        var countLow = (low & ~(0xFUL << 14)) | ((ulong)newCountEncoded << 14);
        var countBytes = new byte[16];
        BitConverter.TryWriteBytes(countBytes.AsSpan(0), countLow);
        BitConverter.TryWriteBytes(countBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var countWorld = SeedWorldResolver.Resolve(new Guid(countBytes));
        Assert.Equal(8, countWorld.TownCount);

        // Change prosperityPalette (bits 18-20): set to Dustbowl.
        var newProsperity = ProsperityPalette.Dustbowl;
        var prosperityLow = (low & ~(0x7UL << 18)) | ((ulong)newProsperity << 18);
        var prosperityBytes = new byte[16];
        BitConverter.TryWriteBytes(prosperityBytes.AsSpan(0), prosperityLow);
        BitConverter.TryWriteBytes(prosperityBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var prosperityWorld = SeedWorldResolver.Resolve(new Guid(prosperityBytes));
        Assert.Equal(ProsperityPalette.Dustbowl, prosperityWorld.ProsperityPalette);

        // Change servicesPalette (bits 21-23): set to AllTelegraph.
        var newServices = ServicesPalette.AllTelegraph;
        var servicesLow = (low & ~(0x7UL << 21)) | ((ulong)newServices << 21);
        var servicesBytes = new byte[16];
        BitConverter.TryWriteBytes(servicesBytes.AsSpan(0), servicesLow);
        BitConverter.TryWriteBytes(servicesBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var servicesWorld = SeedWorldResolver.Resolve(new Guid(servicesBytes));
        Assert.Equal(ServicesPalette.AllTelegraph, servicesWorld.ServicesPalette);
    }

    [Fact]
    public void InvalidUuidSeedCodesFailValidation()
    {
        Assert.False(StartingWorldDescriptorCodeValidator.TryValidate("not-a-uuid", out var errorMessage));
        Assert.Equal("Seed code must be a UUID-shaped string.", errorMessage);
        Assert.False(SeedWorldResolver.TryParseSeedCode("WB1-N-03-000000000000-0000", out _));
    }

    [Fact]
    public void ResolverDerivesTownCountFromSeed()
    {
        // The factory creates 8-town worlds by default.
        var seed = SeedWorldResolver.CreateCanonicalSeedCode();
        var resolved = SeedWorldResolver.Resolve(seed);
        Assert.Equal(8, resolved.TownCount);
        Assert.Equal(8, resolved.SelectedTownIds.Count);
    }

    [Fact]
    public void DifferentEncodedFieldsProduceDifferentTownNames()
    {
        // Different cash bonus values change the derivation seed, producing
        // different name shuffles.
        var seedA = CreateSeedCode(0, 1, 3, 0, tail: 0);
        var seedB = CreateSeedCode(0, 1, 3, 3, tail: 0);

        var namesA = string.Join(",", SeedWorldResolver.Resolve(seedA).SelectedTownIds.OrderBy(id => id));
        var namesB = string.Join(",", SeedWorldResolver.Resolve(seedB).SelectedTownIds.OrderBy(id => id));

        Assert.NotEqual(namesA, namesB);
    }

    [Fact]
    public void SameSeedProducesSameSeedWorld()
    {
        var seed = SeedWorldResolver.CreateCanonicalSeedCode();
        var seedWorldA = SeedWorldResolver.Resolve(seed);
        var seedWorldB = SeedWorldResolver.Resolve(seed);
        Assert.Equal(seedWorldA.SelectedTownIds, seedWorldB.SelectedTownIds);
        Assert.Equal(seedWorldA.Trails.Count, seedWorldB.Trails.Count);
    }

    [Fact]
    public void NamePoolHasAtLeastTwiceMaxTownCount()
    {
        Assert.True(SeedWorldCatalog.NamePool.Count >= SeedWorldResolver.MaxTownCount * 2,
            $"Name pool has {SeedWorldCatalog.NamePool.Count} entries, need at least {SeedWorldResolver.MaxTownCount * 2}.");
    }

    [Fact]
    public void AllTownNamesAreUnique()
    {
        var names = SeedWorldCatalog.NamePool.Select(n => n.Name).ToArray();
        var ids = SeedWorldCatalog.NamePool.Select(n => n.Id).ToArray();
        Assert.Equal(names.Length, names.Distinct().Count());
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    private static Guid CreateSeedWorldWithCount(int townCount)
    {
        var variant = SeedWorldVariant.Canonical;
        var accusationIndex = 1;
        var defaultCulpritIndex = 3;
        var cashBonus = 0;
        var prosperity = ProsperityPalette.UniformProsperous;
        var services = ServicesPalette.HubTelegraph;
        var clusterCount = 1;
        var graphDensity = GraphDensity.Sparse;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperity, services);
        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(services, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = Array.Empty<SeedWorldTrail>();

        return SeedWorldResolver.CreateRepresentativeSeedCode(new SeedWorld(
            Guid.Empty, variant, townCount, services, prosperity, clusterCount, graphDensity,
            accusationIndex, defaultCulpritIndex, cashBonus,
            selectedTownIds, townServices, trails, OutlierSlotType: 0));
    }

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);
}
