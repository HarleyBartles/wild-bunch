namespace WildBunch.Application.Abstractions;

public interface IGameSessionUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
