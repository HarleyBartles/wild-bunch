using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an unrelated wanted criminal was turned in to the sheriff for a bounty.
/// Unlike <see cref="SheriffTurnInSettled"/>, this event carries a <see cref="WarrantId"/>
/// (not a <see cref="SuspectId"/>) because unrelated criminals are not suspects in the
/// gang case — they are independent bounty targets whose warrants surface on wanted posters.
/// Carries only public data. Clock advancement is handled by EnterActionContext, not this event.
/// See BUNCH-107.
/// </summary>
public sealed record UnrelatedCriminalTurnInSettled : IDomainEvent
{
    public required WarrantId WarrantId { get; init; }
    public required string TargetName { get; init; }
    public required WarrantDisposition Disposition { get; init; }
    public required bool IsAlive { get; init; }
    public required decimal BountyAmount { get; init; }
    public required string Message { get; init; }
    public required int Day { get; init; }
    public required int Turn { get; init; }
}
