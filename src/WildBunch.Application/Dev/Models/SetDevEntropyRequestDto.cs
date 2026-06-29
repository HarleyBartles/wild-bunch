using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Models;

/// <summary>
/// Request DTO for the set-entropy dev endpoint.
/// Entropy is a string matching one of the GameEntropy enum values
/// (Boring, Classic, Adventurous, Wild). The handler validates it
/// via Enum.IsDefined in the domain.
/// </summary>
public sealed record SetDevEntropyRequestDto(string Entropy);
