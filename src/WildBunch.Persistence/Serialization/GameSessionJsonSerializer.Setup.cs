using System.Text.Json;
using WildBunch.Domain.Travel;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializeSetup(GameEntropy entropy)
        => JsonSerializer.Serialize(SetupSnapshot.FromDomain(entropy), Options);

    internal GameEntropy DeserializeSetup(string json)
        => DeserializeRequiredComponent("setup", json, () =>
        {
            var snapshot = Deserialize<SetupSnapshot>(json);
            if (snapshot.GameEntropy is null || !Enum.IsDefined(snapshot.GameEntropy.Value))
            {
                throw new InvalidRequiredComponentCacheShapeException("setup", "a supported game entropy is required.");
            }

            return snapshot.GameEntropy.Value;
        });

    private sealed record SetupSnapshot(GameEntropy? GameEntropy)
    {
        public static SetupSnapshot FromDomain(GameEntropy gameEntropy)
            => new(gameEntropy);

    }
}
