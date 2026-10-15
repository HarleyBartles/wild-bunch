using System;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

var seedWorld = new SeedWorld(
    Guid.Empty,
    SeedWorldVariant.Canonical,
    TownCount: 8,
    ReservedTownNameDerivationBits: 1,
    ProsperityPalette.UniformProsperous,
    ClusterCount: 1,
    GraphDensity.Sparse,
    AccusationIndex: 6,
    DefaultCulpritIndex: 4,
    CashBonus: 0,
    OutlierSlotType: 0,
    BuildingLayoutPalette.NoSpurs_SpreadEvenly);
Console.WriteLine(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld).ToString("D").ToLowerInvariant());
