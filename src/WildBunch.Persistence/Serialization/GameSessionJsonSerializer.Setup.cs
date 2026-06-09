using System.Text.Json;
using WildBunch.Domain.Travel;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializeSetup(AdventureRandomnessPolicy entropy)
        => JsonSerializer.Serialize(SetupSnapshot.FromDomain(entropy), Options);

    public AdventureRandomnessPolicy DeserializeSetup(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return Deserialize<SetupSnapshot>(json).ToDomain();
    }

    private sealed record SetupSnapshot(AdventureRandomnessPolicy? Entropy)
    {
        public static SetupSnapshot FromDomain(AdventureRandomnessPolicy entropy)
            => new(entropy);

        public AdventureRandomnessPolicy ToDomain()
            => Entropy ?? AdventureRandomnessPolicy.Standard;
    }
}
