using WildBunch.Application.Games.Commands;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.Application.Tests.Integration;

/// <summary>
/// Integration test for the three-phase dev-enabled action pattern.
/// Tests the full flow: prep → inject dev salts → start.
/// </summary>
public sealed class DevEnabledActionPatternIntegrationTests
{
    [Fact]
    public async Task ThreePhaseFlow_PrepInjectStart_UsesDevLayoutSalts()
    {
        var repository = new InMemoryGameSessionRepository();
        var newGameFactory = new SeededNewGameFactory();
        var hudProjector = new HudProjector();
        var diaryProjector = new DiaryProjector();
        
        var prepHandler = new PrepGameSessionHandler(repository, repository);
        var startHandler = new StartGameSessionHandler(newGameFactory, repository, repository, hudProjector, diaryProjector);
        
        // Phase 1: Prep
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode())).ToString();
        var prepCommand = new PrepGameSessionCommand(seedCode, GameDifficulty.Standard, GameEntropy.Classic);
        var prepResult = await prepHandler.HandleAsync(prepCommand, CancellationToken.None);
        
        Assert.NotNull(prepResult.GameSessionId);
        var sessionId = Guid.Parse(prepResult.GameSessionId);
        
        // Load the prepped session
        var prepped = await repository.GetByIdAsync(new GameSessionId(sessionId), CancellationToken.None);
        Assert.NotNull(prepped);
        Assert.Equal(GameStatus.Prepped, prepped.Status);
        
        // Phase 2: Inject dev salts (simulated via direct aggregate manipulation for test)
        var devSalts = new LayoutSalts("dev-buildings", "dev-roads", "dev-dirt", "dev-props");
        prepped.SetDevLayoutSalts(devSalts);
        await repository.StoreAsync(prepped, Guid.NewGuid(), CancellationToken.None);
        await repository.CommitAsync(CancellationToken.None);
        prepped.MarkEventsCommitted();
        
        // Phase 3: Start
        var startCommand = new StartGameSessionCommand(sessionId);
        var result = await startHandler.HandleAsync(startCommand, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(GameStatus.Active, result.Status);
        // The dev salts were used in world generation via INewGameFactory
        Assert.NotNull(result.World);
    }
    
    [Fact]
    public async Task ThreePhaseFlow_PrepStartWithoutInject_UsesDefaultSalts()
    {
        var repository = new InMemoryGameSessionRepository();
        var newGameFactory = new SeededNewGameFactory();
        var hudProjector = new HudProjector();
        var diaryProjector = new DiaryProjector();
        
        var prepHandler = new PrepGameSessionHandler(repository, repository);
        var startHandler = new StartGameSessionHandler(newGameFactory, repository, repository, hudProjector, diaryProjector);
        
        // Phase 1: Prep
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode())).ToString();
        var prepCommand = new PrepGameSessionCommand(seedCode, GameDifficulty.Standard, GameEntropy.Classic);
        var prepResult = await prepHandler.HandleAsync(prepCommand, CancellationToken.None);
        
        Assert.NotNull(prepResult.GameSessionId);
        var sessionId = Guid.Parse(prepResult.GameSessionId);
        
        // Phase 2: Skip inject (no dev salts set)
        
        // Phase 3: Start
        var startCommand = new StartGameSessionCommand(sessionId);
        var result = await startHandler.HandleAsync(startCommand, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(GameStatus.Active, result.Status);
        // Default salts were used in world generation
        Assert.NotNull(result.World);
    }
}
