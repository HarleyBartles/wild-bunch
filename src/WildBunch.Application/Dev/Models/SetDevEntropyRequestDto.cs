using System.Text.Json.Serialization;

namespace WildBunch.Application.Dev.Models;

public sealed record SetDevEntropyRequestDto
{
    [JsonRequired]
    public required string Entropy { get; init; }
}
