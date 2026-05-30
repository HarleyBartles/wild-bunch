using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.Travel;

public enum TravelDiaryFlavourCategory
{
    DayOpening = 0,
    QuietTexture = 1,
    LuckyEvent = 2,
    UnluckyEvent = 3,
    FoeEncounterIntro = 4,
    ResourceScarcity = 5,
    WaterScarcity = 6,
    WaterRelief = 7,
    HorsePressure = 8,
    ChoiceOutcome = 9,
    ArrivalCompletion = 10
}

public sealed record TravelDiaryFlavourEntry(
    string Id,
    TravelDiaryFlavourCategory Category,
    string TextTemplate,
    IReadOnlyList<string> Tags,
    TrailTerrain? Terrain = null,
    WaterFeature? WaterFeature = null,
    TravelMode? TravelMode = null,
    bool? RequiresRouteWaterSecure = null,
    bool? RequiresHorsePresent = null);

public sealed record TravelDiaryFlavourContext(
    TravelDiaryFlavourCategory Category,
    string JourneyKey,
    int DayNumber,
    int BeatIndex,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    TravelMode TravelMode,
    bool HasHorse,
    bool RouteWaterSecure,
    int CurrentFood,
    int CurrentHorseFeed,
    int CurrentCanteenCharges,
    int CanteenChargesPerDay,
    string? TrailEventId = null,
    string? EncounterKind = null,
    string? ChoiceId = null,
    JourneyStatus? JourneyStatus = null,
    IReadOnlyCollection<string>? PreferredTags = null);

public sealed record TravelDiaryFlavourSelection(
    TravelDiaryFlavourEntry Entry,
    string Text,
    IReadOnlyList<string> SelectedFlavourIds);

