using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Travel;

namespace WildBunch.GameContent.Tests;

public sealed class TravelDiaryFlavourCatalogTests
{
    [Fact]
    public void CatalogUsesUniqueStableIds()
    {
        var ids = TravelDiaryFlavourCatalog.All.Select(entry => entry.Id).ToArray();

        Assert.NotEmpty(ids);
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CatalogMaintainsRichCategoryDepthAndLivingWorldCoverage()
    {
        var entries = TravelDiaryFlavourCatalog.All.ToArray();
        var counts = entries
            .GroupBy(entry => entry.Category)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.True(counts[TravelDiaryFlavourCategory.DayOpening] >= 8);
        Assert.True(counts[TravelDiaryFlavourCategory.QuietTexture] >= 12);
        Assert.True(counts[TravelDiaryFlavourCategory.LuckyEvent] >= 12);
        Assert.True(counts[TravelDiaryFlavourCategory.UnluckyEvent] >= 12);
        Assert.True(counts[TravelDiaryFlavourCategory.FoeEncounterIntro] >= 16);
        Assert.True(counts[TravelDiaryFlavourCategory.ResourceScarcity] >= 10);
        Assert.True(counts[TravelDiaryFlavourCategory.WaterScarcity] >= 6);
        Assert.True(counts[TravelDiaryFlavourCategory.WaterRelief] >= 6);
        Assert.True(counts[TravelDiaryFlavourCategory.HorsePressure] >= 10);
        Assert.True(counts[TravelDiaryFlavourCategory.ChoiceOutcome] >= 24);
        Assert.True(counts[TravelDiaryFlavourCategory.ArrivalCompletion] >= 10);

        var quietTerrains = entries
            .Where(entry => entry.Category == TravelDiaryFlavourCategory.QuietTexture && entry.Terrain is not null)
            .Select(entry => entry.Terrain!.Value)
            .Distinct()
            .ToArray();

        Assert.Contains(TrailTerrain.OpenRange, quietTerrains);
        Assert.Contains(TrailTerrain.Hills, quietTerrains);
        Assert.Contains(TrailTerrain.Badlands, quietTerrains);
        Assert.Contains(TrailTerrain.Mountains, quietTerrains);

        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.QuietTexture && entry.Tags.Contains("peddler", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.QuietTexture && entry.Tags.Contains("ranch", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.QuietTexture && entry.Tags.Contains("smoke", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.LuckyEvent && entry.Tags.Contains("trader", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.UnluckyEvent && entry.Tags.Contains("wagon", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.UnluckyEvent && entry.Tags.Contains("vultures", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.FoeEncounterIntro && entry.Tags.Contains("road-agent", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.FoeEncounterIntro && entry.Tags.Contains("crooked-deputy", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.FoeEncounterIntro && entry.Tags.Contains("claim-jumper", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Category == TravelDiaryFlavourCategory.FoeEncounterIntro && entry.Tags.Contains("hired-gun", StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void CatalogTemplatesAvoidObviousPresentTenseDiaryOpeners()
    {
        var bannedFragments = new[]
        {
            "I am ",
            "I do ",
            "I go ",
            "I see ",
            "I meet ",
            "I find ",
            "I start ",
            "I ride ",
            "I reach ",
            "I get ",
            "I pass ",
            "I come ",
            "I keep "
        };

        foreach (var entry in TravelDiaryFlavourCatalog.All)
        {
            foreach (var fragment in bannedFragments)
            {
                Assert.DoesNotContain(fragment, entry.TextTemplate, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void SelectionIsDeterministicForTheSameContext()
    {
        var context = CreateContext(TravelDiaryFlavourCategory.QuietTexture, dayNumber: 3, beatIndex: 1, terrain: TrailTerrain.Mountains, preferredTags: ["mountains"]);

        var first = TravelDiaryFlavourCatalog.Select(context, new HashSet<string>(StringComparer.Ordinal));
        var second = TravelDiaryFlavourCatalog.Select(context, new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.TextTemplate, second.TextTemplate);
    }

    [Fact]
    public void SelectionAvoidsRepeatingIdsUntilTheNarrowPoolIsExhausted()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var context = CreateContext(TravelDiaryFlavourCategory.DayOpening, dayNumber: 1, beatIndex: 0, terrain: TrailTerrain.OpenRange, preferredTags: ["open-range"]);

        var first = TravelDiaryFlavourCatalog.Select(context, seen);
        var second = TravelDiaryFlavourCatalog.Select(context, seen);
        var third = TravelDiaryFlavourCatalog.Select(context, seen);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Id, third.Id);
        Assert.NotEqual(second.Id, third.Id);
    }

    [Fact]
    public void HorsePressureFallsBackWhenThereIsNoHorse()
    {
        var context = CreateContext(TravelDiaryFlavourCategory.HorsePressure, dayNumber: 5, beatIndex: 1, hasHorse: false, preferredTags: ["horse"]);

        var selected = TravelDiaryFlavourCatalog.Select(context, new HashSet<string>(StringComparer.Ordinal));

        Assert.NotEqual(TravelDiaryFlavourCategory.HorsePressure, selected.Category);
        Assert.NotNull(selected.TextTemplate);
    }

    [Fact]
    public void TerrainTaggedSelectionCanMatchTheTerrain()
    {
        var context = CreateContext(TravelDiaryFlavourCategory.QuietTexture, dayNumber: 7, beatIndex: 0, terrain: TrailTerrain.Mountains, preferredTags: ["mountains"]);

        var selected = TravelDiaryFlavourCatalog.Select(context, new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal(TrailTerrain.Mountains, selected.Terrain);
        Assert.Contains("mountains", selected.Tags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void WaterScarcityAndReliefRespectWaterContext()
    {
        var dryContext = CreateContext(TravelDiaryFlavourCategory.WaterScarcity, dayNumber: 2, beatIndex: 1, routeWaterSecure: false, currentCanteenCharges: 0, preferredTags: ["dry"]);
        var reliefContext = CreateContext(TravelDiaryFlavourCategory.WaterScarcity, dayNumber: 2, beatIndex: 1, routeWaterSecure: true, currentCanteenCharges: 2, preferredTags: ["relief"]);

        var drySelected = TravelDiaryFlavourCatalog.Select(dryContext, new HashSet<string>(StringComparer.Ordinal));
        var reliefSelected = TravelDiaryFlavourCatalog.Select(reliefContext, new HashSet<string>(StringComparer.Ordinal));

        Assert.Equal(TravelDiaryFlavourCategory.WaterScarcity, drySelected.Category);
        Assert.Equal(TravelDiaryFlavourCategory.WaterRelief, reliefSelected.Category);
    }

    [Fact]
    public void NarrowPoolFallsBackToBroaderTaggedEntriesBeforeRepeating()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal)
        {
            "diary.day-opening.mountains-1"
        };
        var context = CreateContext(TravelDiaryFlavourCategory.DayOpening, dayNumber: 9, beatIndex: 0, terrain: TrailTerrain.Mountains, preferredTags: ["mountains"]);

        var selected = TravelDiaryFlavourCatalog.Select(context, seen);

        Assert.NotEqual("diary.day-opening.mountains-1", selected.Id);
        Assert.Equal(TravelDiaryFlavourCategory.DayOpening, selected.Category);
    }

    [Fact]
    public void FoeEncounterIntroPoolRotatesAcrossTheFullCastBeforeRepeating()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var context = CreateContext(TravelDiaryFlavourCategory.FoeEncounterIntro, dayNumber: 6, beatIndex: 1, preferredTags: ["foe"]);

        var selectedIds = new List<string>();
        for (var i = 0; i < 16; i++)
        {
            selectedIds.Add(TravelDiaryFlavourCatalog.Select(context, seen).Id);
        }

        Assert.Equal(16, selectedIds.Distinct(StringComparer.Ordinal).Count());
        Assert.All(selectedIds, id => Assert.StartsWith("diary.foe.intro-", id, StringComparison.Ordinal));
    }

    [Fact]
    public void RepresentativeTenDayThirtyBeatJourneySelectsThirtyUniqueFlavours()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var contexts = BuildStressContexts();

        var selectedIds = new List<string>();

        foreach (var context in contexts)
        {
            selectedIds.Add(TravelDiaryFlavourCatalog.Select(context, seen).Id);
        }

        Assert.Equal(30, selectedIds.Count);
        Assert.Equal(30, selectedIds.Distinct(StringComparer.Ordinal).Count());
    }

    private static IReadOnlyList<TravelDiaryFlavourContext> BuildStressContexts()
    {
        var contexts = new List<TravelDiaryFlavourContext>
        {
            CreateContext(TravelDiaryFlavourCategory.DayOpening, 1, 0, terrain: TrailTerrain.OpenRange, preferredTags: ["opening", "open-range"]),
            CreateContext(TravelDiaryFlavourCategory.QuietTexture, 1, 1, terrain: TrailTerrain.Hills, preferredTags: ["quiet", "hills"]),
            CreateContext(TravelDiaryFlavourCategory.LuckyEvent, 1, 2, preferredTags: ["lucky", "coin"]),

            CreateContext(TravelDiaryFlavourCategory.UnluckyEvent, 2, 0, preferredTags: ["unlucky", "weather"]),
            CreateContext(TravelDiaryFlavourCategory.FoeEncounterIntro, 2, 1, preferredTags: ["foe", "rider"]),
            CreateContext(TravelDiaryFlavourCategory.ResourceScarcity, 2, 2, currentFood: 1, preferredTags: ["resource", "food"]),

            CreateContext(TravelDiaryFlavourCategory.WaterScarcity, 3, 0, routeWaterSecure: false, currentCanteenCharges: 0, preferredTags: ["water", "dry"]),
            CreateContext(TravelDiaryFlavourCategory.WaterRelief, 3, 1, routeWaterSecure: true, currentCanteenCharges: 2, preferredTags: ["water", "relief"]),
            CreateContext(TravelDiaryFlavourCategory.HorsePressure, 3, 2, hasHorse: true, currentHorseFeed: 1, preferredTags: ["horse", "pressure"]),

            CreateContext(TravelDiaryFlavourCategory.ChoiceOutcome, 4, 0, choiceId: "run", preferredTags: ["run"]),
            CreateContext(TravelDiaryFlavourCategory.ChoiceOutcome, 4, 1, choiceId: "fight", preferredTags: ["fight"]),
            CreateContext(TravelDiaryFlavourCategory.ChoiceOutcome, 4, 2, choiceId: "bribe", preferredTags: ["bribe"]),

            CreateContext(TravelDiaryFlavourCategory.ArrivalCompletion, 5, 0, preferredTags: ["arrival", "completion"]),
            CreateContext(TravelDiaryFlavourCategory.QuietTexture, 5, 1, terrain: TrailTerrain.Badlands, preferredTags: ["quiet", "badlands"]),
            CreateContext(TravelDiaryFlavourCategory.LuckyEvent, 5, 2, preferredTags: ["lucky", "water"]),

            CreateContext(TravelDiaryFlavourCategory.DayOpening, 6, 0, terrain: TrailTerrain.Mountains, preferredTags: ["opening", "mountains"]),
            CreateContext(TravelDiaryFlavourCategory.UnluckyEvent, 6, 1, preferredTags: ["unlucky", "dust"]),
            CreateContext(TravelDiaryFlavourCategory.FoeEncounterIntro, 6, 2, preferredTags: ["foe", "scout"]),

            CreateContext(TravelDiaryFlavourCategory.ResourceScarcity, 7, 0, currentFood: 0, preferredTags: ["resource", "horse-feed"]),
            CreateContext(TravelDiaryFlavourCategory.WaterScarcity, 7, 1, routeWaterSecure: false, currentCanteenCharges: 1, preferredTags: ["dry"]),
            CreateContext(TravelDiaryFlavourCategory.WaterRelief, 7, 2, routeWaterSecure: true, currentCanteenCharges: 3, preferredTags: ["relief"]),

            CreateContext(TravelDiaryFlavourCategory.HorsePressure, 8, 0, hasHorse: true, currentHorseFeed: 0, preferredTags: ["horse", "pressure"]),
            CreateContext(TravelDiaryFlavourCategory.ChoiceOutcome, 8, 1, choiceId: "run", preferredTags: ["run"]),
            CreateContext(TravelDiaryFlavourCategory.ChoiceOutcome, 8, 2, choiceId: "fight", preferredTags: ["fight"]),

            CreateContext(TravelDiaryFlavourCategory.ChoiceOutcome, 9, 0, choiceId: "bribe", preferredTags: ["bribe"]),
            CreateContext(TravelDiaryFlavourCategory.ArrivalCompletion, 9, 1, preferredTags: ["arrival", "completion"]),
            CreateContext(TravelDiaryFlavourCategory.QuietTexture, 9, 2, terrain: TrailTerrain.OpenRange, preferredTags: ["quiet", "open-range"]),

            CreateContext(TravelDiaryFlavourCategory.LuckyEvent, 10, 0, preferredTags: ["lucky", "trail"]),
            CreateContext(TravelDiaryFlavourCategory.UnluckyEvent, 10, 1, preferredTags: ["unlucky", "food"]),
            CreateContext(TravelDiaryFlavourCategory.ResourceScarcity, 10, 2, currentFood: 1, preferredTags: ["resource", "general"])
        };

        return contexts;
    }

    private static TravelDiaryFlavourContext CreateContext(
        TravelDiaryFlavourCategory category,
        int dayNumber,
        int beatIndex,
        bool hasHorse = true,
        bool routeWaterSecure = true,
        int currentFood = 2,
        int currentHorseFeed = 2,
        int currentCanteenCharges = 2,
        TrailTerrain terrain = TrailTerrain.OpenRange,
        WaterFeature waterFeature = WaterFeature.Creek,
        TravelMode travelMode = TravelMode.Mounted,
        string? choiceId = null,
        IReadOnlyCollection<string>? preferredTags = null)
        => new(
            category,
            JourneyKey: "test-journey",
            DayNumber: dayNumber,
            BeatIndex: beatIndex,
            Terrain: terrain,
            WaterFeature: waterFeature,
            TravelMode: travelMode,
            HasHorse: hasHorse,
            RouteWaterSecure: routeWaterSecure,
            CurrentFood: currentFood,
            CurrentHorseFeed: currentHorseFeed,
            CurrentCanteenCharges: currentCanteenCharges,
            CanteenChargesPerDay: routeWaterSecure ? 0 : 2,
            ChoiceId: choiceId,
            JourneyStatus: JourneyStatus.Active,
            PreferredTags: preferredTags);
}
