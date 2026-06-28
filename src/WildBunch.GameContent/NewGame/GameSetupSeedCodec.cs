using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class StartingWorldDescriptorResolver
{
    public const string ResolverContractVersion = "resolver-v2";
    private const string SeedCodeFormat = "D";
    private const int RepresentativeSeedSearchLimit = 131072;

    private static readonly Dictionary<StartingLoadoutProfile, (int Food, int HorseFeed, int RevolverAmmo)> LoadoutCounts = new()
    {
        [StartingLoadoutProfile.Standard] = (4, 3, 6),
        [StartingLoadoutProfile.Light] = (3, 2, 4),
        [StartingLoadoutProfile.Stocked] = (6, 4, 8)
    };

    private static readonly Lazy<Guid> CanonicalSeedCode = new(() => CreateCanonicalSeedCodeCore(), true);

    public static Guid CreateCanonicalSeedCode()
        => CanonicalSeedCode.Value;

    public static Guid GenerateRandomSeedCode()
        => Guid.NewGuid();

    public static StartingWorldDescriptor CreateCanonicalDescriptor(
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        GameEntropy gameEntropy = GameEntropy.Classic)
    {
        var descriptor = CreateCanonicalDescriptorShape(gameDifficulty, gameEntropy);

        return gameEntropy == GameEntropy.Classic
            ? descriptor with { SeedCode = CanonicalSeedCode.Value }
            : descriptor with { SeedCode = CreateRepresentativeSeedCode(descriptor) };
    }

    internal static StartingWorldDescriptor Resolve(
        string? seedCode,
        GameDifficulty requestedDifficulty = GameDifficulty.Standard,
        GameEntropy requestedEntropy = GameEntropy.Classic)
    {
        if (string.IsNullOrWhiteSpace(seedCode))
        {
            return CreateCanonicalDescriptor(requestedDifficulty, requestedEntropy);
        }

        if (!TryParseSeedCode(seedCode, out var seed))
        {
            throw new ArgumentException("Seed code must be a UUID-shaped string.", nameof(seedCode));
        }

        return Resolve(seed, requestedDifficulty, requestedEntropy);
    }

    public static StartingWorldDescriptor Resolve(Guid seedCode, GameDifficulty requestedDifficulty = GameDifficulty.Standard, GameEntropy requestedEntropy = GameEntropy.Classic)
    {
        var seedRoot = StartingWorldDescriptorSeedMixer.CreateSeedRoot(seedCode);
        var worldVariant = ResolveWorldVariant(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.WorldVariant));
        var loadoutProfile = ResolveLoadoutProfile(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerLoadoutProfile));
        var startWithHorse = ResolveStartWithHorse(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerHorsePosture));
        var startingTownSelectionKey = startWithHorse
            ? GameSetupDeterministicLabels.WorldStartingTownHorse
            : GameSetupDeterministicLabels.WorldStartingTownFoot;
        var startingCash = ResolveStartingCash(
            requestedDifficulty,
            loadoutProfile,
            startWithHorse,
            requestedEntropy,
            StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerCashBonus));
        var loadoutCounts = ResolveLoadoutCounts(loadoutProfile);
        var accusationIndex = ResolveAccusationIndex(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.CaseAccusationIndex));

        return new StartingWorldDescriptor(
            seedCode,
            requestedDifficulty,
            requestedEntropy,
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

        if (!Enum.IsDefined(typeof(GameDifficulty), descriptor.GameDifficulty))
        {
            return StartingWorldDescriptorValidationResult.Failed("Travel difficulty is invalid.");
        }

        if (!Enum.IsDefined(typeof(GameEntropy), descriptor.GameEntropy))
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

        var baseCash = GetBaseStartingCash(descriptor.GameDifficulty);
        var profileBonus = GetLoadoutProfileBonus(descriptor.Player.LoadoutProfile);
        var horseBonus = descriptor.Player.StartWithHorse ? 2m : 0m;
        var bonus = descriptor.Player.StartingCash - baseCash - profileBonus - horseBonus;
        var maxBonus = descriptor.GameEntropy switch
        {
            GameEntropy.Boring => 0m,
            GameEntropy.Classic => 2m,
            GameEntropy.Adventurous => 5m,
            GameEntropy.Wild => 8m,
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

    public static Guid CreateRepresentativeSeedCode(StartingWorldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var validation = Validate(descriptor);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Starting world descriptor is invalid.", nameof(descriptor));
        }

        var descriptorSignature = StartingWorldDescriptorSeedMixer.CreateDescriptorSignature(descriptor);
        for (var attempt = 0; attempt < RepresentativeSeedSearchLimit; attempt++)
        {
            var candidateSeed = StartingWorldDescriptorSeedMixer.CreateCandidateSeed(descriptorSignature, salt: 0, attempt);
            var resolvedDescriptor = Resolve(candidateSeed);
            if (HasSameSemantics(descriptor, resolvedDescriptor))
            {
                return candidateSeed;
            }
        }

        throw new InvalidOperationException("Could not derive a representative UUID-shaped seed for the requested starting-world descriptor.");
    }

    private static StartingWorldDescriptor CreateCanonicalDescriptorShape(
        GameDifficulty difficulty,
        GameEntropy entropy)
    {
        var startingCash = difficulty switch
        {
            GameDifficulty.Easy => 30m,
            GameDifficulty.Challenging => 20m,
            GameDifficulty.Brutal => 15m,
            _ => 25m
        };

        return new StartingWorldDescriptor(
            Guid.Empty,
            difficulty,
            entropy,
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

    private static Guid CreateCanonicalSeedCodeCore()
        => CreateRepresentativeSeedCode(CreateCanonicalDescriptorShape(GameDifficulty.Standard, GameEntropy.Classic));

    private static bool HasSameSemantics(StartingWorldDescriptor left, StartingWorldDescriptor right)
        => left.World == right.World
            && left.Player == right.Player
            && left.Case == right.Case;

    private static SeedWorldVariant ResolveWorldVariant(ulong seedValue)
        => (seedValue % 3UL) switch
        {
            0 => SeedWorldVariant.Canonical,
            1 => SeedWorldVariant.Frontier,
            _ => SeedWorldVariant.Rail
        };

    private static StartingLoadoutProfile ResolveLoadoutProfile(ulong seedValue)
        => (seedValue % 3UL) switch
        {
            0 => StartingLoadoutProfile.Standard,
            1 => StartingLoadoutProfile.Light,
            _ => StartingLoadoutProfile.Stocked
        };

    private static bool ResolveStartWithHorse(ulong seedValue)
        => (seedValue & 1UL) == 0UL;

    private static int ResolveAccusationIndex(ulong seedValue)
        => (int)(seedValue % 7UL);

    private static (int Food, int HorseFeed, int RevolverAmmo) ResolveLoadoutCounts(StartingLoadoutProfile profile)
        => profile switch
        {
            StartingLoadoutProfile.Light => (3, 2, 4),
            StartingLoadoutProfile.Stocked => (6, 4, 8),
            _ => (4, 3, 6)
        };

    private static decimal ResolveStartingCash(
        GameDifficulty difficulty,
        StartingLoadoutProfile loadoutProfile,
        bool startWithHorse,
        GameEntropy policy,
        ulong cashSeed)
    {
        var baseCash = GetBaseStartingCash(difficulty);
        var profileBonus = GetLoadoutProfileBonus(loadoutProfile);
        var horseBonus = startWithHorse ? 2m : 0m;
        var maxPolicyBonus = policy switch
        {
            GameEntropy.Boring => 0UL,
            GameEntropy.Classic => 2UL,
            GameEntropy.Adventurous => 5UL,
            GameEntropy.Wild => 8UL,
            _ => 0UL
        };

        var policyBonus = (decimal)(cashSeed % (maxPolicyBonus + 1UL));
        return baseCash + profileBonus + horseBonus + policyBonus;
    }

    private static decimal GetBaseStartingCash(GameDifficulty difficulty)
        => difficulty switch
        {
            GameDifficulty.Easy => 28m,
            GameDifficulty.Challenging => 18m,
            GameDifficulty.Brutal => 13m,
            _ => 23m
        };

    private static decimal GetLoadoutProfileBonus(StartingLoadoutProfile loadoutProfile)
        => loadoutProfile switch
        {
            StartingLoadoutProfile.Light => -5m,
            StartingLoadoutProfile.Stocked => 5m,
            _ => 0m
        };
}
