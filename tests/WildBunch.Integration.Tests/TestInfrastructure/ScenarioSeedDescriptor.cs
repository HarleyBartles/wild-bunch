using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal readonly record struct ScenarioSeedCodecVersion(string Value)
{
    public static ScenarioSeedCodecVersion Current => new(SeedWorldResolver.ResolverContractVersion);

    public override string ToString() => Value;
}

internal enum ScenarioStartingTownRole
{
    DefaultPlayableStart = 0
}

internal enum HorseCondition
{
    Absent = 0,
    Healthy = 1,
    Degraded = 2
}

internal enum SaddleState
{
    Absent = 0,
    Present = 1
}

internal sealed record ScenarioPreviewExpectation
{
    public TravelMode? TravelMode { get; init; }

    public int? BaselineRideDays { get; init; }

    public int? ExpectedDays { get; init; }

    public static ScenarioPreviewExpectation Missing() => new();

    public static ScenarioPreviewExpectation Mounted(int baselineRideDays, int expectedDays)
        => new()
        {
            TravelMode = global::WildBunch.Domain.Travel.TravelMode.Mounted,
            BaselineRideDays = baselineRideDays,
            ExpectedDays = expectedDays
        };

    public bool IsMissing => TravelMode is null
        && BaselineRideDays is null
        && ExpectedDays is null;
}

internal sealed record ScenarioSeedDescriptor
{
    public string ScenarioName { get; init; } = string.Empty;

    public ScenarioSeedCodecVersion CodecVersion { get; init; } = ScenarioSeedCodecVersion.Current;

    public GameEntropy? Entropy { get; init; }

    public GameDifficulty? Difficulty { get; init; }

    public ScenarioStartingTownRole? StartingTownRole { get; init; }

    public HorseCondition? HorseCondition { get; init; }

    public SaddleState? SaddleState { get; init; }

    public decimal? Wallet { get; init; }

    public int? ItemCount { get; init; }

    public int? TownCount { get; init; }

    public int? Health { get; init; }

    public TravelMode? TravelMode { get; init; }

    public int? RequiredConnectedTownCount { get; init; }

    public bool? ServicesOnStartingTown { get; init; }

    public ScenarioPreviewExpectation? Preview { get; init; }

    public static ScenarioSeedDescriptor Create(string scenarioName)
        => new()
        {
            ScenarioName = scenarioName
        };

    public ScenarioSeedDescriptor WithCodecVersion(ScenarioSeedCodecVersion codecVersion)
        => this with { CodecVersion = codecVersion };

    public ScenarioSeedDescriptor WithEntropy(GameEntropy entropy)
        => this with { Entropy = entropy };

    public ScenarioSeedDescriptor WithDifficulty(GameDifficulty difficulty)
        => this with { Difficulty = difficulty };

    public ScenarioSeedDescriptor WithStartingTownRole(ScenarioStartingTownRole role)
        => this with { StartingTownRole = role };

    public ScenarioSeedDescriptor WithHorse(HorseCondition horseCondition)
        => this with { HorseCondition = horseCondition };

    public ScenarioSeedDescriptor WithSaddle(SaddleState saddleState)
        => this with { SaddleState = saddleState };

    public ScenarioSeedDescriptor WithWallet(decimal wallet)
        => this with { Wallet = wallet };

    public ScenarioSeedDescriptor WithItemCount(int itemCount)
        => this with { ItemCount = itemCount };

    public ScenarioSeedDescriptor WithTownCount(int townCount)
        => this with { TownCount = townCount };

    public ScenarioSeedDescriptor WithHealth(int health)
        => this with { Health = health };

    public ScenarioSeedDescriptor WithTravelMode(TravelMode travelMode)
        => this with { TravelMode = travelMode };

    public ScenarioSeedDescriptor WithConnectedTownCount(int count)
        => this with { RequiredConnectedTownCount = count };

    public ScenarioSeedDescriptor WithServicesOnStartingTown()
        => this with { ServicesOnStartingTown = true };

    public ScenarioSeedDescriptor WithPreview(ScenarioPreviewExpectation preview)
        => this with { Preview = preview };

    public string FormatRequiredShapeSignature()
    {
        var parts = new List<string>
        {
            CodecVersion.Value,
            ScenarioName
        };

        if (Entropy is not null)
        {
            parts.Add($"entropy={Entropy}");
        }

        if (Difficulty is not null)
        {
            parts.Add($"difficulty={Difficulty}");
        }

        if (StartingTownRole is not null)
        {
            parts.Add($"start={FormatStartingTownRole(StartingTownRole.Value)}");
        }

        if (HorseCondition is not null)
        {
            parts.Add($"horse={HorseCondition.Value.ToString().ToLowerInvariant()}");
        }

        if (SaddleState is not null)
        {
            parts.Add($"saddle={SaddleState.Value.ToString().ToLowerInvariant()}");
        }

        if (Wallet is not null)
        {
            parts.Add($"wallet={Wallet.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (ItemCount is not null)
        {
            parts.Add($"items={ItemCount.Value}");
        }

        if (Health is not null)
        {
            parts.Add($"health={Health.Value}");
        }

        if (TownCount is not null)
        {
            parts.Add($"towns={TownCount.Value}");
        }

        if (TravelMode is not null)
        {
            parts.Add($"travel={TravelMode.Value.ToString().ToLowerInvariant()}");
        }

        if (RequiredConnectedTownCount is not null)
        {
            parts.Add($"routes=count={RequiredConnectedTownCount.Value}");
        }

        if (ServicesOnStartingTown is true)
        {
            parts.Add("services=starting-town");
        }

        if (Preview is not null)
        {
            parts.Add($"preview={FormatPreview(Preview)}");
        }

        return string.Join("|", parts);
    }

    private static string FormatStartingTownRole(ScenarioStartingTownRole role)
        => role switch
        {
            ScenarioStartingTownRole.DefaultPlayableStart => "default-playable-start",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown scenario starting-town role.")
        };

    private static string FormatPreview(ScenarioPreviewExpectation preview)
        => preview.IsMissing
            ? "missing"
            : $"{preview.TravelMode!.Value.ToString().ToLowerInvariant()}:{preview.BaselineRideDays!.Value}/{preview.ExpectedDays!.Value}";
}
