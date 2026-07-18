using WildBunch.Domain.Travel;

namespace WildBunch.Application.Projections;

/// <summary>
/// Projection result: travel diary days derived from the domain event stream.
/// This is a read-only projection — it does not mutate aggregate state.
/// See ADR-0028 and the event sourcing integrity policy.
/// </summary>
public sealed record TravelDiaryDayProjection(
    IReadOnlyList<TravelDiaryDayState> Days) : IProjectionResult;
