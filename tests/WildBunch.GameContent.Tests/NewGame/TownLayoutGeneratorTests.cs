using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorTests
{
    private static LayoutDeterministicSource NewLayoutSource(
        TownId townId,
        Guid? seedCode = null,
        LayoutSalts? salts = null)
    {
        var seedCodeStr = SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode());
        var actualSalts = salts ?? new LayoutSalts("default", "default", "default", "default");
        return new LayoutDeterministicSource(seedCodeStr, townId, 0, "1.0.0", actualSalts);
    }

    private static TownId NewTownId(string value) => new(value);

    private static TownLayout Generate(
        TownProsperity prosperity = TownProsperity.Prosperous,
        string townName = "town-1",
        BuildingLayoutPalette palette = BuildingLayoutPalette.NoSpurs_SpreadEvenly,
        string? resolverVersion = "1.0.0")
    {
        var townId = NewTownId(townName);
        var source = NewLayoutSource(townId);
        return resolverVersion is null
            ? TownLayoutGenerator.GenerateLayout(prosperity, townId, 0, source, palette)
            : TownLayoutGenerator.GenerateLayout(prosperity, townId, 0, source, palette, resolverVersion);
    }

    [Fact]
    public void GenerateLayout_EverySupportedPaletteAndProsperityPlacesEachRequiredBuildingExactlyOnce()
    {
        var supportedPalettes = Enum.GetValues<BuildingLayoutPalette>()
            .Where(palette => palette < BuildingLayoutPalette.Reserved12);

        foreach (var palette in supportedPalettes)
        {
            foreach (var prosperity in Enum.GetValues<TownProsperity>())
            {
                var layout = Generate(prosperity, $"town-{palette}-{prosperity}", palette);

                Assert.Equal(6, layout.Buildings.Count);
                Assert.Equal(1, layout.Buildings.Count(building => building.Kind == BuildingKind.Store));
                Assert.Equal(1, layout.Buildings.Count(building => building.Kind == BuildingKind.Sheriff));
                Assert.Equal(1, layout.Buildings.Count(building => building.Kind == BuildingKind.Saloon));
                Assert.Equal(1, layout.Buildings.Count(building => building.Kind == BuildingKind.Telegraph));
                Assert.Equal(2, layout.Buildings.Count(building => building.Kind == BuildingKind.Trailhead));
            }
        }
    }

    [Theory]
    [InlineData(BuildingLayoutPalette.OneSpurLeft_SpreadEvenly, 4, 3, 2, 25, 35)]
    [InlineData(BuildingLayoutPalette.OneSpurLeft_ClusterMiddle, 4, 3, 2, 25, 35)]
    [InlineData(BuildingLayoutPalette.OneSpurRight_SpreadEvenly, 4, 6, 7, 75, 35)]
    [InlineData(BuildingLayoutPalette.OneSpurRight_ClusterMiddle, 4, 6, 7, 75, 35)]
    public void GenerateLayout_OneSpurPaletteProducesRoadAndBuildingAtItsDeclaredBranch(
        BuildingLayoutPalette palette,
        int spurRow,
        int junctionColumn,
        int extensionColumn,
        int buildingX,
        int buildingY)
    {
        var layout = Generate(townName: $"town-{palette}", palette: palette);
        var grid = Assert.IsType<int[][]>(layout.TileGrid);

        Assert.Equal(3, grid[spurRow][junctionColumn]);
        Assert.Equal(4, grid[spurRow][extensionColumn]);
        Assert.Contains(layout.Buildings, building =>
            building.Kind is BuildingKind.Store or BuildingKind.Sheriff or BuildingKind.Saloon or BuildingKind.Telegraph
            && building.X == buildingX
            && building.Y == buildingY);
    }

    [Theory]
    [InlineData(BuildingLayoutPalette.TwoSpursLeftRight_SpreadEvenly, 3, 3, 2, 25, 25, 6, 6, 7, 75, 55)]
    [InlineData(BuildingLayoutPalette.TwoSpursLeftRight_ClusterMiddle, 3, 3, 2, 25, 25, 6, 6, 7, 75, 55)]
    [InlineData(BuildingLayoutPalette.TwoSpursRightLeft_SpreadEvenly, 3, 6, 7, 75, 25, 6, 3, 2, 25, 55)]
    [InlineData(BuildingLayoutPalette.TwoSpursRightLeft_ClusterMiddle, 3, 6, 7, 75, 25, 6, 3, 2, 25, 55)]
    public void GenerateLayout_TwoSpurPaletteProducesBothRoadsAndBuildingsAtTheirBranches(
        BuildingLayoutPalette palette,
        int firstSpurRow,
        int firstJunctionColumn,
        int firstExtensionColumn,
        int firstBuildingX,
        int firstBuildingY,
        int secondSpurRow,
        int secondJunctionColumn,
        int secondExtensionColumn,
        int secondBuildingX,
        int secondBuildingY)
    {
        var layout = Generate(townName: $"town-{palette}", palette: palette);
        var grid = Assert.IsType<int[][]>(layout.TileGrid);

        Assert.Equal(3, grid[firstSpurRow][firstJunctionColumn]);
        Assert.Equal(4, grid[firstSpurRow][firstExtensionColumn]);
        Assert.Equal(3, grid[secondSpurRow][secondJunctionColumn]);
        Assert.Equal(4, grid[secondSpurRow][secondExtensionColumn]);
        Assert.Contains(layout.Buildings, building =>
            building.Kind is BuildingKind.Store or BuildingKind.Sheriff or BuildingKind.Saloon or BuildingKind.Telegraph
            && building.X == firstBuildingX
            && building.Y == firstBuildingY);
        Assert.Contains(layout.Buildings, building =>
            building.Kind is BuildingKind.Store or BuildingKind.Sheriff or BuildingKind.Saloon or BuildingKind.Telegraph
            && building.X == secondBuildingX
            && building.Y == secondBuildingY);
    }

    [Fact]
    public void GenerateLayout_NoSpurLayoutKeepsRendererGeometryAndPlacementsAligned()
    {
        var layout = Generate();
        var grid = Assert.IsType<int[][]>(layout.TileGrid);

        Assert.Equal(10, grid.Length);
        Assert.All(grid, row => Assert.Equal(10, row.Length));
        for (var row = 0; row < 10; row++)
        {
            Assert.Equal(1, grid[row][4]);
            Assert.Equal(1, grid[row][5]);
        }

        for (var row = 1; row < 9; row++)
        {
            Assert.Equal(2, grid[row][3]);
            Assert.Equal(2, grid[row][6]);
        }

        Assert.Equal((50, 50), (layout.PlayerSpawnX, layout.PlayerSpawnY));
        Assert.Equal(6, layout.Buildings.Count);

        var northTrailhead = Assert.Single(layout.Buildings, building => building.Kind == BuildingKind.Trailhead && building.Y == 5);
        var southTrailhead = Assert.Single(layout.Buildings, building => building.Kind == BuildingKind.Trailhead && building.Y == 95);
        Assert.Equal((50, 5, 20, 10), (northTrailhead.X, northTrailhead.Y, northTrailhead.Width, northTrailhead.Height));
        Assert.Equal((50, 95, 20, 10), (southTrailhead.X, southTrailhead.Y, southTrailhead.Width, southTrailhead.Height));

        var services = layout.Buildings.Where(building => building.Kind != BuildingKind.Trailhead).ToArray();
        Assert.Equal(4, services.Length);
        Assert.All(services, building =>
        {
            Assert.Equal((8, 10), (building.Width, building.Height));
            Assert.True(building.X is 35 or 65);
            Assert.InRange(building.Y, 15, 85);
            Assert.Equal(5, building.X % 10);
            Assert.Equal(5, building.Y % 10);
            Assert.Equal(2, grid[building.Y / 10][building.X / 10]);
        });
        Assert.True(services.Select(building => building.Y / 10).Distinct().Count() > 1);
    }

    [Fact]
    public void GenerateLayout_SameInputsReproduceEveryPersistedLayoutField()
    {
        var townId = NewTownId("repeatable-town");
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        var sourceA = NewLayoutSource(townId, SeedWorldResolver.CreateCanonicalSeedCode(), salts);
        var sourceB = NewLayoutSource(townId, SeedWorldResolver.CreateCanonicalSeedCode(), salts);

        var first = TownLayoutGenerator.GenerateLayout(
            TownProsperity.Poor, townId, 2, sourceA, BuildingLayoutPalette.OneSpurRight_ClusterMiddle, "1.0.0");
        var second = TownLayoutGenerator.GenerateLayout(
            TownProsperity.Poor, townId, 2, sourceB, BuildingLayoutPalette.OneSpurRight_ClusterMiddle, "1.0.0");

        Assert.Equal(first.Buildings, second.Buildings);
        Assert.Equal(first.Paths, second.Paths);
        Assert.Equal((first.PlayerSpawnX, first.PlayerSpawnY), (second.PlayerSpawnX, second.PlayerSpawnY));
        Assert.Equal(first.Prosperity, second.Prosperity);
        Assert.Equal(first.ResolverVersion, second.ResolverVersion);
        Assert.Equal(first.LayoutSalts, second.LayoutSalts);

        var firstGrid = Assert.IsType<int[][]>(first.TileGrid);
        var secondGrid = Assert.IsType<int[][]>(second.TileGrid);
        Assert.Equal(firstGrid.Length, secondGrid.Length);
        for (var row = 0; row < firstGrid.Length; row++)
        {
            Assert.Equal(firstGrid[row], secondGrid[row]);
        }
    }

    [Fact]
    public void GenerateLayout_WithoutExplicitResolverVersion_UsesCurrentVersion()
    {
        var layout = Generate(resolverVersion: null);

        Assert.Equal(TownLayout.CurrentResolverVersion, layout.ResolverVersion);
    }

    [Fact]
    public void GenerateLayout_CurrentHubLayoutHasNoGeneratedPaths()
    {
        Assert.Empty(Generate().Paths);
    }
}
