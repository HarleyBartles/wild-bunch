using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;

namespace WildBunch.Integration.Tests.TestInfrastructure;

/// <summary>
/// HTTP helper that drives the three-step game-start flow
/// (setup -> prologue-viewed -> start) and returns the fully-started session.
/// Replaces the old single-step POST /api/games direct-create route.
/// </summary>
internal static class ThreeStepGameSetup
{
    /// <summary>
    /// Creates a fully-started game session via the three-step flow.
    /// Uses the fixture's seed/difficulty/entropy and the provided player name.
    /// The starting town defaults to the fixture's default starting town (slot-0)
    /// unless <paramref name="startingTownId"/> is specified.
    /// </summary>
    public static async Task<GameSessionDto> CreateStartedGameAsync(
        this HttpClient client,
        ScenarioSeedFixture fixture,
        string playerName,
        string? startingTownId = null)
    {
        // Step 1: setup
        var setupResponse = await client.PostAsJsonAsync("/api/games/setup", new SetupGameRequest(
            playerName,
            fixture.GameDifficulty,
            fixture.SeedCode,
            fixture.GameEntropy));
        setupResponse.EnsureSuccessStatusCode();
        var setupSession = await setupResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        ArgumentNullException.ThrowIfNull(setupSession);

        // Step 2: mark prologue viewed
        var prologueResponse = await client.PostAsync(
            $"/api/games/{setupSession!.Id}/prologue-viewed", content: null);
        prologueResponse.EnsureSuccessStatusCode();

        // Step 3: start with town — use the starting town resolved during setup
        // (discovered dynamically from the session, not hardcoded in the fixture)
        var townId = startingTownId ?? setupSession.Player.CurrentTownId;
        var startResponse = await client.PostAsJsonAsync(
            $"/api/games/{setupSession.Id}/start",
            new StartGameWithTownRequest(townId));
        startResponse.EnsureSuccessStatusCode();
        var startedSession = await startResponse.Content.ReadFromJsonAsync<GameSessionDto>();
        ArgumentNullException.ThrowIfNull(startedSession);

        return startedSession!;
    }

    /// <summary>
    /// Creates a fully-started game session with default player name and fixture defaults.
    /// </summary>
    public static async Task<GameSessionDto> CreateStartedGameAsync(
        this HttpClient client,
        ScenarioSeedFixture fixture)
        => await client.CreateStartedGameAsync(fixture, "Ranger Vale");

    /// <summary>
    /// Creates a fully-started game session via the three-step flow using a
    /// <see cref="BoringScenario"/> (delegates to the fixture overload).
    /// </summary>
    public static async Task<GameSessionDto> CreateStartedGameAsync(
        this HttpClient client,
        BoringScenario scenario,
        string playerName,
        string? startingTownId = null)
        => await client.CreateStartedGameAsync(scenario.Fixture, playerName, startingTownId);

    /// <summary>
    /// Creates a fully-started game session with default player name from a
    /// <see cref="BoringScenario"/>.
    /// </summary>
    public static async Task<GameSessionDto> CreateStartedGameAsync(
        this HttpClient client,
        BoringScenario scenario)
        => await client.CreateStartedGameAsync(scenario.Fixture, "Ranger Vale");
}
