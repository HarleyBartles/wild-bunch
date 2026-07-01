using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class FeatureLanguageServiceTests
{
    [Fact]
    public void LimpLeftLeg_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.Limp, "leg", FeatureSide.Left);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Has a limp in the left leg.", language.HasForm);
        Assert.Equal("a limp in the left leg", language.WithForm);
        Assert.Equal("has a limp in the left leg", language.WhoForm);
        Assert.Equal("The culprit walks with a limp in the left leg.", language.OpeningLeadForm);
    }

    [Fact]
    public void MissingRightEar_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.MissingPart, "ear", FeatureSide.Right);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Is missing the right ear.", language.HasForm);
        Assert.Equal("a missing right ear", language.WithForm);
        Assert.Equal("is missing the right ear", language.WhoForm);
        Assert.Equal("The culprit is missing the right ear.", language.OpeningLeadForm);
    }

    [Fact]
    public void ScarLeftCheek_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.Scar, "cheek", FeatureSide.Left);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Has a scar on the left cheek.", language.HasForm);
        Assert.Equal("a scar on the left cheek", language.WithForm);
        Assert.Equal("has a scar on the left cheek", language.WhoForm);
        Assert.Equal("The culprit has a scar on the left cheek.", language.OpeningLeadForm);
    }

    [Fact]
    public void NoEyebrows_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.Absence, "eyebrows", FeatureSide.None);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Has no eyebrows.", language.HasForm);
        Assert.Equal("no eyebrows", language.WithForm);
        Assert.Equal("has no eyebrows", language.WhoForm);
        Assert.Equal("The culprit has no eyebrows.", language.OpeningLeadForm);
    }

    [Fact]
    public void Raw_FactoryProducesExplicitForms()
    {
        var language = FeatureLanguage.Raw(
            "A pale scar cuts across the left cheek.",
            "a pale scar across the left cheek",
            "has a pale scar across the left cheek");

        Assert.Equal("A pale scar cuts across the left cheek.", language.HasForm);
        Assert.Equal("a pale scar across the left cheek", language.WithForm);
        Assert.Equal("has a pale scar across the left cheek", language.WhoForm);
        Assert.Null(language.OpeningLeadForm);
    }
}
