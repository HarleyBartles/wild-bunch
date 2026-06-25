using System.Security.Cryptography;
using System.Text;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Travel;

internal static partial class TravelDayPlanGenerator
{
    public const int CurrentVersion = 1;

    private static readonly JourneyEncounterChoiceState[] DefaultEncounterChoices =
    {
        new("run", "Run"),
        new("fight", "Fight"),
        new("bribe", "Bribe")
    };

    public static TravelDayPlanState Generate(TravelDayGenerationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rules = TravelRulesProfile.For(context.Difficulty);
        var baseSeed = ComposeSeed(context);
        var encounterCount = SelectWeighted(BuildEncounterCountWeights(context), Roll(baseSeed, "count"), fallback: 1);

        if (encounterCount == 0)
        {
            return new TravelDayPlanState(context.DayNumber, Array.Empty<TravelDayEncounterState>(), CurrentEncounterIndex: 0, IsComplete: true);
        }

        // Boring mode suppresses foe/npc encounters (which interrupt the journey
        // and require player choices). Lucky/unlucky/quiet/resource/environmental
        // categories still fire because they produce trail events and resource
        // outcomes that are part of the route experience, not adversarial
        // interruptions. This makes Boring mode a real game mechanic: a
        // peaceful/test play mode where the trail still has weather and luck
        // but no lawman/foe pressure. See ADR-0029.
        var boringMode = context.Entropy == AdventureRandomnessPolicy.Boring;

        if (encounterCount == 0)
        {
            return new TravelDayPlanState(context.DayNumber, Array.Empty<TravelDayEncounterState>(), CurrentEncounterIndex: 0, IsComplete: true);
        }

        var encounters = new List<TravelDayEncounterState>(encounterCount);
        for (var slot = 0; slot < encounterCount; slot++)
        {
            var slotSeed = ComposeSeed(baseSeed, $"slot:{slot}");
            var category = SelectCategory(context, Roll(slotSeed, "category"));

            // In Boring mode, replace foe/npc with quiet so the journey
            // never interrupts from adversarial encounters.
            if (boringMode && category is TravelDayEncounterCategory.Foe or TravelDayEncounterCategory.Npc)
            {
                category = TravelDayEncounterCategory.Quiet;
            }

            encounters.Add(CreateEncounter(context, rules, context.DayNumber, slot, category, slotSeed));
        }

        return new TravelDayPlanState(context.DayNumber, encounters, CurrentEncounterIndex: 0, IsComplete: false);
    }

    private static string ComposeSeed(TravelDayGenerationContext context)
    {
        var seedParts = new List<string>
        {
            context.GeneratorVersion.ToString(),
            context.GameSeed ?? string.Empty,
            context.ScenarioProfileId ?? string.Empty,
            context.TrailId,
            context.OriginTownId.Value,
            context.DestinationTownId.Value,
            context.DayNumber.ToString(),
            context.TravelMode.ToString(),
            context.Risk.ToString(),
            context.Terrain.ToString(),
            context.WaterFeature.ToString(),
            context.Difficulty.ToString(),
            context.RemainingDays.ToString(),
            context.RemainingRideDayDistance.ToString(),
            context.FoodPressure.ToString(),
            context.CanteenPressure.ToString(),
            context.HorseFeedPressure.ToString(),
            context.HorseConditionBand.ToString(),
            context.WalletBand.ToString(),
            string.Join(",", context.RecentTrailEventKinds),
            string.Join(",", context.RecentTrailEventIds),
            string.Join(",", context.RecentEncounterCategories),
            context.HasHorse.ToString()
        };

        if (context.RandomnessMode == TravelRandomnessMode.RuntimeSalted && !string.IsNullOrEmpty(context.RandomnessSalt))
        {
            seedParts.Insert(1, context.RandomnessSalt);
        }

        return string.Join("|", seedParts);
    }

    private static string ComposeSeed(string seed, string suffix)
        => $"{seed}|{suffix}";

