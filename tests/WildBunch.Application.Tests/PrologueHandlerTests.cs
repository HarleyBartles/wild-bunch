using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.GameContent.Prologue;

namespace WildBunch.Application.Tests;

public sealed class PrologueHandlerTests
{
    [Fact]
    public async Task ReturnsPrologueWithSubstitutedDescriptor()
    {
        var handler = new GetPrologueHandler();
        var query = new GetPrologueQuery();
        var result = await handler.HandleAsync(query);

        Assert.Equal(PrologueContent.StorySoFarHeading, result.Heading);
        Assert.Equal(PrologueContent.StorySoFarPrimaryAction, result.PrimaryAction);
        Assert.DoesNotContain("{trueCulpritMainIdentifier}", result.Body);
        Assert.False(string.IsNullOrEmpty(result.Body));
    }

    [Fact]
    public async Task BodyContainsNoPlaceholder()
    {
        var handler = new GetPrologueHandler();
        var query = new GetPrologueQuery();
        var result = await handler.HandleAsync(query);

        // The descriptor should be something like "a stranger with..." or "an unfamiliar person"
        // It should NOT contain the raw placeholder
        Assert.DoesNotContain("{trueCulpritMainIdentifier}", result.Body);
        // The body should contain the substituted descriptor (it's substituted into the variant text)
        // We can't assert the exact descriptor without resolving it, but we can assert the placeholder is gone
        // and the body still contains the surrounding variant copy
        Assert.Contains("Wild Bunch", result.Body);
    }

    [Fact]
    public async Task SpecificVariantIsReturned()
    {
        var handler = new GetPrologueHandler();
        var query = new GetPrologueQuery(VariantId: "prologue.story-so-far.variant-2");
        var result = await handler.HandleAsync(query);

        Assert.Equal("prologue.story-so-far.variant-2", result.VariantId);
    }

    [Fact]
    public async Task DefaultVariantIsFirst()
    {
        var handler = new GetPrologueHandler();
        var query = new GetPrologueQuery();
        var result = await handler.HandleAsync(query);

        Assert.Equal(PrologueContent.Variants[0].Id, result.VariantId);
    }

    [Fact]
    public async Task HiddenTruthGuard_NoCulpritInternalsExposed()
    {
        var handler = new GetPrologueHandler();
        var query = new GetPrologueQuery();
        var result = await handler.HandleAsync(query);

        // The body must not contain hidden culprit internals
        Assert.DoesNotContain("TrueCulpritId", result.Body);
        Assert.DoesNotContain("isTrueCulprit", result.Body);
        Assert.DoesNotContain("IsTrueCulprit", result.Body);
        Assert.DoesNotContain("suspect-", result.Body); // internal suspect ids like "suspect-4"
        Assert.DoesNotContain("{trueCulpritMainIdentifier}", result.Body); // placeholder must be substituted
    }

    [Fact]
    public async Task AllVariantsAreAvailable()
    {
        var handler = new GetPrologueHandler();
        foreach (var variant in PrologueContent.Variants)
        {
            var query = new GetPrologueQuery(VariantId: variant.Id);
            var result = await handler.HandleAsync(query);
            Assert.Equal(variant.Id, result.VariantId);
            Assert.DoesNotContain("{trueCulpritMainIdentifier}", result.Body);
        }
    }

    [Fact]
    public async Task UnknownVariantIdFallsBackToFirst()
    {
        var handler = new GetPrologueHandler();
        var query = new GetPrologueQuery(VariantId: "unknown-variant");
        var result = await handler.HandleAsync(query);
        Assert.Equal(PrologueContent.Variants[0].Id, result.VariantId);
    }
}
