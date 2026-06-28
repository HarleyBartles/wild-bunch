using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Catalog of starting-world descriptors for deterministic travel tests.
/// Each entry describes a world state (variant, difficulty, entropy, loadout) and
/// the expected day-1 travel behavior when journeying along a specific route profile.
///
/// Tests derive UUIDs on the fly via <see cref="StartingWorldDescriptorResolver.CreateRepresentativeSeedCode"/>
/// rather than storing UUIDs. When the codec evolves, the same descriptor still resolves to a valid UUID.
///
/// See AGENTS.md "UUID Seed Codec" section for the full guidance.
/// </summary>
internal static class TravelTestSeedCatalog
{
    /// <summary>
    /// Canonical world, Normal difficulty, Standard entropy, mounted.
    /// Starts in Pinecross. Used as a baseline for mounted travel tests.
    /// Route: pinecross -> redmesa (Low/OpenRange/Creek, 4m).
    /// </summary>
    internal static readonly StartingWorldDescriptor CanonicalMountedNormal =
        StartingWorldDescriptorResolver.CreateCanonicalDescriptor(
            GameDifficulty.Standard,
            GameEntropy.Classic);

    /// <summary>
    /// Canonical world, Normal difficulty, Boring entropy, mounted.
    /// Encounters suppressed. Used for resource-mechanics and trail-event tests
    /// that need a quiet journey without heat priming.
    /// Route: pinecross -> hardpan (Low/Badlands/None, 3m) — dry resource pressure.
    /// </summary>
    internal static readonly StartingWorldDescriptor CanonicalMountedBoring =
        StartingWorldDescriptorResolver.CreateCanonicalDescriptor(
            GameDifficulty.Standard,
            GameEntropy.Boring);

    /// <summary>
    /// Canonical world, Normal difficulty, Boring entropy, no horse, light loadout.
    /// Encounters suppressed. Used for foot-travel resource tests.
    /// Route: pinecross -> hardpan (Low/Badlands/None, 3m) — dry resource pressure on foot.
    /// </summary>
    internal static StartingWorldDescriptor CanonicalFootBoringLight = new(
        Guid.Empty,
        GameDifficulty.Standard,
        GameEntropy.Boring,
        new StartingWorldDescriptorWorld(SeedWorldVariant.Canonical, GameSetupDeterministicLabels.WorldStartingTownFoot),
        new StartingWorldDescriptorPlayer(
            StartWithHorse: false,
            LoadoutProfile: StartingLoadoutProfile.Light,
            StartingCash: 18m,
            Loadout: new StartingWorldDescriptorLoadout(
                Food: 3,
                HorseFeed: 2,
                RevolverAmmo: 4,
                IncludeHorse: false,
                IncludeSaddle: false)),
        new StartingWorldDescriptorCase(AccusationIndex: 0));

    /// <summary>
    /// Canonical world, Easy difficulty, Standard entropy, mounted.
    /// Used for lucky trail-event tests (LuckyFoodCache, LuckyCoinCache).
    /// Routes from Pinecross: redmesa (Low/OpenRange/Creek), hardpan (Low/Badlands/None), openpass (Low/OpenRange/None).
    /// </summary>
    internal static readonly StartingWorldDescriptor CanonicalMountedEasyStandard =
        StartingWorldDescriptorResolver.CreateCanonicalDescriptor(
            GameDifficulty.Easy,
            GameEntropy.Classic);

    /// <summary>
    /// Canonical world, Hard difficulty, Standard entropy, mounted.
    /// Used for bad-luck trail-event tests (BadLuckSpookedHorse) and NPC encounters.
    /// Routes from Pinecross: redmesa (Low/OpenRange/Creek), hardpan (Low/Badlands/None), openpass (Low/OpenRange/None).
    /// </summary>
    internal static readonly StartingWorldDescriptor CanonicalMountedHardStandard =
        StartingWorldDescriptorResolver.CreateCanonicalDescriptor(
            GameDifficulty.Challenging,
            GameEntropy.Classic);

    /// <summary>
    /// Frontier world, Standard difficulty, Classic entropy, no horse, light loadout.
    /// Frontier variant makes pinecross->holloway Moderate/Hills/Spring.
    /// Route/setup guardrail for tests that need a moderate-risk foot journey shape.
    /// Foe-encounter determinism now comes from ForceDevTravelOverride, not from
    /// this seed profile. See BUNCH-87.
    /// Note: starting town is seed-derived; the guardrail test verifies a route matching
    /// the expected profile exists from wherever the session starts.
    /// </summary>
    internal static readonly StartingWorldDescriptor FrontierFootNormalFoe = new(
        Guid.Empty,
        GameDifficulty.Standard,
        GameEntropy.Classic,
        new StartingWorldDescriptorWorld(SeedWorldVariant.Frontier, GameSetupDeterministicLabels.WorldStartingTownFoot),
        new StartingWorldDescriptorPlayer(
            StartWithHorse: false,
            LoadoutProfile: StartingLoadoutProfile.Light,
            StartingCash: 18m,
            Loadout: new StartingWorldDescriptorLoadout(
                Food: 3,
                HorseFeed: 2,
                RevolverAmmo: 4,
                IncludeHorse: false,
                IncludeSaddle: false)),
        new StartingWorldDescriptorCase(AccusationIndex: 0));

