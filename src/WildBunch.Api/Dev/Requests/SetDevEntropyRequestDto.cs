using System.Text.Json.Serialization;

namespace WildBunch.Api.Dev;

public sealed record SetDevEntropyRequestDto
{
    [JsonRequired]
    public required string Entropy { get; init; }
}
