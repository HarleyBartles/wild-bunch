using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

public sealed record DevEntropyChanged : IDomainEvent
{
    public required GameEntropy NewEntropy { get; init; }
}
