using System.Text.Json;
using WildBunch.Domain.Game;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string Serialize(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return JsonSerializer.Serialize(GameSessionSnapshot.FromDomain(session), Options);
    }

    public GameSession Deserialize(string stateJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateJson);

        return Deserialize<GameSessionSnapshot>(stateJson).ToDomain();
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new InvalidOperationException($"Unable to deserialize {typeof(T).Name}.");
}
