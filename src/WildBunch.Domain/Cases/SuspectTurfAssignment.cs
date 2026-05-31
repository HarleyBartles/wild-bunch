using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Cases;

public readonly record struct SuspectTurfAssignment(SuspectId SuspectId, TownId TurfTownId);
