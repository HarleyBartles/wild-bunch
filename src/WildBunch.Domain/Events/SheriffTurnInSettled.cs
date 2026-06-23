using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a wanted suspect was turned in to the sheriff for a bounty.
/// Carries only public data. Clock advancement is handled by EnterActionContext, not this event.
/// See ADR-0028 and BUNCH-80.
/// </summary>
public sealed record SheriffTurnInSettled : IDomainEvent
{
    public required SuspectId TargetSuspectId { get; init; }
    public required string TargetName { get; init; }
    public required WarrantDisposition Disposition { get; init; }
    public required bool IsAlive { get; init; }
    public required decimal BountyAmount { get; init; }
    public required string Message { get; init; }
    public required int Day { get; init; }
    public required int Turn { get; init; }
}
