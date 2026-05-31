using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Queries;

public sealed class GetJournalHandler
{
    private readonly IGameJournalReadRepository _gameJournalReadRepository;

    public GetJournalHandler(IGameJournalReadRepository gameJournalReadRepository)
    {
        _gameJournalReadRepository = gameJournalReadRepository;
    }

    public async Task<JournalDto> HandleAsync(GetJournalQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new WildBunch.Domain.Game.GameSessionId(query.GameSessionId);
        var snapshot = await _gameJournalReadRepository.GetByIdAsync(sessionId, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new GameSessionNotFoundException(sessionId);
        return JournalMapper.ToDto(snapshot);
    }
}
