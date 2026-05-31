using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Abstractions;

public interface IGameJournalReadRepository
{
    Task<JournalSnapshot?> GetByIdAsync(
        GameSessionId id,
        int skip = 0,
        int? take = null,
        CancellationToken cancellationToken = default);
}
