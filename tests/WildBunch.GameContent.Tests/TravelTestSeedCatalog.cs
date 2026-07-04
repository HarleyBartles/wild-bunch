using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Catalog of seed worlds + difficulty/entropy for deterministic travel tests.
/// Each entry describes a world state (variant, difficulty, entropy) and the expected
/// day-1 travel behavior when journeying along a specific route profile.
///
/// Tests derive UUIDs on the fly via <see cref="SeedWorldResolver.CreateRepresentativeSeedCode"/>
/// rather than storing UUIDs. When the codec evolves, the same seed world still resolves to a valid UUID.
///
/// BUNCH-107 transitional note: horse/saddle/loadout variety was lost when these fields moved
/// from seed-owned to difficulty-owned. All entries now get horse+saddle+Standard loadout
/// (transitional defaults from DifficultyEnvelope). BUNCH-94 will restore variety via
/// difficulty-owned horse/saddle/loadout envelopes. Entries that previously specified no-horse
/// or light-loadout (CanonicalFootBoringLight, FrontierFootNormalFoe) now get the transitional
/// defaults — their difficulty/entropy/world-variant are preserved but the horse/loadout posture
/// is transitional.
///
/// Starting town is NOT seed-owned. All entries default to pinecross (the safe default from
/// StartingTownPolicy) unless a test explicitly passes a starting town override.
/// </summary>
internal static class TravelTestSeedCatalog
{
    /// <summary>
    /// Canonical world, Standard difficulty, Classic entropy, mounted.
    /// Starts in Pinecross (safe default). Used as a baseline for mounted travel tests.
    /// Route: pinecross -> redmesa (Low/OpenRange/Creek, 4m).
    /// </summary>
    internal static readonly SeedWorldEntry CanonicalMountedStandard = new(
        SeedWorldResolver.CreateCanonicalSeedWorld(),
        GameDifficulty.Standard,
        GameEntropy.Classic);

    /// <summary>
    /// Canonical world, Standard difficulty, Boring entropy, mounted.
    /// Encounters suppressed. Used for resource-mechanics and trail-event tests
    /// that need a quiet journey without heat priming.
    /// Route: pinecross -> hardpan (Low/Badlands/None, 3m) — dry resource pressure.
    /// </summary>
    internal static readonly SeedWorldEntry CanonicalMountedBoring = new(
        SeedWorldResolver.CreateCanonicalSeedWorld(),
        GameDifficulty.Standard,
        GameEntropy.Boring);

    /// <summary>
    /// Canonical world (all 8 towns), Standard difficulty, Boring entropy.
    /// Transitional: was no-horse/light-loadout, now gets transitional defaults
    /// (horse+saddle+Standard). Encounters suppressed. Used for foot-travel resource tests.
    /// BUNCH-94 will restore no-horse variety via difficulty-owned envelopes.
    /// </summary>
    internal static readonly SeedWorldEntry CanonicalFootBoringLight = new(
        CreateFullTownSeedWorld(SeedWorldVariant.Canonical, 0, 3, 0),
        GameDifficulty.Standard,
        GameEntropy.Boring);

    /// <summary>
    /// Canonical world, Easy difficulty, Standard entropy, mounted.
    /// Used for lucky trail-event tests (LuckyFoodCache, LuckyCoinCache).
    /// Routes from Pinecross: redmesa (Low/OpenRange/Creek), hardpan (Low/Badlands/None), openpass (Low/OpenRange/None).
    /// </summary>
    internal static readonly SeedWorldEntry CanonicalMountedEasyStandard = new(
        SeedWorldResolver.CreateCanonicalSeedWorld(),
        GameDifficulty.Easy,
        GameEntropy.Classic);

    /// <summary>
    /// Canonical world, Hard difficulty, Standard entropy, mounted.
    /// Used for bad-luck trail-event tests (BadLuckSpookedHorse) and NPC encounters.
    /// Routes from Pinecross: redmesa (Low/OpenRange/Creek), hardpan (Low/Badlands/None), openpass (Low/OpenRange/None).
    /// </summary>
    internal static readonly SeedWorldEntry CanonicalMountedHardStandard = new(
        SeedWorldResolver.CreateCanonicalSeedWorld(),
        GameDifficulty.Challenging,
        GameEntropy.Classic);

    /// <summary>
    /// Frontier world (all 8 towns), Standard difficulty, Classic entropy.
    /// Transitional: was no-horse/light-loadout, now gets transitional defaults
    /// (horse+saddle+Standard). Frontier variant makes pinecross->holloway Moderate/Hills/Spring.
    /// Foe-encounter determinism now comes from ForceDevTravelOverride, not from
    /// this seed profile. See BUNCH-87.
    /// </summary>
    internal static readonly SeedWorldEntry FrontierFootNormalFoe = new(
        CreateFullTownSeedWorld(SeedWorldVariant.Frontier, 0, 3, 0),
        GameDifficulty.Standard,
        GameEntropy.Classic);

