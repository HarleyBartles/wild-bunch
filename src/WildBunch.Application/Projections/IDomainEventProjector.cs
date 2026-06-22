using WildBunch.Domain.Events;

namespace WildBunch.Application.Projections;

/// <summary>
/// Marker interface for projection results. Each projection type has its own
/// concrete result type. See ADR-0028 for the projection taxonomy.
/// </summary>
public interface IProjectionResult { }

/// <summary>
/// Projector interface for deriving read-model projections from typed domain events.
/// Projectors are read-only; they do not mutate aggregate state.
/// See ADR-0028 for the four projection types: diary, HUD, case file view, full audit.
/// </summary>
/// <typeparam name="TResult">The projection result type.</typeparam>
public interface IDomainEventProjector<out TResult> where TResult : IProjectionResult
{
    TResult Project(IReadOnlyList<IDomainEvent> events);
}