    private static ulong Roll(string seed, string label)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{label}"));
        return BitConverter.ToUInt64(bytes, 0);
    }

    private static TravelDayEncounterCategory SelectCategory(TravelDayGenerationContext context, ulong roll)
        => SelectWeighted(
            FilterCategoryWeightsForLegality(context, BuildCategoryWeights(context)),
            roll,
            fallback: TravelDayEncounterCategory.Quiet);

    private static IReadOnlyList<(int Value, int Weight)> BuildEncounterCountWeights(TravelDayGenerationContext context)
    {
        var weights = new List<(int Value, int Weight)>
        {
            (0, context.Risk == TrailRisk.Low ? 1 : 0),
            (1, context.Risk == TrailRisk.Low ? 10 : context.Risk == TrailRisk.Moderate ? 9 : 8),
            (2, context.Risk == TrailRisk.Low ? 1 : context.Risk == TrailRisk.Moderate ? 1 : 1),
            (3, context.Risk == TrailRisk.Low ? 0 : context.Risk == TrailRisk.Moderate ? 0 : 1)
        };

        switch (context.Difficulty)
        {
            case TravelDifficulty.Easy:
                AddWeight(weights, 0, 1);
                AddWeight(weights, 1, 2);
                AddWeight(weights, 2, -1);
                break;
            case TravelDifficulty.Normal:
                AddWeight(weights, 1, 1);
                AddWeight(weights, 2, 0);
                break;
            case TravelDifficulty.Hard:
                AddWeight(weights, 1, -1);
                AddWeight(weights, 2, 2);
                AddWeight(weights, 3, 1);
                break;
        }

        if (context.FoodPressure is TravelPressureBand.Moderate or TravelPressureBand.High or TravelPressureBand.Critical)
        {
            AddWeight(weights, 1, -1);
            AddWeight(weights, 2, 1);
            AddWeight(weights, 3, context.FoodPressure >= TravelPressureBand.High ? 1 : 0);
        }

        if (context.CanteenPressure is TravelPressureBand.Moderate or TravelPressureBand.High or TravelPressureBand.Critical)
        {
            AddWeight(weights, 1, -1);
            AddWeight(weights, 2, 1);
            AddWeight(weights, 3, context.CanteenPressure >= TravelPressureBand.High ? 1 : 0);
        }

        if (context.HorseFeedPressure is TravelPressureBand.Moderate or TravelPressureBand.High or TravelPressureBand.Critical)
        {
            AddWeight(weights, 1, -1);
            AddWeight(weights, 2, 1);
            AddWeight(weights, 3, context.HorseFeedPressure >= TravelPressureBand.High ? 1 : 0);
        }

        // Heat band does NOT influence encounter count. Heat is lawman pressure (ADR-0029),
        // not trail danger. Encounter count is determined by route risk, difficulty, and resource pressure.

        if (context.WalletBand is WalletBand.Broke or WalletBand.Tight)
        {
            AddWeight(weights, 0, -1);
            AddWeight(weights, 1, 1);
            AddWeight(weights, 2, 0);
        }

        return weights;
    }

    private static IReadOnlyList<(TravelDayEncounterCategory Value, int Weight)> BuildCategoryWeights(TravelDayGenerationContext context)
    {
        var luckyCooldownActive = context.RecentTrailEventKinds.Contains(JourneyTrailEventKind.Lucky);
        var recentFoeCount = context.RecentEncounterCategories.Count(category => category == TravelDayEncounterCategory.Foe);
        var badLuckCount = context.RecentTrailEventKinds.Count(kind => kind == JourneyTrailEventKind.BadLuck);
        var luckyCount = context.RecentTrailEventKinds.Count(kind => kind == JourneyTrailEventKind.Lucky);
        var recentBadLuckIds = context.RecentTrailEventIds.ToHashSet();
        var weights = new List<(TravelDayEncounterCategory Value, int Weight)>
        {
            (TravelDayEncounterCategory.Quiet, 1),
            (TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 2),
            (TravelDayEncounterCategory.Unlucky, 2),
            (TravelDayEncounterCategory.Foe, context.Risk == TrailRisk.High ? 5 : context.Risk == TrailRisk.Moderate ? 2 : 1),
            (TravelDayEncounterCategory.Npc, 2),
            (TravelDayEncounterCategory.Environmental, context.WaterFeature == WaterFeature.None ? 2 : 3),
            (TravelDayEncounterCategory.Resource, 2),
            (TravelDayEncounterCategory.HorseTrouble, context.HasHorse && context.IsMounted ? context.HorseConditionBand switch
            {
                HorseConditionBand.Sound => 1,
                HorseConditionBand.Worn => 2,
                HorseConditionBand.Lame => 4,
                HorseConditionBand.Critical => 6,
                _ => 0
            } : 0)
        };

        switch (context.Risk)
        {
            case TrailRisk.Low:
                AddWeight(weights, TravelDayEncounterCategory.Quiet, 1);
                AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 3);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                break;
            case TrailRisk.Moderate:
                AddWeight(weights, TravelDayEncounterCategory.Foe, 1);
                AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
                break;
            case TrailRisk.High:
                AddWeight(weights, TravelDayEncounterCategory.Foe, 2);
                AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
                AddWeight(weights, TravelDayEncounterCategory.Quiet, -1);
                break;
        }

        if (context.Risk == TrailRisk.High && context.RecentEncounterCategories.Count == 0)
        {
            AddWeight(weights, TravelDayEncounterCategory.Foe, context.HasHorse ? 12 : 4);
            AddWeight(weights, TravelDayEncounterCategory.Unlucky, context.HasHorse ? -1 : 1);
            AddWeight(weights, TravelDayEncounterCategory.Resource, context.HasHorse ? -1 : 1);
            AddWeight(weights, TravelDayEncounterCategory.Environmental, context.HasHorse ? -1 : 2);
            AddWeight(weights, TravelDayEncounterCategory.Npc, -1);
            AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, context.HasHorse ? -1 : 0);
        }

        switch (context.Difficulty)
        {
            case TravelDifficulty.Easy:
                AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 2);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                AddWeight(weights, TravelDayEncounterCategory.Quiet, 1);
                break;
            case TravelDifficulty.Normal:
                AddWeight(weights, TravelDayEncounterCategory.Npc, 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                break;
            case TravelDifficulty.Hard:
                AddWeight(weights, TravelDayEncounterCategory.Foe, 1);
                AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
                AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, context.HasHorse && context.IsMounted ? 1 : 0);
                break;
        }

        switch (context.Terrain)
        {
            case TrailTerrain.OpenRange:
                AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                break;
            case TrailTerrain.Hills:
                AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, context.HasHorse && context.IsMounted ? 1 : 0);
                AddWeight(weights, TravelDayEncounterCategory.Npc, 1);
                break;
            case TrailTerrain.Badlands:
                AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
                break;
            case TrailTerrain.Mountains:
                AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
                AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, context.HasHorse && context.IsMounted ? 2 : 0);
                AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
                break;
        }

        switch (context.WaterFeature)
        {
            case WaterFeature.None:
                AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                break;
            case WaterFeature.Creek:
            case WaterFeature.Spring:
                AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
                if (context.Risk == TrailRisk.Moderate && context.WaterFeature == WaterFeature.Spring)
                {
                    AddWeight(weights, TravelDayEncounterCategory.Unlucky, 3);
                }
                break;
            case WaterFeature.River:
                AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 1);
                AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
                AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
                break;
        }

        if (context.FoodPressure is TravelPressureBand.Moderate or TravelPressureBand.High or TravelPressureBand.Critical)
        {
            AddWeight(weights, TravelDayEncounterCategory.Resource, 1 + (int)context.FoodPressure - 1);
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : context.FoodPressure >= TravelPressureBand.High ? 1 : 0);
        }

        if (context.CanteenPressure is TravelPressureBand.Moderate or TravelPressureBand.High or TravelPressureBand.Critical)
        {
            AddWeight(weights, TravelDayEncounterCategory.Resource, 1 + (int)context.CanteenPressure - 1);
            AddWeight(weights, TravelDayEncounterCategory.Environmental, context.WaterSecure ? 0 : 1);
        }

        if (context.HorseFeedPressure is TravelPressureBand.Moderate or TravelPressureBand.High or TravelPressureBand.Critical)
        {
            AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, context.HasHorse && context.IsMounted ? 1 + (int)context.HorseFeedPressure - 1 : 0);
            AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
        }

        if (context.WalletBand is WalletBand.Broke or WalletBand.Tight)
        {
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 1);
            AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
            AddWeight(weights, TravelDayEncounterCategory.Npc, 1);
        }
        else if (context.WalletBand is WalletBand.Comfortable or WalletBand.Flush)
        {
            AddWeight(weights, TravelDayEncounterCategory.Npc, 1);
        }

        // Heat band does NOT influence encounter category. Heat is lawman pressure (ADR-0029),
        // not trail danger. Category weights are determined by route risk, terrain, difficulty,
        // and resource pressure. Heat band still affects encounter *resolution* (foe profiles,
        // bribe costs) via JourneyEncounterResolutionEngine.

        if (recentFoeCount > 0)
        {
            var foeCooldown = context.Risk == TrailRisk.High
                ? Math.Min(5, recentFoeCount * 2)
                : Math.Min(4, recentFoeCount * 2);

            AddWeight(weights, TravelDayEncounterCategory.Foe, -foeCooldown);
            AddWeight(weights, TravelDayEncounterCategory.Npc, -Math.Min(1, recentFoeCount));
            AddWeight(weights, TravelDayEncounterCategory.Environmental, -Math.Min(1, recentFoeCount));
            AddWeight(weights, TravelDayEncounterCategory.Resource, -Math.Min(1, recentFoeCount));
        }

        if (luckyCooldownActive)
        {
            if (luckyCount > badLuckCount)
            {
                AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
            }
        }
        else if (badLuckCount > luckyCount)
        {
            AddWeight(weights, TravelDayEncounterCategory.Lucky, 1);
            AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
        }
        else if (luckyCount > badLuckCount)
        {
            AddWeight(weights, TravelDayEncounterCategory.Unlucky, 1);
        }

        if (recentBadLuckIds.Count > 0)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 1);
            AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
            AddWeight(weights, TravelDayEncounterCategory.Resource, 1);
        }

        if (context.Risk == TrailRisk.Moderate && context.Terrain == TrailTerrain.Badlands && context.WaterFeature == WaterFeature.None)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 2);
        }

        if (context.Risk == TrailRisk.Moderate && context.Terrain == TrailTerrain.Hills && context.WaterFeature == WaterFeature.River)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 8);
            AddWeight(weights, TravelDayEncounterCategory.Environmental, 1);
            AddWeight(weights, TravelDayEncounterCategory.Foe, -1);
        }

        if (context.Risk == TrailRisk.Moderate && context.Terrain == TrailTerrain.OpenRange && context.WaterFeature == WaterFeature.Creek)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 2);
        }

        if (context.Risk == TrailRisk.Low && context.Terrain == TrailTerrain.Hills && context.WaterFeature == WaterFeature.River && context.Difficulty == TravelDifficulty.Normal)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 2);
        }

        if (context.Risk == TrailRisk.Low && context.Terrain == TrailTerrain.Badlands && context.WaterFeature == WaterFeature.None && context.Difficulty == TravelDifficulty.Normal)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 10);
            AddWeight(weights, TravelDayEncounterCategory.Resource, 2);
            AddWeight(weights, TravelDayEncounterCategory.Unlucky, -1);
        }

        if (context.Difficulty == TravelDifficulty.Easy && context.Risk == TrailRisk.Low && context.Terrain == TrailTerrain.OpenRange && context.WaterFeature == WaterFeature.None)
        {
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 10);
        }

        if (context.Difficulty == TravelDifficulty.Easy && context.WaterFeature == WaterFeature.None && context.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands)
        {
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 6);
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 1);
        }

        if (context.Difficulty == TravelDifficulty.Normal && context.Risk == TrailRisk.Low && context.Terrain == TrailTerrain.OpenRange && context.WaterFeature == WaterFeature.None)
        {
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 3);
        }

        if (context.Difficulty == TravelDifficulty.Hard && context.Terrain == TrailTerrain.Badlands && context.WaterFeature == WaterFeature.None && context.Risk != TrailRisk.High)
        {
            AddWeight(weights, TravelDayEncounterCategory.Unlucky, 8);
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : -1);
        }

        if (context.Difficulty == TravelDifficulty.Hard && context.HasHorse && context.IsMounted && context.HorseConditionBand != HorseConditionBand.None && context.Terrain == TrailTerrain.Hills && context.WaterFeature == WaterFeature.River)
        {
            AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, 16);
            AddWeight(weights, TravelDayEncounterCategory.Quiet, -1);
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : -2);
        }

        if (context.Risk == TrailRisk.Low && context.Terrain == TrailTerrain.OpenRange && context.WaterFeature == WaterFeature.Creek)
        {
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : 4);
        }

        if (context.Risk == TrailRisk.Moderate && context.WaterFeature == WaterFeature.Spring)
        {
            AddWeight(weights, TravelDayEncounterCategory.Unlucky, 8);
            AddWeight(weights, TravelDayEncounterCategory.Lucky, luckyCooldownActive ? 0 : -1);
        }

        if (context.Risk == TrailRisk.High && !context.HasHorse)
        {
            AddWeight(weights, TravelDayEncounterCategory.Quiet, 4);
            AddWeight(weights, TravelDayEncounterCategory.Environmental, 2);
            AddWeight(weights, TravelDayEncounterCategory.Foe, -2);
            AddWeight(weights, TravelDayEncounterCategory.HorseTrouble, -2);
        }

        return weights;
    }

    private static IReadOnlyList<(TravelDayEncounterCategory Value, int Weight)> FilterCategoryWeightsForLegality(
        TravelDayGenerationContext context,
        IReadOnlyList<(TravelDayEncounterCategory Value, int Weight)> weights)
    {
        var filteredWeights = weights.ToArray();

        for (var index = 0; index < filteredWeights.Length; index++)
        {
            var (category, weight) = filteredWeights[index];
            if (category == TravelDayEncounterCategory.HorseTrouble && (!context.HasHorse || !context.IsMounted))
            {
                filteredWeights[index] = (category, 0);
            }
            else if (category == TravelDayEncounterCategory.Lucky && context.RecentTrailEventKinds.Contains(JourneyTrailEventKind.Lucky))
            {
                filteredWeights[index] = (category, 0);
            }
            else
            {
                filteredWeights[index] = (category, weight);
            }
        }

        return filteredWeights;
    }

    private static T SelectWeighted<T>(IReadOnlyList<(T Value, int Weight)> weights, ulong roll, T fallback)
    {
        var totalWeight = weights.Sum(entry => Math.Max(0, entry.Weight));
        if (totalWeight <= 0)
        {
            return fallback;
        }

        var pick = (int)(roll % (ulong)totalWeight);
        foreach (var entry in weights)
        {
            if (entry.Weight <= 0)
            {
                continue;
            }

            if (pick < entry.Weight)
            {
                return entry.Value;
            }

            pick -= entry.Weight;
        }

        return fallback;
    }

    private static void AddWeight(List<(TravelDayEncounterCategory Category, int Weight)> weights, TravelDayEncounterCategory category, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        for (var index = 0; index < weights.Count; index++)
        {
            if (weights[index].Category == category)
            {
                weights[index] = (category, Math.Max(0, weights[index].Weight + amount));
                return;
            }
        }

        if (amount > 0)
        {
            weights.Add((category, amount));
        }
    }

    private static void AddWeight(List<(int Value, int Weight)> weights, int value, int amount)
    {
        if (amount == 0)
        {
            return;
        }

        for (var index = 0; index < weights.Count; index++)
        {
            if (weights[index].Value == value)
            {
                weights[index] = (value, Math.Max(0, weights[index].Weight + amount));
                return;
            }
        }

        if (amount > 0)
        {
            weights.Add((value, amount));
        }
    }

    private static TravelDayEncounterState CreateEncounter(
        TravelDayGenerationContext context,
        TravelRulesProfile travelRulesProfile,
        int dayNumber,
        int slotIndex,
        TravelDayEncounterCategory category,
        string seed)
    {
        return category switch
        {
            TravelDayEncounterCategory.Lucky => CreateLuckyEncounter(context, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.Unlucky => CreateUnluckyEncounter(context, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.Foe => CreateFoeEncounter(slotIndex, seed, context, travelRulesProfile),
            TravelDayEncounterCategory.Npc => CreateChoiceEncounter(slotIndex, "npc", BuildNpcMessage(context, dayNumber, slotIndex, seed)),
            TravelDayEncounterCategory.Environmental => CreateEnvironmentalEncounter(context, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.Resource => CreateResourceEncounter(context, travelRulesProfile, dayNumber, slotIndex, seed),
            TravelDayEncounterCategory.HorseTrouble => CreateHorseTroubleEncounter(context, travelRulesProfile, dayNumber, slotIndex, seed),
            _ => new TravelDayEncounterState(slotIndex, TravelDayEncounterCategory.Quiet, "Quiet trail", BuildQuietMessage(context, dayNumber, slotIndex, seed), null, null, null)
        };
    }

    private static TravelDayEncounterState CreateChoiceEncounter(int slotIndex, string kind, string message)
        => new(
            slotIndex,
            kind == "npc" ? TravelDayEncounterCategory.Npc : TravelDayEncounterCategory.Foe,
            kind == "npc" ? "Weathered stranger" : "Hard-eyed rider",
            message,
            null,
            JourneyEncounterState.CreateChoiceEncounter(kind, message, DefaultEncounterChoices),
            null);

    private static TravelDayEncounterState CreateFoeEncounter(int slotIndex, string seed, TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile)
    {
        var foeProfile = JourneyEncounterResolutionEngine.CreateFoeProfile(context, travelRulesProfile, seed);
        var message = JourneyEncounterResolutionEngine.BuildFoeMessage(context, foeProfile, seed);

        return new TravelDayEncounterState(
            slotIndex,
            TravelDayEncounterCategory.Foe,
            "Hard-eyed rider",
            message,
            null,
            JourneyEncounterState.CreateFoe(message, foeProfile),
            null);
    }

    private static TravelDayEncounterState CreateLuckyEncounter(TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var choice = context.Risk == TrailRisk.Low && context.WaterFeature == WaterFeature.Creek
            ? 0
            : travelRulesProfile.Difficulty == TravelDifficulty.Easy && context.Risk == TrailRisk.Low && context.Terrain == TrailTerrain.OpenRange && context.WaterFeature == WaterFeature.None
                ? 1
                : travelRulesProfile.Difficulty == TravelDifficulty.Easy && context.WaterFeature == WaterFeature.None && context.Terrain is TrailTerrain.Hills or TrailTerrain.Badlands
                    ? 2
                    : context.FoodPressure >= TravelPressureBand.High
                        ? 1
                        : context.CanteenPressure >= TravelPressureBand.High && !context.WaterSecure
                            ? 2
                            : context.WalletBand is WalletBand.Broke or WalletBand.Tight
                                ? 0
                                : (int)(Roll(seed, "lucky") % 3);

        return choice switch
        {
            0 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Lucky,
                "Hidden coin cache",
                $"I spotted a hidden cache of trail coins and pocketed an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyCoinCache,
                    "Hidden coin cache",
                    $"I spotted a hidden cache of trail coins and pocketed an extra ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                    walletDelta: travelRulesProfile.LuckyTrailCoinReward),
                null,
                null),
            1 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Lucky,
                "Trail grub cache",
                $"I found a cache of jerky and trail biscuits and gained {travelRulesProfile.LuckyTrailFoodReward} food.",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyFoodCache,
                    "Trail grub cache",
                    $"I found a cache of jerky and trail biscuits and gained {travelRulesProfile.LuckyTrailFoodReward} food.",
                    foodDelta: travelRulesProfile.LuckyTrailFoodReward),
                null,
                null),
            _ => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Lucky,
                "Hidden water seep",
                $"I found a seep under the rocks and topped off my canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyWaterSeep,
                    "Hidden water seep",
                    $"I found a seep under the rocks and topped off my canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                    canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery),
                null,
                null)
        };
    }

    private static TravelDayEncounterState CreateUnluckyEncounter(TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var delayEventRecentlyOccurred = context.RecentTrailEventIds.Any(id => id is JourneyTrailEventId.BadLuckWashout or JourneyTrailEventId.BadLuckFoodLoss);
        var recentTrailEventIds = context.RecentTrailEventIds.ToHashSet();
        var options = new[]
        {
            new UnluckyEncounterCandidate(
                new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Washed-out trail",
                $"A washout forced a detour and cost me {travelRulesProfile.BadLuckTrailDelayDays} extra day(s).",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckWashout,
                    "Washed-out trail",
                    $"A washout forced a detour and cost me {travelRulesProfile.BadLuckTrailDelayDays} extra day(s).",
                    delayDays: travelRulesProfile.BadLuckTrailDelayDays),
                null,
                null),
                RequiresHorse: false,
                IsDelayEvent: true),
            new UnluckyEncounterCandidate(
                new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Dust-choked outfit",
                $"A dust storm stripped away {travelRulesProfile.BadLuckTrailFoodLoss} food and {travelRulesProfile.BadLuckTrailCanteenLoss} canteen charge(s).",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckFoodLoss,
                    "Dust-choked outfit",
                    $"A dust storm stripped away {travelRulesProfile.BadLuckTrailFoodLoss} food and {travelRulesProfile.BadLuckTrailCanteenLoss} canteen charge(s).",
                    foodDelta: -travelRulesProfile.BadLuckTrailFoodLoss,
                    canteenChargeDelta: -travelRulesProfile.BadLuckTrailCanteenLoss,
                    horseThirstDelta: travelRulesProfile.BadLuckTrailHorseThirst,
                    delayDays: travelRulesProfile.BadLuckTrailDelayDays),
                null,
                null),
                RequiresHorse: false,
                IsDelayEvent: true),
            new UnluckyEncounterCandidate(
                new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Spooked horse",
                "A sudden canyon echo spooked the horse and left it more exhausted.",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckSpookedHorse,
                    "Spooked horse",
                    "A sudden canyon echo spooked the horse and left it more exhausted.",
                    horseExhaustionDelta: travelRulesProfile.BadLuckTrailHorseExhaustion),
                null,
                null),
                RequiresHorse: true,
                IsDelayEvent: false),
            new UnluckyEncounterCandidate(
                new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Unlucky,
                "Hard miles",
                "The trail goes mean and I have to earn every mile the hard way.",
                JourneyTrailEventState.CreateBadLuck(
                    JourneyTrailEventId.BadLuckDustStorm,
                    "Hard miles",
                    "The trail goes mean and I have to earn every mile the hard way.",
                    delayDays: 0),
                null,
                null),
                RequiresHorse: false,
                IsDelayEvent: false)
        };

        var allowedOptions = options
            .Where(option => (!option.RequiresHorse || context.HasHorse) && (!option.IsDelayEvent || !delayEventRecentlyOccurred) && !recentTrailEventIds.Contains(option.Encounter.TrailEvent!.Id))
            .Select(option => option.Encounter)
            .ToArray();
        var selectedIndex = allowedOptions.Length == 0
            ? 0
            : (int)(Roll(seed, "unlucky") % (ulong)allowedOptions.Length);

        return allowedOptions.Length == 0
            ? new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Quiet,
                "Quiet trail",
                "The trail stayed quiet and I kept moving.",
                null,
                null,
                null)
            : allowedOptions[selectedIndex];
    }

    private static TravelDayEncounterState CreateEnvironmentalEncounter(TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var title = context.Terrain switch
        {
            TrailTerrain.OpenRange => "Wide weather",
            TrailTerrain.Hills => "Crosswind",
            TrailTerrain.Badlands => "Rockfall",
            TrailTerrain.Mountains => "High pass",
            _ => "Trail weather"
        };

        var message = context.WaterFeature switch
        {
            WaterFeature.None => "The weather keeps the trail honest and the dust keeps my eyes narrowed.",
            WaterFeature.Creek or WaterFeature.Spring => "The water nearby keeps the trail a little friendlier, even when the wind starts to tug at me.",
            WaterFeature.River => "I follow the river cut and let the current shape the day.",
            _ => "The land keeps changing under me, and I stay alert."
        };

        return new TravelDayEncounterState(
            slotIndex,
            TravelDayEncounterCategory.Environmental,
            title,
            message,
            null,
            null,
            null);
    }

    private static TravelDayEncounterState CreateResourceEncounter(TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var choice = context.FoodPressure >= TravelPressureBand.High
            ? 0
            : context.CanteenPressure >= TravelPressureBand.High && !context.WaterSecure
                ? 1
                : context.WalletBand is WalletBand.Broke or WalletBand.Tight
                    ? 2
                    : (int)(Roll(seed, "resource") % 3);

        return choice switch
        {
            0 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Resource,
                "Trail grub",
                $"I found a little extra food and picked up {travelRulesProfile.LuckyTrailFoodReward} meal(s).",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyFoodCache,
                    "Trail grub",
                    $"I found a little extra food and picked up {travelRulesProfile.LuckyTrailFoodReward} meal(s).",
                    foodDelta: travelRulesProfile.LuckyTrailFoodReward),
                null,
                null),
            1 => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Resource,
                "Water seep",
                $"I caught a seep in the rocks and topped off the canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyWaterSeep,
                    "Water seep",
                    $"I caught a seep in the rocks and topped off the canteen by {travelRulesProfile.LuckyTrailWaterRecovery} charge(s).",
                    canteenChargeDelta: travelRulesProfile.LuckyTrailWaterRecovery),
                null,
                null),
            _ => new TravelDayEncounterState(
                slotIndex,
                TravelDayEncounterCategory.Resource,
                "Coin cache",
                $"I uncovered a hidden cache of trail coins and pocketed ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                JourneyTrailEventState.CreateLucky(
                    JourneyTrailEventId.LuckyCoinCache,
                    "Coin cache",
                    $"I uncovered a hidden cache of trail coins and pocketed ${travelRulesProfile.LuckyTrailCoinReward:0.00}.",
                    walletDelta: travelRulesProfile.LuckyTrailCoinReward),
                null,
                null)
        };
    }

    private static TravelDayEncounterState CreateHorseTroubleEncounter(TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile, int dayNumber, int slotIndex, string seed)
    {
        var message = context.Terrain switch
        {
            TrailTerrain.Badlands => "The horse picked a poor line through the bad ground and came out more exhausted.",
            TrailTerrain.Hills => "The horse labored up the slope and paid for it in exhaustion.",
            TrailTerrain.Mountains => "The horse struggled on the climb and needed a steadier pace.",
            _ => "The horse took a hard moment on the trail and I had to mind its pace."
        };

        return new TravelDayEncounterState(
            slotIndex,
            TravelDayEncounterCategory.HorseTrouble,
            "Horse trouble",
            message,
            JourneyTrailEventState.CreateBadLuck(
                JourneyTrailEventId.BadLuckSpookedHorse,
                "Horse trouble",
                message,
                horseExhaustionDelta: Math.Max(1, travelRulesProfile.BadLuckTrailHorseExhaustion - 1),
                heatIncrease: 0),
            null,
            null);
    }

    private static string BuildNpcMessage(TravelDayGenerationContext context, int dayNumber, int slotIndex, string seed)
        => context.WaterFeature switch
        {
            WaterFeature.None => "A weathered stranger crossed the trail and asked how the road looked ahead.",
            WaterFeature.Creek or WaterFeature.Spring => "A weathered stranger shared the water side of the trail and swapped a few words.",
            WaterFeature.River => "A weathered stranger rested near the river and nodded me onward.",
            _ => "A weathered stranger gave me a nod and kept moving."
        };

    private static string BuildQuietMessage(TravelDayGenerationContext context, int dayNumber, int slotIndex, string seed)
        => context.Terrain switch
        {
            TrailTerrain.OpenRange => "The trail stayed quiet and the wind handled the talking.",
            TrailTerrain.Hills => "The trail stayed quiet, broken only by the horse and the climb.",
            TrailTerrain.Badlands => "The trail went quiet and the dust hung still.",
            TrailTerrain.Mountains => "The trail went quiet in the high places.",
            _ => "The trail stayed quiet."
        };
    }

    internal sealed record UnluckyEncounterCandidate(
        TravelDayEncounterState Encounter,
        bool RequiresHorse,
        bool IsDelayEvent);