    /// <summary>
    /// Frontier world, Hard difficulty, Standard entropy, mounted.
    /// Frontier variant makes holloway->sagewell Low/Hills/River.
    /// Used for NPC-encounter tests.
    /// </summary>
    internal static readonly SeedWorldEntry FrontierMountedHardNpc = new(
        CreateFullTownSeedWorld(SeedWorldVariant.Frontier, 0, 3, 0),
        GameDifficulty.Challenging,
        GameEntropy.Classic);

    /// <summary>
    /// Frontier world, Normal difficulty, Standard entropy, mounted.
    /// Frontier variant makes redmesa->dryfork High/Badlands/None and pinecross->holloway Moderate/Hills/Spring.
    /// Used for high-risk trail-event tests (BadLuckSpookedHorse on High/Badlands/None).
    /// </summary>
    internal static readonly SeedWorldEntry FrontierMountedNormalHighRisk = new(
        CreateFullTownSeedWorld(SeedWorldVariant.Frontier, 0, 3, 0),
        GameDifficulty.Standard,
        GameEntropy.Classic);

    /// <summary>
    /// Creates a SeedWorld with all 8 towns selected (full catalog) for the given
    /// variant and case fields. Used by travel test entries that need the full trail
    /// graph for specific route assertions.
    /// </summary>
    private static SeedWorld CreateFullTownSeedWorld(SeedWorldVariant variant, int accusationIndex, int defaultCulpritIndex, int cashBonus)
    {
        var townCount = 8;
        var prosperityPalette = ProsperityPalette.UniformProsperous;
        var servicesPalette = ServicesPalette.HubTelegraph;
        var clusterCount = 1;
        var graphDensity = GraphDensity.Sparse;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette);
        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = Array.Empty<SeedWorldTrail>();

        return new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            clusterCount,
            graphDensity,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails,
            OutlierSlotType: 0);
    }

    /// <summary>
    /// Derives a UUID seed code from a seed world. The seed world's SeedCode field is ignored;
    /// a fresh UUID is found via round-trip search through the codec.
    /// </summary>
    internal static string ResolveSeedCode(SeedWorldEntry entry)
    {
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(entry.SeedWorld);
        return SeedWorldResolver.FormatSeedCode(seedCode);
    }

    /// <summary>
    /// Creates a game session from a seed world entry by deriving a UUID and passing it through
    /// <see cref="SeededNewGameFactory"/>. This is the canonical way to start a test game.
    /// Uses a FixedSaltSourceFactory for deterministic trail distance salting.
    /// </summary>
    internal static GameSession CreateSession(SeedWorldEntry entry, string playerName = "Ranger Vale")
    {
        var seedCode = ResolveSeedCode(entry);
        var factory = new SeededNewGameFactory(new FixedSaltSourceFactory());
        return CanonicalStartFlow.StartGame(
            factory,
            playerName,
            entry.GameDifficulty,
            seedCode,
            entry.GameEntropy);
    }

    /// <summary>
    /// Creates a game session with Runtime salt (default factory) for tests that
    /// verify Runtime salt mode behavior. Most tests should use <see cref="CreateSession"/>
    /// instead for deterministic trail distance salting.
    /// </summary>
    internal static GameSession CreateSessionWithRuntimeSalt(SeedWorldEntry entry, string playerName = "Ranger Vale")
    {
        var seedCode = ResolveSeedCode(entry);
        var factory = new SeededNewGameFactory();
        return CanonicalStartFlow.StartGame(
            factory,
            playerName,
            entry.GameDifficulty,
            seedCode,
            entry.GameEntropy);
    }

    /// <summary>
    /// Creates a game session with an explicit starting town. The starting town
    /// is player-selected — it must exist in the generated world.
    /// </summary>
    internal static GameSession CreateSession(SeedWorldEntry entry, string startingTownId, string playerName = "Ranger Vale")
    {
        var seedCode = ResolveSeedCode(entry);
        var factory = new SeededNewGameFactory(new FixedSaltSourceFactory());
        return CanonicalStartFlow.StartGame(
            factory,
            playerName,
            entry.GameDifficulty,
            seedCode,
            entry.GameEntropy,
            startingTownId);
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

    /// <summary>
    /// Finds a town in the world that has a trail matching the specified risk,
    /// terrain, and water profile. Returns the town ID of one endpoint, or null
    /// if no such trail exists. Used to pick a starting town for tests that need
    /// a specific route profile.
    /// </summary>
    internal static TownId? FindTownWithRoute(World world, TrailRisk risk, TrailTerrain terrain, WaterFeature water)
    {
        var trail = world.Trails.FirstOrDefault(t =>
            t.Risk == risk && t.Terrain == terrain && t.WaterFeature == water);
        return trail?.FromTownId;
    }
}

/// <summary>
/// Test-only salt source factory that always produces a Fixed salt.
/// Ensures deterministic trail distance salting across test sessions.
/// </summary>
internal sealed class FixedSaltSourceFactory : ISaltSourceFactory
{
    public SaltSource Create(string? setupSeedCode, GameDifficulty gameDifficulty)
        => SaltSource.CreateFixed("test-fixed-salt");
}

/// <summary>
/// A seed world paired with player-selected difficulty and entropy.
/// Replaces the former StartingWorldDescriptor-based catalog entries.
/// </summary>
internal sealed record SeedWorldEntry(
    SeedWorld SeedWorld,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy);
