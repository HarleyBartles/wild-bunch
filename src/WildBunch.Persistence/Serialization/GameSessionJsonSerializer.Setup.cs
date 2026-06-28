using System.Text.Json;
using WildBunch.Domain.Travel;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializeSetup(GameEntropy entropy)
        => JsonSerializer.Serialize(SetupSnapshot.FromDomain(entropy), Options);

    public GameEntropy DeserializeSetup(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return Deserialize<SetupSnapshot>(json).ToDomain();
    }

    private sealed record SetupSnapshot(GameEntropy? Entropy)
    {
        public static SetupSnapshot FromDomain(GameEntropy entropy)
            => new(entropy);

        public GameEntropy ToDomain()
            => Entropy ?? GameEntropy.Standard;
    }
}