    /// <summary>
    /// Frontier world, Hard difficulty, Standard entropy, mounted.
    /// Frontier variant makes holloway->sagewell Low/Hills/River.
    /// Used for NPC-encounter tests.
    /// </summary>
    internal static readonly StartingWorldDescriptor FrontierMountedHardNpc = new(
        Guid.Empty,
        GameDifficulty.Challenging,
        GameEntropy.Classic,
        new StartingWorldDescriptorWorld(SeedWorldVariant.Frontier, GameSetupDeterministicLabels.WorldStartingTownHorse),
        new StartingWorldDescriptorPlayer(
            StartWithHorse: true,
            LoadoutProfile: StartingLoadoutProfile.Standard,
            StartingCash: 20m,
            Loadout: new StartingWorldDescriptorLoadout(
                Food: 4,
                HorseFeed: 3,
                RevolverAmmo: 6,
                IncludeHorse: true,
                IncludeSaddle: true)),
        new StartingWorldDescriptorCase(AccusationIndex: 0));

    /// <summary>
    /// Frontier world, Normal difficulty, Standard entropy, mounted.
    /// Frontier variant makes redmesa->dryfork High/Badlands/None and pinecross->holloway Moderate/Hills/Spring.
    /// Used for high-risk trail-event tests (BadLuckSpookedHorse on High/Badlands/None).
    /// </summary>
    internal static readonly StartingWorldDescriptor FrontierMountedNormalHighRisk = new(
        Guid.Empty,
        GameDifficulty.Standard,
        GameEntropy.Classic,
        new StartingWorldDescriptorWorld(SeedWorldVariant.Frontier, GameSetupDeterministicLabels.WorldStartingTownHorse),
        new StartingWorldDescriptorPlayer(
            StartWithHorse: true,
            LoadoutProfile: StartingLoadoutProfile.Standard,
            StartingCash: 25m,
            Loadout: new StartingWorldDescriptorLoadout(
                Food: 4,
                HorseFeed: 3,
                RevolverAmmo: 6,
                IncludeHorse: true,
                IncludeSaddle: true)),
        new StartingWorldDescriptorCase(AccusationIndex: 0));

    /// <summary>
    /// Derives a UUID seed code from a descriptor. The descriptor's SeedCode field is ignored;
    /// a fresh UUID is found via round-trip search through the codec.
    /// </summary>
    internal static string ResolveSeedCode(StartingWorldDescriptor descriptor)
    {
        var seedCode = StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(descriptor);
        return StartingWorldDescriptorResolver.FormatSeedCode(seedCode);
    }

    /// <summary>
    /// Creates a game session from a descriptor by deriving a UUID and passing it through
    /// <see cref="SeededNewGameFactory"/>. This is the canonical way to start a test game.
    /// </summary>
    internal static GameSession CreateSession(StartingWorldDescriptor descriptor, string playerName = "Ranger Vale")
    {
        var seedCode = ResolveSeedCode(descriptor);
        var factory = new SeededNewGameFactory();
        return factory.Create(
            playerName,
            descriptor.Difficulty,
            seedCode,
            descriptor.Entropy);
    }

    /// <summary>
    /// Finds a trail from the session's current town matching the specified risk, terrain, and water profile.
    /// Throws if no matching trail exists so the test fails fast with a clear message.
    /// </summary>
    internal static Trail FindRouteFromCurrentTown(GameSession session, TrailRisk risk, TrailTerrain terrain, WaterFeature water)
    {
        var trail = session.World.Trails.FirstOrDefault(t =>
            (t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId) &&
            t.Risk == risk && t.Terrain == terrain && t.WaterFeature == water);

        if (trail is null)
        {
            var available = string.Join(", ", session.World.Trails
                .Where(t => t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId)
                .Select(t => $"{t.Risk}/{t.Terrain}/{t.WaterFeature}"));
            throw new InvalidOperationException(
                $"No route from {session.Player.CurrentTownId.Value} matching {risk}/{terrain}/{water}. " +
                $"Available: {available}");
        }

        return trail;
    }

    /// <summary>
    /// Resolves the destination town ID for a trail from the session's current town.
    /// </summary>
    internal static TownId ResolveDestination(GameSession session, Trail trail)
        => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId;
}
