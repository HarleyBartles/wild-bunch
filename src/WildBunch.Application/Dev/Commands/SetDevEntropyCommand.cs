using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed record SetDevEntropyCommand(Guid GameSessionId, GameEntropy Entropy);
