using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

public sealed class StartNewGameHandler : GameSessionCommandHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public StartNewGameHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    public async Task<GameSessionDto> HandleAsync(StartNewGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dto = await ExecuteNewSessionAsync(async ct =>
        {
            var session = _newGameFactory.Create(command.PlayerName, command.TravelDifficulty, command.SetupSeedCode, command.Entropy);
            return (session, GameSessionMapper.ToDto(session));
        }, cancellationToken).ConfigureAwait(false);

        var events = await GameSessionRepository.GetEventStreamAsync(
            new GameSessionId(dto.Id), 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}
