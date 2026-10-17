using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Prologue;

namespace WildBunch.Application.Tests.Handlers;

public sealed class PrologueHandlerTests
{
    [Fact]
    public async Task ReturnsPrologueWithDescriptorFromTheSettledSessionCase()
    {
        var (handler, session) = CreateHandler();
        var culprit = session.CaseFile.Suspects.Single(suspect => suspect.Id == session.CaseFile.TrueCulpritId);
        var expectedDescriptor = SaloonPersonOfInterestDescriptor.Describe(culprit, session.CaseFile);

        var result = await handler.HandleAsync(new GetPrologueQuery(session.Id.Value));

        Assert.Equal(PrologueContent.StorySoFarHeading, result.Heading);
        Assert.Equal(PrologueContent.StorySoFarPrimaryAction, result.PrimaryAction);
        Assert.Contains(expectedDescriptor, result.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("{trueCulpritMainIdentifier}", result.Body);
        Assert.Contains("Wild Bunch", result.Body);
    }

    [Fact]
    public async Task MissingSessionCannotProduceAPrologueFromDefaultSetupValues()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new GetPrologueHandler(repository);

        await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetPrologueQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task SpecificVariantUsesTheSettledSessionCase()
    {
        var (handler, session) = CreateHandler();

        var result = await handler.HandleAsync(
            new GetPrologueQuery(session.Id.Value, "prologue.story-so-far.variant-2"));

        Assert.Equal("prologue.story-so-far.variant-2", result.VariantId);
        Assert.DoesNotContain("{trueCulpritMainIdentifier}", result.Body);
    }

    [Fact]
    public async Task UnknownVariantFallsBackToTheFirstVariant()
    {
        var (handler, session) = CreateHandler();

        var result = await handler.HandleAsync(new GetPrologueQuery(session.Id.Value, "unknown-variant"));

        Assert.Equal(PrologueContent.Variants[0].Id, result.VariantId);
    }

    private static (GetPrologueHandler Handler, GameSession Session) CreateHandler()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = new StubNewGameFactory().CreatedSession;
        repository.Seed(session);
        return (new GetPrologueHandler(repository), session);
    }
}
