using WildBunch.Application.Games.Models;
using WildBunch.Domain.Actions;

namespace WildBunch.Application.Games.Mapping;

public static class AvailableActionMapper
{
    public static IReadOnlyList<AvailableActionDto> ToDto(IEnumerable<AvailableAction> availableActions)
    {
        ArgumentNullException.ThrowIfNull(availableActions);

        return availableActions
            .Select(action => new AvailableActionDto(action.Kind, action.Label))
            .ToArray();
    }
}
