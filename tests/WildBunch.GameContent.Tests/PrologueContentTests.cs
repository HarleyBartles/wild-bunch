using WildBunch.GameContent.Prologue;

namespace WildBunch.GameContent.Tests;

public sealed class PrologueContentTests
{
    [Fact]
    public void VariantsHaveUniqueIds()
    {
        var ids = PrologueContent.Variants.Select(v => v.Id).ToArray();

        Assert.Equal(3, ids.Length);
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AllVariantsContainTrueCulpritMainIdentifierPlaceholder()
    {
        Assert.All(PrologueContent.Variants, variant =>
            Assert.Contains("{trueCulpritMainIdentifier}", variant.BodyTemplate));
    }

    [Fact]
    public void AllVariantsPreserveRequiredFacts()
    {
        // Each variant must mention: dying/dead man, Wild Bunch, sheriff, fugitive/wanted/warrant, take in/clear/prove.
        Assert.All(PrologueContent.Variants, variant =>
        {
            Assert.Contains("Wild Bunch", variant.BodyTemplate, StringComparison.OrdinalIgnoreCase);

            // Dying man / dead man / bleeding
            Assert.True(
                variant.BodyTemplate.Contains("dying", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("dead man", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("bleeding", StringComparison.OrdinalIgnoreCase));

            // Sheriff
            Assert.Contains("sheriff", variant.BodyTemplate, StringComparison.OrdinalIgnoreCase);

            // Fugitive / wanted / warrant / the law (semantic equivalents across variants)
            Assert.True(
                variant.BodyTemplate.Contains("fugitive", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("warrant", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("wanted", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("the law", StringComparison.OrdinalIgnoreCase));

            // Take in / clear / prove / real killer / truth (semantic equivalents across variants)
            Assert.True(
                variant.BodyTemplate.Contains("take", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("clear", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("prove", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("real killer", StringComparison.OrdinalIgnoreCase) ||
                variant.BodyTemplate.Contains("truth", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void GetVariantReturnsCorrectVariant()
    {
        var variant = PrologueContent.GetVariant("prologue.story-so-far.variant-2");

        Assert.Equal("prologue.story-so-far.variant-2", variant.Id);
    }

    [Fact]
    public void GetVariantFallsBackToFirstForUnknownId()
    {
        var variant = PrologueContent.GetVariant("unknown");

        Assert.Equal(PrologueContent.Variants[0].Id, variant.Id);
    }

    [Fact]
    public void StaticCopyMatchesCopyDoc()
    {
        Assert.Equal("Howdy, pard'ner. What name d'you go by?", PrologueContent.NameEntryHeading);
        Assert.Equal("A name's a useful thing to have when folks start shouting after you.", PrologueContent.NameEntryHelper);
        Assert.Equal("Continue", PrologueContent.NameEntryPrimaryAction);
        Assert.Equal("Tell me what name you go by before we ride on.", PrologueContent.NameEntryValidation);

        Assert.Equal("The story so far", PrologueContent.StorySoFarHeading);
        Assert.Equal("Ride on", PrologueContent.StorySoFarPrimaryAction);

        Assert.Equal("Pick a starting town", PrologueContent.StartingTownHeading);
        Assert.Equal("Saddling up the map…", PrologueContent.StartingTownEmptyState);
        Assert.Equal("Start in {townName}", PrologueContent.StartingTownPrimaryActionTemplate);
        Assert.Equal("Pick a town before you ride.", PrologueContent.StartingTownValidation);

        Assert.Equal("Game Settings", PrologueContent.SettingsEntryLabel);
        Assert.Equal("Game Settings", PrologueContent.SettingsHeading);
        Assert.Equal("Playthrough", PrologueContent.SettingsSectionHeading);
        Assert.Equal("Start Over", PrologueContent.StartOverActionLabel);
        Assert.Equal("Archive this playthrough and begin again from the start.", PrologueContent.StartOverHelper);
        Assert.Equal("Start over?", PrologueContent.StartOverConfirmTitle);
        Assert.Equal("Cancel", PrologueContent.StartOverCancelLabel);
        Assert.Equal("Archive and Start Over", PrologueContent.StartOverConfirmLabel);
        Assert.Equal("Your old playthrough has been archived. Start a new one when you are ready.", PrologueContent.StartOverSuccessCopy);
    }
}
