using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;

namespace WildBunch.Domain.Actions;

public sealed record ActionAvailabilityContext(
    StartFlowPhase StartFlowPhase,
    DomainWorld World,
    TownId? CurrentTownId,
    TravelJourneySnapshot? Journey);
