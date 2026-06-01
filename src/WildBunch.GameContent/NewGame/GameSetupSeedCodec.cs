using System.Text;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class StartingWorldDescriptorResolver
{
    private const string SeedCodeFormat = "D";
    private static readonly Dictionary<StartingLoadoutProfile, (int Food, int HorseFeed, int RevolverAmmo)> LoadoutCounts = new()
    {
        [StartingLoadoutProfile.Standard] = (4, 3, 6),
        [StartingLoadoutProfile.Light] = (3, 2, 4),
        [StartingLoadoutProfile.Stocked] = (6, 4, 8)
    };

    public static Guid CreateCanonicalSeedCode(TravelDifficulty difficulty = TravelDifficulty.Normal)
        => CreateSeedCode(
            AdventureRandomnessPolicy.Standard,
            SeedWorldVariant.Canonical,
            StartingLoadoutProfile.Standard,
            startWithHorse: true,
            accusationIndex: 1,
            startingCashBonus: 0,
            difficulty);

    public static Guid GenerateRandomSeedCode()
        => Guid.NewGuid();

    internal static StartingWorldDescriptor CreateCanonicalDescriptor(TravelDifficulty difficulty = TravelDifficulty.Normal)
    {
        var startingCash = difficulty switch
        {
            TravelDifficulty.Easy => 30m,
            TravelDifficulty.Hard => 20m,
            _ => 25m
        };

        return new StartingWorldDescriptor(
            CreateCanonicalSeedCode(difficulty),
            difficulty,
            AdventureRandomnessPolicy.Standard,
            new StartingWorldDescriptorWorld(SeedWorldVariant.Canonical, GameSetupDeterministicLabels.WorldStartingTownHorse),
            new StartingWorldDescriptorPlayer(
                StartWithHorse: true,
                LoadoutProfile: StartingLoadoutProfile.Standard,
                StartingCash: startingCash,
                Loadout: new StartingWorldDescriptorLoadout(
                Food: 4,
                HorseFeed: 3,
                RevolverAmmo: 6,
                IncludeHorse: true,
                IncludeSaddle: true)),
            new StartingWorldDescriptorCase(1));
    }

    internal static StartingWorldDescriptor Resolve(string? seedCode, TravelDifficulty requestedDifficulty = TravelDifficulty.Normal)
    {
        if (string.IsNullOrWhiteSpace(seedCode))
        {
            return CreateCanonicalDescriptor(requestedDifficulty);
        }

        if (!TryParseSeedCode(seedCode, out var seed))
        {
            throw new ArgumentException("Seed code must be a UUID-shaped string.", nameof(seedCode));
        }

        return Resolve(seed);
    }

    internal static StartingWorldDescriptor Resolve(Guid seedCode)
    {
        var bytes = seedCode.ToByteArray();
        var policy = ResolveAdventureRandomnessPolicy(bytes[0]);
        var worldVariant = ResolveWorldVariant(bytes[1]);
        var loadoutProfile = ResolveLoadoutProfile(bytes[2]);
        var startWithHorse = (bytes[3] & 0x01) == 0;
        var startingTownSelectionKey = startWithHorse
            ? GameSetupDeterministicLabels.WorldStartingTownHorse
            : GameSetupDeterministicLabels.WorldStartingTownFoot;
        var difficulty = ResolveDifficulty(bytes[6]);
        var startingCash = ResolveStartingCash(difficulty, loadoutProfile, startWithHorse, policy, bytes[5]);
        var loadoutCounts = ResolveLoadoutCounts(loadoutProfile);
        var accusationIndex = bytes[4] % 7;

        return new StartingWorldDescriptor(
            seedCode,
            difficulty,
            policy,
            new StartingWorldDescriptorWorld(worldVariant, startingTownSelectionKey),
            new StartingWorldDescriptorPlayer(
                startWithHorse,
                loadoutProfile,
                startingCash,
                new StartingWorldDescriptorLoadout(
                    loadoutCounts.Food,
                    loadoutCounts.HorseFeed,
                    loadoutCounts.RevolverAmmo,
                    startWithHorse,
                    startWithHorse)),
            new StartingWorldDescriptorCase(accusationIndex));
    }

    internal static StartingWorldDescriptorValidationResult Validate(StartingWorldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!Enum.IsDefined(typeof(TravelDifficulty), descriptor.Difficulty))
        {
            return StartingWorldDescriptorValidationResult.Failed("Travel difficulty is invalid.");
        }

        if (!Enum.IsDefined(typeof(AdventureRandomnessPolicy), descriptor.AdventureRandomnessPolicy))
        {
            return StartingWorldDescriptorValidationResult.Failed("Adventure randomness policy is invalid.");
        }

        if (!Enum.IsDefined(typeof(SeedWorldVariant), descriptor.World.Variant))
        {
            return StartingWorldDescriptorValidationResult.Failed("World variant is invalid.");
        }

        var expectedTownSelectionKey = descriptor.Player.StartWithHorse
            ? GameSetupDeterministicLabels.WorldStartingTownHorse
            : GameSetupDeterministicLabels.WorldStartingTownFoot;

        if (descriptor.World.StartingTownSelectionKey is not (GameSetupDeterministicLabels.WorldStartingTownHorse or GameSetupDeterministicLabels.WorldStartingTownFoot)
            || descriptor.World.StartingTownSelectionKey != expectedTownSelectionKey)
        {
            return StartingWorldDescriptorValidationResult.Failed("Starting town selection key is invalid.");
        }

        if (!Enum.IsDefined(typeof(StartingLoadoutProfile), descriptor.Player.LoadoutProfile))
        {
            return StartingWorldDescriptorValidationResult.Failed("Loadout profile is invalid.");
        }

        var expectedLoadout = ResolveLoadoutCounts(descriptor.Player.LoadoutProfile);
        if (descriptor.Player.Loadout.Food != expectedLoadout.Food
            || descriptor.Player.Loadout.HorseFeed != expectedLoadout.HorseFeed
            || descriptor.Player.Loadout.RevolverAmmo != expectedLoadout.RevolverAmmo)
        {
            return StartingWorldDescriptorValidationResult.Failed("Loadout counts do not match the selected loadout profile.");
        }

        if (descriptor.Player.Loadout.IncludeHorse != descriptor.Player.StartWithHorse
            || descriptor.Player.Loadout.IncludeSaddle != descriptor.Player.StartWithHorse)
        {
            return StartingWorldDescriptorValidationResult.Failed("Horse and saddle flags must match the selected start-with-horse posture.");
        }

        if (descriptor.Player.StartingCash < 10m || descriptor.Player.StartingCash > 40m || descriptor.Player.StartingCash != decimal.Truncate(descriptor.Player.StartingCash))
        {
            return StartingWorldDescriptorValidationResult.Failed("Starting cash is outside the legal envelope.");
        }

        var baseCash = descriptor.Difficulty switch
        {
            TravelDifficulty.Easy => 28m,
            TravelDifficulty.Hard => 18m,
            _ => 23m
        };

        var profileBonus = descriptor.Player.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => -5m,
            StartingLoadoutProfile.Stocked => 5m,
            _ => 0m
        };

        var horseBonus = descriptor.Player.StartWithHorse ? 2m : 0m;
        var bonus = descriptor.Player.StartingCash - baseCash - profileBonus - horseBonus;
        var maxBonus = descriptor.AdventureRandomnessPolicy switch
        {
            AdventureRandomnessPolicy.Boring => 0m,
            AdventureRandomnessPolicy.Standard => 2m,
            AdventureRandomnessPolicy.Adventurous => 5m,
            AdventureRandomnessPolicy.Wild => 8m,
            _ => 0m
        };

        if (bonus < 0m || bonus > maxBonus || bonus != decimal.Truncate(bonus))
        {
            return StartingWorldDescriptorValidationResult.Failed("Starting cash does not fit the selected policy envelope.");
        }

        if (descriptor.Case.AccusationIndex is < 0 or > 6)
        {
            return StartingWorldDescriptorValidationResult.Failed("Accusation index is outside the legal envelope.");
        }

        return StartingWorldDescriptorValidationResult.Ok();
    }

    public static bool TryParseSeedCode(string? seedCode, out Guid seed)
    {
        seed = default;
        if (string.IsNullOrWhiteSpace(seedCode))
        {
            return false;
        }

        return Guid.TryParseExact(seedCode.Trim(), SeedCodeFormat, out seed);
    }

    public static string FormatSeedCode(Guid seedCode)
        => seedCode.ToString(SeedCodeFormat);

    internal static Guid CreateRepresentativeSeedCode(StartingWorldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var validation = Validate(descriptor);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Starting world descriptor is invalid.", nameof(descriptor));
        }

        var bytes = new byte[16];
        bytes[0] = (byte)descriptor.AdventureRandomnessPolicy;
        bytes[1] = (byte)descriptor.World.Variant;
        bytes[2] = (byte)descriptor.Player.LoadoutProfile;
        bytes[3] = descriptor.Player.StartWithHorse ? (byte)0 : (byte)1;
        bytes[4] = (byte)descriptor.Case.AccusationIndex;
        bytes[5] = EncodeStartingCashBonus(descriptor);
        bytes[6] = EncodeDifficulty(descriptor.Difficulty);
        return new Guid(bytes);
    }

    private static AdventureRandomnessPolicy ResolveAdventureRandomnessPolicy(byte seedByte)
        => (seedByte & 0x03) switch
        {
            0 => AdventureRandomnessPolicy.Boring,
            1 => AdventureRandomnessPolicy.Standard,
            2 => AdventureRandomnessPolicy.Adventurous,
            _ => AdventureRandomnessPolicy.Wild
        };

    private static SeedWorldVariant ResolveWorldVariant(byte seedByte)
        => (seedByte % 3) switch
        {
            0 => SeedWorldVariant.Canonical,
            1 => SeedWorldVariant.Frontier,
            _ => SeedWorldVariant.Rail
        };

    private static StartingLoadoutProfile ResolveLoadoutProfile(byte seedByte)
        => (seedByte % 3) switch
        {
            0 => StartingLoadoutProfile.Standard,
            1 => StartingLoadoutProfile.Light,
            _ => StartingLoadoutProfile.Stocked
        };

    private static (int Food, int HorseFeed, int RevolverAmmo) ResolveLoadoutCounts(StartingLoadoutProfile profile)
    {
        return profile switch
        {
            StartingLoadoutProfile.Light => (3, 2, 4),
            StartingLoadoutProfile.Stocked => (6, 4, 8),
            _ => (4, 3, 6)
        };
    }

    private static decimal ResolveStartingCash(
        TravelDifficulty difficulty,
        StartingLoadoutProfile loadoutProfile,
        bool startWithHorse,
        AdventureRandomnessPolicy policy,
        byte cashSeed)
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
        var policyBonus = policy switch
        {
            AdventureRandomnessPolicy.Boring => 0m,
            AdventureRandomnessPolicy.Standard => cashSeed % 3,
            AdventureRandomnessPolicy.Adventurous => cashSeed % 6,
            AdventureRandomnessPolicy.Wild => cashSeed % 9,
            _ => 0m
        };

        return Math.Max(10m, baseCash + profileBonus + horseBonus + policyBonus);
    }

    private static TravelDifficulty ResolveDifficulty(byte seedByte)
        => (seedByte & 0x03) switch
        {
            0 => TravelDifficulty.Easy,
            1 => TravelDifficulty.Normal,
            2 => TravelDifficulty.Hard,
            _ => TravelDifficulty.Normal
        };

    private static byte EncodeDifficulty(TravelDifficulty difficulty)
        => difficulty switch
        {
            TravelDifficulty.Easy => 0,
            TravelDifficulty.Normal => 1,
            TravelDifficulty.Hard => 2,
            _ => 1
        };

    private static Guid CreateSeedCode(
        AdventureRandomnessPolicy policy,
        SeedWorldVariant worldVariant,
        StartingLoadoutProfile loadoutProfile,
        bool startWithHorse,
        int accusationIndex,
        byte startingCashBonus,
        TravelDifficulty difficulty)
    {
        var bytes = new byte[16];
        bytes[0] = (byte)policy;
        bytes[1] = (byte)worldVariant;
        bytes[2] = (byte)loadoutProfile;
        bytes[3] = startWithHorse ? (byte)0 : (byte)1;
        bytes[4] = (byte)accusationIndex;
        bytes[5] = startingCashBonus;
        bytes[6] = EncodeDifficulty(difficulty);
        return new Guid(bytes);
    }

    private static byte EncodeStartingCashBonus(StartingWorldDescriptor descriptor)
    {
        var baseCash = descriptor.Difficulty switch
        {
            TravelDifficulty.Easy => 28m,
            TravelDifficulty.Hard => 18m,
            _ => 23m
        };

        var profileBonus = descriptor.Player.LoadoutProfile switch
        {
            StartingLoadoutProfile.Light => -5m,
            StartingLoadoutProfile.Stocked => 5m,
            _ => 0m
        };

        var horseBonus = descriptor.Player.StartWithHorse ? 2m : 0m;
        var bonus = descriptor.Player.StartingCash - baseCash - profileBonus - horseBonus;
        if (bonus < 0m || bonus > 8m || bonus != decimal.Truncate(bonus))
        {
            return 0;
        }

        return (byte)bonus;
    }
}
