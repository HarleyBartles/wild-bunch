using WildBunch.Domain.Actions;

namespace WildBunch.Application.Games.Models;

public sealed record AvailableActionDto(
    AvailableActionKind Kind,
    string Label);
