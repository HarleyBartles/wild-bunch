using WildBunch.Domain.Cases;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class SaloonPersonOfInterestDescriptorTests
{
    [Fact]
    public void Describe_MissingEarFeatureProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Is missing the right ear.", "a missing right ear", "is missing the right ear"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a missing right ear", descriptor);
    }

    [Fact]
    public void Describe_LimpFeatureProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Has a limp in the left leg.", "a limp in the left leg", "has a limp in the left leg"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a limp in the left leg", descriptor);
    }

    [Fact]
    public void Describe_ScarFeatureProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a scar on the left cheek", descriptor);
    }

    [Fact]
    public void Describe_AccessoryWithKeepsVerbProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Keeps a split-finger glove tucked into a coat pocket.",
            "a split-finger glove tucked into a coat pocket",
            "keeps a split-finger glove tucked into a coat pocket"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a split-finger glove tucked into a coat pocket", descriptor);
    }

    private static Suspect CreateSuspect(FeatureLanguage language)
        => new(
            new SuspectId("suspect-1"),
            "Mira Cline",
            new SuspectProfile(Array.Empty<SuspectAlias>(), new[] { new SuspectIdentityFact(language) }),
            SuspectTraits.Empty,
            SuspectStatus.AtLarge);

    private static CaseFile CreateCaseFile(Suspect suspect)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        return new CaseFile(
            accusation: null,
            new[] { suspect },
            trueCulpritId: suspect.Id,
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());
    }
}
