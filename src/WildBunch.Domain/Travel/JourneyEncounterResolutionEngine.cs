using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Inventory;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;

namespace WildBunch.Domain.Travel;

internal static class JourneyEncounterResolutionEngine
{
    internal sealed record JourneyEncounterResolutionPlan(
        bool Resolved,
        string Message,
        int HealthDelta,
        decimal WalletDelta,
        int AmmoSpent,
        int HeatIncrease,
        int HorseExhaustionDelta,
        bool ContinuedOnFoot,
        ItemKind? StolenItemKind,
        int StolenItemQuantity,
        JourneyEncounterState UpdatedEncounter)
    {
        public bool SessionChanged => true;
    }

    public static JourneyFoeProfile CreateFoeProfile(TravelDayGenerationContext context, TravelRulesProfile travelRulesProfile, string seed)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);

        var speed = 3
            + (context.Risk switch
            {
                TrailRisk.High => 3,
                TrailRisk.Moderate => 1,
                _ => 0
            })
            + (context.Terrain switch
            {
                TrailTerrain.Mountains => 2,
                TrailTerrain.Badlands => 1,
                _ => 0
            })
            + (context.TravelMode == TravelMode.Mounted ? 0 : 1)
            + (context.Difficulty switch
            {
                TravelDifficulty.Hard => 1,
                TravelDifficulty.Easy => -1,
                _ => 0
            });

        speed += (int)(Roll(seed, "speed") % 3);

        var fightStrength = 3
            + (context.Risk switch
            {
                TrailRisk.High => 3,
                TrailRisk.Moderate => 1,
                _ => 0
            })
            + (context.PursuitHeatBand switch
            {
                PursuitHeatBand.Hunted => 2,
                PursuitHeatBand.Hot => 1,
                _ => 0
            })
            + (context.Terrain switch
            {
                TrailTerrain.Badlands => 1,
                TrailTerrain.Mountains => 2,
                _ => 0
            })
            + (context.Difficulty switch
            {
                TravelDifficulty.Hard => 1,
                TravelDifficulty.Easy => -1,
                _ => 0
            });

        fightStrength += (int)(Roll(seed, "fight") % 3);

        var bribeBase = travelRulesProfile.EncounterBribeCash
            + (context.Risk switch
            {
                TrailRisk.High => 4m,
                TrailRisk.Moderate => 2m,
                _ => 0m
            })
            + (context.PursuitHeatBand switch
            {
                PursuitHeatBand.Hunted => 3m,
                PursuitHeatBand.Hot => 2m,
                _ => 0m
            });

        bribeBase += context.WalletBand switch
        {
            WalletBand.Flush => 2m,
            WalletBand.Comfortable => 1m,
            _ => 0m
        };

        bribeBase += (decimal)(Roll(seed, "bribe") % 4);

        return new JourneyFoeProfile(
            Speed: Math.Clamp(speed, 1, 10),
            FightStrength: Math.Clamp(fightStrength, 1, 10),
            MinimumBribe: Math.Max(1m, decimal.Round(bribeBase, 0, MidpointRounding.AwayFromZero)));
    }

    public static string BuildFoeMessage(TravelDayGenerationContext context, JourneyFoeProfile profile, string seed)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);

        var opening = context.Risk switch
        {
            TrailRisk.High => "A",
            TrailRisk.Moderate => "A wary",
            _ => "A rough"
        };

        return $"{opening} {profile.DescribeSpeedBand()}, {profile.DescribeFightBand()} rider with a {profile.DescribeBribeBand()} look cuts across my path.";
    }

    public static JourneyEncounterResolutionPlan ResolveRun(
        JourneyEncounterState encounter,
        TravelMode travelMode,
        HorseTravelState? horseState,
        int playerHealth,
        TravelRulesProfile travelRulesProfile,
        ulong roll)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        var foeProfile = GetFoeProfile(encounter, travelRulesProfile);
        var attempts = encounter.ResolutionAttempts + 1;
        var escapeBand = travelMode == TravelMode.Mounted && horseState is not null && !horseState.IsDeadFor(travelRulesProfile) && !horseState.IsLameFor(travelRulesProfile)
            ? 6 + Math.Max(0, 3 - horseState.Exhaustion)
            : 3 + Math.Max(0, playerHealth / 250);

        var chance = ClampChance(42 + (escapeBand - foeProfile.Speed) * 11 + Math.Min(16, (attempts - 1) * 5));
        var success = RollPercent(roll) < chance;

        if (success)
        {
            var healthDelta = travelMode == TravelMode.Foot
                ? -Math.Max(1, travelRulesProfile.EncounterRunFootHealthLoss - Math.Max(0, escapeBand - foeProfile.Speed))
                : 0;

            var horseExhaustionDelta = travelMode == TravelMode.Mounted
                ? Math.Max(1, travelRulesProfile.EncounterRunMountedHorseExhaustion)
                : 0;

            return new JourneyEncounterResolutionPlan(
                true,
                travelMode == TravelMode.Foot ? "I ran for it and got away on foot." : "I spurred the horse and got away before the rider could close in.",
                healthDelta,
                0m,
                0,
                travelMode == TravelMode.Foot ? travelRulesProfile.EncounterRunFootHeatIncrease : travelRulesProfile.EncounterRunMountedHeatIncrease,
                horseExhaustionDelta,
                travelMode == TravelMode.Foot,
                null,
                0,
                encounter);
        }

        var failedHealthDelta = travelMode == TravelMode.Foot
            ? -Math.Max(travelRulesProfile.EncounterRunFootHealthLoss, travelRulesProfile.EncounterRunFootHealthLoss + foeProfile.Speed - escapeBand)
            : 0;

        var failedHorseExhaustionDelta = travelMode == TravelMode.Mounted
            ? Math.Max(1, travelRulesProfile.EncounterRunMountedHorseExhaustion + 1)
            : 0;

        return new JourneyEncounterResolutionPlan(
            false,
            travelMode == TravelMode.Foot
                ? "I tried to run on foot, but the rider kept me pinned to the trail."
                : "I tried to outrun the rider, but the horse still had to work for it.",
            failedHealthDelta,
            0m,
            0,
            travelMode == TravelMode.Foot ? travelRulesProfile.EncounterRunFootHeatIncrease + 1 : travelRulesProfile.EncounterRunMountedHeatIncrease + 1,
            failedHorseExhaustionDelta,
            travelMode == TravelMode.Foot,
            null,
            0,
            encounter.IncrementResolutionAttempts());
    }

    public static JourneyEncounterResolutionPlan ResolveFight(
        JourneyEncounterState encounter,
        int playerHealth,
        TravelRulesProfile travelRulesProfile,
        int availableAmmo,
        bool hasKnife,
        int? requestedBullets,
        ulong roll)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        var foeProfile = GetFoeProfile(encounter, travelRulesProfile);

        var bulletSpend = availableAmmo <= 0
            ? 0
            : requestedBullets is null
                ? 1
                : Math.Clamp(requestedBullets.Value, 1, Math.Min(6, availableAmmo));

        var usingFirearm = bulletSpend > 0;
        if (!usingFirearm && !hasKnife)
        {
            return new JourneyEncounterResolutionPlan(
                false,
                "You need a knife or firearm ammo to stand and fight.",
                0,
                0m,
                0,
                0,
                0,
                false,
                null,
                0,
                encounter);
        }

        var fightBand = Math.Max(1, playerHealth / 250) + (hasKnife ? 1 : 0) + (usingFirearm ? 2 + bulletSpend : 0);
        var chance = ClampChance(34 + (fightBand - foeProfile.FightStrength) * 9 + (usingFirearm ? bulletSpend * 4 : 0));
        var success = RollPercent(roll) < chance;

        var healthLoss = usingFirearm
            ? travelRulesProfile.EncounterFightAmmoHealthLoss
            : travelRulesProfile.EncounterFightUnarmedHealthLoss;
        healthLoss = success
            ? Math.Max(1, healthLoss - Math.Min(3, foeProfile.FightStrength / 2))
            : healthLoss + foeProfile.FightStrength;

        return new JourneyEncounterResolutionPlan(
            success,
            usingFirearm
                ? success
                    ? $"I spend {bulletSpend} round(s) and force the rider off the trail."
                    : $"I spend {bulletSpend} round(s), but the rider keeps coming."
                : success
                    ? "I fight with my knife and force the rider off the trail."
                    : "I fight with my knife, but the rider keeps the pressure on.",
            -healthLoss,
            0m,
            bulletSpend,
            travelRulesProfile.EncounterFightHeatIncrease + (success ? 0 : 1),
            0,
            false,
            null,
            0,
            success ? encounter : encounter.IncrementResolutionAttempts());
    }

    public static JourneyEncounterResolutionPlan ResolveBribe(
        JourneyEncounterState encounter,
        decimal playerCash,
        TravelRulesProfile travelRulesProfile,
        decimal? requestedBribe,
        int availableFood,
        int availableHorseFeed,
        int availableRevolverAmmo,
        int availableRifleAmmo,
        ulong roll)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);
        var foeProfile = GetFoeProfile(encounter, travelRulesProfile);

        var offer = requestedBribe ?? travelRulesProfile.EncounterBribeCash;
        if (offer < 0m)
        {
            offer = 0m;
        }

        if (offer > playerCash)
        {
            return new JourneyEncounterResolutionPlan(
                false,
                $"You need ${offer:0.00} to make that offer.",
                0,
                0m,
                0,
                0,
                0,
                false,
                null,
                0,
                encounter);
        }

        var attempts = encounter.ResolutionAttempts + 1;
        var chance = ClampChance(24 + (int)Math.Round((offer / Math.Max(1m, foeProfile.MinimumBribe)) * 52m, MidpointRounding.AwayFromZero) + Math.Min(12, (attempts - 1) * 4));
        var success = RollPercent(roll) < chance;
        if (success)
        {
            return new JourneyEncounterResolutionPlan(
                true,
                $"I bribe the rider with ${offer:0.00} and continue on.",
                0,
                -offer,
                0,
                0,
                0,
                false,
                null,
                0,
                encounter);
        }

        var insultThreshold = foeProfile.MinimumBribe * 0.35m;
        var retaliates = offer <= insultThreshold && RollPercent(Roll($"{roll}", "retaliate")) < 70;
        if (!retaliates)
        {
            return new JourneyEncounterResolutionPlan(
                false,
                offer <= insultThreshold
                    ? $"I offer ${offer:0.00}, but the rider is not impressed."
                    : $"I offer ${offer:0.00}, but the rider still wants more.",
                0,
                0m,
                0,
                1,
                0,
                false,
                null,
                0,
                encounter.IncrementResolutionAttempts());
        }

        var stolenItem = SelectTheftItem(availableFood, availableHorseFeed, availableRevolverAmmo, availableRifleAmmo);
        var healthDelta = -Math.Max(2, foeProfile.FightStrength * 2);
        var walletDelta = stolenItem is null
            ? -Math.Min(playerCash, Math.Max(1m, Math.Round(foeProfile.MinimumBribe / 2m, 0, MidpointRounding.AwayFromZero)))
            : 0m;

        return new JourneyEncounterResolutionPlan(
            false,
            $"I offer ${offer:0.00}, and the rider takes it as an insult.",
            healthDelta,
            walletDelta,
            0,
            1,
            0,
            false,
            stolenItem,
            stolenItem is null ? 0 : 1,
            encounter.IncrementResolutionAttempts());
    }

    private static JourneyFoeProfile GetFoeProfile(JourneyEncounterState encounter, TravelRulesProfile travelRulesProfile)
    {
        if (encounter.FoeProfile is not null)
        {
            return encounter.FoeProfile;
        }

        return new JourneyFoeProfile(
            Speed: 5,
            FightStrength: 5,
            MinimumBribe: Math.Max(1m, travelRulesProfile.EncounterBribeCash));
    }

    private static int ClampChance(int chance)
        => Math.Clamp(chance, 8, 92);

    private static int RollPercent(ulong roll)
        => (int)(roll % 100);

    internal static ulong Roll(string seed, string label)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{label}"));
        return BitConverter.ToUInt64(bytes, 0);
    }

    public static string ComposeRollSeed(
        JourneyEncounterState encounter,
        string choiceId,
        int attempts,
        string context)
        => string.Join(
            "|",
            encounter.Kind,
            encounter.Message,
            encounter.FoeProfile?.Speed ?? 0,
            encounter.FoeProfile?.FightStrength ?? 0,
            encounter.FoeProfile?.MinimumBribe ?? 0m,
            choiceId,
            attempts,
            context);

    private static ItemKind? SelectTheftItem(int availableFood, int availableHorseFeed, int availableRevolverAmmo, int availableRifleAmmo)
        => availableFood > 0
            ? ItemKind.Food
            : availableHorseFeed > 0
                ? ItemKind.HorseFeed
                : availableRevolverAmmo > 0
                    ? ItemKind.RevolverAmmo
                    : availableRifleAmmo > 0
                        ? ItemKind.RifleAmmo
                        : null;
}
