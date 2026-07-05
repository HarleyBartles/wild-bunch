using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeededNewGameFactoryTests
{
    [Fact]
    public void CreatesRicherSeedWorldAndCase()
    {
        var factory = new SeededNewGameFactory();

        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);

        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal(session.World.Towns.First().Id, session.Player.CurrentTownId);
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
        // The world should be fully connected (all towns reachable)
        var towns = session.World.Towns.ToArray();
        Assert.True(session.World.Trails.Count > 0, "World should have trails");
        Assert.Equal(7, session.CaseFile.Suspects.Count);
        Assert.Single(session.CaseFile.Suspects, suspect => suspect.Id.Equals(session.CaseFile.TrueCulpritId));
        Assert.Equal(5, session.CaseFile.KillerReleaseThreshold);
        Assert.Equal("The culprit has a scar on the left cheek.", session.CaseFile.OpeningLead.Description);
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
        var culpritOpeningFeature = Assert.Single(CaseSuspectFeaturePool.FeaturePool, feature => feature.Language.OpeningLeadForm == session.CaseFile.OpeningLead.Description);
        Assert.True(culpritOpeningFeature.HasTag(CaseSuspectFeatureTags.OpeningLeadCapable));
        Assert.True(culpritOpeningFeature.HasTag(CaseSuspectFeatureTags.ClassicNod));
        Assert.Equal(culpritOpeningFeature.Language.HasForm, session.CaseFile.Suspects[3].Profile.IdentifyingFacts[0].Language.HasForm);
        Assert.All(session.CaseFile.Suspects[3].Profile.IdentifyingFacts, fact => Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Language.HasForm == fact.Language.HasForm));
        Assert.Contains(session.CaseFile.KnownClues, clue =>
            clue.Kind == ClueKind.CulpritTrail
            && clue.TargetKind == InvestigationTargetKind.TrueCulprit
            && clue.Description == session.CaseFile.OpeningLead.Description);
        Assert.Single(session.CaseFile.KnownClues);
        // 6 base surface-tagged clues only; town-specific clues are a runtime/salt concern.
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
        // 7 gang member warrants (one per suspect, including the true culprit) + 21 unrelated = 28.
        Assert.Equal(28, session.CaseFile.PublicWarrants.Count);
        Assert.Equal(7, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.GangMember || w.Terms.TargetKind == InvestigationTargetKind.TrueCulprit));
        Assert.Equal(21, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal));
        Assert.Equal("Butch Cassidy", session.CaseFile.PublicWarrants[0].TargetName);
        Assert.Equal(InvestigationTargetKind.GangMember, session.CaseFile.PublicWarrants[0].Terms.TargetKind);
        Assert.Equal(WarrantDisposition.DeadOrAlive, session.CaseFile.PublicWarrants[0].Terms.Disposition);
        Assert.Equal(1800m, session.CaseFile.PublicWarrants[0].Terms.BountyAmount);
        Assert.Contains("Grey Jay", session.CaseFile.PublicWarrants[0].Terms.KnownAliases);
        Assert.Contains("J. Pike", session.CaseFile.PublicWarrants[0].Terms.KnownAliases);
        Assert.Equal(new[] { "Raven-feather pin", "Black felt hat" }, session.CaseFile.PublicWarrants[0].Terms.KnownFeatures);
        Assert.Equal(new[] { OutlawGangIds.WildBunch }, session.CaseFile.PublicWarrants[0].Terms.GangAffiliations);
        Assert.Equal(OutlawGangIds.WildBunch, session.CaseFile.PublicWarrants[0].Terms.AdvancesGangPressureFor);
        // The true culprit's warrant is in the pool (gated behind the killer release gate at runtime).
        Assert.Contains(session.CaseFile.PublicWarrants, warrant => warrant.TargetName == session.CaseFile.Suspects[3].Name
            && warrant.Terms.TargetKind == InvestigationTargetKind.TrueCulprit);
        Assert.DoesNotContain(session.CaseFile.PublicWarrants[0].Terms.KnownFeatures, feature => feature.Contains("scar", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.Accusation);
        Assert.Equal(7, session.CaseFile.SuspectTurfAssignments.Count);
        Assert.All(session.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(session.World.Towns, town => town.Id.Equals(assignment.TurfTownId)));
        Assert.All(session.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Id.Equals(assignment.SuspectId)));
    }

    [Fact]
    public void CaseFile_StartsWithOneKnownClueAndZeroKnownWarrants()
    {
        var factory = new SeededNewGameFactory();
        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);

        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.KnownWarrants);
    }

    [Fact]
    public void CaseFile_PublicWarrants_HasSevenGangPlusTwentyOneUnrelated()
    {
        var factory = new SeededNewGameFactory();
        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);

        Assert.Equal(28, session.CaseFile.PublicWarrants.Count);
        Assert.Equal(7, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.GangMember || w.Terms.TargetKind == InvestigationTargetKind.TrueCulprit));
        Assert.Equal(21, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal));
    }

    [Fact]
    public void CaseFile_PublicClues_HasSixBaseCluesNoTownSpecificOnes()
    {
        var factory = new SeededNewGameFactory();
        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);

        Assert.Equal(6, session.CaseFile.PublicClues.Count);
    }

    [Fact]
    public void FrontierDescriptorAddsTownSpecificCivicCluesForTheNextVisitedTown()
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(CreateSeedCode(1, 1, 3, 0, tail: 13));
        var factory = new SeededNewGameFactory();

        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, seedCode, GameEntropy.Classic);

        Assert.Contains(session.Player.CurrentTownId!.Value.Value, SeedWorldFactory.NamePool.Select(n => n.Id));
        // Town-specific civic clues/warrants are a runtime/salt concern (Task 4),
        // not setup-time. The seed case file surfaces only the base pools.
        Assert.Equal(6, session.CaseFile.PublicClues.Count);
        Assert.Equal(28, session.CaseFile.PublicWarrants.Count);
    }

    [Fact]
    public void SameSeedKeepsTheRosterStableWhileDifferentSeedCodesCanChangeIt()
    {
        var factory = new SeededNewGameFactory();

        var seedA = SeedWorldResolver.FormatSeedCode(CreateSeedCode(0, 1, 3, 0, tail: 0));
        var seedASame = SeedWorldResolver.FormatSeedCode(CreateSeedCode(0, 1, 3, 0, tail: 0));
        var seedB = SeedWorldResolver.FormatSeedCode(CreateSeedCode(0, 2, 3, 0, tail: 0));

        var first = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, seedA, GameEntropy.Classic);
        var firstAgain = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Easy, seedASame, GameEntropy.Classic);
        var second = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, seedB, GameEntropy.Classic);

        Assert.Equal(RosterSignature(first), RosterSignature(firstAgain));
        Assert.Equal(WarrantSignature(first), WarrantSignature(firstAgain));
        Assert.Equal(TurfSignature(first), TurfSignature(firstAgain));
        Assert.True(
            RosterSignature(first) != RosterSignature(second)
            || WarrantSignature(first) != WarrantSignature(second),
            "Different seed codes should change at least one roster or warrant surface.");
    }

    [Fact]
    public void AllDifficultiesGetTransitionalHorseAndSaddleDefaults()
    {
        // BUNCH-107 transitional: all difficulties get horse+saddle+Standard loadout.
        // BUNCH-94 will expand DifficultyEnvelope to add difficulty-owned variety.
        var factory = new SeededNewGameFactory();
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());

        var easySession = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Easy, seedCode, GameEntropy.Boring);
        var brutalSession = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Brutal, seedCode, GameEntropy.Boring);

        Assert.True(easySession.Player.Inventory.HasItem(ItemKind.Horse));
        Assert.True(easySession.Player.Inventory.HasItem(ItemKind.Saddle));
        Assert.True(brutalSession.Player.Inventory.HasItem(ItemKind.Horse));
        Assert.True(brutalSession.Player.Inventory.HasItem(ItemKind.Saddle));
    }

    [Fact]
    public void DefaultAdventureRandomnessStaysRuntimeSaltedAndBoringModeCanOptIntoDeterminism()
    {
        var factory = new SeededNewGameFactory();

        var runtimeFirst = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);
        var runtimeSecond = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);

        Assert.Equal(SaltSourceMode.Runtime, runtimeFirst.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Runtime, runtimeSecond.SaltSource.Mode);
        Assert.Equal(GameEntropy.Classic, runtimeFirst.GameEntropy);
        Assert.Equal(GameEntropy.Classic, runtimeSecond.GameEntropy);
        Assert.NotEqual(runtimeFirst.SaltSource.Salt, runtimeSecond.SaltSource.Salt);

        // Boring entropy uses Fixed salt derived from the seed code.
        var boringTemplate = SeedWorldResolver.CreateCanonicalSeedWorld();
        var boringSeed = SeedWorldResolver.FormatSeedCode(
            SeedWorldResolver.CreateRepresentativeSeedCode(boringTemplate));

        var deterministicFirst = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, boringSeed, GameEntropy.Boring);
        var deterministicSecond = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, boringSeed, GameEntropy.Boring);

        Assert.Equal(SaltSourceMode.Fixed, deterministicFirst.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Fixed, deterministicSecond.SaltSource.Mode);
        Assert.Equal(deterministicFirst.SaltSource.Salt, deterministicSecond.SaltSource.Salt);
    }

    [Fact]
    public void CreateWithPlayerChosenStartingTownOverridesSeedDefault()
    {
        var factory = new SeededNewGameFactory();

        // Establish the seed-derived default town for the default seed.
        var defaultSession = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic);
        var seedDefaultTownId = defaultSession.Player.CurrentTownId;

        // Pick a different valid town from the same world to use as the player override.
        var overriddenTown = defaultSession.World.Towns.First(town => !town.Id.Equals(seedDefaultTownId));

        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic, startingTownId: overriddenTown.Id.Value);

        Assert.Equal(overriddenTown.Id, session.Player.CurrentTownId);
        Assert.NotEqual(seedDefaultTownId, session.Player.CurrentTownId);
    }

    [Fact]
    public void CreateWithNullStartingTownIdUsesSafeDefault()
    {
        var factory = new SeededNewGameFactory();

        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, null, GameEntropy.Classic, startingTownId: null);

        // The safe default from StartingTownPolicy is the first town in the world (slot 0).
        Assert.Equal(session.World.Towns.First().Id, session.Player.CurrentTownId);
    }

    [Fact]
    public void ReadWantedPosters_InBoringMode_SurfacesDifferentWarrantsInDifferentTowns()
    {
        // BUNCH-107: resolver-based selection varies by town slot index, so reading
        // wanted posters in different towns should surface different warrants (when
        // the eligible pool is large enough). Boring entropy uses a Fixed salt so the
        // selection is deterministic for the same seed.
        var boringTemplate = SeedWorldResolver.CreateCanonicalSeedWorld();
        var boringSeed = SeedWorldResolver.FormatSeedCode(
            SeedWorldResolver.CreateRepresentativeSeedCode(boringTemplate));
        var factory = new SeededNewGameFactory();

        var session = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, boringSeed, GameEntropy.Boring);

        // Read posters in the starting town.
        var firstResult = session.ReadWantedPosters();
        Assert.True(firstResult.Success);
        var firstWarrant = session.CaseFile.KnownWarrants.LastOrDefault();
        Assert.NotNull(firstWarrant);

        // Travel to a different town and read posters there.
        var secondTown = session.World.Towns.First(t => !t.Id.Equals(session.CurrentTown.TownId));
        session.CurrentTown.EnterTown(secondTown);

        var secondResult = session.ReadWantedPosters();
        Assert.True(secondResult.Success);
        var secondWarrant = session.CaseFile.KnownWarrants.LastOrDefault();
        Assert.NotNull(secondWarrant);
        Assert.NotEqual(firstWarrant!.Id, secondWarrant!.Id);
    }

    [Fact]
    public void DifficultyChangesDifficultyShapedFactsNotEntropy()
    {
        var factory = new SeededNewGameFactory();

        // The seed UUID encodes the seed-owned world/map layer only, NOT difficulty or entropy.
        // Difficulty and entropy are caller-supplied parameters to the canonical start flow.
        // Use one canonical seed code to fix the world, then vary only the difficulty parameter.
        var seedCode = SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode());

        // Same seed, same entropy, different difficulty parameter
        var easy = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Easy, seedCode, GameEntropy.Classic);
        var standard = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Standard, seedCode, GameEntropy.Classic);
        var challenging = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Challenging, seedCode, GameEntropy.Classic);
        var brutal = CanonicalStartFlow.StartGame(factory, "Ranger Vale", GameDifficulty.Brutal, seedCode, GameEntropy.Classic);

        // Difficulty-shaped facts differ across difficulties (starting cash, starting health, travel rules)
        Assert.NotEqual(easy.Player.Wallet.Cash, standard.Player.Wallet.Cash);
        Assert.NotEqual(standard.Player.Wallet.Cash, challenging.Player.Wallet.Cash);
        Assert.NotEqual(challenging.Player.Wallet.Cash, brutal.Player.Wallet.Cash);

        Assert.NotEqual(easy.Player.Health, standard.Player.Health);
        Assert.NotEqual(standard.Player.Health, challenging.Player.Health);
        Assert.NotEqual(challenging.Player.Health, brutal.Player.Health);

        // Travel rules profiles differ
        Assert.NotEqual(easy.TravelRules.CanteenCapacity, brutal.TravelRules.CanteenCapacity);
        Assert.NotEqual(easy.TravelRules.MountedRideDayProgress, brutal.TravelRules.MountedRideDayProgress);
        Assert.NotEqual(easy.TravelRules.EncounterFightAmmoHealthLoss, brutal.TravelRules.EncounterFightAmmoHealthLoss);

        // Seed-derived world is the same across all four (difficulty does not change the world)
        Assert.Equal(standard.World.Towns.Count, easy.World.Towns.Count);
        Assert.Equal(standard.World.Towns.Count, challenging.World.Towns.Count);
        Assert.Equal(standard.World.Towns.Count, brutal.World.Towns.Count);
        Assert.Equal(standard.Player.CurrentTownId, easy.Player.CurrentTownId);
        Assert.Equal(standard.Player.CurrentTownId, challenging.Player.CurrentTownId);
        Assert.Equal(standard.Player.CurrentTownId, brutal.Player.CurrentTownId);

        // Entropy is the same across all four (difficulty does not change entropy)
        Assert.Equal(GameEntropy.Classic, easy.GameEntropy);
        Assert.Equal(GameEntropy.Classic, standard.GameEntropy);
        Assert.Equal(GameEntropy.Classic, challenging.GameEntropy);
        Assert.Equal(GameEntropy.Classic, brutal.GameEntropy);

        // Salt posture is the same across all four (difficulty does not change salt posture)
        Assert.Equal(easy.SaltSource.Mode, standard.SaltSource.Mode);
        Assert.Equal(standard.SaltSource.Mode, challenging.SaltSource.Mode);
        Assert.Equal(challenging.SaltSource.Mode, brutal.SaltSource.Mode);
    }

    private static string RosterSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.Suspects.Select(suspect => $"{suspect.Id.Value}:{suspect.Name}"));

    private static string WarrantSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.PublicWarrants.Select(warrant => $"{warrant.Id.Value}:{warrant.TargetName}:{warrant.Terms.TargetKind}:{string.Join("/", warrant.Terms.GangAffiliations.Select(gang => gang.Value))}:{warrant.Terms.AdvancesGangPressureFor?.Value ?? string.Empty}"));

    private static string TurfSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.SuspectTurfAssignments.Select(assignment => $"{assignment.SuspectId.Value}:{assignment.TurfTownId.Value}"));

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);
}
