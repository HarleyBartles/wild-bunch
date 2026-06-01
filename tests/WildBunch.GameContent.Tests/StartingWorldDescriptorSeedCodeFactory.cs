using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

internal static class StartingWorldDescriptorSeedCodeFactory
{
    private const int SearchLimit = 131072;

    internal static Guid CreateSeedCode(byte policy, byte worldVariant, byte loadoutProfile, byte startWithHorse, byte accusationIndex, byte startingCashBonus, byte difficulty, ulong salt)
    {
        var descriptor = CreateDescriptor(policy, worldVariant, loadoutProfile, startWithHorse, accusationIndex, startingCashBonus, difficulty);
        return FindSeedCode(descriptor, salt);
    }

    internal static Guid FindSeedCode(StartingWorldDescriptor descriptor, ulong salt = 0)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var validation = StartingWorldDescriptorResolver.Validate(descriptor);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Starting world descriptor is invalid.", nameof(descriptor));
        }

        var descriptorSignature = StartingWorldDescriptorSeedMixer.CreateDescriptorSignature(descriptor);
        for (var attempt = 0; attempt < SearchLimit; attempt++)
        {
            var candidate = StartingWorldDescriptorSeedMixer.CreateCandidateSeed(descriptorSignature, salt, attempt);
            var resolved = StartingWorldDescriptorResolver.Resolve(candidate);
            if (descriptor with { SeedCode = resolved.SeedCode } == resolved)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not find a representative UUID-shaped seed for the requested descriptor.");
    }

    private static StartingWorldDescriptor CreateDescriptor(byte policy, byte worldVariant, byte loadoutProfile, byte startWithHorse, byte accusationIndex, byte startingCashBonus, byte difficulty)
    {
        var travelDifficulty = (TravelDifficulty)difficulty;
        var adventurePolicy = (AdventureRandomnessPolicy)policy;
        var world = (SeedWorldVariant)worldVariant;
        var loadout = (StartingLoadoutProfile)loadoutProfile;
        var mounted = startWithHorse == 0;
        var startingCash = ResolveStartingCash(travelDifficulty, loadout, mounted, adventurePolicy, startingCashBonus);
        var loadoutCounts = ResolveLoadoutCounts(loadout);
        var startingTownSelectionKey = mounted
            ? GameSetupDeterministicLabels.WorldStartingTownHorse
            : GameSetupDeterministicLabels.WorldStartingTownFoot;

        return new StartingWorldDescriptor(
            Guid.Empty,
            travelDifficulty,
            adventurePolicy,
            new StartingWorldDescriptorWorld(world, startingTownSelectionKey),
            new StartingWorldDescriptorPlayer(
                mounted,
                loadout,
                startingCash,
                new StartingWorldDescriptorLoadout(
                    loadoutCounts.Food,
                    loadoutCounts.HorseFeed,
                    loadoutCounts.RevolverAmmo,
                    mounted,
                    mounted)),
            new StartingWorldDescriptorCase(accusationIndex));
    }

    private static decimal ResolveStartingCash(
        TravelDifficulty difficulty,
        StartingLoadoutProfile loadoutProfile,
        bool startWithHorse,
        AdventureRandomnessPolicy policy,
        byte cashBonus)
    {
        var baseCash = difficulty switch
        {
            TravelDifficulty.Easy => 28m,
            TravelDifficulty.Hard => 18m,
            _ => 23m
        };

        var profileBonus = loadoutProfile switch
        {
            StartingLoadoutProfile.Light => -5m,
            StartingLoadoutProfile.Stocked => 5m,
            _ => 0m
        };

        var horseBonus = startWithHorse ? 2m : 0m;
        var maxPolicyBonus = policy switch
        {
            AdventureRandomnessPolicy.Boring => 0,
            AdventureRandomnessPolicy.Standard => 2,
            AdventureRandomnessPolicy.Adventurous => 5,
            AdventureRandomnessPolicy.Wild => 8,
            _ => 0
        };

        var policyBonus = maxPolicyBonus == 0 ? 0m : cashBonus % (maxPolicyBonus + 1);
        return baseCash + profileBonus + horseBonus + policyBonus;
    }

    private static (int Food, int HorseFeed, int RevolverAmmo) ResolveLoadoutCounts(StartingLoadoutProfile profile)
        => profile switch
        {
            StartingLoadoutProfile.Light => (3, 2, 4),
            StartingLoadoutProfile.Stocked => (6, 4, 8),
            _ => (4, 3, 6)
        };
}