public static class TravelDiaryFlavourCatalog
{
    private static readonly TravelDiaryFlavourEntry[] Entries =
    [
        Entry("diary.day-opening.open-range-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with the open range spread out in front of me.", tags: ["opening", "open-range"], terrain: TrailTerrain.OpenRange),
        Entry("diary.day-opening.hills-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with the hills already asking for more of me.", tags: ["opening", "hills"], terrain: TrailTerrain.Hills),
        Entry("diary.day-opening.badlands-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with hard ground under me and badlands ahead.", tags: ["opening", "badlands"], terrain: TrailTerrain.Badlands),
        Entry("diary.day-opening.mountains-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with the mountain trail climbing out of sight.", tags: ["opening", "mountains"], terrain: TrailTerrain.Mountains),
        Entry("diary.day-opening.general-1", TravelDiaryFlavourCategory.DayOpening, "I start the day with a steady hand and a road that still has teeth.", tags: ["opening", "general"]),

        Entry("diary.terrain.open-range-1", TravelDiaryFlavourCategory.QuietTexture, "The open range keeps me honest, and the wind does most of the talking.", tags: ["quiet", "open-range"], terrain: TrailTerrain.OpenRange),
        Entry("diary.terrain.hills-1", TravelDiaryFlavourCategory.QuietTexture, "The hills make me earn every mile, but they do not lie to me.", tags: ["quiet", "hills"], terrain: TrailTerrain.Hills),
        Entry("diary.terrain.badlands-1", TravelDiaryFlavourCategory.QuietTexture, "The badlands are dry, plain, and not impressed by my mood.", tags: ["quiet", "badlands"], terrain: TrailTerrain.Badlands),
        Entry("diary.terrain.mountains-1", TravelDiaryFlavourCategory.QuietTexture, "The mountains keep their own counsel while I keep climbing.", tags: ["quiet", "mountains"], terrain: TrailTerrain.Mountains),

        Entry("diary.lucky.coin-cache-1", TravelDiaryFlavourCategory.LuckyEvent, "I find a little luck in the dust and tuck it away before the trail notices.", tags: ["lucky", "coin"]),
        Entry("diary.lucky.food-cache-1", TravelDiaryFlavourCategory.LuckyEvent, "I stumble onto a cache of trail grub and let myself grin about it.", tags: ["lucky", "food"]),
        Entry("diary.lucky-water-seep-1", TravelDiaryFlavourCategory.LuckyEvent, "I catch a hidden seep and feel the day loosen its grip on me.", tags: ["lucky", "water"]),
        Entry("diary.lucky-waypoint-1", TravelDiaryFlavourCategory.LuckyEvent, "I find the old track right where I needed it and save a hard detour.", tags: ["lucky", "trail"]),

        Entry("diary.unlucky.washout-1", TravelDiaryFlavourCategory.UnluckyEvent, "A washout makes me work for every inch, and I do not get to complain about it.", tags: ["unlucky", "weather"]),
        Entry("diary.unlucky.food-loss-1", TravelDiaryFlavourCategory.UnluckyEvent, "A rough patch costs me supplies, and the trail keeps its poker face.", tags: ["unlucky", "food"]),
        Entry("diary.unlucky.spooked-horse-1", TravelDiaryFlavourCategory.UnluckyEvent, "My horse jumps at the wrong noise, and I spend the rest of the day paying for it.", tags: ["unlucky", "horse"], requiresHorsePresent: true),
        Entry("diary.unlucky.dust-storm-1", TravelDiaryFlavourCategory.UnluckyEvent, "The dust storm rolls through like it has a grudge against me.", tags: ["unlucky", "dust"]),

        Entry("diary.foe.intro-rider-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A hard-eyed rider cuts across my path, and I keep my hand close.", tags: ["foe", "rider"]),
        Entry("diary.foe.intro-scout-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A wary scout blocks the trail, and I do not like the look of that pause.", tags: ["foe", "scout"]),
        Entry("diary.foe.intro-bandit-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A bandit shows himself early, which means the day just got meaner.", tags: ["foe", "bandit"]),
        Entry("diary.foe.intro-gunman-1", TravelDiaryFlavourCategory.FoeEncounterIntro, "A gunman rides in like he owns the horizon, and I do not share that opinion.", tags: ["foe", "gunman"]),

        Entry("diary.resources.low-food-1", TravelDiaryFlavourCategory.ResourceScarcity, "My food is getting thin, and I count every bite like it matters.", tags: ["resource", "food"]),
        Entry("diary.resources.low-food-2", TravelDiaryFlavourCategory.ResourceScarcity, "I am down to the kind of provisions that make a man think ahead.", tags: ["resource", "food"]),
        Entry("diary.resources.low-feed-1", TravelDiaryFlavourCategory.ResourceScarcity, "My horse feed is running low, so I keep a close eye on the next stop.", tags: ["resource", "horse-feed"], requiresHorsePresent: true),
        Entry("diary.resources.low-all-1", TravelDiaryFlavourCategory.ResourceScarcity, "The trail has me watching food, feed, and time like a gambler watches a table.", tags: ["resource", "general"]),

        Entry("diary.water.dry-canteen-1", TravelDiaryFlavourCategory.WaterScarcity, "My canteen is light, and the trail is not handing out mercy.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
        Entry("diary.water.dry-canteen-2", TravelDiaryFlavourCategory.WaterScarcity, "Every dry mile reminds me to keep the canteen close and my temper closer.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
        Entry("diary.water.dry-canteen-3", TravelDiaryFlavourCategory.WaterScarcity, "I am watching water the way a prospector watches a last gold speck.", tags: ["water", "dry"], requiresRouteWaterSecure: false),
        Entry("diary.water.dry-canteen-4", TravelDiaryFlavourCategory.WaterScarcity, "The dry trail keeps asking questions my canteen cannot answer for long.", tags: ["water", "dry"], requiresRouteWaterSecure: false),

        Entry("diary.water.relief-1", TravelDiaryFlavourCategory.WaterRelief, "I get to breathe easier when the water holds and the canteen stops feeling so loud.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
        Entry("diary.water.relief-2", TravelDiaryFlavourCategory.WaterRelief, "A good stretch of water makes the trail feel a little less personal.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
        Entry("diary.water.relief-3", TravelDiaryFlavourCategory.WaterRelief, "I find enough water to keep my head straight and my pace honest.", tags: ["water", "relief"], requiresRouteWaterSecure: true),
        Entry("diary.water.relief-4", TravelDiaryFlavourCategory.WaterRelief, "The route water is holding, and that alone buys me some peace.", tags: ["water", "relief"], requiresRouteWaterSecure: true),

        Entry("diary.horse.pressure-1", TravelDiaryFlavourCategory.HorsePressure, "My horse is feeling the miles, so I keep one eye on every step.", tags: ["horse", "pressure"], requiresHorsePresent: true),
        Entry("diary.horse.pressure-2", TravelDiaryFlavourCategory.HorsePressure, "The horse is working hard enough to earn my attention, and I give it freely.", tags: ["horse", "pressure"], requiresHorsePresent: true),
        Entry("diary.horse.pressure-3", TravelDiaryFlavourCategory.HorsePressure, "I can feel the horse start to wear down, and I do not like what that means for tomorrow.", tags: ["horse", "pressure"], requiresHorsePresent: true),
        Entry("diary.horse.pressure-4", TravelDiaryFlavourCategory.HorsePressure, "The horse takes the strain, and I make a note to treat it better than I was treated.", tags: ["horse", "pressure"], requiresHorsePresent: true),

        Entry("diary.choice.run-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I run for it and let the dust think it won.", tags: ["choice", "run"]),
        Entry("diary.choice.run-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I choose speed over pride and keep moving.", tags: ["choice", "run"]),
        Entry("diary.choice.fight-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I stand my ground and make the rider respect the trail.", tags: ["choice", "fight"]),
        Entry("diary.choice.fight-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I answer hard and leave no doubt that I am not backing down.", tags: ["choice", "fight"]),
        Entry("diary.choice.bribe-1", TravelDiaryFlavourCategory.ChoiceOutcome, "I pay my way through and let the problem ride off with my money.", tags: ["choice", "bribe"]),
        Entry("diary.choice.bribe-2", TravelDiaryFlavourCategory.ChoiceOutcome, "I settle the matter with cash and keep the trail moving.", tags: ["choice", "bribe"]),

        Entry("diary.arrival.completion-1", TravelDiaryFlavourCategory.ArrivalCompletion, "I reach town with dust on my boots and the trail behind me.", tags: ["arrival", "completion"]),
        Entry("diary.arrival.completion-2", TravelDiaryFlavourCategory.ArrivalCompletion, "I make it in with enough of the day left to call it a victory.", tags: ["arrival", "completion"]),
        Entry("diary.arrival.completion-3", TravelDiaryFlavourCategory.ArrivalCompletion, "I roll into town with the journey finally out of my hands.", tags: ["arrival", "completion"]),
        Entry("diary.arrival.completion-4", TravelDiaryFlavourCategory.ArrivalCompletion, "I finish the road and let the town lights take over from here.", tags: ["arrival", "completion"])
    ];

    public static IReadOnlyList<TravelDiaryFlavourEntry> All => Entries;

    public static TravelDiaryFlavourEntry Select(TravelDiaryFlavourContext context, ISet<string>? selectedFlavourIds = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        selectedFlavourIds ??= new HashSet<string>(StringComparer.Ordinal);
        var seenIds = new HashSet<string>(selectedFlavourIds, StringComparer.Ordinal);

        var effectiveCategory = ResolveCategory(context);
        var strictPool = Entries.Where(entry => MatchesStrict(entry, context, effectiveCategory)).ToArray();
        var broadPool = strictPool.Length == 0
            ? Entries.Where(entry => entry.Category == effectiveCategory).ToArray()
            : strictPool;

        var preferredPool = broadPool.Where(entry => !seenIds.Contains(entry.Id)).ToArray();
        var candidatePool = preferredPool.Length > 0 ? preferredPool : broadPool.Length > 0 ? broadPool : Entries;

        var selectedEntry = candidatePool
            .OrderByDescending(entry => PreferredTagMatchCount(entry, context))
            .ThenBy(entry => StableScore(context, entry.Id))
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .First();

        selectedFlavourIds.Add(selectedEntry.Id);
        return selectedEntry;
    }

    private static TravelDiaryFlavourCategory ResolveCategory(TravelDiaryFlavourContext context)
        => context.Category switch
        {
            TravelDiaryFlavourCategory.HorsePressure when !context.HasHorse => TravelDiaryFlavourCategory.QuietTexture,
            TravelDiaryFlavourCategory.WaterScarcity when context.RouteWaterSecure => TravelDiaryFlavourCategory.WaterRelief,
            _ => context.Category
        };

    private static bool MatchesStrict(TravelDiaryFlavourEntry entry, TravelDiaryFlavourContext context, TravelDiaryFlavourCategory effectiveCategory)
    {
        if (entry.Category != effectiveCategory)
        {
            return false;
        }

        if (entry.Terrain is not null && entry.Terrain != context.Terrain)
        {
            return false;
        }

        if (entry.WaterFeature is not null && entry.WaterFeature != context.WaterFeature)
        {
            return false;
        }

        if (entry.TravelMode is not null && entry.TravelMode != context.TravelMode)
        {
            return false;
        }

        if (entry.RequiresRouteWaterSecure is not null && entry.RequiresRouteWaterSecure != context.RouteWaterSecure)
        {
            return false;
        }

        if (entry.RequiresHorsePresent is true && !context.HasHorse)
        {
            return false;
        }

        if (entry.RequiresHorsePresent is false && context.HasHorse)
        {
            return false;
        }

        return true;
    }

    private static int PreferredTagMatchCount(TravelDiaryFlavourEntry entry, TravelDiaryFlavourContext context)
    {
        if (context.PreferredTags is null || context.PreferredTags.Count == 0)
        {
            return 0;
        }

        var matches = 0;
        foreach (var tag in context.PreferredTags)
        {
            if (entry.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                matches++;
            }
        }

        return matches;
    }

    private static ulong StableScore(TravelDiaryFlavourContext context, string entryId)
    {
        var key = string.Join('|',
            context.JourneyKey,
            context.Category,
            context.DayNumber,
            context.BeatIndex,
            context.Terrain,
            context.WaterFeature,
            context.TravelMode,
            context.HasHorse,
            context.RouteWaterSecure,
            context.CurrentFood,
            context.CurrentHorseFeed,
            context.CurrentCanteenCharges,
            context.CanteenChargesPerDay,
            context.TrailEventId ?? string.Empty,
            context.EncounterKind ?? string.Empty,
            context.ChoiceId ?? string.Empty,
            context.JourneyStatus?.ToString() ?? string.Empty,
            entryId);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToUInt64(hash, 0);
    }

    private static TravelDiaryFlavourEntry Entry(
        string id,
        TravelDiaryFlavourCategory category,
        string textTemplate,
        string[] tags,
        TrailTerrain? terrain = null,
        WaterFeature? waterFeature = null,
        TravelMode? travelMode = null,
        bool? requiresRouteWaterSecure = null,
        bool? requiresHorsePresent = null)
        => new(
            id,
            category,
            textTemplate,
            tags,
            terrain,
            waterFeature,
            travelMode,
            requiresRouteWaterSecure,
            requiresHorsePresent);
}
