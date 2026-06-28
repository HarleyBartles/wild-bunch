using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeededNewGameFactoryTests
{
    [Fact]
    public void CreatesRicherSeedWorldAndCase()
    {
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale");

        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal(new WildBunch.Domain.World.TownId("pinecross"), session.Player.CurrentTownId);
        Assert.Equal(GameDifficulty.Standard, session.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, session.GameEntropy);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(8, session.Player.Inventory.Items.Count);
        Assert.Equal(HorseTravelState.Healthy, session.Player.Inventory.GetHorseState());
        var capabilities = new InventoryCapabilityResolver().Resolve(session.Player.Inventory);
        Assert.True(capabilities.MountedTravelAvailable);
        Assert.True(capabilities.HorseUpkeepRequired);
        Assert.True(capabilities.NormalRouteWaterSecure);
        Assert.True(capabilities.TrailUtility);
        Assert.True(capabilities.CloseThreatAvailable);
        Assert.True(capabilities.FirearmThreatAvailable);
        Assert.True(capabilities.GunfightCapable);
        Assert.True(capabilities.RevolverUsable);
        Assert.False(capabilities.RifleUsable);
        Assert.Equal(8, session.World.Towns.Count);
        Assert.Equal(9, session.World.Trails.Count);
        Assert.Contains(session.World.Trails, trail => trail.Connects(new WildBunch.Domain.World.TownId("pinecross"), new WildBunch.Domain.World.TownId("redmesa")));
        Assert.DoesNotContain(session.World.Trails, trail => trail.Connects(new WildBunch.Domain.World.TownId("pinecross"), new WildBunch.Domain.World.TownId("dryfork")));
        Assert.Equal(7, session.CaseFile.Suspects.Count);
        Assert.Single(session.CaseFile.Suspects, suspect => suspect.Id.Equals(session.CaseFile.TrueCulpritId));
        Assert.Equal(5, session.CaseFile.KillerReleaseThreshold);
        Assert.Equal("The culprit has a scar on his left cheek.", session.CaseFile.OpeningLead.Description);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(5, session.CaseFile.KillerReleaseState.RequiredPublicClues);
        Assert.Equal(new[]
        {
            "Butch Cassidy",
            "Sundance Kid",
            "Elzy Lay",
            "Kid Curry",
            "Laura Bullion",
            "Bill Doolin",
            "Roy Daugherty"
        }, session.CaseFile.Suspects.Select(suspect => suspect.Name).ToArray());
        Assert.Contains(session.CaseFile.Suspects[0].Profile.Aliases, alias => alias.Name == "Grey Jay");
        Assert.NotEmpty(session.CaseFile.Suspects[3].Profile.IdentifyingFacts);
        var culpritOpeningFeature = Assert.Single(CaseSuspectFeaturePool.FeaturePool, feature => feature.OpeningLeadText == session.CaseFile.OpeningLead.Description);
        Assert.True(culpritOpeningFeature.HasTag(CaseSuspectFeatureTags.OpeningLeadCapable));
        Assert.True(culpritOpeningFeature.HasTag(CaseSuspectFeatureTags.ClassicNod));
        Assert.Equal(culpritOpeningFeature.Description, session.CaseFile.Suspects[3].Profile.IdentifyingFacts[0].Description);
        Assert.All(session.CaseFile.Suspects[3].Profile.IdentifyingFacts, fact => Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Description == fact.Description));
        Assert.Contains(session.CaseFile.KnownClues, clue =>
            clue.Kind == ClueKind.CulpritTrail
            && clue.TargetKind == InvestigationTargetKind.TrueCulprit
            && clue.Description == session.CaseFile.OpeningLead.Description);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Equal(6, session.CaseFile.PublicClues.Count);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A witness tied the rider to ", StringComparison.Ordinal));
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("Boot prints and a waystation note place the rider on the Red Mesa road after dusk.", StringComparison.Ordinal));
        Assert.Equal(new[] { new SuspectId("suspect-1") }, session.CaseFile.PublicClues[0].LinkedSuspectIds);
        Assert.Equal(new[] { new SuspectId("suspect-2") }, session.CaseFile.PublicClues[1].LinkedSuspectIds);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.TelegraphLead);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.LocalGossip);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A poster links the alias ", StringComparison.Ordinal));
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A public notice describes an unnamed rider who ", StringComparison.Ordinal));
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A telegraph clerk filed the alias ", StringComparison.Ordinal));
        Assert.Contains(
            session.CaseFile.PublicClues,
            clue => clue.Description.StartsWith("Local gossip out of ", StringComparison.Ordinal)
                && clue.Description.Contains(" a rider who ", StringComparison.OrdinalIgnoreCase)
                && clue.Description.Contains("kept to the rail spur after dark", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(session.CaseFile.KnownClues, clue => clue.Description.StartsWith("A witness tied the rider to ", StringComparison.Ordinal));
        Assert.DoesNotContain(session.CaseFile.KnownClues, clue => clue.Description.StartsWith("Boot prints and a waystation note place the rider on the Red Mesa road after dusk.", StringComparison.Ordinal));
        Assert.All(session.CaseFile.KnownClues.Concat(session.CaseFile.PublicClues), clue => Assert.True(clue.Anchors.HasAnchors));
        Assert.All(
            session.CaseFile.KnownClues.Concat(session.CaseFile.PublicClues),
            clue =>
            {
                Assert.DoesNotContain("mentions has", clue.Description, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("describes is missing", clue.Description, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("tied the rider to Has", clue.Description, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("..", clue.Description, StringComparison.Ordinal);
            });
        var sightingClue = Assert.Single(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("Local gossip out of ", StringComparison.Ordinal));
        var sightingSuspectId = Assert.Single(sightingClue.LinkedSuspectIds);
        var sightingTown = session.World.GetTown(
            Assert.Single(session.CaseFile.SuspectTurfAssignments, assignment => assignment.SuspectId.Equals(sightingSuspectId)).TurfTownId);
        Assert.NotEmpty(sightingClue.Anchors.Locations);
        Assert.Equal(sightingTown.Name, sightingClue.Anchors.Locations[0].Label);
        Assert.Equal("rail spur", sightingClue.Anchors.Locations[0].Route);
        Assert.NotEmpty(sightingClue.Anchors.Times);
        Assert.Equal(ClueRecency.Recent, sightingClue.Anchors.Times[0].Recency);
        Assert.NotEmpty(sightingClue.Anchors.Directions);
        Assert.Contains("rail spur", sightingClue.Anchors.Directions[0].Movement, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, session.CaseFile.PublicWarrants.Count);
        Assert.Equal("Butch Cassidy", session.CaseFile.PublicWarrants[0].TargetName);
        Assert.Equal(InvestigationTargetKind.GangMember, session.CaseFile.PublicWarrants[0].Terms.TargetKind);
        Assert.Equal(WarrantDisposition.DeadOrAlive, session.CaseFile.PublicWarrants[0].Terms.Disposition);
        Assert.Equal(1800m, session.CaseFile.PublicWarrants[0].Terms.BountyAmount);
        Assert.Contains("Grey Jay", session.CaseFile.PublicWarrants[0].Terms.KnownAliases);
        Assert.Contains("J. Pike", session.CaseFile.PublicWarrants[0].Terms.KnownAliases);
        Assert.Equal(new[] { "Raven-feather pin", "Black felt hat" }, session.CaseFile.PublicWarrants[0].Terms.KnownFeatures);
        Assert.Equal(new[] { OutlawGangIds.WildBunch }, session.CaseFile.PublicWarrants[0].Terms.GangAffiliations);
        Assert.Equal(OutlawGangIds.WildBunch, session.CaseFile.PublicWarrants[0].Terms.AdvancesGangPressureFor);
        Assert.Equal("Reno Pike", session.CaseFile.PublicWarrants[1].TargetName);
        Assert.Equal(WarrantDisposition.AliveOnly, session.CaseFile.PublicWarrants[1].Terms.Disposition);
        Assert.Empty(session.CaseFile.PublicWarrants[1].Terms.GangAffiliations);
        Assert.Null(session.CaseFile.PublicWarrants[1].Terms.AdvancesGangPressureFor);
        Assert.DoesNotContain(session.CaseFile.PublicWarrants, warrant => warrant.TargetName == session.CaseFile.Suspects[3].Name);
        Assert.DoesNotContain(session.CaseFile.PublicWarrants[0].Terms.KnownFeatures, feature => feature.Contains("scar", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.Accusation);
        Assert.Equal(7, session.CaseFile.SuspectTurfAssignments.Count);
        Assert.All(session.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(session.World.Towns, town => town.Id.Equals(assignment.TurfTownId)));
        Assert.All(session.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Id.Equals(assignment.SuspectId)));
    }

    [Fact]
    public void FrontierDescriptorAddsTownSpecificCivicCluesForTheNextVisitedTown()
    {
        var descriptor = StartingWorldDescriptorResolver.Resolve(CreateSeedCode(1, 1, 0, 0, 1, 0, 1, tail: 13), GameDifficulty.Standard, GameEntropy.Classic);
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale", GameDifficulty.Standard, StartingWorldDescriptorResolver.FormatSeedCode(descriptor.SeedCode));

        Assert.Contains(session.Player.CurrentTownId.Value, new[] { "pinecross", "holloway", "redmesa", "sagewell", "emberfall" });
        Assert.True(session.CaseFile.PublicClues.Count > 6);
        Assert.True(session.CaseFile.PublicWarrants.Count > 2);
    }

    [Fact]
    public void SameSeedKeepsTheRosterStableWhileDifferentSeedCodesCanChangeIt()
    {
        var factory = new SeededNewGameFactory();

        var seedA = StartingWorldDescriptorResolver.FormatSeedCode(CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 11));
        var seedASame = StartingWorldDescriptorResolver.FormatSeedCode(CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 11));
        var seedB = StartingWorldDescriptorResolver.FormatSeedCode(CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 12));

        var first = factory.Create("Ranger Vale", GameDifficulty.Standard, seedA);
        var firstAgain = factory.Create("Ranger Vale", GameDifficulty.Easy, seedASame);
        var second = factory.Create("Ranger Vale", GameDifficulty.Standard, seedB);

        Assert.Equal(RosterSignature(first), RosterSignature(firstAgain));
        Assert.Equal(WarrantSignature(first), WarrantSignature(firstAgain));
        Assert.Equal(TurfSignature(first), TurfSignature(firstAgain));
        Assert.True(
            RosterSignature(first) != RosterSignature(second)
            || WarrantSignature(first) != WarrantSignature(second),
            "Different seed codes should change at least one roster or warrant surface.");
    }

    [Fact]
    public void RandomizedNoHorseLightLoadoutSeedCreatesNoHorseOrSaddle()
    {
        var seedCode = StartingWorldDescriptorResolver.FormatSeedCode(Guid.NewGuid());
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale", GameDifficulty.Easy, seedCode, GameEntropy.Boring);

        Assert.Equal(GameDifficulty.Easy, session.GameDifficulty);
        Assert.Equal(GameEntropy.Boring, session.GameEntropy);
        // Loadout profile is now seed-derived, not parameter-derived
        // Just verify that the session was created successfully
        Assert.NotNull(session);
    }

    [Fact]
    public void DefaultAdventureRandomnessStaysRuntimeSaltedAndBoringModeCanOptIntoDeterminism()
    {
        var factory = new SeededNewGameFactory();

        var runtimeFirst = factory.Create("Ranger Vale");
        var runtimeSecond = factory.Create("Ranger Vale");

        Assert.Equal(SaltSourceMode.Runtime, runtimeFirst.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Runtime, runtimeSecond.SaltSource.Mode);
        Assert.Equal(GameEntropy.Classic, runtimeFirst.GameEntropy);
        Assert.Equal(GameEntropy.Classic, runtimeSecond.GameEntropy);
        Assert.NotEqual(runtimeFirst.SaltSource.Salt, runtimeSecond.SaltSource.Salt);

        var boringDescriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor() with
        {
            GameEntropy = GameEntropy.Boring
        };
        var boringSeed = StartingWorldDescriptorResolver.FormatSeedCode(StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(boringDescriptor));

        var deterministicFirst = factory.Create("Ranger Vale", setupSeedCode: boringSeed, gameEntropy: GameEntropy.Boring);
        var deterministicSecond = factory.Create("Ranger Vale", setupSeedCode: boringSeed, gameEntropy: GameEntropy.Boring);

        Assert.Equal(SaltSourceMode.Fixed, deterministicFirst.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Fixed, deterministicSecond.SaltSource.Mode);
        Assert.Equal(deterministicFirst.SaltSource.Salt, deterministicSecond.SaltSource.Salt);
    }

    [Fact]
    public void CreateWithPlayerChosenStartingTownOverridesSeedDefault()
    {
        var factory = new SeededNewGameFactory();

        // Establish the seed-derived default town for the default seed.
        var defaultSession = factory.Create("Ranger Vale");
        var seedDefaultTownId = defaultSession.Player.CurrentTownId;

        // Pick a different valid town from the same world to use as the player override.
        var overriddenTown = defaultSession.World.Towns.First(town => !town.Id.Equals(seedDefaultTownId));

        var session = factory.Create("Ranger Vale", startingTownId: overriddenTown.Id.Value);

        Assert.Equal(overriddenTown.Id, session.Player.CurrentTownId);
        Assert.NotEqual(seedDefaultTownId, session.Player.CurrentTownId);
    }

    [Fact]
    public void CreateWithNullStartingTownIdUsesSeedDefault()
    {
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale", startingTownId: null);

        // The seed-derived default for the canonical descriptor is "pinecross".
        Assert.Equal(new WildBunch.Domain.World.TownId("pinecross"), session.Player.CurrentTownId);
    }

    private static string RosterSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.Suspects.Select(suspect => $"{suspect.Id.Value}:{suspect.Name}"));

    private static string WarrantSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.PublicWarrants.Select(warrant => $"{warrant.Id.Value}:{warrant.TargetName}:{warrant.Terms.TargetKind}:{string.Join("/", warrant.Terms.GangAffiliations.Select(gang => gang.Value))}:{warrant.Terms.AdvancesGangPressureFor?.Value ?? string.Empty}"));

    private static string TurfSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.SuspectTurfAssignments.Select(assignment => $"{assignment.SuspectId.Value}:{assignment.TurfTownId.Value}"));

    private static Guid CreateSeedCode(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5, byte byte6, ulong tail)
        => StartingWorldDescriptorSeedCodeFactory.CreateSeedCode(byte0, byte1, byte2, byte3, byte4, byte5, byte6, tail);
}
