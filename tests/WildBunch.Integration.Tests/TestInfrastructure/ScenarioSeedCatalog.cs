using System.Collections.Generic;
using System.Linq;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Xunit;
using Xunit.Sdk;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal static class ScenarioSeedCatalog
{
    public static readonly ScenarioSeedFixture CanonicalMountedNormal = new(
        Name: "CanonicalMountedNormal",
        SeedCode: "WB1-N-01-000000000000-C438",
        TravelDifficulty: TravelDifficulty.Normal,
        AssertCreatedSessionContract: session =>
        {
            RequireEqual("CanonicalMountedNormal", "start-game.travelDifficulty", TravelDifficulty.Normal, session.TravelDifficulty);
            RequireEqual("CanonicalMountedNormal", "start-game.currentTownId", "pinecross", session.Player.CurrentTownId);
            RequireEqual("CanonicalMountedNormal", "start-game.health", 1000, session.Player.Health);
            RequireEqual("CanonicalMountedNormal", "start-game.wallet.cash", 25m, session.Inventory.Wallet.Cash);
            RequireEqual("CanonicalMountedNormal", "start-game.world.towns", 6, session.World.Towns.Count);
            RequireEqual("CanonicalMountedNormal", "start-game.world.trails", 7, session.World.Trails.Count);
            RequireEqual("CanonicalMountedNormal", "start-game.caseFile.openingLead", "A pale scar cuts across the left cheek.", session.CaseFile.OpeningLead);
            RequireEqual("CanonicalMountedNormal", "start-game.caseFile.killerReleaseState.isReleased", false, session.CaseFile.KillerReleaseState.IsReleased);
            RequireEqual("CanonicalMountedNormal", "start-game.caseFile.killerReleaseState.progress", 0, session.CaseFile.KillerReleaseState.Progress);
            RequireEqual("CanonicalMountedNormal", "start-game.caseFile.discoveredSuspects", 0, session.CaseFile.DiscoveredSuspects.Count);
            RequireEqual("CanonicalMountedNormal", "start-game.inventory.items.count", 8, session.Inventory.Items.Count);
            Require("CanonicalMountedNormal", "start-game.inventory.horseState", session.Inventory.HorseState is not null, "expected the player to start mounted.");
            Require("CanonicalMountedNormal", "start-game.capabilities.mountedTravelAvailable", session.Inventory.Capabilities.MountedTravelAvailable, "expected mounted travel to be available.");
            Require("CanonicalMountedNormal", "start-game.capabilities.gunfightCapable", session.Inventory.Capabilities.GunfightCapable, "expected gunfight capability to be available.");
            Require("CanonicalMountedNormal", "start-game.capabilities.rifleUsable", !session.Inventory.Capabilities.RifleUsable, "expected rifles to stay unusable at start.");
            Require("CanonicalMountedNormal", "start-game.logEntries", session.LogEntries.Count > 0, "expected the new game log to be populated.");
        },
        AssertTravelPreviewContract: (session, destinationTownId, preview) =>
        {
            RequireEqual("CanonicalMountedNormal", "travel-preview.success", true, preview.Success);
            RequireEqual("CanonicalMountedNormal", "travel-preview.destinationTownId", destinationTownId, preview.Preview?.DestinationTownId);
            RequireEqual("CanonicalMountedNormal", "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
            RequireEqual("CanonicalMountedNormal", "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
            RequireEqual("CanonicalMountedNormal", "travel-preview.baselineRideDays", 2, preview.Preview?.BaselineRideDays);
            RequireEqual("CanonicalMountedNormal", "travel-preview.expectedDays", 2, preview.Preview?.ExpectedDays);
            RequireEqual("CanonicalMountedNormal", "travel-preview.routeProfile.risk", TrailRisk.Moderate, preview.Preview?.RouteProfile.Risk);
            RequireEqual("CanonicalMountedNormal", "travel-preview.routeProfile.terrain", TrailTerrain.OpenRange, preview.Preview?.RouteProfile.Terrain);
            RequireEqual("CanonicalMountedNormal", "travel-preview.routeProfile.waterFeature", WaterFeature.Creek, preview.Preview?.RouteProfile.WaterFeature);
        });

    public static readonly ScenarioSeedFixture NoHorseLightEasy = new(
        Name: "NoHorseLightEasy",
        SeedCode: "WB1-E-02-0000000004D2-9B4A",
        TravelDifficulty: TravelDifficulty.Easy,
        AssertCreatedSessionContract: session =>
        {
            RequireEqual("NoHorseLightEasy", "start-game.travelDifficulty", TravelDifficulty.Easy, session.TravelDifficulty);
            RequireEqual("NoHorseLightEasy", "start-game.health", 1250, session.Player.Health);
            RequireEqual("NoHorseLightEasy", "start-game.horseState", null, session.Inventory.HorseState);
            Require("NoHorseLightEasy", "start-game.inventory.noHorseItem", !session.Inventory.Items.Any(item => item.Kind == ItemKind.Horse), "expected the starting inventory to omit a horse.");
            Require("NoHorseLightEasy", "start-game.inventory.noSaddleItem", !session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle), "expected the starting inventory to omit a saddle.");
        },
        AssertTravelPreviewContract: (session, destinationTownId, preview) =>
        {
            RequireEqual("NoHorseLightEasy", "travel-preview.success", true, preview.Success);
            RequireEqual("NoHorseLightEasy", "travel-preview.destinationTownId", destinationTownId, preview.Preview?.DestinationTownId);
            RequireEqual("NoHorseLightEasy", "travel-preview.travelMode", TravelMode.Foot, preview.Preview?.TravelMode);
            RequireEqual("NoHorseLightEasy", "travel-preview.mountedTravelAvailable", false, preview.Preview?.MountedTravelAvailable);
            RequireEqual("NoHorseLightEasy", "travel-preview.requiredHorseFeed", 0, preview.Preview?.RequiredHorseFeed);
            Require("NoHorseLightEasy", "travel-preview.expectedDays", preview.Preview is not null && preview.Preview.ExpectedDays > preview.Preview.BaselineRideDays, "expected a longer foot route than the mounted baseline.");
        },
        AssertTravelTurnContract: (session, destinationTownId, preview, turn) =>
        {
            RequireEqual("NoHorseLightEasy", "travel-turn.success", true, turn.Success);
            RequireEqual("NoHorseLightEasy", "travel-turn.destinationTownId", destinationTownId, turn.CurrentSession.Journey?.DestinationTownId);
            RequireEqual("NoHorseLightEasy", "travel-turn.travelMode", TravelMode.Foot, turn.CurrentSession.Journey?.TravelMode);
            RequireEqual("NoHorseLightEasy", "travel-turn.baselineRideDays", preview.Preview?.BaselineRideDays, turn.CurrentSession.Journey?.BaselineRideDays);
            RequireEqual("NoHorseLightEasy", "travel-turn.expectedDays", preview.Preview?.ExpectedDays, turn.CurrentSession.Journey?.ExpectedDays);
            var travelDiary = turn.TravelDiary;
            Require("NoHorseLightEasy", "travel-turn.openingNarration", travelDiary is not null && travelDiary.Days.Count == 1, "expected a single opening travel day.");

            var openingNarration = travelDiary!.Days[0].OpeningNarration;
            Require("NoHorseLightEasy", "travel-turn.openingNarration", openingNarration is not null && openingNarration.Contains("on foot", StringComparison.OrdinalIgnoreCase), "expected the narration to describe foot travel.");
            Require("NoHorseLightEasy", "travel-turn.openingNarration", openingNarration is not null && openingNarration.Contains("without a horse", StringComparison.OrdinalIgnoreCase), "expected the narration to mention traveling without a horse.");
        });

    public static StartGameRequest CreateRequest(this ScenarioSeedFixture fixture, string playerName)
        => new(playerName, fixture.TravelDifficulty, fixture.SeedCode);

    public static void AssertCreatedSession(this ScenarioSeedFixture fixture, GameSessionDto session)
        => fixture.AssertCreatedSessionContract(session);

    public static void AssertTravelPreview(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, TravelPreviewResultDto preview)
    {
        if (fixture.AssertTravelPreviewContract is null)
        {
            throw new InvalidOperationException($"Scenario '{fixture.Name}' does not define a travel preview contract.");
        }

        fixture.AssertTravelPreviewContract(session, destinationTownId, preview);
    }

    public static void AssertTravelTurn(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, GameTurnResultDto turn, TravelPreviewResultDto preview)
    {
        if (fixture.AssertTravelTurnContract is null)
        {
            throw new InvalidOperationException($"Scenario '{fixture.Name}' does not define a travel turn contract.");
        }

        fixture.AssertTravelTurnContract(session, destinationTownId, preview, turn);
    }

    public sealed record ScenarioSeedFixture(
        string Name,
        string SeedCode,
        TravelDifficulty TravelDifficulty,
        Action<GameSessionDto> AssertCreatedSessionContract,
        Action<GameSessionDto, string, TravelPreviewResultDto>? AssertTravelPreviewContract = null,
        Action<GameSessionDto, string, TravelPreviewResultDto, GameTurnResultDto>? AssertTravelTurnContract = null);

    private static void Require(string scenarioName, string contractName, bool condition, string detail)
        => Assert.True(condition, $"Scenario '{scenarioName}' violated contract '{contractName}': {detail}");

    private static void RequireEqual<T>(string scenarioName, string contractName, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new XunitException($"Scenario '{scenarioName}' violated contract '{contractName}': expected '{expected}', got '{actual}'.");
        }
    }
}
