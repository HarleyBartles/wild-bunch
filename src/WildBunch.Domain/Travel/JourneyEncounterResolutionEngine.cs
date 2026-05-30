using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Inventory;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;

namespace WildBunch.Domain.Travel;

internal static class JourneyEncounterResolutionEngine
{
    private const int MaxBribeOffers = 2;

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
        var hiddenState = encounter.HiddenState ?? new JourneyEncounterHiddenState();
        var escapeBand = travelMode == TravelMode.Mounted && horseState is not null && !horseState.IsDeadFor(travelRulesProfile) && !horseState.IsLameFor(travelRulesProfile)
            ? 6 + Math.Max(0, 3 - horseState.Exhaustion)
            : 3 + Math.Max(0, playerHealth / 250);

        var fatigueBonus = Math.Min(12, hiddenState.ChaseFatigue * 4);
        var annoyancePenalty = Math.Min(18, hiddenState.Annoyance * 6);
        var shakenBonus = hiddenState.Shaken ? 8 : 0;
        var chance = ClampChance(42 + (escapeBand - foeProfile.Speed) * 11 + fatigueBonus + shakenBonus - annoyancePenalty);
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
            encounter.WithHiddenState(hiddenState.RecordFailedRun()).IncrementResolutionAttempts());
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
        var hiddenState = encounter.HiddenState ?? new JourneyEncounterHiddenState();

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

        var fightBand = Math.Max(1, playerHealth / 250) + (hasKnife ? 1 : 0) + (usingFirearm ? 2 + bulletSpend * 2 : 0);
        var chance = ClampChance(30 + (fightBand - foeProfile.FightStrength) * 8 + (usingFirearm ? bulletSpend * 3 : 0) + (hiddenState.Shaken ? 6 : 0) - Math.Min(12, hiddenState.Annoyance * 4));
        var success = RollPercent(roll) < chance;

        var healthLoss = usingFirearm
            ? travelRulesProfile.EncounterFightAmmoHealthLoss
            : travelRulesProfile.EncounterFightUnarmedHealthLoss;
        healthLoss = success
            ? Math.Max(1, healthLoss - Math.Min(4, foeProfile.FightStrength / 2 + bulletSpend))
            : healthLoss + Math.Max(1, foeProfile.FightStrength - bulletSpend);

        var annoyedTheFoe = !usingFirearm || bulletSpend <= 2 || !success;
        var shookTheFoe = success && usingFirearm && bulletSpend >= 3;
        var updatedEncounter = encounter.WithHiddenState(hiddenState.RecordFightPressure(shookTheFoe, annoyedTheFoe));
        var message = usingFirearm
            ? success
                ? bulletSpend >= 4
                    ? $"I spent {bulletSpend} round(s) and drove the rider off with more lead than sense."
                    : $"I spent {bulletSpend} round(s) and forced the rider off the trail."
                : $"I spent {bulletSpend} round(s), but the rider kept coming."
            : success
                ? "I fought with my knife and forced the rider off the trail."
                : "I fought with my knife, but the rider kept the pressure on.";

        return new JourneyEncounterResolutionPlan(
            success,
            message,
            -healthLoss,
            0m,
            bulletSpend,
            travelRulesProfile.EncounterFightHeatIncrease + (success ? 0 : 1),
            0,
            false,
            null,
            0,
            success ? updatedEncounter : updatedEncounter.IncrementResolutionAttempts());
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
        var hiddenState = encounter.HiddenState ?? new JourneyEncounterHiddenState();

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

        if (hiddenState.BribeLockedOut || hiddenState.BribeOffersMade >= MaxBribeOffers)
        {
            return new JourneyEncounterResolutionPlan(
                false,
                "The rider will not take any more money.",
                0,
                0m,
                0,
                0,
                0,
                false,
                null,
                0,
                encounter.WithHiddenState(hiddenState with { BribeLockedOut = true }).WithoutChoice("bribe"));
        }

        var cumulativeBribePaid = hiddenState.CumulativeBribePaid + offer;
        var offersMade = hiddenState.BribeOffersMade + 1;
        var lockout = offersMade >= MaxBribeOffers && cumulativeBribePaid < foeProfile.MinimumBribe;
        var updatedHiddenState = hiddenState.RecordBribeOffer(cumulativeBribePaid, lockout);
        var updatedEncounter = encounter.WithHiddenState(updatedHiddenState);
        var success = cumulativeBribePaid >= foeProfile.MinimumBribe;
        if (success)
        {
            var overpay = cumulativeBribePaid - foeProfile.MinimumBribe;
            var message = overpay <= 0.5m
                ? $"I finally got the rider to take ${cumulativeBribePaid:0.00} and let me pass."
                : cumulativeBribePaid >= foeProfile.MinimumBribe * 1.5m
                    ? $"I paid ${cumulativeBribePaid:0.00}, and the rider grinned at how richly he was paid."
                    : $"I paid ${cumulativeBribePaid:0.00}, and the rider grudgingly let me by.";

            return new JourneyEncounterResolutionPlan(
                true,
                message,
                0,
                -offer,
                0,
                0,
                0,
                false,
                null,
                0,
                updatedEncounter);
        }

        var insultThreshold = foeProfile.MinimumBribe * 0.35m;
        var insulting = cumulativeBribePaid <= insultThreshold;
        var retaliates = insulting && RollPercent(Roll($"{roll}", "retaliate")) < (updatedHiddenState.Shaken ? 40 : 70);
        if (!retaliates)
        {
            return new JourneyEncounterResolutionPlan(
                false,
                $"I offered ${offer:0.00}, and the rider pocketed it without moving aside.",
                0,
                -offer,
                0,
                1,
                0,
                false,
                null,
                0,
                lockout
                    ? updatedEncounter.WithHiddenState(updatedHiddenState).WithoutChoice("bribe")
                    : updatedEncounter.WithHiddenState(updatedHiddenState));
        }

        var stolenItem = SelectTheftItem(availableFood, availableHorseFeed, availableRevolverAmmo, availableRifleAmmo);
        var healthDelta = -Math.Max(2, foeProfile.FightStrength * 2);
        var walletDelta = stolenItem is null
            ? -Math.Min(playerCash - offer, Math.Max(1m, Math.Round(foeProfile.MinimumBribe / 2m, 0, MidpointRounding.AwayFromZero)))
            : 0m;

        return new JourneyEncounterResolutionPlan(
            false,
            $"I offered ${offer:0.00}, and the rider took it badly.",
            healthDelta,
            walletDelta - offer,
            0,
            1,
            0,
            false,
            stolenItem,
            stolenItem is null ? 0 : 1,
            lockout
                ? updatedEncounter.WithHiddenState(updatedHiddenState).WithoutChoice("bribe")
                : updatedEncounter.WithHiddenState(updatedHiddenState));
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
            encounter.HiddenState?.BribeOffersMade ?? 0,
            encounter.HiddenState?.CumulativeBribePaid ?? 0m,
            encounter.HiddenState?.BribeLockedOut ?? false,
            encounter.HiddenState?.ChaseFatigue ?? 0,
            encounter.HiddenState?.Annoyance ?? 0,
            encounter.HiddenState?.Shaken ?? false,
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
