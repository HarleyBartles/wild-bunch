using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Application.Games.Exceptions;

public sealed class TownNotFoundException : InvalidOperationException
{
    public TownNotFoundException(TownId townId)
        : base($"Town '{townId.Value}' was not found in the current world.")
    {
        TownId = townId;
    }

    public TownId TownId { get; }
}
