using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class SaloonPersonOfInterestDescriptorParityTests
{
    [Fact]
    public void LookAroundSaloonAndMappedDtoUseTheSameDescriptorForWarrantBasedPersonOfInterest()
    {
        var session = CreateSession(
            suspectProfile: SuspectProfile.Empty,
            suspectTraits: SuspectTraits.Empty,
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Grey Jay" },
                        new[] { "Has a scar on the left cheek." },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery.")
            });

        AssertDescriptorParity(session, "a stranger with a scar on the left cheek");
    }

    [Fact]
    public void LookAroundSaloonAndMappedDtoUseTheSameDescriptorForProfileBasedPersonOfInterest()
    {
        var session = CreateSession(
            suspectProfile: new SuspectProfile(
                Array.Empty<SuspectAlias>(),
                new[] { new SuspectIdentityFact("a brass buckle with a cracked star engraving") }),
            suspectTraits: SuspectTraits.Empty);

        AssertDescriptorParity(session, "a stranger with a brass buckle with a cracked star engraving");
    }

    [Fact]
    public void LookAroundSaloonAndMappedDtoUseTheSameDescriptorForTraitFallbackPersonOfInterest()
    {
        var session = CreateSession(
            suspectProfile: SuspectProfile.Empty,
            suspectTraits: SuspectTraits.FromTags(SuspectTraitTags.Desperate));

        AssertDescriptorParity(session, "a stranger who is desperate");
    }

    [Fact]
    public void SaloonPersonOfInterestPathsExposeAnExplicitKindSeam()
    {
        var citizenSession = CreateCitizenSession();
        citizenSession.LookAroundSaloon();

        var citizenMappedSession = GameSessionMapper.ToDto(citizenSession);
        var citizenConfrontation = citizenSession.ConfrontSaloonPersonOfInterest("warrant-1");

        Assert.Equal("Citizen", ReadEnumName(citizenMappedSession.ActiveSaloonPersonOfInterest!, "Kind"));
        Assert.Equal("Citizen", ReadEnumName(citizenConfrontation, "PersonOfInterestKind"));

        var wantedSession = CreateSession(
            suspectProfile: SuspectProfile.Empty,
            suspectTraits: SuspectTraits.Empty,
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Grey Jay" },
                        new[] { "Has a scar on the left cheek." },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery.")
            });
        wantedSession.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);
        wantedSession.LookAroundSaloon();

        var wantedMappedSession = GameSessionMapper.ToDto(wantedSession);
        var wantedConfrontation = wantedSession.ConfrontSaloonPersonOfInterest("warrant-1");

        Assert.Equal("WantedSuspect", ReadEnumName(wantedMappedSession.ActiveSaloonPersonOfInterest!, "Kind"));
        Assert.Equal("WantedSuspect", ReadEnumName(wantedConfrontation, "PersonOfInterestKind"));
    }

    private static void AssertDescriptorParity(GameSession session, string expectedDescriptor)
    {
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);
        var lookAround = session.LookAroundSaloon();
        var mappedSession = GameSessionMapper.ToDto(session);

        Assert.True(lookAround.Success);
        Assert.Equal($"You look around the saloon and spot {expectedDescriptor}.", lookAround.Message);
        Assert.NotNull(mappedSession.ActiveSaloonPersonOfInterest);
        Assert.Equal(expectedDescriptor, mappedSession.ActiveSaloonPersonOfInterest!.Descriptor);
        Assert.Equal(mappedSession.ActiveSaloonPersonOfInterest.Descriptor, lookAround.Message["You look around the saloon and spot ".Length..^1]);
    }

    private static string? ReadEnumName(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return property!.GetValue(target)?.ToString();
    }

    private static GameSession CreateSession(
        SuspectProfile suspectProfile,
        SuspectTraits suspectTraits,
        IEnumerable<Warrant>? knownWarrants = null)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", suspectProfile, suspectTraits, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: knownWarrants ?? Array.Empty<Warrant>());

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }

    private static GameSession CreateCitizenSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var caseFile = new CaseFile(
            accusation: null,
            Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }
}
