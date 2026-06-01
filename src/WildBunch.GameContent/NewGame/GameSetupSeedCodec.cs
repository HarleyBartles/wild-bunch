using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class StartingWorldDescriptorResolver
{
    private const string SeedCodeFormat = "D";
    private const int RepresentativeSeedSearchLimit = 131072;

    private static readonly Dictionary<StartingLoadoutProfile, (int Food, int HorseFeed, int RevolverAmmo)> LoadoutCounts = new()
    {
        [StartingLoadoutProfile.Standard] = (4, 3, 6),
        [StartingLoadoutProfile.Light] = (3, 2, 4),
        [StartingLoadoutProfile.Stocked] = (6, 4, 8)
    };

    private static readonly Lazy<Guid> CanonicalEasySeedCode = new(() => CreateCanonicalSeedCodeCore(TravelDifficulty.Easy), true);
    private static readonly Lazy<Guid> CanonicalNormalSeedCode = new(() => CreateCanonicalSeedCodeCore(TravelDifficulty.Normal), true);
    private static readonly Lazy<Guid> CanonicalHardSeedCode = new(() => CreateCanonicalSeedCodeCore(TravelDifficulty.Hard), true);

    public static Guid CreateCanonicalSeedCode(TravelDifficulty difficulty = TravelDifficulty.Normal)
        => GetCanonicalSeedCode(difficulty);

    public static Guid GenerateRandomSeedCode()
        => Guid.NewGuid();

    internal static StartingWorldDescriptor CreateCanonicalDescriptor(TravelDifficulty difficulty = TravelDifficulty.Normal)
        => CreateCanonicalDescriptorShape(difficulty) with
        {
            SeedCode = GetCanonicalSeedCode(difficulty)
        };

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
        var seedRoot = StartingWorldDescriptorSeedMixer.CreateSeedRoot(seedCode);
        var policy = ResolveAdventureRandomnessPolicy(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.AdventureRandomnessPolicy));
        var worldVariant = ResolveWorldVariant(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.WorldVariant));
        var loadoutProfile = ResolveLoadoutProfile(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerLoadoutProfile));
        var startWithHorse = ResolveStartWithHorse(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerHorsePosture));
        var difficulty = ResolveDifficulty(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.TravelDifficulty));
        var startingTownSelectionKey = startWithHorse
            ? GameSetupDeterministicLabels.WorldStartingTownHorse
            : GameSetupDeterministicLabels.WorldStartingTownFoot;
        var startingCash = ResolveStartingCash(
            difficulty,
            loadoutProfile,
            startWithHorse,
            policy,
            StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerCashBonus));
        var loadoutCounts = ResolveLoadoutCounts(loadoutProfile);
        var accusationIndex = ResolveAccusationIndex(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.CaseAccusationIndex));

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

        var baseCash = GetBaseStartingCash(descriptor.Difficulty);
        var profileBonus = GetLoadoutProfileBonus(descriptor.Player.LoadoutProfile);
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

    private static StartingWorldDescriptor CreateCanonicalDescriptorShape(TravelDifficulty difficulty)
    {
        var startingCash = difficulty switch
        {
            TravelDifficulty.Easy => 30m,
            TravelDifficulty.Hard => 20m,
            _ => 25m
        };

        return new StartingWorldDescriptor(
            Guid.Empty,
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

    private static Guid GetCanonicalSeedCode(TravelDifficulty difficulty)
        => difficulty switch
        {
            TravelDifficulty.Easy => CanonicalEasySeedCode.Value,
            TravelDifficulty.Hard => CanonicalHardSeedCode.Value,
            _ => CanonicalNormalSeedCode.Value
        };

    private static Guid CreateCanonicalSeedCodeCore(TravelDifficulty difficulty)
        => CreateRepresentativeSeedCode(CreateCanonicalDescriptorShape(difficulty));

    private static bool HasSameSemantics(StartingWorldDescriptor left, StartingWorldDescriptor right)
        => left.Difficulty == right.Difficulty
            && left.AdventureRandomnessPolicy == right.AdventureRandomnessPolicy
            && left.World == right.World
            && left.Player == right.Player
            && left.Case == right.Case;

    private static AdventureRandomnessPolicy ResolveAdventureRandomnessPolicy(ulong seedValue)
        => (seedValue % 4UL) switch
        {
            0 => AdventureRandomnessPolicy.Boring,
            1 => AdventureRandomnessPolicy.Standard,
            2 => AdventureRandomnessPolicy.Adventurous,
            _ => AdventureRandomnessPolicy.Wild
        };

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

    private static TravelDifficulty ResolveDifficulty(ulong seedValue)
        => (seedValue % 3UL) switch
        {
            0 => TravelDifficulty.Easy,
            1 => TravelDifficulty.Normal,
            _ => TravelDifficulty.Hard
        };

    private static (int Food, int HorseFeed, int RevolverAmmo) ResolveLoadoutCounts(StartingLoadoutProfile profile)
        => profile switch
        {
            StartingLoadoutProfile.Light => (3, 2, 4),
            StartingLoadoutProfile.Stocked => (6, 4, 8),
            _ => (4, 3, 6)
        };

    private static decimal ResolveStartingCash(
        TravelDifficulty difficulty,
        StartingLoadoutProfile loadoutProfile,
        bool startWithHorse,
        AdventureRandomnessPolicy policy,
        ulong cashSeed)
    {
        var baseCash = GetBaseStartingCash(difficulty);
        var profileBonus = GetLoadoutProfileBonus(loadoutProfile);
        var horseBonus = startWithHorse ? 2m : 0m;
        var maxPolicyBonus = policy switch
        {
            AdventureRandomnessPolicy.Boring => 0UL,
            AdventureRandomnessPolicy.Standard => 2UL,
            AdventureRandomnessPolicy.Adventurous => 5UL,
            AdventureRandomnessPolicy.Wild => 8UL,
            _ => 0UL
        };

        var policyBonus = (decimal)(cashSeed % (maxPolicyBonus + 1UL));
        return baseCash + profileBonus + horseBonus + policyBonus;
    }

    private static decimal GetBaseStartingCash(TravelDifficulty difficulty)
        => difficulty switch
        {
            TravelDifficulty.Easy => 28m,
            TravelDifficulty.Hard => 18m,
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
