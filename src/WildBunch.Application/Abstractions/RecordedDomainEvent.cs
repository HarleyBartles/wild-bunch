using WildBunch.Domain.Events;

namespace WildBunch.Application.Abstractions;

public sealed record RecordedDomainEvent(
    IDomainEvent Event,
    long Sequence,
    DateTime OccurredAtUtc);
